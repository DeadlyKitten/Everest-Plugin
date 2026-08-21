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
                createFunc: CreateSkeleton,
                actionOnGet: (skeleton) => { },
                actionOnRelease: (skeleton) => skeleton.transform.position = Vector3.down * 1000,
                actionOnDestroy: (skeleton) => GameObject.Destroy(skeleton.gameObject),
                collectionCheck: false,
                defaultCapacity: 10,
                maxSize: ConfigHandler.MaxVisibleSkeletons
            );
        }

        public void Prewarm(int count)
        {
            var skeletons = new Skeleton[count];

            for (int i = 0; i < count; i++)
            {
                skeletons[i] = _pool.Get();
            }

            for (int i = 0; i < count; i++)
            {
                _pool.Release(skeletons[i]);
            }
        }

        private Skeleton CreateSkeleton()
        {
            var skeleton = GameObject.Instantiate(_skeletonPrefab, _parentTransform);
            skeleton.gameObject.SetActive(true);
            return skeleton;
        }

        public Skeleton Get() => _pool.Get();
        public PooledObject<Skeleton> Get(out Skeleton v) => _pool.Get(out v);
        public void Release(Skeleton skeleton) => _pool.Release(skeleton);
        public void Clear() => _pool.Clear();
    }
}
