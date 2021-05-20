using System.Numerics;
using System.Runtime.InteropServices;

//https://www.khronos.org/registry/vulkan/specs/1.0-wsi_extensions/html/vkspec.html#interfaces-resources-layout

namespace RayTracingInDotNet
{
	[StructLayout(LayoutKind.Sequential)]
	public struct UniformBufferObject
	{
		public Matrix4x4 ModelView;
		public Matrix4x4 Projection;
		public Matrix4x4 ModelViewInverse;
		public Matrix4x4 ProjectionInverse;
		public Vector4 SkyColor1;
		public Vector4 SkyColor2;
		public float Aperture;
		public float FocusDistance;
		public float HeatmapScale;
		public uint TotalNumberOfSamples;
		public uint NumberOfSamples;
		public uint NumberOfBounces;
		public uint RandomSeed;
		public uint ShowHeatmap; // bool
	}
}
