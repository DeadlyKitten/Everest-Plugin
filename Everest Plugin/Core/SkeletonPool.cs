using UnityEngine;
using UnityEngine.Pool;

namespace Everest.Core
{
    internal class SkeletonPool : IObjectPool<Skeleton>
    {
        public int CountInactive => _pool.CountInactive;

        private IObjectPool<Skeleton> _pool;
        private Skeleton _skeletonPrefab;
        private Transform _parentTransform;

        public SkeletonPool(Skeleton skeletonPrefab, Transform parentTransform)
        {
            _skeletonPrefab = skeletonPrefab;
            _parentTransform = parentTransform;

            _pool = new ObjectPool<Skeleton>(
                createFunc: () => GameObject.Instantiate(_skeletonPrefab, _parentTransform),
                actionOnGet: (skeleton) => skeleton.gameObject.SetActive(true),
                actionOnRelease: (skeleton) => skeleton.gameObject.SetActive(false),
                actionOnDestroy: (skeleton) => GameObject.Destroy(skeleton.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: ConfigHandler.MaxVisibleSkeletons
            );
        }

        public Skeleton Get() => _pool.Get();
        public PooledObject<Skeleton> Get(out Skeleton v) => _pool.Get(out v);
        public void Release(Skeleton skeleton) => _pool.Release(skeleton);
        public void Clear() => _pool.Clear();
    }
}
