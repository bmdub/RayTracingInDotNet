using System;
using System.Collections.Generic;
using System.Numerics;

namespace RayTracingInDotNet.Scene
{
	[Scene("Ray Tracing In One Weekend")]
	class RayTracingInOneWeekend : IScene
	{
		public List<Model> Models { get; private set; } = new List<Model>();
		public List<Texture> Textures { get; private set; } = new List<Texture>();

		public void Reset(CameraInitialState camera)
		{
			Models.Clear();
			Textures.Clear();

			// Final scene from Ray Tracing In One Weekend book.

			camera.ModelView = Matrix4x4.CreateLookAt(new Vector3(13, 2, 3), new Vector3(0, 0, 0), new Vector3(0, 1, 0));
			camera.FieldOfView = 20;
			camera.Aperture = 0.1f;
			camera.FocusDistance = 10.0f;
			camera.ControlSpeed = 5.0f;
			camera.GammaCorrection = true;
			camera.SkyColor1 = new Vector4(1);
			camera.SkyColor2 = new Vector4(.5f, .7f, 1f, 1f);

			const bool isProc = true;

			var random = new Random(42);

			Models.Add(Model.CreateSphere(new Vector3(0, -1000, 0), 1000, Material.Lambertian(new Vector3(0.5f, 0.5f, 0.5f)), isProc));

			for (int a = -11; a < 11; ++a)
			{
				for (int b = -11; b < 11; ++b)
				{
					float chooseMat = (float)random.NextDouble();
					var center = new Vector3(a + 0.9f * (float)random.NextDouble(), 0.2f, b + 0.9f * (float)random.NextDouble());

					if ((center - new Vector3(4, 0.2f, 0)).Length() > 0.9)
					{
						if (chooseMat < 0.8f) // Diffuse
						{
							Models.Add(Model.CreateSphere(center, 0.2f, Material.Lambertian(new Vector3(
								(float)random.NextDouble() * (float)random.NextDouble(),
								(float)random.NextDouble() * (float)random.NextDouble(),
								(float)random.NextDouble() * (float)random.NextDouble())),
								isProc));
						}
						else if (chooseMat < 0.95f) // Metal
						{
							Models.Add(Model.CreateSphere(center, 0.2f, Material.Metallic(
								new Vector3(0.5f * (1 + (float)random.NextDouble()), 0.5f * (1 + (float)random.NextDouble()), 0.5f * (1 + (float)random.NextDouble())),
								0.5f * (float)random.NextDouble()),
								isProc));
						}
						else // Glass
						{
							Models.Add(Model.CreateSphere(center, 0.2f, Material.Dielectric(1.5f), isProc));
						}
					}
				}
			}

			Models.Add(Model.CreateSphere(new Vector3(0, 1, 0), 1.0f, Material.Dielectric(1.5f), isProc));
			Models.Add(Model.CreateSphere(new Vector3(-4, 1, 0), 1.0f, Material.Lambertian(new Vector3(0.4f, 0.2f, 0.1f)), isProc));
			Models.Add(Model.CreateSphere(new Vector3(4, 1, 0), 1.0f, Material.Metallic(new Vector3(0.7f, 0.6f, 0.5f), 0.0f), isProc));
		}
	}
}
