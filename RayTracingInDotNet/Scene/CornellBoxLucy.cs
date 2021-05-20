using System;
using System.Collections.Generic;
using System.Numerics;

namespace RayTracingInDotNet.Scene
{
	[Scene("Cornell Box & Lucy")]
	class CornellBoxLucy : IScene
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

			var sphere = Model.CreateSphere(new Vector3(555 - 130, 165.0f, -165.0f / 2 - 65), 80.0f, Material.Dielectric(1.5f), true);
			var lucy0 = Model.LoadModel("./assets/models/lucy.obj");

			lucy0.TransformVertices(
				(Matrix4x4.CreateScale(new Vector3(.6f)) * Matrix4x4.CreateTranslation(new Vector3(555 - 300 - 165 / 2, -9, -295 - 165 / 2)))
				.RotateBy(new Vector3(0, MathExtensions.ToRadians(75), 0)));

			Models.Add(Model.CreateCornellBox(555));
			Models.Add(sphere);
			Models.Add(lucy0);
		}
	}
}
