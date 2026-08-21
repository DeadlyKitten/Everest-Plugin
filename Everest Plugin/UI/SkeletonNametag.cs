using System;
using System.Text;
using Everest.Core;
using TMPro;
using UnityEngine;

namespace Everest.UI
{
    public class SkeletonNametag : MonoBehaviour
    {
        public CanvasGroup CanvasGroup { get; private set; }

        public Skeleton TargetSkeleton;
        public Vector2 CurrentPosition;
        public float CurrentScale;

        private TextMeshProUGUI _text;
        private string _nickname;
        private DateTime _timestamp;
        private float _timeSinceLastUpdate;
        private StringBuilder _stringBuilder = new();

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
            _stringBuilder.Clear();
            _stringBuilder.AppendLine(_nickname);

            if (ConfigHandler.ShowTimeSinceDeath)
                _stringBuilder.Append($"Died {CreateFormattedTime(_timestamp)} ago");

            _text.text = _stringBuilder.ToString();
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

        public void Toggle(bool enable)
        {
            if (enable)
                enabled = true;
            else
            {
                enabled = false;
                CanvasGroup.alpha = 0;
            }
        }
    }
}
