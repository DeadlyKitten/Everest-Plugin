using System.Text;
using Cysharp.Threading.Tasks;
using Everest.Core;
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

        public static async UniTaskVoid SubmitDeath(SubmissionRequest request)
        {
            var response = await SubmitAsync(request, request.MapId);

            if (response != null && response.message != null)
            {
                UIHandler.Instance.Toast(response.message, Color.grey, 2f, 2f);
                EverestPlugin.LogDebug(response.message);
            }
        }

        public static async UniTask<SubmissionResponse> SubmitAsync(SubmissionRequest request, int mapId)
        {
            var endpoint = $"/Skeletons/submit";

            var payload = JsonConvert.SerializeObject(request, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

            return await UnityPostRequest<SubmissionResponse>(payload, endpoint);
        }

        public static async UniTask<ServerResponse> RetrieveAsync(int mapId)
        {
            var maxSkeletons = ConfigHandler.MaxSkeletons;
            var excludeCrashSite = ConfigHandler.ExcludeNearCrashSite;
            var excludeCampfires = ConfigHandler.ExcludeNearCampfires;

            var endpoint = $"/Skeletons/recent?map_id={mapId}&limit={maxSkeletons}&excludeCrashSite={excludeCrashSite}&excludeCampfire={excludeCampfires}";
            return await UnityGetRequest<ServerResponse>(endpoint);
        }

        public static async UniTask<ServerResponse> RetrieveAsync(string identifier)
        {
            var endpoint = $"/Skeletons/recent/{identifier}";
            return await UnityGetRequest<ServerResponse>(endpoint);
        }

        public static async UniTask<DailyCountResponse> RetrieveCountForDay()
        {
            var endpoint = "/Stats/daily/submissions";
            return await UnityGetRequest<DailyCountResponse>(endpoint);
        }

        public static async UniTask<ServerStatusResponse> RetrieveServerStatus(string version)
        {
            var endpoint = $"/Status?clientVersion={version}";
            return await UnityGetRequest<ServerStatusResponse>(endpoint, false);
        }

        private static async UniTask<T> UnityGetRequest<T>(string endpoint, bool bailOnFail = true)
        {
            using var downloadHandler = new DownloadHandlerBuffer();
            using var unityWebRequest = new UnityWebRequest($"{SERVER_BASE_URL}{endpoint}", "GET", downloadHandler, null);

            _ = unityWebRequest.SendWebRequest();
            await UniTask.WaitUntil(() => unityWebRequest.isDone);

            if (unityWebRequest.result != UnityWebRequest.Result.Success)
            {
                EverestPlugin.LogError(unityWebRequest.error);
                if (bailOnFail) return default;
            }

            return await UniTask.RunOnThreadPool(() => JsonConvert.DeserializeObject<T>(downloadHandler.text));
        }

        private static async UniTask<T> UnityPostRequest<T>(string requestPayload, string endpoint)
        {
            using var downloadHandler = new DownloadHandlerBuffer();
            using var uploadHandler = new UploadHandlerRaw(Encoding.ASCII.GetBytes(requestPayload));
            using var unityWebRequest = new UnityWebRequest($"{SERVER_BASE_URL}{endpoint}", "POST", downloadHandler, uploadHandler);

            uploadHandler.contentType = "application/json";

            _ = unityWebRequest.SendWebRequest();
            await UniTask.WaitUntil(() => unityWebRequest.isDone);

            if (unityWebRequest.responseCode != 200)
            {
                EverestPlugin.LogError($"Server responded with status code {unityWebRequest.responseCode}");
                EverestPlugin.LogError(JsonConvert.DeserializeObject<ErrorResponse>(downloadHandler.text).error);

                if (unityWebRequest.responseCode == 429)
                {
                    var timeToWait = unityWebRequest.GetResponseHeader("Retry-After");
                    UIHandler.Instance.Toast($"You are being rate limited. Please wait {timeToWait} seconds before dying again.", Color.red, 3f, 2f);
                }
            }

            return JsonConvert.DeserializeObject<T>(downloadHandler.text);
        }
    }
}
