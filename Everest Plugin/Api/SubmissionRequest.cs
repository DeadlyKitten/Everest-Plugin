using Everest.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace Everest.Api
{
    public class SubmissionRequest
    {
        [JsonProperty("steam_id")]
        public string SteamId { get; set; }
        [JsonProperty("auth_session_ticket")]
        public string AuthSessionTicket { get; set; }
        [JsonProperty("map_id")]
        public int MapId { get; set; }
        [JsonProperty("map_segment")]
        public int MapSegment { get; set; }

        [JsonProperty("crash_site")]
        public bool IsNearCrashSite { get; set; }

        [JsonProperty("campfire")]
        public bool IsNearCampfire { get; set; }

        [JsonProperty("global_position")]
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 GlobalPosition { get; set; }

        [JsonProperty("global_rotation")]
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 GlobalRotation { get; set; }

        [JsonProperty("bone_local_positions")]
        public Vector3[] BoneLocalPositions { get; set; }
        [JsonProperty("bone_local_rotations")]
        public Vector3[] BoneLocalRotations { get; set; }
    }
}
