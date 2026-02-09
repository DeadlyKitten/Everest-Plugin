using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;

namespace Everest.Jobs
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NametagJobNative
    {
        public float3* SkeletonPositions;
        public NametagResult* Results;

        public Matrix4x4 ViewProjectionMatrix;
        public float3 CameraPosition;
        public float3 CameraForward;
        public float ScreenWidth, ScreenHeight;

        public float MaxDistanceSquared, MinDistanceSquared;
        public float MaxViewAngleCosine;
        public float TextVerticalOffset;
        public float MinTextScale;
        public float MaxTextScale;
    }
}
