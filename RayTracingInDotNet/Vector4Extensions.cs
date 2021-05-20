using System.Numerics;

namespace RayTracingInDotNet
{
	static class Vector4Extensions
	{
		public static Vector3 ToVector3(this in Vector4 vec) => 
			new Vector3(vec.X, vec.Y, vec.Z);
	}
}
