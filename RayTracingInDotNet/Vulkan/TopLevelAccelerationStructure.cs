using System.Runtime.CompilerServices;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace RayTracingInDotNet.Vulkan
{
	class TopLevelAccelerationStructure : AccelerationStructure
	{
		private readonly uint _instancesCount;
		private readonly PinnedStruct<AccelerationStructureGeometryKHR> _topASGeometry = new PinnedStruct<AccelerationStructureGeometryKHR>();

		public unsafe TopLevelAccelerationStructure(Api api, RayTracingProperties rayTracingProperties, ulong instanceAddress, uint instancesCount)
			: base(api, rayTracingProperties)
		{
			_instancesCount = instancesCount;

			// Create VkAccelerationStructureGeometryInstancesDataKHR. This wraps a device pointer to the above uploaded instances.
			var instancesVk = new AccelerationStructureGeometryInstancesDataKHR();
			instancesVk.SType = StructureType.AccelerationStructureGeometryInstancesDataKhr;
			instancesVk.ArrayOfPointers = new Silk.NET.Core.Bool32(false);
			instancesVk.Data.DeviceAddress = instanceAddress;

			// Put the above into a VkAccelerationStructureGeometryKHR. We need to put the
			// instances struct in a union and label it as instance data.
			_topASGeometry.Value.SType = StructureType.AccelerationStructureGeometryKhr;
			_topASGeometry.Value.GeometryType = GeometryTypeKHR.GeometryTypeInstancesKhr;
			_topASGeometry.Value.Geometry.Instances = instancesVk;

			_buildGeometryInfo.SType = StructureType.AccelerationStructureBuildGeometryInfoKhr;
			_buildGeometryInfo.Flags = _flags;
			_buildGeometryInfo.GeometryCount = 1;
			_buildGeometryInfo.PGeometries = _topASGeometry.Ptr;
			_buildGeometryInfo.Mode = BuildAccelerationStructureModeKHR.BuildAccelerationStructureModeBuildKhr;
			_buildGeometryInfo.Type = AccelerationStructureTypeKHR.AccelerationStructureTypeTopLevelKhr;
			_buildGeometryInfo.SrcAccelerationStructure = default;

			GetBuildSizes(&instancesCount, out _buildSizesInfo);
		}

		public unsafe void Generate(
			in CommandBuffer commandBuffer,
			Buffer scratchBuffer,
			ulong scratchOffset,
			Buffer resultBuffer,
			ulong resultOffset)
		{
			// Create the acceleration structure.
			CreateAccelerationStructure(resultBuffer, resultOffset);

			// Build the actual bottom-level acceleration structure
			var buildOffsetInfo = new AccelerationStructureBuildRangeInfoKHR();
			buildOffsetInfo.PrimitiveCount = _instancesCount;

			AccelerationStructureBuildRangeInfoKHR* pBuildOffsetInfo = &buildOffsetInfo;

			_buildGeometryInfo.DstAccelerationStructure = _accelerationStructureKhr;
			_buildGeometryInfo.ScratchData.DeviceAddress = scratchBuffer.GetDeviceAddress() + scratchOffset;

			_api.KhrAccelerationStructure.CmdBuildAccelerationStructures(commandBuffer, 1, _buildGeometryInfo, &pBuildOffsetInfo);
		}

		public unsafe void Rebuild(
			in CommandBuffer commandBuffer,
			Buffer scratchBuffer,
			ulong scratchOffset)
		{
			// Build the actual bottom-level acceleration structure
			var buildOffsetInfo = new AccelerationStructureBuildRangeInfoKHR();
			buildOffsetInfo.PrimitiveCount = _instancesCount;

			AccelerationStructureBuildRangeInfoKHR* pBuildOffsetInfo = &buildOffsetInfo;

			_buildGeometryInfo.DstAccelerationStructure = _accelerationStructureKhr;
			_buildGeometryInfo.ScratchData.DeviceAddress = scratchBuffer.GetDeviceAddress() + scratchOffset;

			_api.KhrAccelerationStructure.CmdBuildAccelerationStructures(commandBuffer, 1, _buildGeometryInfo, &pBuildOffsetInfo);
		}

		public static unsafe AccelerationStructureInstanceKHR CreateInstance(
			Api api, 
			in BottomLevelAccelerationStructure bottomLevelAs,
			in TransformMatrixKHR transform,
			uint instanceId,
			uint hitGroupId)
		{
			var addressInfo = new AccelerationStructureDeviceAddressInfoKHR();
			addressInfo.SType = StructureType.AccelerationStructureDeviceAddressInfoKhr;
			addressInfo.AccelerationStructure = bottomLevelAs.VkAccelerationStructure;

			ulong address = api.KhrAccelerationStructure.GetAccelerationStructureDeviceAddress(api.Device.VkDevice, (AccelerationStructureDeviceAddressInfoKHR*)Unsafe.AsPointer(ref addressInfo));

			var instance = new AccelerationStructureInstanceKHR();
			instance.InstanceCustomIndex = instanceId;
			instance.Mask = 0xFF; // The visibility mask is always set of 0xFF, but if some instances would need to be ignored in some cases, this flag should be passed by the application.
			instance.InstanceShaderBindingTableRecordOffset = hitGroupId; // Set the hit group index, that will be used to find the shader code to execute when hitting the geometry.
			instance.Flags = GeometryInstanceFlagsKHR.GeometryInstanceTriangleFacingCullDisableBitKhr; // Disable culling - more fine control could be provided by the application
			instance.AccelerationStructureReference = address;

			// The instance.transform value only contains 12 values, corresponding to a 4x3 matrix,
			// hence saving the last row that is anyway always (0,0,0,1).
			// Since the matrix is row-major, we simply copy the first 12 values of the original 4x4 matrix
			instance.Transform = transform;

			return instance;
		}

		/*protected override void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				if (_topASGeometryPtr != default)
					Marshal.DestroyStructure<AccelerationStructureGeometryKHR>(_topASGeometryPtr);
				_disposedValue = true;
			}

			base.Dispose(disposing);
		}*/
	}
}
