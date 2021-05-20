using System.Collections.Generic;
using System.Numerics;

namespace RayTracingInDotNet.Scene
{
	[Scene("Cube And Spheres")]
	class CubeAndSpheres : IScene
	{
		public List<Model> Models { get; private set; } = new List<Model>();
		public List<Texture> Textures { get; private set; } = new List<Texture>();

		public void Reset(CameraInitialState camera)
		{
			Models.Clear();
			Textures.Clear();

			// Basic test scene.

			camera.ModelView = Matrix4x4.CreateTranslation(new Vector3(0, 0, -2));
			camera.FieldOfView = 90;
			camera.Aperture = 0.05f;
			camera.FocusDistance = 2.0f;
			camera.ControlSpeed = 2.0f;
			camera.GammaCorrection = false;
			camera.SkyColor1 = new Vector4(1);
			camera.SkyColor2 = new Vector4(.5f, .7f, 1f, 1f);

			Models.Add(Model.LoadModel("./assets/models/cube_multi.obj"));
			Models.Add(Model.CreateSphere(new Vector3(1, 0, 0), 0.5f, Material.Metallic(new Vector3(0.7f, 0.5f, 0.8f), 0.2f), true));
			Models.Add(Model.CreateSphere(new Vector3(-1, 0, 0), 0.5f, Material.Dielectric(1.5f), true));
			Models.Add(Model.CreateSphere(new Vector3(0, 1, 0), 0.5f, Material.Lambertian(new Vector3(1.0f), 0), true));

			Textures.Add(Texture.LoadTexture("./assets/textures/land_ocean_ice_cloud_2048.png"));
		}
	}
}
