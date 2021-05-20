using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;

namespace RayTracingInDotNet.Vulkan
{
	class BottomLevelAccelerationStructure : AccelerationStructure
	{
		private readonly AccelerationStructureGeometryKHR[] _geometry;
		private readonly AccelerationStructureBuildRangeInfoKHR[] _buildOffsetInfo;

		public unsafe BottomLevelAccelerationStructure(Api api, RayTracingProperties rayTracingProperties, BottomLevelGeometry geometries)
			: base(api, rayTracingProperties)
		{
			_geometry = GC.AllocateArray<AccelerationStructureGeometryKHR>(geometries.Geometry.Count, true);
			geometries.Geometry.CopyTo(_geometry);

			_buildOffsetInfo = GC.AllocateArray<AccelerationStructureBuildRangeInfoKHR>(geometries.BuildOffsetInfo.Count, true);
			geometries.BuildOffsetInfo.CopyTo(_buildOffsetInfo);

			_buildGeometryInfo.SType = StructureType.AccelerationStructureBuildGeometryInfoKhr;
			_buildGeometryInfo.Flags = _flags;
			_buildGeometryInfo.GeometryCount = (uint)_geometry.Length;
			_buildGeometryInfo.PGeometries = (AccelerationStructureGeometryKHR*)Unsafe.AsPointer(ref _geometry[0]);
			_buildGeometryInfo.Mode = BuildAccelerationStructureModeKHR.BuildAccelerationStructureModeBuildKhr;
			_buildGeometryInfo.Type = AccelerationStructureTypeKHR.AccelerationStructureTypeBottomLevelKhr;
			_buildGeometryInfo.SrcAccelerationStructure = default;
			
			// Note: Could use stackalloc here, but the model count might be too high
			uint[] maxPrimCount = new uint[_buildOffsetInfo.Length];

			for (int i = 0; i != maxPrimCount.Length; i++)
				maxPrimCount[i] = _buildOffsetInfo[i].PrimitiveCount;

			fixed (uint* maxPrimCountPtr = &maxPrimCount[0])
				GetBuildSizes(maxPrimCountPtr, out _buildSizesInfo);
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
			AccelerationStructureBuildRangeInfoKHR* pBuildOffsetInfo = (AccelerationStructureBuildRangeInfoKHR*)Unsafe.AsPointer(ref _buildOffsetInfo[0]);

			_buildGeometryInfo.DstAccelerationStructure = _accelerationStructureKhr;
			_buildGeometryInfo.ScratchData.DeviceAddress = scratchBuffer.GetDeviceAddress() + scratchOffset;

			_api.KhrAccelerationStructure.CmdBuildAccelerationStructures(commandBuffer, 1, _buildGeometryInfo, &pBuildOffsetInfo);
		}
	}
}
