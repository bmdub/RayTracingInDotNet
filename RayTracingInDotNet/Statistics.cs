
namespace RayTracingInDotNet
{
	class Statistics
	{
		public (int Width, int Height) FramebufferSize { get; set; }
		public float FrameRate { get; set; }
		public float RayRate { get; set; }
		public uint TotalSamples { get; set; }
	};
}
