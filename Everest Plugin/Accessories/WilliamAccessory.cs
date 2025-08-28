using UnityEngine;

namespace Everest.Accessories
{
    public class WilliamAccessory : SkeletonAccessory
    {
        [SerializeField]
        private Material _material;

#if PLUGIN

        private Material _originalMaterial;

        private void Start()
        {
            var renderer = GetComponentInParent<SkinnedMeshRenderer>();
            if (renderer)
            {
                _originalMaterial = new Material(renderer.sharedMaterial);
                renderer.material = _material;
            }
        }

        private void OnDestroy()
        {
            var renderer = GetComponentInParent<SkinnedMeshRenderer>();
            if (renderer && _originalMaterial)
            {
                renderer.material = _originalMaterial;
            }
        }
#endif
    }
}
