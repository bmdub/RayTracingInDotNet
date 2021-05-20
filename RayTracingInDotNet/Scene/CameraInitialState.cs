using System.Numerics;

namespace RayTracingInDotNet.Scene
{
	public class CameraInitialState
	{
		public Matrix4x4 ModelView;
		public Vector4 SkyColor1;
		public Vector4 SkyColor2;
		public float FieldOfView;
		public float Aperture;
		public float FocusDistance;
		public float ControlSpeed;
		public bool GammaCorrection;
	};
}
