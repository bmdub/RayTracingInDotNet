using Silk.NET.Assimp;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace RayTracingInDotNet
{
	class Model
	{
		private readonly List<Vertex> _vertices;
		private readonly List<uint> _indices;
		private readonly List<Material> _materials;
		private readonly Procedural _procedural;

		private Model(in Matrix4x4 transform, List<Vertex> vertices, List<uint> indices, List<Material> materials, Procedural procedural) =>
			(Transform, _vertices, _indices, _materials, _procedural) = (transform, vertices, indices, materials, procedural);

		public Model Clone()
		{
			return new Model(Transform, new List<Vertex>(_vertices), new List<uint>(_indices), new List<Material>(_materials), _procedural);
		}

		public void SetMaterial(in Material material)
		{
			if (_materials.Count != 1)
				throw new Exception($"{nameof(Model)}: Cannot change material on a multi-material model");

			_materials[0] = material;
		}

		public void TransformVertices(in Matrix4x4 transform)
		{
			bool success = Matrix4x4.Invert(Matrix4x4.Transpose(transform), out var transformIT);
			if (!success)
				throw new Exception($"{nameof(Model)}: Matrix4x4.Invert() failed");

			for (int i = 0; i < _vertices.Count; i++)
			{
				_vertices[i] = new Vertex(
					transform.MultiplyColumnOriented(new Vector4(_vertices[i].Position, 1)).ToVector3(),
					transformIT.MultiplyColumnOriented(new Vector4(_vertices[i].Normal, 0)).ToVector3(),
					_vertices[i].TexCoord,
					_vertices[i].MaterialIndex
				);
			}
		}

		public Matrix4x4 Transform { get; set; }
		public List<Vertex> Vertices => _vertices;
		public List<uint> Indices => _indices;
		public List<Material> Materials => _materials;
		public Procedural Procedural => _procedural;

		public static unsafe Model LoadModel(string filename)
		{
			List<Material> materials = new List<Material>();

			const uint aiProcess_JoinIdenticalVertices = 0x2;
			const uint aiProcess_Triangulate = 0x8;
			const uint aiProcess_GenSmoothNormals = 0x40;
			//const uint aiProcess_GenUVCoords = 0x40000;
			//const uint aiProcess_TransformUVCoords = 0x80000;
			const uint aiProcess_CalcTangentSpace = 0x1;
			//const uint aiProcess_GenNormals = 0x20;
			//const uint aiProcess_FlipWindingOrder = 0x1000000;
			//const uint aiProcess_MakeLeftHanded = 0x4;
			//const uint aiProcess_FlipUVs = 0x800000;
			//const uint aiProcess_EmbedTextures = 0x10000000;
			const uint aiProcess_PreTransformVertices = 0x100;
			//const uint aiProcess_ConvertToLeftHanded = aiProcess_MakeLeftHanded | aiProcess_FlipUVs | aiProcess_FlipWindingOrder;

			var pScene = Assimp.GetApi().ImportFile(filename, aiProcess_JoinIdenticalVertices | aiProcess_Triangulate | aiProcess_PreTransformVertices | aiProcess_CalcTangentSpace | aiProcess_GenSmoothNormals);

			for (uint i = 0; i < pScene->MNumMaterials; i++)
			{
				string name = null;
				Material mat = default;

				for (int p = 0; p < pScene->MMaterials[i]->MNumProperties; p++)
				{
					var key = pScene->MMaterials[i]->MProperties[p]->MKey;
					var index = pScene->MMaterials[i]->MProperties[p]->MIndex;
					var semantic = pScene->MMaterials[i]->MProperties[p]->MSemantic;
					var type = pScene->MMaterials[i]->MProperties[p]->MType;
					var datalength = (int)pScene->MMaterials[i]->MProperties[p]->MDataLength;
					//Console.WriteLine($"{key}, {index}, {semantic}, {type}, {datalength}");

					var dataSpan = new ReadOnlySpan<byte>(pScene->MMaterials[i]->MProperties[p]->MData, datalength);

					switch (key)
					{
						case "$clr.diffuse":
							float r = BitConverter.ToSingle(dataSpan.Slice(0, 4));
							float g = BitConverter.ToSingle(dataSpan.Slice(4, 4));
							float b = BitConverter.ToSingle(dataSpan.Slice(8, 4));
							mat = new Material(new Vector4(r, g, b, 1.0f), -1, 0, 0, Material.MaterialModel.Lambertian);
							break;
						case "?mat.name":
							// Field has length integer and null terminator; slice them out
							name = System.Text.Encoding.UTF8.GetString(dataSpan.Slice(4, datalength - (4 + 1)));
							break;
					}
				}

				materials.Add(mat);
			}

			List<Vertex> vertices = new List<Vertex>();
			List<uint> indices = new List<uint>();
			Dictionary<Vertex, uint> uniqueVertices = new Dictionary<Vertex, uint>();

			bool hasNormals = false;

			for (int m=0; m<pScene->MNumMeshes; m++)
			{
				var materialIndex = (int)pScene->MMeshes[m]->MMaterialIndex;

				for (int f = 0; f < pScene->MMeshes[m]->MNumFaces; f++)
				{
					if (pScene->MMeshes[m]->MFaces[f].MNumIndices != 3)
						continue; // Ignore points/lines. Polys should be converted to triangles using the import flags.
					for (int i = 0; i < pScene->MMeshes[m]->MFaces[f].MNumIndices; i++)
						indices.Add(pScene->MMeshes[m]->MFaces[f].MIndices[i] + (uint)vertices.Count);
				}

				for (int v = 0; v < pScene->MMeshes[m]->MNumVertices; v++)
				{
					var vertex = new Vertex();

					vertex.Position = pScene->MMeshes[m]->MVertices[v];
					vertex.Normal = pScene->MMeshes[m]->MNormals[v];
					hasNormals = hasNormals || vertex.Normal != default;
					if (pScene->MMeshes[m]->MNumUVComponents[0] == 2) //
						vertex.TexCoord = new Vector2(pScene->MMeshes[m]->MTextureCoords.Element0->X, pScene->MMeshes[m]->MTextureCoords.Element0->Y);
					vertex.MaterialIndex = materialIndex;

					vertices.Add(vertex);
				}
			}

			// Perhaps add texture loading some day
			/*Texture texture = null;
			if(pScene->MNumTextures > 0)
			{
				// Handle 1 texture for now
				int width = (int)pScene->MTextures[0]->MWidth;
				int height = (int)pScene->MTextures[0]->MHeight;

				if (pScene->MTextures[0]->MHeight == 0) // Compressed
					texture = Texture.LoadTexture(new ReadOnlySpan<byte>((void*)pScene->MTextures[0]->PcData, width));
				else
				{
					var pixels = new Span<byte>(pScene->MTextures[0]->PcData, width * height * 4).ToArray();
					texture = Texture.LoadTexture(pixels, width, height);
				}
			}*/

			return new Model(Matrix4x4.Identity, vertices, indices, materials, null);
		}

		public static Model CreateCornellBox(float scale)
		{
			var cornellBox = CornellBox.Create(scale);

			return new Model(Matrix4x4.Identity, cornellBox.Vertices, cornellBox.Indices, cornellBox.Materials, null);
		}

		public static Model CreateGroundRect(in Vector3 p0, float width, float depth, in Material material, float uvDim)
		{
			width *= .5f;
			depth *= .5f;

			List<Vertex> vertices = new List<Vertex>()
			{
				new Vertex(new Vector3(p0.X + width, p0.Y, p0.Z - depth), new Vector3(0, 1, 0), new Vector2(uvDim, 0), 0),
				new Vertex(new Vector3(p0.X - width, p0.Y, p0.Z - depth), new Vector3(0, 1, 0), new Vector2(0, 0), 0),
				new Vertex(new Vector3(p0.X - width, p0.Y, p0.Z + depth), new Vector3(0, 1, 0), new Vector2(0, uvDim), 0),
				new Vertex(new Vector3(p0.X + width, p0.Y, p0.Z + depth), new Vector3(0, 1, 0), new Vector2(uvDim, uvDim), 0),
			};

			List<uint> indices = new List<uint>()
			{
				0, 1, 2, 0, 2, 3
			};

			return new Model(Matrix4x4.Identity, vertices, indices, new List<Material>() { material }, null);
		}

		// Note: CreateBox() corners must be provided in the correct order, or else it will invert (along with it's apparent normals)
		public static Model CreateBox(in Vector3 p0, in Vector3 p1, in Material material)
		{
			List<Vertex> vertices = new List<Vertex>()
			{
				new Vertex(new Vector3(p0.X, p0.Y, p0.Z), new Vector3(-1, 0, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p0.X, p0.Y, p1.Z), new Vector3(-1, 0, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p0.X, p1.Y, p1.Z), new Vector3(-1, 0, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p0.X, p1.Y, p0.Z), new Vector3(-1, 0, 0), new Vector2(0), 0),

				new Vertex(new Vector3(p1.X, p0.Y, p1.Z), new Vector3(1, 0, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p1.X, p0.Y, p0.Z), new Vector3(1, 0, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p1.X, p1.Y, p0.Z), new Vector3(1, 0, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p1.X, p1.Y, p1.Z), new Vector3(1, 0, 0), new Vector2(0), 0),

				new Vertex(new Vector3(p1.X, p0.Y, p0.Z), new Vector3(0, 0, -1), new Vector2(0), 0),
				new Vertex(new Vector3(p0.X, p0.Y, p0.Z), new Vector3(0, 0, -1), new Vector2(0), 0),
				new Vertex(new Vector3(p0.X, p1.Y, p0.Z), new Vector3(0, 0, -1), new Vector2(0), 0),
				new Vertex(new Vector3(p1.X, p1.Y, p0.Z), new Vector3(0, 0, -1), new Vector2(0), 0),

				new Vertex(new Vector3(p0.X, p0.Y, p1.Z), new Vector3(0, 0, 1), new Vector2(0), 0),
				new Vertex(new Vector3(p1.X, p0.Y, p1.Z), new Vector3(0, 0, 1), new Vector2(0), 0),
				new Vertex(new Vector3(p1.X, p1.Y, p1.Z), new Vector3(0, 0, 1), new Vector2(0), 0),
				new Vertex(new Vector3(p0.X, p1.Y, p1.Z), new Vector3(0, 0, 1), new Vector2(0), 0),

				new Vertex(new Vector3(p0.X, p0.Y, p0.Z), new Vector3(0, -1, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p1.X, p0.Y, p0.Z), new Vector3(0, -1, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p1.X, p0.Y, p1.Z), new Vector3(0, -1, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p0.X, p0.Y, p1.Z), new Vector3(0, -1, 0), new Vector2(0), 0),

				new Vertex(new Vector3(p1.X, p1.Y, p0.Z), new Vector3(0, 1, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p0.X, p1.Y, p0.Z), new Vector3(0, 1, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p0.X, p1.Y, p1.Z), new Vector3(0, 1, 0), new Vector2(0), 0),
				new Vertex(new Vector3(p1.X, p1.Y, p1.Z), new Vector3(0, 1, 0), new Vector2(0), 0),
			};

			List<uint> indices = new List<uint>()
			{
				0, 1, 2, 0, 2, 3,
				4, 5, 6, 4, 6, 7,
				8, 9, 10, 8, 10, 11,
				12, 13, 14, 12, 14, 15,
				16, 17, 18, 16, 18, 19,
				20, 21, 22, 20, 22, 23
			};

			return new Model(Matrix4x4.Identity, vertices, indices, new List<Material>() { material }, null);
		}

		public static Model CreateSphere(in Vector3 center, float radius, in Material material, bool isProcedural)
		{
			int slices = 32;
			int stacks = 16;

			List<Vertex> vertices = new List<Vertex>();
			List<uint> indices = new List<uint>();

			float pi = 3.14159265358979f;

			for (int j = 0; j <= stacks; j++)
			{
				float j0 = pi * j / stacks;

				// Vertex
				float v = radius * -MathF.Sin(j0);
				float z = radius * MathF.Cos(j0);

				// Normals		
				float n0 = -MathF.Sin(j0);
				float n1 = MathF.Cos(j0);

				for (int i = 0; i <= slices; i++)
				{
					float i0 = 2 * pi * i / slices;

					var position = new Vector3(
						center.X + v * MathF.Sin(i0),
						center.Y + z,
						center.Z + v * MathF.Cos(i0));

					var normal = new Vector3(
						n0 * MathF.Sin(i0),
						n1,
						n0 * MathF.Cos(i0));

					var texCoord = new Vector2((float)i / slices, (float)j / stacks);

					vertices.Add(new Vertex(position, normal, texCoord, 0));
				}
			}

			for (int j = 0; j < stacks; j++)
			{
				for (int i = 0; i < slices; i++)
				{
					var j0 = (j + 0) * (slices + 1);
					var j1 = (j + 1) * (slices + 1);
					var i0 = i + 0;
					var i1 = i + 1;

					indices.Add((uint)j0 + (uint)i0);
					indices.Add((uint)j1 + (uint)i0);
					indices.Add((uint)j1 + (uint)i1);

					indices.Add((uint)j0 + (uint)i0);
					indices.Add((uint)j1 + (uint)i1);
					indices.Add((uint)j0 + (uint)i1);
				}
			}

			return new Model(Matrix4x4.Identity, vertices, indices, new List<Material> { material }, isProcedural ? new Sphere(center, radius) : null);
		}
	}
}
