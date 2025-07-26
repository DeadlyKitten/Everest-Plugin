namespace Everest.Api
{
    public class ServerStatusResponse
    {
        public string status { get; set; } = string.Empty;
        public string serverTimeUTC { get; set; } = string.Empty;
        public string messageOfTheDay { get; set; } = string.Empty;
        public string updateInfo { get; set; } = string.Empty;
        public string message { get; set; } = string.Empty;
    }
}
