namespace RayTracingInDotNet
{
	public static class MathExtensions
    {
        public const float RadiansPerDegree = 0.0174533f;
        public const float DegreesPerRadian = 57.2958f;

        public static float ToRadians(float degrees) => degrees * RadiansPerDegree;

        public static float ToDegrees(float radians) => radians * DegreesPerRadian;
    }
}
