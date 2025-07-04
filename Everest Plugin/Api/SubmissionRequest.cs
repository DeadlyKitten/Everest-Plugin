using Everest.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace Everest.Api
{
    public class SubmissionRequest
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

        public SubmissionRequest(string steamId, int mapId, int mapSegment, Vector3 globalPos, Vector3 globalRot, Vector3[] boneLocalPos, Vector3[] boneLocalRot)
        {
            this.steam_id = steamId;
            this.map_id = mapId;
            this.map_segment = mapSegment;
            this.global_position = globalPos;
            this.global_rotation = globalRot;
            this.bone_local_positions = boneLocalPos;
            this.bone_local_rotations = boneLocalRot;
        }

    }
}
