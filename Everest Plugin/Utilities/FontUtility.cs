using Cysharp.Threading.Tasks;
using TMPro;

namespace Everest.Utilities
{
    public class FontUtility
    {
        private static TMP_FontAsset _font;

        public static async UniTask<TMP_FontAsset> GetFont()
        {
            if (_font) return _font;

            while (_font == null)
            {
                if (GUIManager.instance != null && GUIManager.instance.heroDayText != null)
                    _font = GUIManager.instance.heroDayText.font;
                else
                    await UniTask.Delay(100);
            }

            return _font;
        }
    }
}
