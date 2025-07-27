using System;
using Everest.Utilities;
using MessagePack;
using Newtonsoft.Json;
using UnityEngine;

namespace Everest.Api
{
    [MessagePackObject]
    public class SkeletonData
    {
        [JsonProperty("steam_id"), Key(0)]
        public ulong steam_id;
        [JsonProperty("map_id"), Key(1)]
        public int map_id;

        [JsonProperty("global_position"), Key(2)]
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 global_position;

        [JsonProperty("global_rotation"), Key(3)]
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 global_rotation;

        [JsonProperty("bone_local_positions"), Key(4)]
        public Vector3[] bone_local_positions;
        [JsonProperty("bone_local_rotations"), Key(5)]
        public Vector3[] bone_local_rotations;

        [JsonProperty("timestamp"), Key(6)]
        public DateTime timestamp;
    }
}
