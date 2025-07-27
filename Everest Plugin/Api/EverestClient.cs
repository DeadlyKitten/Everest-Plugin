using System.Text;
using Cysharp.Threading.Tasks;
using Everest.Core;
using MessagePack;
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
            var response = await SubmitAsync(request, request.map_id);

            if (response != null && response.message != null)
            {
                UIHandler.Instance.Toast(response.message, Color.grey, 2f, 2f);
                EverestPlugin.LogDebug(response.message);
            }
        }

        public static async UniTask<SubmissionResponse> SubmitAsync(SubmissionRequest request, int mapId)
        {
            var endpoint = $"/Skeletons/submit?map_id={mapId}";

            var payload = MessagePackSerializer.Serialize(request);

            return await UnityPostRequest<SubmissionResponse>(payload, endpoint);
        }

        public static async UniTask<ServerResponse> RetrieveAsync(int mapId)
        {
            var endpoint = $"/Skeletons/recent?map_id={mapId}&limit={ConfigHandler.MaxSkeletons}";
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
            unityWebRequest.SetRequestHeader("Accept", "application/x-msgpack");

            _ = unityWebRequest.SendWebRequest();
            await UniTask.WaitUntil(() => unityWebRequest.isDone);

            if (unityWebRequest.result != UnityWebRequest.Result.Success)
            {
                EverestPlugin.LogError(unityWebRequest.error);
                if (bailOnFail) return default;
                return JsonConvert.DeserializeObject<T>(downloadHandler.text);
            }

            return await UniTask.RunOnThreadPool(() => MessagePackSerializer.Deserialize<T>(downloadHandler.data));
        }

        private static async UniTask<T> UnityPostRequest<T>(byte[] requestPayload, string endpoint)
        {
            using var downloadHandler = new DownloadHandlerBuffer();
            using var uploadHandler = new UploadHandlerRaw(requestPayload);
            using var unityWebRequest = new UnityWebRequest($"{SERVER_BASE_URL}{endpoint}", "POST", downloadHandler, uploadHandler);

            uploadHandler.contentType = "application/x-msgpack";

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
                    return default;
                }
            }

            return JsonConvert.DeserializeObject<T>(downloadHandler.text);
        }
    }
}
