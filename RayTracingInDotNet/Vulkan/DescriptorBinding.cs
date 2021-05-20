using Silk.NET.Vulkan;

namespace RayTracingInDotNet.Vulkan
{
	readonly struct DescriptorBinding
	{
		public readonly uint Binding;
		public readonly uint DescriptorCount;
		public readonly DescriptorType Type;
		public readonly ShaderStageFlags Stage;

		public DescriptorBinding(uint binding, uint descriptorCount, DescriptorType type, ShaderStageFlags stage) =>
			(Binding, DescriptorCount, Type, Stage) = (binding, descriptorCount, type, stage);
	}
}
