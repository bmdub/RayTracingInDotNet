using System.Collections.Generic;
using System.Numerics;

namespace RayTracingInDotNet
{
	static class CornellBox
	{
		public static (List<Vertex> Vertices, List<uint> Indices, List<Material> Materials)	Create(float scale)
		{
			var vertices = new List<Vertex>();
			var indices = new List<uint>();
			var materials = new List<Material>();

			materials.Add(Material.Lambertian(new Vector3(0.65f, 0.05f, 0.05f))); // red
			materials.Add(Material.Lambertian(new Vector3(0.12f, 0.45f, 0.15f))); // green
			materials.Add(Material.Lambertian(new Vector3(0.73f, 0.73f, 0.73f))); // white
			materials.Add(Material.DiffuseLight(new Vector3(15.0f))); // light

			float s = scale;

			var l0 = new Vector3(0, 0, 0);
			var l1 = new Vector3(0, 0, -s);
			var l2 = new Vector3(0, s, -s);
			var l3 = new Vector3(0, s, 0);

			var r0 = new Vector3(s, 0, 0);
			var r1 = new Vector3(s, 0, -s);
			var r2 = new Vector3(s, s, -s);
			var r3 = new Vector3(s, s, 0);

			// Left green panel
			var i = (uint)vertices.Count;
			vertices.Add(new Vertex(l0, new Vector3(1, 0, 0), new Vector2(0, 1), 1));
			vertices.Add(new Vertex(l1, new Vector3(1, 0, 0), new Vector2(1, 1), 1));
			vertices.Add(new Vertex(l2, new Vector3(1, 0, 0), new Vector2(1, 0), 1));
			vertices.Add(new Vertex(l3, new Vector3(1, 0, 0), new Vector2(0, 0), 1));

			AddTriangle(indices, i, 0, 1, 2);
			AddTriangle(indices, i, 0, 2, 3);

			// Right red panel
			i = (uint)vertices.Count;
			vertices.Add(new Vertex(r0, new Vector3(-1, 0, 0), new Vector2(0, 1), 0));
			vertices.Add(new Vertex(r1, new Vector3(-1, 0, 0), new Vector2(1, 1), 0));
			vertices.Add(new Vertex(r2, new Vector3(-1, 0, 0), new Vector2(1, 0), 0));
			vertices.Add(new Vertex(r3, new Vector3(-1, 0, 0), new Vector2(0, 0), 0));

			AddTriangle(indices, i, 2, 1, 0);
			AddTriangle(indices, i, 3, 2, 0);

			// Back white panel
			i = (uint)vertices.Count;
			vertices.Add(new Vertex(l1, new Vector3(0, 0, 1), new Vector2(0, 1), 2));
			vertices.Add(new Vertex(r1, new Vector3(0, 0, 1), new Vector2(1, 1), 2));
			vertices.Add(new Vertex(r2, new Vector3(0, 0, 1), new Vector2(1, 0), 2));
			vertices.Add(new Vertex(l2, new Vector3(0, 0, 1), new Vector2(0, 0), 2));

			AddTriangle(indices, i, 0, 1, 2);
			AddTriangle(indices, i, 0, 2, 3);

			// Bottom white panel
			i = (uint)vertices.Count;
			vertices.Add(new Vertex(l0, new Vector3(0, 1, 0), new Vector2(0, 1), 2));
			vertices.Add(new Vertex(r0, new Vector3(0, 1, 0), new Vector2(1, 1), 2));
			vertices.Add(new Vertex(r1, new Vector3(0, 1, 0), new Vector2(1, 0), 2));
			vertices.Add(new Vertex(l1, new Vector3(0, 1, 0), new Vector2(0, 0), 2));

			AddTriangle(indices, i, 0, 1, 2);
			AddTriangle(indices, i, 0, 2, 3);

			// Top white panel
			i = (uint)vertices.Count;
			vertices.Add(new Vertex(l2, new Vector3(0, -1, 0), new Vector2(0, 1), 2));
			vertices.Add(new Vertex(r2, new Vector3(0, -1, 0), new Vector2(1, 1), 2));
			vertices.Add(new Vertex(r3, new Vector3(0, -1, 0), new Vector2(1, 0), 2));
			vertices.Add(new Vertex(l3, new Vector3(0, -1, 0), new Vector2(0, 0), 2));

			AddTriangle(indices, i, 0, 1, 2);
			AddTriangle(indices, i, 0, 2, 3);

			// Light
			i = (uint)vertices.Count;

			float x0 = s * (213.0f / 555.0f);
			float x1 = s * (343.0f / 555.0f);
			float z0 = s * (-555.0f + 332.0f) / 555.0f;
			float z1 = s * (-555.0f + 227.0f) / 555.0f;
			float y1 = s * 0.998f;

			vertices.Add(new Vertex(new Vector3(x0, y1, z1), new Vector3(0, -1, 0), new Vector2(0, 1), 3));
			vertices.Add(new Vertex(new Vector3(x1, y1, z1), new Vector3(0, -1, 0), new Vector2(1, 1), 3));
			vertices.Add(new Vertex(new Vector3(x1, y1, z0), new Vector3(0, -1, 0), new Vector2(1, 0), 3));
			vertices.Add(new Vertex(new Vector3(x0, y1, z0), new Vector3(0, -1, 0), new Vector2(0, 0), 3));

			AddTriangle(indices, i, 0, 1, 2);
			AddTriangle(indices, i, 0, 2, 3);

			return (vertices, indices, materials);
		}

		static void AddTriangle(List<uint> indices, uint offset, uint i0, uint i1, uint i2)
		{
			indices.Add(offset + i0);
			indices.Add(offset + i1);
			indices.Add(offset + i2);
		}
	}
}
