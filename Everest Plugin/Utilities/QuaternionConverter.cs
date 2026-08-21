using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Everest.Utilities
{
    internal class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var eulerAngles = serializer.Deserialize<Vector3>(reader);
            return Quaternion.Euler(eulerAngles);
        }

        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value.eulerAngles);
        }
    }
}
