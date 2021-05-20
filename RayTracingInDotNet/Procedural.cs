using System.Numerics;

namespace RayTracingInDotNet
{
	abstract class Procedural
	{
		public virtual (Vector3, Vector3) BoundingBox => default;
	}
}
