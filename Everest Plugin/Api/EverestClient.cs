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
        private const string SERVER_BASE_URL = "https://peak-everest.com";

        public static async UniTaskVoid SubmitDeath(SubmissionRequest request)
        {
            var response = await SubmitAsync(request, request.map_id);

            if (response.message != null)
            {
                UIHandler.Instance.Toast(response.message, Color.green, 2f, 2f);
                EverestPlugin.LogDebug(response.message);
            }
        }

        public static async UniTask<SubmissionResponse> SubmitAsync(SubmissionRequest request, int mapId)
        {
            var endpoint = $"/submit_data?map_id={mapId}";

            var payload = JsonConvert.SerializeObject(request, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

            return await UnityPostRequest<SubmissionResponse>(payload, endpoint);
        }

        public static async UniTask<ServerResponse> RetrieveAsync(int mapId)
        {
            var endpoint = $"/get_data?map_id={mapId}&limit={ConfigHandler.MaxSkeletons}";
            return await UnityGetRequest<ServerResponse>(endpoint);
        }

        public static async UniTask<ServerResponse> RetrieveAsync(string identifier)
        {
            var endpoint = $"/get_data_by_identifier/{identifier}";
            return await UnityGetRequest<ServerResponse>(endpoint);
        }

        public static async UniTask<DailyCountResponse> RetrieveCountForDay()
        {
            var endpoint = "/get_current_day_count";
            return await UnityGetRequest<DailyCountResponse>(endpoint);
        }

        private static async UniTask<T> UnityGetRequest<T>(string endpoint)
        {
            using var downloadHandler = new DownloadHandlerBuffer();
            using var unityWebRequest = new UnityWebRequest($"{SERVER_BASE_URL}{endpoint}", "GET", downloadHandler, null);

            _ = unityWebRequest.SendWebRequest();
            await UniTask.WaitUntil(() => unityWebRequest.isDone);

            if (unityWebRequest.result != UnityWebRequest.Result.Success)
            {
                EverestPlugin.LogError(unityWebRequest.error);
                return default;
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

                return default;
            }

            return JsonConvert.DeserializeObject<T>(downloadHandler.text);
        }
    }
}
