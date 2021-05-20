using Silk.NET.Vulkan;
using System.Collections.Generic;

namespace RayTracingInDotNet.Vulkan
{
	class BottomLevelGeometry
	{
		public List<AccelerationStructureGeometryKHR> Geometry => _geometry;
		public List<AccelerationStructureBuildRangeInfoKHR> BuildOffsetInfo => _buildOffsetInfo;

		private readonly List<AccelerationStructureGeometryKHR> _geometry = new List<AccelerationStructureGeometryKHR>();
		private readonly List<AccelerationStructureBuildRangeInfoKHR> _buildOffsetInfo = new List<AccelerationStructureBuildRangeInfoKHR>();

		public unsafe void AddGeometryTriangles(
			VulkanScene scene,
			uint vertexOffset, uint vertexCount,
			uint indexOffset, uint indexCount,
			bool isOpaque)
		{
			var geometry = new AccelerationStructureGeometryKHR();
			geometry.SType = StructureType.AccelerationStructureGeometryKhr;
			geometry.PNext = null;
			geometry.GeometryType = GeometryTypeKHR.GeometryTypeTrianglesKhr;
			geometry.Geometry.Triangles.SType = StructureType.AccelerationStructureGeometryTrianglesDataKhr;
			geometry.Geometry.Triangles.PNext = null;
			geometry.Geometry.Triangles.VertexData.DeviceAddress = scene.VertexBuffer.GetDeviceAddress();
			geometry.Geometry.Triangles.VertexStride = (ulong)sizeof(Vertex);
			geometry.Geometry.Triangles.MaxVertex = vertexCount;
			geometry.Geometry.Triangles.VertexFormat = Format.R32G32B32Sfloat;
			geometry.Geometry.Triangles.IndexData.DeviceAddress = scene.IndexBuffer.GetDeviceAddress();
			geometry.Geometry.Triangles.IndexType = IndexType.Uint32;
			geometry.Geometry.Triangles.TransformData = default;
			geometry.Flags = isOpaque ? GeometryFlagsKHR.GeometryOpaqueBitKhr : 0;

			var buildOffsetInfo = new AccelerationStructureBuildRangeInfoKHR();
			buildOffsetInfo.FirstVertex = vertexOffset / (uint)sizeof(Vertex);
			buildOffsetInfo.PrimitiveOffset = indexOffset;
			buildOffsetInfo.PrimitiveCount = indexCount / 3;
			buildOffsetInfo.TransformOffset = 0;

			_geometry.Add(geometry);
			_buildOffsetInfo.Add(buildOffsetInfo);
		}

		public unsafe void AddGeometryAabb(
			VulkanScene scene,
			uint aabbOffset,
			uint aabbCount,
			bool isOpaque)
		{
			var geometry = new AccelerationStructureGeometryKHR();
			geometry.SType = StructureType.AccelerationStructureGeometryKhr;
			geometry.PNext = null;
			geometry.GeometryType = GeometryTypeKHR.GeometryTypeAabbsKhr;
			geometry.Geometry.Aabbs.SType = StructureType.AccelerationStructureGeometryAabbsDataKhr;
			geometry.Geometry.Aabbs.PNext = null;
			geometry.Geometry.Aabbs.Data.DeviceAddress = scene.AabbBuffer.GetDeviceAddress();
			geometry.Geometry.Aabbs.Stride = (ulong)sizeof(AabbPositionsKHR);
			geometry.Flags = isOpaque ? GeometryFlagsKHR.GeometryOpaqueBitKhr : 0;

			var buildOffsetInfo = new AccelerationStructureBuildRangeInfoKHR();
			buildOffsetInfo.FirstVertex = 0;
			buildOffsetInfo.PrimitiveOffset = aabbOffset;
			buildOffsetInfo.PrimitiveCount = aabbCount;
			buildOffsetInfo.TransformOffset = 0;

			_geometry.Add(geometry);
			_buildOffsetInfo.Add(buildOffsetInfo);
		}
	}
}
