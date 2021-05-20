using Serilog;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VkDevice = Silk.NET.Vulkan.Device;

namespace RayTracingInDotNet.Vulkan
{
	class Device : IDisposable
	{
		private readonly List<string> _deviceExtensions = new List<string>() {
			KhrSwapchain.ExtensionName,					// For rendering
			KhrAccelerationStructure.ExtensionName,		// For path tracing
			KhrDeferredHostOperations.ExtensionName,	// For path tracing
			KhrRayTracingPipeline.ExtensionName,		// For path tracing
			"VK_KHR_shader_clock",						// For heat map			
		};

		private readonly Api _api;
		private readonly PhysicalDevice _physicalDevice;
		private readonly Surface _surface;
		private readonly DebugUtilsMessengerEXT _debugMessenger;
		private readonly VkDevice _vkDevice;
		private readonly Queue _graphicsQueue;
		private readonly uint _graphicsFamilyIndex;
		private readonly Queue _computeQueue;
		private readonly uint _computeFamilyIndex;
		private readonly Queue _transferQueue;
		private readonly uint _transferFamilyIndex;
		private readonly Queue _presentationQueue;
		private readonly uint _presentationFamilyIndex;
		private bool _disposedValue;

		public unsafe Device(Api api, in PhysicalDevice physicalDevice, Surface surface, in PhysicalDeviceFeatures deviceFeatures, void* nextDeviceFeatures)
		{
			(_api, _physicalDevice, _surface) = (api, physicalDevice, surface);

			_debugMessenger = new DebugUtilsMessengerEXT(_surface.VkServiceKHR.Handle);

			CheckRequiredExtensions(physicalDevice, _deviceExtensions);

			// Find the graphics queue.
			var queueFamilies = Enumerate.Get<PhysicalDevice, QueueFamilyProperties>(physicalDevice, (device, count, values) => 
				_api.Vk.GetPhysicalDeviceQueueFamilyProperties(device, (uint*)count, (QueueFamilyProperties*)values));

			_graphicsFamilyIndex = (uint)FindQueue(queueFamilies, "graphics", QueueFlags.QueueGraphicsBit, 0);
			_computeFamilyIndex = (uint)FindQueue(queueFamilies, "compute", QueueFlags.QueueComputeBit, QueueFlags.QueueGraphicsBit);
			_transferFamilyIndex = (uint)FindQueue(queueFamilies, "transfer", QueueFlags.QueueTransferBit, QueueFlags.QueueGraphicsBit | QueueFlags.QueueComputeBit);

			// Find the presentation queue (usually the same as graphics queue).
			_presentationFamilyIndex = 0;
			bool found = false;
			for (var i = 0; i < queueFamilies.Count; i++)
			{
				api.KhrSurface.GetPhysicalDeviceSurfaceSupport(physicalDevice, (uint)i, _surface.VkServiceKHR, out var presentSupport);

				found = queueFamilies[i].QueueCount > 0 && presentSupport == Vk.True;

				if (found)
				{
					_presentationFamilyIndex = (uint)i;
					break;
				}
			}
			if (!found)
				throw new Exception($"{nameof(Device)}: Could not find presentation queue");

			// Create the logical device			
			var families = new uint[] {
				_graphicsFamilyIndex,
				_computeFamilyIndex,
				_presentationFamilyIndex,
				_transferFamilyIndex
			}
			.Distinct()
			.ToArray();
			
			var queueCreateInfos = GC.AllocateArray<DeviceQueueCreateInfo>(families.Length, true);
			var pQueueCreateInfos = (DeviceQueueCreateInfo*)Unsafe.AsPointer(ref queueCreateInfos[0]);

			var queuePriority = 1f;

			for (int i = 0; i < families.Length; i++)
			{
				pQueueCreateInfos[i] = new DeviceQueueCreateInfo
				{
					SType = StructureType.DeviceQueueCreateInfo,
					QueueFamilyIndex = families[i],
					QueueCount = 1,
					PQueuePriorities = &queuePriority
				};
			}

			fixed (PhysicalDeviceFeatures* pDeviceFeatures = &deviceFeatures)
			{
				var deviceCreateInfo = new DeviceCreateInfo();
				deviceCreateInfo.SType = StructureType.DeviceCreateInfo;
				deviceCreateInfo.PNext = nextDeviceFeatures;
				deviceCreateInfo.QueueCreateInfoCount = (uint)families.Length;
				deviceCreateInfo.PQueueCreateInfos = pQueueCreateInfos;
				deviceCreateInfo.PEnabledFeatures = pDeviceFeatures;
				deviceCreateInfo.EnabledExtensionCount = (uint)_deviceExtensions.Count;

				var enabledExtensionNames = SilkMarshal.StringArrayToPtr(_deviceExtensions.ToArray());
				deviceCreateInfo.PpEnabledExtensionNames = (byte**)enabledExtensionNames;

				deviceCreateInfo.EnabledLayerCount = (uint)api.Instance.ValidationLayers.Count;

				var layerNames = SilkMarshal.StringArrayToPtr(api.Instance.ValidationLayers.ToArray());
				deviceCreateInfo.PpEnabledLayerNames = (byte**)layerNames;

				fixed (VkDevice* device = &_vkDevice)
				{
					if (_api.Vk.CreateDevice(_physicalDevice, &deviceCreateInfo, null, device) != Result.Success)
						throw new Exception($"{nameof(Device)}: Failed to create logical device.");
				}

				SilkMarshal.Free(enabledExtensionNames);
				SilkMarshal.Free(layerNames);
			}

			_api.Device = this;

			fixed (Queue* graphicsQueue = &_graphicsQueue)
				_api.Vk.GetDeviceQueue(_vkDevice, _graphicsFamilyIndex, 0, graphicsQueue);

			fixed (Queue* computeQueue = &_computeQueue)
				_api.Vk.GetDeviceQueue(_vkDevice, _computeFamilyIndex, 0, computeQueue);

			fixed (Queue* transferQueue = &_transferQueue)
				_api.Vk.GetDeviceQueue(_vkDevice, _transferFamilyIndex, 0, transferQueue);

			fixed (Queue* presentationQueue = &_presentationQueue)
				_api.Vk.GetDeviceQueue(_vkDevice, _presentationFamilyIndex, 0, presentationQueue);
		}

		public VkDevice VkDevice => _vkDevice;
		public PhysicalDevice PhysicalDevice => _physicalDevice;

		public Queue GraphicsQueue => _graphicsQueue;
		public Queue ComputeQueue => _computeQueue;
		public Queue TransferQueue => _transferQueue;
		public Queue PresentationQueue => _presentationQueue;

		public uint GraphicsFamilyIndex => _graphicsFamilyIndex;
		public uint ComputeFamilyIndex => _computeFamilyIndex;
		public uint TransferFamilyIndex => _transferFamilyIndex;
		public uint PresentationFamilyIndex => _presentationFamilyIndex;

		private unsafe void CheckRequiredExtensions(PhysicalDevice physicalDevice, List<string> requiredExtensions)
		{
			var availableExtensions = Enumerate.Get<PhysicalDevice, nint, ExtensionProperties>(physicalDevice, 0, (dev, ln, ct, props) =>
				_api.Vk.EnumerateDeviceExtensionProperties(dev, (byte*)ln, (uint*)ct, (ExtensionProperties*)props));

			foreach (var extension in availableExtensions)
				_api.Logger.Debug($"{nameof(Device)}: Extension: {Marshal.PtrToStringAnsi((nint)extension.ExtensionName)}");

			var extensionSet = requiredExtensions.ToHashSet();

			foreach (var extension in availableExtensions)
				extensionSet.Remove(Marshal.PtrToStringAnsi((nint)extension.ExtensionName));

			if (extensionSet.Count > 0)
				throw new Exception($"{nameof(Device)}: Missing required extension(s): {string.Join(",", extensionSet)}");
		}

		private unsafe int FindQueue(List<QueueFamilyProperties> properties, string name, QueueFlags requiredBits, QueueFlags excludedBits)
		{
			for (var i = 0; i < properties.Count; i++)
			{
				if (properties[i].QueueCount > 0 &&
					(properties[i].QueueFlags & requiredBits) != 0 &&
					!((properties[i].QueueFlags & excludedBits) != 0))
					return i;
			}

			throw new Exception($"{nameof(Device)}: Could not find queue matching name '{name}'");
		}

		public void WaitIdle() =>
			Util.Verify(_api.Vk.DeviceWaitIdle(_vkDevice), $"{nameof(Device)}: DeviceWaitIdle failed");


		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyDevice(_vkDevice, null);
				_disposedValue = true;
			}
		}

		~Device()
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
