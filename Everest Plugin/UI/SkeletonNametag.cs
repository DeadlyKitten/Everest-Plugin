using System;
using System.Text;
using Everest.Core;
using TMPro;
using UnityEngine;

namespace Everest.UI
{
    internal class SkeletonNametag : MonoBehaviour
    {
        public CanvasGroup CanvasGroup { get; private set; }

        private TextMeshProUGUI _text;
        private string _nickname;
        private DateTime _timestamp;
        private float _timeSinceLastUpdate;

        public void Initialize(string nickname, DateTime timestamp)
        {
            _nickname = nickname;
            _timestamp = timestamp;
            gameObject.name = _nickname;
            UpdateText();
            _timeSinceLastUpdate = 0f;
        }

        private void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (!ConfigHandler.ShowTimeSinceDeath) return;

            if (_timeSinceLastUpdate >= 1f)
            {
                UpdateText();
                _timeSinceLastUpdate = 0f;
            }
            else _timeSinceLastUpdate += Time.deltaTime;
        }

        private void UpdateText()
        {
            var builder = new StringBuilder();
            builder.AppendLine(_nickname);

            if (ConfigHandler.ShowTimeSinceDeath)
                builder.Append($"Died {CreateFormattedTime(_timestamp)} ago");

            _text.text = builder.ToString();
        }

        private string CreateFormattedTime(DateTime timestamp)
        {
            TimeSpan elapsed = DateTime.UtcNow - timestamp;

            if (elapsed.TotalHours >= 1)
                return $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m {(ConfigHandler.ShowSecondsAlways ? $"{elapsed.Seconds}s" : string.Empty)}";
            else if (elapsed.TotalMinutes >= 1)
                return $"{elapsed.Minutes}m {elapsed.Seconds}s";
            else
                return $"{elapsed.Seconds} seconds";
        }
    }
}
