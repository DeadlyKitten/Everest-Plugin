using MessagePack;
using Newtonsoft.Json;

namespace Everest.Api
{
    [MessagePackObject]
    public class DailyCountResponse
    {
        [JsonProperty("count"), Key(0)]
        public int Count { get; set; }

        [JsonProperty("start_time_utc"), Key(1)]
        public string StartTimeUTC { get; set; }

        [JsonProperty("current_time_utc"), Key(2)]
        public string CurrentTimeUTC { get; set; }

        [JsonProperty("message"), Key(3)]
        public string Message { get; set; }
    }
}
