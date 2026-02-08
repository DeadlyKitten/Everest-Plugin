using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Everest.Core;
using Everest.Jobs;
using Everest.Utilities;
using TMPro;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Everest.UI
{
    internal class SkeletonUIController : MonoBehaviour
    {
        private Camera _playerCamera;
        private Matrix4x4 _gpuProjectionMatrix;

        private float _textVerticalOffset = 0.6f;

        private SkeletonNametag _nametagTemplate;

        private ObjectPool<SkeletonNametag> _nametagPool;

        private Dictionary<Skeleton, SkeletonNametag> _activeTextElements = new();
        HashSet<Skeleton> _activeSkeletonsThisFrame = new();
        HashSet<Skeleton> _activeSkeletonsLastFrame = new();

        private NativeArray<float3> _skeletonPositions;
        private NativeArray<NametagResult> _jobResults;

        private NametagJobNative _job;

        private async UniTaskVoid Awake()
        {
            EverestPlugin.LogInfo("Initializing Skeleton UI Controller...");

            _playerCamera = Camera.main;
            _gpuProjectionMatrix = GL.GetGPUProjectionMatrix(_playerCamera.projectionMatrix, false);

            await PrepareTemplateNametag();

            _nametagPool = new(
                CreateTextObject,
                obj => obj.gameObject.SetActive(true),
                obj => obj.gameObject.SetActive(false),
                obj => Destroy(obj),
                false,
                5, 20
            );

            _skeletonPositions = new NativeArray<float3>(ConfigHandler.MaxVisibleSkeletons, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            _jobResults = new NativeArray<NametagResult>(ConfigHandler.MaxVisibleSkeletons, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            unsafe
            {
                _job = new NametagJobNative()
                {
                    SkeletonPositions = (float3*)_skeletonPositions.GetUnsafeReadOnlyPtr(),
                    Results = (NametagResult*)_jobResults.GetUnsafePtr(),
                    ScreenWidth = Screen.width,
                    ScreenHeight = Screen.height,
                    MaxDistanceSquared = Mathf.Pow(ConfigHandler.MaxDistanceForVisibleNametag, 2),
                    MinDistanceSquared = Mathf.Pow(ConfigHandler.MinDistanceForVisibleNametag, 2),
                    MaxViewAngleCosine = Mathf.Cos(ConfigHandler.MaxAngleForVisibleNametag * Mathf.Deg2Rad),
                    TextVerticalOffset = _textVerticalOffset,
                    MinTextScale = ConfigHandler.MinNametagSize,
                    MaxTextScale = ConfigHandler.MaxNametagSize
                };
            }

            EverestPlugin.LogInfo("Skeleton UI Controller Initialized!");
        }

        private void OnDestroy()
        {
            _skeletonPositions.Dispose();
            _jobResults.Dispose();
        }

        private void LateUpdate()
        {
            HandleNametagsAsync().Forget();
        }

        private async UniTaskVoid HandleNametagsAsync()
        {
            _playerCamera.transform.GetPositionAndRotation(out var camPosition, out var camRotation);

            var skeletons = Skeleton.AllActiveSkeletons;
            var count = skeletons.Count;

            var viewProjectionMatrix = _gpuProjectionMatrix * _playerCamera.worldToCameraMatrix;

            for (int i = 0; i < count; i++)
            {
                var position = skeletons[i].HeadBone.position;
                _skeletonPositions[i] = position;
            }

            _job.ViewProjectionMatrix = viewProjectionMatrix;
            _job.CameraPosition = camPosition;
            _job.CameraForward = camRotation * Vector3.forward;

            var jobHandle = _job.ScheduleNative(count, 128);

            await UniTask.Yield(PlayerLoopTiming.PreLateUpdate);
            if (this.destroyCancellationToken.IsCancellationRequested) return;
            jobHandle.Complete();

            _activeSkeletonsThisFrame.Clear();

            for (int i = 0; i < count; i++)
            {
                var result = _jobResults[i];

                if (result.IsVisible == 0) continue;

                var skeleton = skeletons[i];
                _activeSkeletonsThisFrame.Add(skeleton);

                if (!_activeTextElements.TryGetValue(skeleton, out var nametag))
                {
                    nametag = _nametagPool.Get();
                    _activeTextElements[skeleton] = nametag;
                    nametag.Initialize(skeleton.Nickname, skeleton.Timestamp);
                }

                nametag.CanvasGroup.alpha = result.Alpha;
                nametag.transform.position = new Vector3(result.ScreenX, result.ScreenY, 0);
                nametag.transform.localScale = new Vector3(result.Scale, result.Scale, 1f);
            }

            CleanupPool(_activeTextElements, _nametagPool);
        }

        private void CleanupPool(Dictionary<Skeleton, SkeletonNametag> activeElements, ObjectPool<SkeletonNametag> pool)
        {
            if (_activeSkeletonsThisFrame.SetEquals(_activeSkeletonsLastFrame))
                return;

            var toRemove = new List<Skeleton>();

            foreach (var pair in activeElements)
            {
                if (!_activeSkeletonsThisFrame.Contains(pair.Key))
                {
                    pool.Release(pair.Value);
                    toRemove.Add(pair.Key);
                }
            }

            foreach (var skeleton in toRemove)
            {
                activeElements.Remove(skeleton);
            }

            _activeSkeletonsLastFrame.Clear();
            _activeSkeletonsLastFrame.UnionWith(_activeSkeletonsThisFrame);
        }

        private async UniTask PrepareTemplateNametag()
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
