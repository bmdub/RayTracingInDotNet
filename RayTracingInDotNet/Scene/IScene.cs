
using System.Collections.Generic;
using System.Numerics;

namespace RayTracingInDotNet.Scene
{
	interface IScene
	{
		public List<Model> Models { get; }
		public List<Texture> Textures { get; }

		public void Reset(CameraInitialState camera);

		public bool UpdateTransforms(double delta, UserSettings userSettings, Matrix4x4[] transforms)
		{
			return false;
		}
	}
}
