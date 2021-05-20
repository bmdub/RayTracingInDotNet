using Serilog;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System;

namespace RayTracingInDotNet.Vulkan
{
	class Api : IDisposable
	{
		private readonly Vk _vk;
		private readonly KhrSurface _khrSurface;
		private KhrSwapchain _khrSwapchain;
		private KhrAccelerationStructure _khrAccelerationStructure;
		private KhrRayTracingPipeline _khrRayTracingPipeline;
		private ExtDebugUtils _extDebugUtils;
		private DebugUtilsMessenger _debugUtilsMessenger;
		private Device _device;
		private Instance _instance;
		private bool _disposedValue;

		public Api(bool enableDebugLogging, ILogger logger)
		{
			DebugLoggingEnabled = enableDebugLogging;

			_vk = Vk.GetApi();
			_khrSurface = new KhrSurface(_vk.Context);
			Logger = logger;
		}

		public Vk Vk => _vk;
		public KhrSurface KhrSurface => _khrSurface;
		public KhrSwapchain KhrSwapchain => _khrSwapchain;
		public KhrAccelerationStructure KhrAccelerationStructure => _khrAccelerationStructure;
		public KhrRayTracingPipeline KhrRayTracingPipeline => _khrRayTracingPipeline;
		public ExtDebugUtils ExtDebugUtils => _extDebugUtils;
		public bool DebugLoggingEnabled { get; init; }
		public ILogger Logger { get; init; }

		public Device Device
		{
			get => _device;
			set
			{
				_device = value;
				_vk.CurrentDevice = _device.VkDevice;
			}
		}

		public Instance Instance
		{
			get => _instance;
			set
			{
				_instance = value;
				_vk.CurrentInstance = _instance.VkInstance;
			}
		}

		public void InitializeExtensions()
		{
			if (Device == null) throw new InvalidOperationException($"{nameof(Api)}: {nameof(Device)} must be set to a device before initializing extensions.");
			if (Instance == null) throw new InvalidOperationException($"{nameof(Api)}: {nameof(Instance)} must be set to a device before initializing extensions.");

			if (!_vk.TryGetDeviceExtension<KhrSwapchain>(Instance.VkInstance, _vk.CurrentDevice.Value, out _khrSwapchain))
				throw new Exception($"{nameof(VulkanRenderer)}: Could not load the {nameof(KhrSwapchain)} extension.");
			if (!_vk.TryGetDeviceExtension<KhrAccelerationStructure>(Instance.VkInstance, _vk.CurrentDevice.Value, out _khrAccelerationStructure))
				throw new Exception($"{nameof(VulkanRenderer)}: Could not load the {nameof(KhrAccelerationStructure)} extension.");
			if (!_vk.TryGetDeviceExtension<KhrRayTracingPipeline>(Instance.VkInstance, _vk.CurrentDevice.Value, out _khrRayTracingPipeline))
				throw new Exception($"{nameof(VulkanRenderer)}: Could not load the {nameof(KhrRayTracingPipeline)} extension.");
			if (!_vk.TryGetInstanceExtension<ExtDebugUtils>(Instance.VkInstance, out _extDebugUtils))
				throw new Exception($"{nameof(VulkanRenderer)}: Could not load the {nameof(ExtDebugUtils)} extension.");

			_debugUtilsMessenger = new DebugUtilsMessenger(this);
		}

		public void SetDebugName(ulong handle, string name, ObjectType type) =>
			_debugUtilsMessenger?.SetDebugName(handle, name, type);

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_debugUtilsMessenger?.Dispose();
					_khrSurface?.Dispose();
					_khrSwapchain?.Dispose();
					_khrAccelerationStructure?.Dispose();
					_khrRayTracingPipeline?.Dispose();
					_extDebugUtils?.Dispose();
				}

				_disposedValue = true;
			}
		}

		// ~Api()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
