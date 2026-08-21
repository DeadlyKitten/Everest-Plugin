using System;
using UnityEngine;

namespace Everest.Api
{
    public class SkeletonData
    {
        public string steam_id;
        public string nickname;
        public int map_id;
        public int map_segment;
        public Vector3 global_position;
        public Vector3 global_rotation;
        public Vector3[] bone_local_positions;
        public Quaternion[] bone_local_rotations;
        public DateTime timestamp;
    }
}
