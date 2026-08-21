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
        private Transform _playerCameraTransform;
        private Vector3 _positionLastFrame;
        private Quaternion _rotationLastFrame;
        private Matrix4x4 _gpuProjectionMatrix;
        private float _textVerticalOffset = 0.6f;
        private SkeletonNametag _nametagTemplate;
        private ObjectPool<SkeletonNametag> _nametagPool;
        private List<SkeletonNametag> _activeNametags = new();
        private NativeArray<float3> _skeletonPositions;
        private NativeArray<NametagResult> _jobResults;
        private NametagJobNative _job;

        private async UniTaskVoid Awake()
        {
            EverestPlugin.LogInfo("Initializing Skeleton UI Controller...");

            _playerCamera = Camera.main;
            _playerCameraTransform = _playerCamera.transform;
            _gpuProjectionMatrix = GL.GetGPUProjectionMatrix(_playerCamera.projectionMatrix, false);

            await PrepareTemplateNametag();

            _nametagPool = new(
                CreateTextObject,
                obj => obj.Toggle(true),
                obj => obj.Toggle(false),
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

        private void Start()
        {
            Skeleton.OnSkeletonSpawned += OnSkeletonSpawned;
            Skeleton.OnSkeletonDespawned += OnSkeletonDespawned;
        }

        private void OnDestroy()
        {
            Skeleton.OnSkeletonSpawned -= OnSkeletonSpawned;
            Skeleton.OnSkeletonDespawned -= OnSkeletonDespawned;

            _skeletonPositions.Dispose();
            _jobResults.Dispose();
        }

        private void LateUpdate()
        {
            _playerCameraTransform.GetPositionAndRotation(out var camPosition, out var camRotation);

            if (camPosition == _positionLastFrame && camRotation == _rotationLastFrame) return;

            _positionLastFrame = camPosition;
            _rotationLastFrame = camRotation;

            var count = Skeleton.Count;

            if (count == 0) return;

            var viewProjectionMatrix = _gpuProjectionMatrix * _playerCamera.worldToCameraMatrix;

            _job.ViewProjectionMatrix = viewProjectionMatrix;
            _job.CameraPosition = camPosition;
            _job.CameraForward = camRotation * Vector3.forward;

            unsafe
            {
                var jobPointer = UnsafeUtility.AddressOf(ref _job);
                NativeInterop.ExecuteNametagJob(jobPointer, count);

                var resultsPtr = (NametagResult*)NativeArrayUnsafeUtility.GetUnsafePtr(_jobResults);

                for (int i = 0; i < count; i++)
                {
                    var result = resultsPtr[i];

                    if (result.IsVisible == 0) continue;

                    var skeleton = Skeleton.AllActiveSkeletons[i];
                    skeleton.LastRenderedFrame = Time.frameCount;

                    if (skeleton.ActiveNametag == null)
                    {
                        skeleton.ActiveNametag = _nametagPool.Get();
                        skeleton.ActiveNametag.Initialize(skeleton.Nickname, skeleton.Timestamp);

                        skeleton.ActiveNametag.TargetSkeleton = skeleton;
                        _activeNametags.Add(skeleton.ActiveNametag);
                    }

                    skeleton.ActiveNametag.CanvasGroup.alpha = result.Alpha;

                    if (result.Alpha <= 0.01f) continue;

                    var newPosition = new Vector2(result.ScreenX, result.ScreenY);

                    if ((skeleton.ActiveNametag.CurrentPosition - newPosition).sqrMagnitude > 0.01f)
                    {
                        skeleton.ActiveNametag.CurrentPosition = newPosition;
                        skeleton.ActiveNametag.transform.position = newPosition;
                    }

                    if (Mathf.Abs(skeleton.ActiveNametag.CurrentScale - result.Scale) > 0.01f)
                    {
                        skeleton.ActiveNametag.CurrentScale = result.Scale;
                        skeleton.ActiveNametag.transform.localScale = Vector3.one * result.Scale;
                    }
                }
            }

            CleanupPool();
        }

        private void OnSkeletonSpawned(Skeleton newSkeleton)
        {
            _skeletonPositions[newSkeleton.ArrayIndex] = newSkeleton.HeadBonePosition;
        }

        private void OnSkeletonDespawned(Skeleton oldSkeleton)
        {
            var lastIndex = Skeleton.Count;
            _skeletonPositions[oldSkeleton.ArrayIndex] = _skeletonPositions[lastIndex];
        }

        private void CleanupPool()
        {
            for (int i = _activeNametags.Count - 1; i >= 0; i--)
            {
                var nametag = _activeNametags[i];

                if (nametag.TargetSkeleton == null || nametag.TargetSkeleton.LastRenderedFrame != Time.frameCount)
                {
                    if (nametag.TargetSkeleton != null)
                        nametag.TargetSkeleton.ActiveNametag = null;

                    _nametagPool.Release(nametag);

                    var lastIndex = _activeNametags.Count - 1;
                    _activeNametags[i] = _activeNametags[lastIndex];
                    _activeNametags.RemoveAt(lastIndex);
                }
            }
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

            _nametagTemplate.Toggle(false);
        }

        private SkeletonNametag CreateTextObject()
        {
            var nametag = Instantiate(_nametagTemplate, transform);
            nametag.Toggle(true);
            return nametag;
        }
    }
}
