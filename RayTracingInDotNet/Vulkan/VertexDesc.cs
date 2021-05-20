using Silk.NET.Vulkan;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RayTracingInDotNet.Vulkan
{
	static class VertexDesc
	{
		public static void GetBindingDescription(out VertexInputBindingDescription bindingDescription)
		{
			bindingDescription = new VertexInputBindingDescription();
			bindingDescription.Binding = 0;
			bindingDescription.Stride = (uint)Unsafe.SizeOf<Vertex>();
			bindingDescription.InputRate = VertexInputRate.Vertex;
		}

		public static VertexInputAttributeDescription[] GetAttributeDescriptions()
		{
			var attributeDescriptions = new VertexInputAttributeDescription[4];

			attributeDescriptions[0].Binding = 0;
			attributeDescriptions[0].Location = 0;
			attributeDescriptions[0].Format = Format.R32G32B32Sfloat;
			attributeDescriptions[0].Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Position));

			attributeDescriptions[1].Binding = 0;
			attributeDescriptions[1].Location = 1;
			attributeDescriptions[1].Format = Format.R32G32B32Sfloat;
			attributeDescriptions[1].Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.Normal));

			attributeDescriptions[2].Binding = 0;
			attributeDescriptions[2].Location = 2;
			attributeDescriptions[2].Format = Format.R32G32Sfloat;
			attributeDescriptions[2].Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoord));

			attributeDescriptions[3].Binding = 0;
			attributeDescriptions[3].Location = 3;
			attributeDescriptions[3].Format = Format.R32Sint;
			attributeDescriptions[3].Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(Vertex.MaterialIndex));

			return attributeDescriptions;
		}
	}
}
