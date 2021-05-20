using System;
using System.Collections.Generic;
using System.Numerics;

namespace RayTracingInDotNet.Scene
{
	[Scene("Cascading Mirrors")]
	class CascadingMirrors : IScene
	{
		public List<Model> Models { get; private set; } = new List<Model>();
		public List<Texture> Textures { get; private set; } = new List<Texture>();

		public void Reset(CameraInitialState camera)
		{
			Models.Clear();
			Textures.Clear();

			camera.ModelView = Matrix4x4.CreateLookAt(new Vector3(3.5f, 3, 1.5f), new Vector3(-15, 1.0f, 0), new Vector3(0, 1, 0));
			camera.FieldOfView = 20;
			camera.Aperture = 0.01f;
			camera.FocusDistance = 20.0f;
			camera.ControlSpeed = 5.0f;
			camera.GammaCorrection = true;
			camera.SkyColor1 = new Vector4(1);
			camera.SkyColor2 = new Vector4(.5f, .7f, 1f, 1f);

			Models.Add(Model.CreateGroundRect(new Vector3(0, 0, 0), 80, 80, Material.Metallic(new Vector3(.4f, .4f, .5f), .002f), 10f));

			var mirror1 = Model.CreateGroundRect(new Vector3(0, 0, 0), 6, 10, Material.Metallic(new Vector3(1), 0), 1);
			mirror1.Transform = Matrix4x4.CreateTranslation(5, 5.05f, 0).RotateBy(new Vector3(MathExtensions.ToRadians(90), MathExtensions.ToRadians(-90), 0));
			Models.Add(mirror1);
			var mirror1Frame = Model.CreateGroundRect(new Vector3(0, 0, 0), 6.1f, 10.1f, Material.Lambertian(new Vector3(0), 0), 1);
			mirror1Frame.Transform = Matrix4x4.CreateTranslation(5.01f, 5.05f, 0).RotateBy(new Vector3(MathExtensions.ToRadians(90), MathExtensions.ToRadians(-90), 0));
			Models.Add(mirror1Frame);

			var mirror2 = Model.CreateGroundRect(new Vector3(0, 0, 0), 6, 10, Material.Metallic(new Vector3(1), 0), 1);
			mirror2.Transform = Matrix4x4.CreateTranslation(-5, 5, 0).RotateBy(new Vector3(MathExtensions.ToRadians(-90), MathExtensions.ToRadians(-90), 0));
			Models.Add(mirror2);
			var mirror2Frame = Model.CreateGroundRect(new Vector3(0, 0, 0), 6.1f, 10.1f, Material.Lambertian(new Vector3(0), 0), 1);
			mirror2Frame.Transform = Matrix4x4.CreateTranslation(-5.01f, 5.05f, 0).RotateBy(new Vector3(MathExtensions.ToRadians(-90), MathExtensions.ToRadians(-90), 0));
			Models.Add(mirror2Frame);

			var lucy = Model.LoadModel("./assets/models/lucy.obj");

			lucy.TransformVertices(
				(Matrix4x4.CreateScale(new Vector3(0.0035f)) * Matrix4x4.CreateTranslation(new Vector3(0, -0.08f, 0)))
				.RotateBy(new Vector3(0, MathExtensions.ToRadians(0), 0)));

			lucy.SetMaterial(Material.Lambertian(new Vector3(0.2f, 0.4f, 0.2f)));

			Models.Add(lucy);
		}
	}
}
