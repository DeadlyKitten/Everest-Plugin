using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Everest.Core;
using Everest.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Everest.UI
{
    internal class SkeletonUIController : MonoBehaviour
    {
        private Camera _playerCamera;

        private float _maxDistance = ConfigHandler.MaxDistanceForVisibleNametag;
        private float _minDistance = ConfigHandler.MinDistanceForVisibleNametag;
        private float _maxViewAngle = ConfigHandler.MaxAngleForVisibleNametag;

        private float _textVerticalOffset = 0.6f;
        private AnimationCurve _textScaleCurve = new AnimationCurve(new Keyframe(0, 0.8f), new Keyframe(0.2f, 1.2f), new Keyframe(1f, 2.0f));
        private float _minTextScale = ConfigHandler.MinNametagSize;
        private float _maxTextScale = ConfigHandler.MaxNametagSize;

        private float _maxDistanceSquared;
        private float _minDistanceSquared;

        private SkeletonNametag _nametagTemplate;

        private Transform _canvasTransform;
        private ObjectPool<SkeletonNametag> _nametagPool;

        private Dictionary<Skeleton, SkeletonNametag> _activeTextElements = new();

        private void Awake()
        {
            _playerCamera = Camera.main;

            _maxDistanceSquared = _maxDistance * _maxDistance;
            _minDistanceSquared = _minDistance * _minDistance;

            PrepareUI().Forget();

            _nametagPool = new(
                CreateTextObject,
                obj => obj.gameObject.SetActive(true),
                obj => obj.gameObject.SetActive(false),
                obj => Destroy(obj),
                false,
                5, 20
            );
        }

        private void LateUpdate()
        {
            var activeSkeletonsThisFrame = new HashSet<Skeleton>();

            foreach (var skeleton in Skeleton.AllActiveSkeletons)
            {
                var center = skeleton.HeadBone.position;

                var directionToSkeleton = center - _playerCamera.transform.position;
                var distanceSquared = directionToSkeleton.sqrMagnitude;

                if (distanceSquared > _maxDistanceSquared) continue;

                var angle = Vector3.Angle(_playerCamera.transform.forward, directionToSkeleton);
                if (angle > _maxViewAngle) continue;

                activeSkeletonsThisFrame.Add(skeleton);

                var distanceScore = 1f - Mathf.InverseLerp(_minDistanceSquared, _maxDistanceSquared, distanceSquared);
                var angleScore = 1f - Mathf.InverseLerp(0, _maxViewAngle, angle);
                var finalScore = distanceScore * angleScore;

                if (!_activeTextElements.TryGetValue(skeleton, out var nametag))
                {
                    nametag = _nametagPool.Get();
                    _activeTextElements[skeleton] = nametag;
                    nametag.Initialize(skeleton.Nickname, skeleton.Timestamp);
                }

                var screenSpacePosition = _playerCamera.WorldToScreenPoint(center + Vector3.up * _textVerticalOffset);

                nametag.transform.position = screenSpacePosition;
                nametag.CanvasGroup.alpha = finalScore;

                var scaleFactor = Mathf.InverseLerp(_minDistanceSquared, _maxDistanceSquared, distanceSquared);
                var targetScale = _textScaleCurve.Evaluate(scaleFactor);
                targetScale = Mathf.Clamp(targetScale, _minTextScale, _maxTextScale);
                nametag.transform.localScale = new Vector3(targetScale, targetScale, 1f);

            }

            CleanupPool(_activeTextElements, _nametagPool, activeSkeletonsThisFrame);
        }

        private void CleanupPool(Dictionary<Skeleton, SkeletonNametag> activeElements, ObjectPool<SkeletonNametag> pool, HashSet<Skeleton> processedSkeletons)
        {
            var toRemove = new List<Skeleton>();

            foreach (var pair in activeElements)
            {
                if (!processedSkeletons.Contains(pair.Key))
                {
                    pool.Release(pair.Value);
                    toRemove.Add(pair.Key);
                }
            }

            foreach (var skeleton in toRemove)
            {
                activeElements.Remove(skeleton);
            }
        }

        private async UniTaskVoid PrepareUI()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.AddComponent<CanvasScaler>();

            var templateObject = new GameObject("Template");
            templateObject.transform.SetParent(transform, false);

            var textComponent = templateObject.AddComponent<TextMeshProUGUI>();
            textComponent.fontSize = 24;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = ConfigHandler.NametagColor;
            textComponent.textWrappingMode = TextWrappingModes.NoWrap;
            textComponent.font = await FontUtility.GetFont();
            textComponent.outlineColor = ConfigHandler.NametagOutlineColor;
            textComponent.outlineWidth = ConfigHandler.NametagOutlineWidth;
            templateObject.AddComponent<CanvasGroup>();

            var textRectTransform = templateObject.GetComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.zero;
            textRectTransform.pivot = new Vector2(0.5f, 0f);
            textRectTransform.sizeDelta = new Vector2(200, 100);

            _nametagTemplate = templateObject.AddComponent<SkeletonNametag>();

            templateObject.SetActive(false);
        }

        private SkeletonNametag CreateTextObject() => Instantiate(_nametagTemplate, transform);
    }
}
