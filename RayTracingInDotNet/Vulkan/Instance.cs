using Serilog;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VkInstance = Silk.NET.Vulkan.Instance;

namespace RayTracingInDotNet.Vulkan
{
	class Instance : IDisposable
	{
		private List<string> _validationLayers = new List<string>() {
			"VK_LAYER_KHRONOS_validation"	// For debug messaging
		};

		private readonly Api _api;
		private readonly Window _window;
		private readonly VkInstance _vkInstance;
		private IReadOnlyCollection<PhysicalDevice> _physicalDevices;
		private List<string> _extensions;
		private bool _disposedValue;

		public unsafe Instance(Api api, Window window, uint vulkanVersion)
		{
			(_api, _window) = (api, window);

			// Check the minimum version.
			CheckVulkanMinimumVersion(vulkanVersion);

			// Get the list of required extensions.
			List<string> extensions = new List<string>();

			extensions.AddRange(window.GetRequiredInstanceExtensions());

			// Check the validation layers and add them to the list of required extensions.
			if (api.DebugLoggingEnabled == false)
				_validationLayers.Clear();
			CheckVulkanValidationLayerSupport(_validationLayers);
			if (_validationLayers.Count > 0)
				extensions.Add(ExtDebugUtils.ExtensionName);

			// Create the Vulkan instance.
			var appInfo = new ApplicationInfo
			{
				SType = StructureType.ApplicationInfo,
				PApplicationName = (byte*)Marshal.StringToHGlobalAnsi(nameof(RayTracingInDotNet)),
				ApplicationVersion = new Version32(1, 0, 0),
				PEngineName = (byte*)Marshal.StringToHGlobalAnsi($"{nameof(RayTracingInDotNet)} engine"),
				EngineVersion = new Version32(1, 0, 0),
				ApiVersion = vulkanVersion
			};

			var createInfo = new InstanceCreateInfo
			{
				SType = StructureType.InstanceCreateInfo,
				PApplicationInfo = &appInfo,
				EnabledExtensionCount = (uint)extensions.Count,
				PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions.ToArray()),
				EnabledLayerCount = (uint)_validationLayers.Count,
				PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(_validationLayers.ToArray())
			};

#if DEBUG
			// For using debugPrintfEXT in shaders
			var enabled = stackalloc ValidationFeatureEnableEXT[] { ValidationFeatureEnableEXT.ValidationFeatureEnableDebugPrintfExt };
			var features = new ValidationFeaturesEXT
			{
				SType = StructureType.ValidationFeaturesExt,
				DisabledValidationFeatureCount = 0,
				EnabledValidationFeatureCount = 1,
				PDisabledValidationFeatures = null,
				PEnabledValidationFeatures = enabled,
				PNext = createInfo.PNext
			};
			createInfo.PNext = (void*)Unsafe.AsPointer(ref features);
#endif

			fixed (VkInstance* instance = &_vkInstance)
			{
				Util.Verify(_api.Vk.CreateInstance(&createInfo, null, instance), $"{nameof(VulkanRenderer)}: Failed to create Vulkan instance");
			}

			_api.Instance = this;

			Marshal.FreeHGlobal((nint)appInfo.PApplicationName);
			Marshal.FreeHGlobal((nint)appInfo.PEngineName);
			SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);
			SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);

			GetVulkanPhysicalDevices();
			GetVulkanLayers();
			GetVulkanExtensions();

			foreach (var layer in _validationLayers)
				_api.Logger.Debug($"{nameof(Instance)}: Validation Layer: {layer}");
			foreach (var extension in _extensions)
				_api.Logger.Debug($"{nameof(Instance)}: Extension: {extension}");
		}

		public List<string> ValidationLayers => _validationLayers;

		public VkInstance VkInstance => _vkInstance;

		public IReadOnlyCollection<PhysicalDevice> PhysicalDevices => _physicalDevices;

		private void CheckVulkanMinimumVersion(in uint vulkanVersion)
		{
			// Verify we have a high enough version of Vulkan
			Version32 requiredVersion = (Version32)vulkanVersion;

			uint versionInteger = 0;
			Util.Verify(_api.Vk.EnumerateInstanceVersion(ref versionInteger), $"{nameof(Instance)}: Unable to get the Vulkan instance version.");
			var version = (Version32)versionInteger;

			if (version.Value < requiredVersion.Value)
				throw new Exception($"{nameof(Instance)}: Found Vulkan version {version.Major}.{version.Minor}.{version.Patch}, but require minimum version {requiredVersion.Major}.{requiredVersion.Minor}.{requiredVersion.Patch}");

			_api.Logger.Debug($"{nameof(Instance)}: API Version available: {version.Major}.{version.Minor}.{version.Patch}");
			_api.Logger.Debug($"{nameof(Instance)}: API Version required: {requiredVersion.Major}.{requiredVersion.Minor}.{requiredVersion.Patch}");
		}

		private unsafe void CheckVulkanValidationLayerSupport(in IList<string> validationLayers)
		{
			if (validationLayers.Count == 0)
				return;

			uint layerCount = 0;
			_api.Vk.EnumerateInstanceLayerProperties(&layerCount, null);

			var availableLayers = new LayerProperties[layerCount];
			fixed (LayerProperties* availableLayersPtr = availableLayers)
				_api.Vk.EnumerateInstanceLayerProperties(&layerCount, availableLayersPtr);

			foreach (var validationLayer in validationLayers)
			{
				bool found = false;
				foreach (var layerProperties in availableLayers)
				{
					var layerName = Marshal.PtrToStringAnsi((nint)layerProperties.LayerName);
					if (layerName == validationLayer)
					{
						found = true;
						break;
					}
				}

				if (!found)
					throw new Exception($"{nameof(Instance)}: Could not find requested validation layer: {validationLayer}");
			}
		}

		private unsafe void GetVulkanPhysicalDevices()
		{
			// do NOT use this line; it makes the app terminally broken (following vkDestroyInstance()'s)
			//_physicalDevices = Api._api.Vk.GetPhysicalDevices(_apiInstance);

			_physicalDevices = Enumerate.Get<VkInstance, PhysicalDevice>(_vkInstance, (inst, ct, dev) =>
				_api.Vk.EnumeratePhysicalDevices(inst, (uint*)ct, (PhysicalDevice*)dev));

			if (!_physicalDevices.Any())
				throw new NotSupportedException($"{nameof(Instance)}: Failed to find a GPU with Vulkan support.");

			foreach (var device in _physicalDevices)
			{
				var driverProps = new PhysicalDeviceDriverProperties();
				driverProps.SType = StructureType.PhysicalDeviceDriverProperties;

				var deviceProps = new PhysicalDeviceProperties2();
				deviceProps.SType = StructureType.PhysicalDeviceProperties2;
				deviceProps.PNext = &driverProps;

				_api.Vk.GetPhysicalDeviceProperties2(device, &deviceProps);

				var prop = deviceProps.Properties;

				var vulkanVersion = (Version32)prop.ApiVersion;
				var driverVersion = (Version32)prop.DriverVersion;

				_api.Logger.Debug($@"{nameof(Instance)}: Device ID: {prop.DeviceID}");
				_api.Logger.Debug($@"{nameof(Instance)}:  Vendor ID: {prop.VendorID}");
				_api.Logger.Debug($@"{nameof(Instance)}:  Device Name: {Marshal.PtrToStringAnsi((nint)prop.DeviceName)}");
				_api.Logger.Debug($@"{nameof(Instance)}:  Device Type: {Enum.GetName(prop.DeviceType)}");
				_api.Logger.Debug($@"{nameof(Instance)}:  Vulkan Version: {vulkanVersion.Major}.{vulkanVersion.Minor}.{vulkanVersion.Patch}");
				_api.Logger.Debug($@"{nameof(Instance)}:  Driver: {Marshal.PtrToStringAnsi((nint)driverProps.DriverName)} {Marshal.PtrToStringAnsi((nint)driverProps.DriverInfo)} - {driverVersion.Major}.{driverVersion.Minor}.{driverVersion.Patch}");
			}
		}

		private unsafe void GetVulkanLayers()
		{
			uint count = 0;
			_api.Vk.EnumerateInstanceLayerProperties(&count, null);

			var availableLayers = new LayerProperties[count];
			fixed (LayerProperties* availableLayersPtr = availableLayers)
				_api.Vk.EnumerateInstanceLayerProperties(&count, availableLayersPtr);

			_validationLayers = new List<string>((int)count);
			foreach (var layerProperties in availableLayers)
			{
				var layerName = Marshal.PtrToStringAnsi((nint)layerProperties.LayerName);
				_validationLayers.Add(layerName);
			}
		}

		private unsafe void GetVulkanExtensions()
		{
			uint count = 0;
			_api.Vk.EnumerateInstanceExtensionProperties((byte*)0, &count, null);

			var availableExtensions = new ExtensionProperties[count];
			fixed (ExtensionProperties* availableLayersPtr = availableExtensions)
				_api.Vk.EnumerateInstanceExtensionProperties((byte*)0, &count, availableLayersPtr);

			_extensions = new List<string>((int)count);
			foreach (var extensionProperty in availableExtensions)
			{
				var layerName = Marshal.PtrToStringAnsi((nint)extensionProperty.ExtensionName);
				_extensions.Add(layerName);
			}
		}

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyInstance(_vkInstance, null);

				_disposedValue = true;
			}
		}

		~Instance()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
