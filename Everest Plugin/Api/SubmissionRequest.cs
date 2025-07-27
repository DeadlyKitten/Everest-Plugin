using Everest.Utilities;
using MessagePack;
using Newtonsoft.Json;
using UnityEngine;

namespace Everest.Api
{
    [MessagePackObject]
    public class SubmissionRequest
    {
        [JsonProperty("steam_id"), Key(0)]
        public ulong steam_id;

        [JsonProperty("auth_session_ticket"), Key(1)]
        public string auth_session_ticket;

        [JsonProperty("map_id"), Key(2)]
        public int map_id;

        [JsonProperty("global_position"), Key(3)]
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 global_position;

        [JsonProperty("global_rotation"), Key(4)]
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 global_rotation;

        [JsonProperty("bone_local_positions"), Key(5)]
        public Vector3[] bone_local_positions;

        [JsonProperty("bone_local_rotations"), Key(6)]
        public Vector3[] bone_local_rotations;

        public SubmissionRequest(ulong steamId, string authTicket, int mapId, Vector3 globalPos, Vector3 globalRot, Vector3[] boneLocalPos, Vector3[] boneLocalRot)
        {
            this.steam_id = steamId;
            this.auth_session_ticket = authTicket;
            this.map_id = mapId;
            this.global_position = globalPos;
            this.global_rotation = globalRot;
            this.bone_local_positions = boneLocalPos;
            this.bone_local_rotations = boneLocalRot;
        }

    }
}
