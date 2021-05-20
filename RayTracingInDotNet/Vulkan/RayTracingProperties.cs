using Silk.NET.Vulkan;

namespace RayTracingInDotNet.Vulkan
{
	unsafe class RayTracingProperties
	{
		private readonly PinnedStruct<PhysicalDeviceAccelerationStructurePropertiesKHR> _accelProps = new PinnedStruct<PhysicalDeviceAccelerationStructurePropertiesKHR>();
		private readonly PinnedStruct<PhysicalDeviceRayTracingPipelinePropertiesKHR> _pipelineProps = new PinnedStruct<PhysicalDeviceRayTracingPipelinePropertiesKHR>();

		public unsafe RayTracingProperties(Api api)
		{			
			_accelProps.Value.SType = StructureType.PhysicalDeviceAccelerationStructurePropertiesKhr;

			_pipelineProps.Value.SType = StructureType.PhysicalDeviceRayTracingPipelinePropertiesKhr;
			_pipelineProps.Value.PNext = _accelProps.Ptr;

			var props = new PhysicalDeviceProperties2();
			props.SType = StructureType.PhysicalDeviceProperties2;
			props.PNext = _pipelineProps.Ptr;

			api.Vk.GetPhysicalDeviceProperties2(api.Device.PhysicalDevice, &props);
		}

		public uint MaxDescriptorSetAccelerationStructures => _accelProps.Value.MaxDescriptorSetAccelerationStructures;
		public ulong MaxGeometryCount => _accelProps.Value.MaxGeometryCount;
		public ulong MaxInstanceCount => _accelProps.Value.MaxInstanceCount;
		public ulong MaxPrimitiveCount => _accelProps.Value.MaxPrimitiveCount;
		public uint MaxRayRecursionDepth => _pipelineProps.Value.MaxRayRecursionDepth;
		public uint MaxShaderGroupStride => _pipelineProps.Value.MaxShaderGroupStride;
		public uint MinAccelerationStructureScratchOffsetAlignment => _accelProps.Value.MinAccelerationStructureScratchOffsetAlignment;
		public uint ShaderGroupBaseAlignment => _pipelineProps.Value.ShaderGroupBaseAlignment;
		public uint ShaderGroupHandleCaptureReplaySize => _pipelineProps.Value.ShaderGroupHandleCaptureReplaySize;
		public uint ShaderGroupHandleSize => _pipelineProps.Value.ShaderGroupHandleSize;
	}
}
