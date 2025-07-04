using Everest.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace Everest.Api
{
    public class SkeletonData
    {
        public string steam_id;
        public int map_id;
        public int map_segment;

        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 global_position;

        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 global_rotation;

        public Vector3[] bone_local_positions;
        public Vector3[] bone_local_rotations;

        public string timestamp;
    }
}
