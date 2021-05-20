using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;

namespace RayTracingInDotNet.Vulkan
{
	abstract class AccelerationStructure : IDisposable
	{
		protected readonly Api _api;
		protected readonly BuildAccelerationStructureFlagsKHR _flags;
		protected AccelerationStructureBuildGeometryInfoKHR _buildGeometryInfo;
		protected AccelerationStructureBuildSizesInfoKHR _buildSizesInfo;
		protected AccelerationStructureKHR _accelerationStructureKhr;
		private readonly RayTracingProperties _rayTracingProperties;
		private bool _disposedValue;

		protected AccelerationStructure(Api api, RayTracingProperties rayTracingProperties) =>
			(_api, _rayTracingProperties, _flags) = 
			(api, rayTracingProperties, BuildAccelerationStructureFlagsKHR.BuildAccelerationStructurePreferFastTraceBitKhr);

		public ref AccelerationStructureBuildSizesInfoKHR BuildSizes => ref _buildSizesInfo;
		public ref AccelerationStructureKHR VkAccelerationStructure => ref _accelerationStructureKhr;

		protected unsafe void GetBuildSizes(uint* pMaxPrimitiveCounts, out AccelerationStructureBuildSizesInfoKHR sizeInfo)
		{
			// Query both the size of the finished acceleration structure and the amount of scratch memory needed.
			sizeInfo = new AccelerationStructureBuildSizesInfoKHR();
			sizeInfo.SType = StructureType.AccelerationStructureBuildSizesInfoKhr;

			_api.KhrAccelerationStructure.GetAccelerationStructureBuildSizes(
				_api.Vk.CurrentDevice.Value,
				AccelerationStructureBuildTypeKHR.AccelerationStructureBuildTypeDeviceKhr,
				_buildGeometryInfo,
				pMaxPrimitiveCounts,
				(AccelerationStructureBuildSizesInfoKHR*)Unsafe.AsPointer(ref sizeInfo));

			// AccelerationStructure offset needs to be 256 bytes aligned (official Vulkan specs, don't ask me why).
			const ulong AccelerationStructureAlignment = 256;
			ulong ScratchAlignment = _rayTracingProperties.MinAccelerationStructureScratchOffsetAlignment;

			sizeInfo.AccelerationStructureSize = RoundUp(sizeInfo.AccelerationStructureSize, AccelerationStructureAlignment);
			sizeInfo.BuildScratchSize = RoundUp(sizeInfo.BuildScratchSize, ScratchAlignment);
		}

		protected unsafe void CreateAccelerationStructure(Buffer resultBuffer, ulong resultOffset)
		{
			var createInfo = new AccelerationStructureCreateInfoKHR();
			createInfo.SType = StructureType.AccelerationStructureCreateInfoKhr;
			createInfo.PNext = null;
			createInfo.Type = _buildGeometryInfo.Type;
			createInfo.Size = _buildSizesInfo.AccelerationStructureSize;
			createInfo.Buffer = resultBuffer.VkBuffer;
			createInfo.Offset = resultOffset;

			Util.Verify(
				_api.KhrAccelerationStructure.CreateAccelerationStructure(
					_api.Vk.CurrentDevice.Value, createInfo, default, out _accelerationStructureKhr),
				$"{nameof(AccelerationStructure)}: Could not create acceleration structure");
		}

		public static unsafe void MemoryBarrier(Api api, CommandBuffer commandBuffer)
		{
			// Wait for the builder to complete by setting a barrier on the resulting buffer. This is
			// particularly important as the construction of the top-level hierarchy may be called right
			// afterwards, before executing the command list.
			var memoryBarrier = new MemoryBarrier();
			memoryBarrier.SType = StructureType.MemoryBarrier;
			memoryBarrier.PNext = null;
			memoryBarrier.SrcAccessMask = AccessFlags.AccessAccelerationStructureWriteBitKhr | AccessFlags.AccessAccelerationStructureReadBitKhr;
			memoryBarrier.DstAccessMask = AccessFlags.AccessAccelerationStructureWriteBitKhr | AccessFlags.AccessAccelerationStructureReadBitKhr;

			api.Vk.CmdPipelineBarrier(
				commandBuffer,
				PipelineStageFlags.PipelineStageAccelerationStructureBuildBitKhr,
				PipelineStageFlags.PipelineStageAccelerationStructureBuildBitKhr,
				0, 1, memoryBarrier, 0, default, 0, default);
		}

		private static ulong RoundUp(ulong size, ulong granularity)
		{
			var divUp = (size + granularity - 1) / granularity;
			return divUp * granularity;
		}

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.KhrAccelerationStructure.DestroyAccelerationStructure(_api.Device.VkDevice, _accelerationStructureKhr, default);
				_disposedValue = true;
			}
		}

		~AccelerationStructure()
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
