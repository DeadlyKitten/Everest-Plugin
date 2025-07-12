using TMPro;
using UnityEngine;
using System;
using System.Globalization;

#if PLUGIN
using Cysharp.Threading.Tasks;
using Everest.Api;
using Everest.Utilities;
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

            _deathCountText.text = $"{response.count:n0}";

            var dateTime = DateTimeOffset.Parse(response.start_time_utc, CultureInfo.InvariantCulture);
            _dateText.text = dateTime.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture);
        }
#endif
    }
}
