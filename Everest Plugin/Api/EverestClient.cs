using System.Net;
using System.Net.Http;
using System.Text;
using Cysharp.Threading.Tasks;
using Everest.Core;
using Newtonsoft.Json;
using UnityEngine;

namespace Everest.Api
{
    public static class EverestClient
    {
#if DEBUG
        private const string SERVER_BASE_URL = "https://dev.peak-everest.com/api/v2";
#else
        private const string SERVER_BASE_URL = "https://peak-everest.com/api/v2";
#endif

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate });

        public static async UniTaskVoid SubmitDeathAsync(SubmissionRequest request)
        {
            var endpoint = $"/Skeletons/submit";
            var payload = JsonConvert.SerializeObject(request);
            var response = await PostAsync<SubmissionResponse>(endpoint, payload);

            if (response != null && response.message != null)
            {
                UIHandler.Instance.Toast(response.message, Color.grey, 2f, 2f);
                EverestPlugin.LogDebug(response.message);
            }
        }

        public static async UniTask<ServerResponse> RetrieveAsync(int mapId)
        {
            var maxSkeletons = ConfigHandler.MaxSkeletons;
            var excludeCrashSite = ConfigHandler.ExcludeNearCrashSite;
            var excludeCampfires = ConfigHandler.ExcludeNearCampfires;

            var endpoint = $"/Skeletons/recent?map_id={mapId}&limit={maxSkeletons}&excludeCrashSite={excludeCrashSite}&excludeCampfire={excludeCampfires}";
            return await GetAsync<ServerResponse>(endpoint);
        }

        public static async UniTask<ServerResponse> RetrieveAsync(string identifier)
        {
            var endpoint = $"/Skeletons/recent/{identifier}";
            return await GetAsync<ServerResponse>(endpoint);
        }

        public static async UniTask<DailyCountResponse> RetrieveCountForDayAsync()
        {
            var endpoint = "/Stats/daily/submissions";
            return await GetAsync<DailyCountResponse>(endpoint);
        }

        public static async UniTask<ServerStatusResponse> RetrieveServerStatusAsync(string version)
        {
            var endpoint = $"/Status?clientVersion={version}";
            return await GetAsync<ServerStatusResponse>(endpoint, false);
        }

        private static async UniTask<T> GetAsync<T>(string endpoint, bool bailOnFail = true)
        {
            var response = await _httpClient.GetAsync($"{SERVER_BASE_URL}{endpoint}");

            if (response.StatusCode != HttpStatusCode.OK)
            {
                EverestPlugin.LogError($"Server responded with status code {response.StatusCode}");
                if (response.StatusCode == (HttpStatusCode)521)
                {
                    EverestPlugin.LogError("Everest server is offline. Please try again later.");
                    return default;
                }
                EverestPlugin.LogError(JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync()).error);
                if (bailOnFail) return default;
            }

            return await UniTask.RunOnThreadPool(async () => JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync()));
        }

        private static async UniTask<T> PostAsync<T>(string endpoint, string requestPayload)
        {

            var response = await _httpClient.PostAsync($"{SERVER_BASE_URL}{endpoint}", 
                new StringContent(requestPayload, Encoding.UTF8, "application/json"));


            if (response.StatusCode != HttpStatusCode.OK)
            {
                EverestPlugin.LogError($"Server responded with status code {response.StatusCode}");
                EverestPlugin.LogError(JsonConvert.DeserializeObject<ErrorResponse>(await response.Content.ReadAsStringAsync()).error);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var timeToWait = response.Headers.RetryAfter.Delta.Value.Seconds;
                    UIHandler.Instance.Toast($"You are being rate limited. Please wait {timeToWait} seconds before dying again.", Color.red, 3f, 2f);
                    return default;
                }
            }

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }
    }
}
