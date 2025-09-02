using System.Text;
using Cysharp.Threading.Tasks;
using Everest.Core;
using Everest.UI;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Everest.Api
{
    public static class EverestClient
    {
#if DEBUG
        private const string SERVER_BASE_URL = "https://dev.peak-everest.com/api/v2";
#else
        private const string SERVER_BASE_URL = "https://peak-everest.com/api/v2";
#endif

        public static async UniTaskVoid SubmitDeathAsync(SubmissionRequest request)
        {
            EverestPlugin.LogInfo("Submitting death...");

            var endpoint = $"/Skeletons/submit";
            var payload = JsonConvert.SerializeObject(request);
            var response = await PostAsync<SubmissionResponse>(endpoint, payload);

            if (response != null && response.message != null)
            {
                ToastController.Instance.Toast(response.message, Color.grey, 2f, 2f);
                EverestPlugin.LogDebug(response.message);
            }
        }

        public static async UniTask<ServerResponse> RetrieveAsync(int mapId)
        {
            EverestPlugin.LogInfo("Retrieving skeletons by map ID...");

            var maxSkeletons = ConfigHandler.MaxSkeletons;
            var excludeCrashSite = ConfigHandler.ExcludeNearCrashSite;
            var excludeCampfires = ConfigHandler.ExcludeNearCampfires;

            var endpoint = $"/Skeletons/recent?map_id={mapId}&limit={maxSkeletons}&excludeCrashSite={excludeCrashSite}&excludeCampfire={excludeCampfires}";
            return await GetAsync<ServerResponse>(endpoint);
        }

        public static async UniTask<ServerResponse> RetrieveAsync(string identifier)
        {
            EverestPlugin.LogInfo("Retrieving skeletons by identifier...");
            var endpoint = $"/Skeletons/recent/{identifier}";
            return await GetAsync<ServerResponse>(endpoint);
        }

        public static async UniTask<DailyCountResponse> RetrieveCountForDayAsync()
        {
            EverestPlugin.LogInfo("Retrieving daily count...");
            var endpoint = "/Stats/daily/submissions";
            return await GetAsync<DailyCountResponse>(endpoint);
        }

        public static async UniTask<ServerStatusResponse> RetrieveServerStatusAsync(string version)
        {
            EverestPlugin.LogInfo("Retrieving server status...");
            var endpoint = $"/Status?clientVersion={version}";
            return await GetAsync<ServerStatusResponse>(endpoint, false);
        }

        private static async UniTask<T> GetAsync<T>(string endpoint, bool bailOnFail = true)
        {
            using var downloadHandler = new DownloadHandlerBuffer();
            using var unityWebRequest = new UnityWebRequest($"{SERVER_BASE_URL}{endpoint}", "GET", downloadHandler, null);

            _ = unityWebRequest.SendWebRequest();
            await UniTask.WaitUntil(() => unityWebRequest.isDone);

            if (unityWebRequest.result != UnityWebRequest.Result.Success)
            {
                EverestPlugin.LogError($"Server responded with status code {unityWebRequest.responseCode}");
                if (unityWebRequest.responseCode == 521)
                {
                    EverestPlugin.LogError("Everest server is offline. Please try again later.");
                    return default;
                }
                EverestPlugin.LogError(JsonConvert.DeserializeObject<ErrorResponse>(downloadHandler.text).message);
                if (bailOnFail) return default;
            }

            return await UniTask.RunOnThreadPool(() => JsonConvert.DeserializeObject<T>(downloadHandler.text));
        }

        private static async UniTask<T> PostAsync<T>(string endpoint, string requestPayload)
        {
            using var downloadHandler = new DownloadHandlerBuffer();
            using var uploadHandler = new UploadHandlerRaw(Encoding.ASCII.GetBytes(requestPayload));
            using var unityWebRequest = new UnityWebRequest($"{SERVER_BASE_URL}{endpoint}", "POST", downloadHandler, uploadHandler);

            uploadHandler.contentType = "application/json";

            _ = unityWebRequest.SendWebRequest();
            await UniTask.WaitUntil(() => unityWebRequest.isDone);

            if (unityWebRequest.result != UnityWebRequest.Result.Success)
            {
                EverestPlugin.LogError($"Server responded with status code {unityWebRequest.responseCode}");
                EverestPlugin.LogError(JsonConvert.DeserializeObject<ErrorResponse>(downloadHandler.text).message);

                if (unityWebRequest.responseCode == 429)
                {
                    var timeToWait = unityWebRequest.GetResponseHeader("Retry-After");
                    ToastController.Instance.Toast($"You are being rate limited. Please wait {timeToWait} seconds before dying again.", Color.red, 3f, 2f);
                    return default;
                }
            }

            return JsonConvert.DeserializeObject<T>(downloadHandler.text);
        }
    }
}
