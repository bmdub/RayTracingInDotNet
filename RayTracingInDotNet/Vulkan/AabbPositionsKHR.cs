namespace RayTracingInDotNet.Vulkan
{
	struct AabbPositionsKHR
    {
        public readonly float MinX;
        public readonly float MinY;
        public readonly float MinZ;
        public readonly float MaxX;
        public readonly float MaxY;
        public readonly float MaxZ;

        public AabbPositionsKHR(float minX, float minY, float minZ, float maxX, float maxY, float maxZ) =>
            (MinX, MinY, MinZ, MaxX, MaxY, MaxZ) = (minX, minY, minZ, maxX, maxY, maxZ);
    }
}
