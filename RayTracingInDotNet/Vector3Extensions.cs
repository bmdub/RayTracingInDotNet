using System.Numerics;

namespace RayTracingInDotNet
{
	static class Vector3Extensions
	{
		public static Vector3 Add(this in Vector3 vec, float value) =>
			new Vector3(vec.X + value, vec.Y + value, vec.Z + value);
	}
}
