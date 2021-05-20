using System;
using System.Collections.Generic;
using System.Numerics;

namespace RayTracingInDotNet.Scene
{
	[Scene("Cornell Box")]
	class CornellBox : IScene
	{
		public List<Model> Models { get; private set; } = new List<Model>();
		public List<Texture> Textures { get; private set; } = new List<Texture>();

		public void Reset(CameraInitialState camera)
		{
			Models.Clear();
			Textures.Clear();

			camera.ModelView = Matrix4x4.CreateLookAt(new Vector3(278, 278, 800), new Vector3(278, 278, 0), new Vector3(0, 1, 0));
			camera.FieldOfView = 40;
			camera.Aperture = 0.0f;
			camera.FocusDistance = 10.0f;
			camera.ControlSpeed = 500.0f;
			camera.GammaCorrection = true;
			camera.SkyColor1 = new Vector4(0);
			camera.SkyColor2 = new Vector4(0);

			var white = Material.Lambertian(new Vector3(0.73f, 0.73f, 0.73f));

			var box0 = Model.CreateBox(new Vector3(0, 0, -165), new Vector3(165, 165, 0), white);
			var box1 = Model.CreateBox(new Vector3(0, 0, -165), new Vector3(165, 330, 0), white);

			box0.TransformVertices(
				Matrix4x4.CreateTranslation(new Vector3(555 - 130 - 165, 0, -65))
				.RotateBy(new Vector3(0, MathExtensions.ToRadians(-18), 0)));

			box1.TransformVertices(
				Matrix4x4.CreateTranslation(new Vector3(555 - 265 - 165, 0, -295))
				.RotateBy(new Vector3(0, MathExtensions.ToRadians(15), 0)));

			Models.Add(Model.CreateCornellBox(555));
			Models.Add(box0);
			Models.Add(box1);
		}
	}
}
