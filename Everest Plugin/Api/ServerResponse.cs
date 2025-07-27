using System;
using MessagePack;
using Newtonsoft.Json;

namespace Everest.Api
{
    [MessagePackObject]
    public class ServerResponse
    {
        [JsonProperty("identifier"), Key(0)]
        public Guid Guid;
        [JsonProperty("data"), Key(1)]
        public SkeletonData[] Skeletons;
    }
}
