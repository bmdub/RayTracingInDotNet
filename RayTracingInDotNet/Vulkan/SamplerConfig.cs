using Silk.NET.Vulkan;

namespace RayTracingInDotNet.Vulkan
{
	record SamplerConfig
	{
		public Filter MagFilter { get; set; } = Filter.Linear;
		public Filter MinFilter { get; set; } = Filter.Linear;
		public SamplerAddressMode AddressModeU { get; set; } = SamplerAddressMode.ClampToEdge;
		public SamplerAddressMode AddressModeV { get; set; } = SamplerAddressMode.ClampToEdge;
		public SamplerAddressMode AddressModeW { get; set; } = SamplerAddressMode.ClampToEdge;
		public bool AnisotropyEnable { get; set; } = true;
		public float MaxAnisotropy { get; set; } = 16;
		public BorderColor BorderColor { get; set; } = BorderColor.IntOpaqueBlack;
		public bool UnnormalizedCoordinates { get; set; } = false;
		public bool CompareEnable { get; set; } = false;
		public CompareOp CompareOp { get; set; } = CompareOp.Always;
		public SamplerMipmapMode MipmapMode { get; set; } = SamplerMipmapMode.Linear;
		public float MipLodBias { get; set; } = 0.0f;
		public float MinLod { get; set; } = 0.0f;
		public float MaxLod { get; set; } = 0.0f;
	};
}
