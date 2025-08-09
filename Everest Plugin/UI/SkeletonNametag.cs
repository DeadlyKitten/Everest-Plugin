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

        public void Initialize(string nickname, DateTime timestamp)
        {
            _nickname = nickname;
            _timestamp = timestamp;
            gameObject.name = _nickname;
            UpdateText();
        }

        private void Awake()
        {
            CanvasGroup = GetComponent<CanvasGroup>();
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void Update()
        {
            if (ConfigHandler.ShowTimeSinceDeath)
                UpdateText();
        }

        private void UpdateText()
        {
            var builder = new StringBuilder();
            builder.AppendLine(_nickname);

            if (ConfigHandler.ShowTimeSinceDeath)
                builder.Append($"Died {CreateFormatedTime(_timestamp)} ago");

            _text.text = builder.ToString();
        }

        private string CreateFormatedTime(DateTime timestamp)
        {
            TimeSpan elapsed = DateTime.UtcNow - timestamp;

            if (elapsed.TotalHours >= 1)
                return $"{(int)elapsed.TotalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
            else if (elapsed.TotalMinutes >= 1)
                return $"{elapsed.Minutes}:{elapsed.Seconds:00}";
            else
                return $"{elapsed.Seconds} seconds";

        }
    }
}
