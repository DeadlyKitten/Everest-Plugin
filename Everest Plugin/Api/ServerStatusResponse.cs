using System;
using MessagePack;

namespace Everest.Api
{
    [MessagePackObject]
    public class ServerStatusResponse
    {
        [Key(0)]
        public string status { get; set; } = string.Empty;

        [Key(1)]
        public DateTime serverTimeUTC { get; set; }

        [Key(2)]
        public string messageOfTheDay { get; set; } = string.Empty;

        [Key(3)]
        public string updateInfo { get; set; } = string.Empty;

        [Key(4)]
        public string message { get; set; } = string.Empty;
    }
}
