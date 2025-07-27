using TMPro;
using UnityEngine;
using System;

#if PLUGIN
using Cysharp.Threading.Tasks;
using Everest.Api;
using Everest.Utilities;
using System.Globalization;
#endif

namespace Everest.Core
{
    public class Tombstone : MonoBehaviour
    {
        [SerializeField]
        private TextMeshPro _deathCountText;
        [SerializeField]
        private TextMeshPro _braveClimbersText;
        [SerializeField]
        private TextMeshPro _dateText;

#if PLUGIN
        private void Awake()
        {
            SetupFonts().Forget();
            Initialize().Forget();
        }

        private async UniTaskVoid SetupFonts()
        {
            var font = await FontUtility.GetFont();
            _deathCountText.font = font;
            _braveClimbersText.font = font;
            _dateText.font = font;
        }

        private async UniTaskVoid Initialize()
        {
            var response = await EverestClient.RetrieveCountForDay();

            if (response == null)
            {
                EverestPlugin.LogError("Failed to retrieve daily count for tombstone.");
                Destroy(gameObject);
                return;
            }

            _deathCountText.text = $"{response.Count:n0}";

            var dateTime = DateTimeOffset.Parse(response.StartTimeUTC, CultureInfo.InvariantCulture);
            _dateText.text = dateTime.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);
        }
#endif
    }
}
