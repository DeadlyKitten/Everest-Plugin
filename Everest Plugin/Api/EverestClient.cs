using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Everest.Api
{
    public static class EverestClient
    {
        private const string SERVER_BASE_URL = "https://peak-everest.com";

        public static async UniTaskVoid SubmitDeath(SubmissionRequest request)
        {
            var response = await SubmitAsync(request, request.map_id);

            EverestPlugin.LogInfo(response.message);
        }

        public static async UniTask<SubmissionResponse> SubmitAsync(SubmissionRequest request, int mapId)
        {
            var endpoint = $"/submit_data?map_id={mapId}";

            var payload = JsonConvert.SerializeObject(request, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

            return await UnityPostRequest<SubmissionResponse>(payload, endpoint);
        }

        public static async UniTask<ServerResponse> RetrieveAsync(int mapId)
        {
            var endpoint = $"/get_data?map_id={mapId}";
            return await UnityGetRequest<ServerResponse>(endpoint);
        }

        public static async UniTask<ServerResponse> RetrieveAsync(string identifier)
        {
            var endpoint = $"/get_data_by_identifier/{identifier}";
            return await UnityGetRequest<ServerResponse>(endpoint);
        }

        private static async UniTask<T> UnityGetRequest<T>(string endpoint)
        {
            using var downloadHandler = new DownloadHandlerBuffer();
            using var unityWebRequest = new UnityWebRequest($"{SERVER_BASE_URL}{endpoint}", "GET", downloadHandler, null);
            await unityWebRequest.SendWebRequest();

            if (unityWebRequest.result != UnityWebRequest.Result.Success)
            {
                EverestPlugin.LogError(unityWebRequest.error);
                return default;
            }

            return JsonConvert.DeserializeObject<T>(downloadHandler.text);
        }

        private static async UniTask<T> UnityPostRequest<T>(string requestPayload, string endpoint)
        {
            using var downloadHandler = new DownloadHandlerBuffer();
            using var uploadHandler = new UploadHandlerRaw(Encoding.ASCII.GetBytes(requestPayload));
            using var unityWebRequest = new UnityWebRequest($"{SERVER_BASE_URL}{endpoint}", "POST", downloadHandler, uploadHandler);

            uploadHandler.contentType = "application/json";
            await unityWebRequest.SendWebRequest();

            if (unityWebRequest.result != UnityWebRequest.Result.Success)
            {
                EverestPlugin.LogError(unityWebRequest.error);
                return default;
            }
            return JsonConvert.DeserializeObject<T>(downloadHandler.text);
        }
    }
}
