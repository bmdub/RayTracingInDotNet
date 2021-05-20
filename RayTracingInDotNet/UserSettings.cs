
namespace RayTracingInDotNet
{
	record UserSettings
	{
		// Scene
		public int SceneIndex;
		public int Speed = 100;

		// Renderer
		public bool AccumulateRays;
		public uint NumberOfSamples;
		public uint NumberOfBounces;
		public uint MaxNumberOfSamples;
		public bool VSync;
		public bool Fullscreen;

		// Camera
		public float FieldOfView;
		public float Aperture;
		public float FocusDistance;

		// Profiler
		public bool ShowHeatmap;
		public float HeatmapScale;

		// UI
		public bool ShowSettings;
		public bool ShowOverlay;

		public const float FieldOfViewMinValue = 10.0f;
		public const float FieldOfViewMaxValue = 90.0f;

		public bool RequiresAccumulationReset(UserSettings prev) =>
			AccumulateRays != prev.AccumulateRays ||
			NumberOfBounces != prev.NumberOfBounces ||
			FieldOfView != prev.FieldOfView ||
			Aperture != prev.Aperture ||
			FocusDistance != prev.FocusDistance;
	}
}
