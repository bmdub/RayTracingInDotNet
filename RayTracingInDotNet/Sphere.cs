using System.Numerics;

namespace RayTracingInDotNet
{
	class Sphere : Procedural
	{
		public readonly Vector3 Center;
		public readonly float Radius;

		public Sphere(in Vector3 center, float radius) =>
			(Center, Radius) = (center, radius);

		public override (Vector3, Vector3) BoundingBox =>
			(Center.Add(-Radius), Center.Add(Radius));
	}
}
