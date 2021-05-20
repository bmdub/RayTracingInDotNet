namespace RayTracingInDotNet
{
	class WindowConfig
	{
		public string Title { get; init; }
		public uint Width { get; init; }
		public uint Height { get; init; }
		public bool CursorDisabled { get; init; }
		public bool Fullscreen { get; init; }
		public bool Resizable { get; init; }
	};
}
