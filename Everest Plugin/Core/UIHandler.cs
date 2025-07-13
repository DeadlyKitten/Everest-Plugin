using System.Threading;
using Cysharp.Threading.Tasks;
using Everest.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Everest
{
    public class UIHandler : MonoBehaviour
    {
        public static UIHandler Instance;

        TextMeshProUGUI textMesh;

        CancellationTokenSource cancellationTokenSource;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            EverestPlugin.LogDebug("Initializing UI...");

            var canvas = new GameObject("canvas").AddComponent<Canvas>();
            canvas.transform.SetParent(transform);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue; // Ensure it's on top of other UI elements
            canvas.gameObject.AddComponent<CanvasScaler>();

            var textGO = new GameObject("text");
            textGO.transform.SetParent(canvas.transform, false);

            var rectTransform = textGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(1000, 100);
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(5, -20);

            textMesh = textGO.AddComponent<TextMeshProUGUI>();
            textMesh.fontSize = 26;
            textMesh.fontSizeMin = 26;
            textMesh.fontSizeMax = 26;

            EverestPlugin.LogDebug("UI Initialized!");

            FindFontEventually().Forget();
        }

        public void Toast(string text, Color color, float duration = 2f, float fadeTime = 1f)
        {
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();

            if (textMesh == null) return;

            textMesh.text = text;
            textMesh.color = color;
            textMesh.alpha = 1f;

            cancellationTokenSource = new CancellationTokenSource();
            ClearToast(duration, fadeTime, cancellationTokenSource.Token).Forget();
        }

        private async UniTask ClearToast(float duration, float fadeTime = 1f, CancellationToken cancellationToken = default)
        {
            if (textMesh == null) return;

            await UniTask.Delay((int)(duration * 1000), cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested) return;

            var timeElapsed = 0f;

            while (timeElapsed < fadeTime)
            {
                timeElapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(timeElapsed / fadeTime);
                textMesh.alpha = Mathf.Lerp(1f, 0f, progress);
                await UniTask.NextFrame(cancellationToken: cancellationToken);
            }
        }

        private async UniTaskVoid FindFontEventually() => textMesh.font = await FontUtility.GetFont();
    }
}
