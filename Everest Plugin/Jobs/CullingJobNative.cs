using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace Everest.Jobs
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct CullingJobNative
    {
        public float3 CameraPosition;
        public float SquaredDrawDistance;

        public float* SkeletonPositionsX;
        public float* SkeletonPositionsY;
        public float* SkeletonPositionsZ;

        public DistanceCullingResult* Results;
    }
}
