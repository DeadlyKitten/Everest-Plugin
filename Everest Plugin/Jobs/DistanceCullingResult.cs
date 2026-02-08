using System;
using System.Runtime.InteropServices;

namespace Everest.Jobs
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DistanceCullingResult : IComparable<DistanceCullingResult>
    {
        public int index;
        public float distance;

        public DistanceCullingResult(int index, float distance)
        {
            this.index = index;
            this.distance = distance;
        }

        public int CompareTo(DistanceCullingResult other)
        {
            return distance.CompareTo(other.distance);
        }
    }
}