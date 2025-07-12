using UnityEngine;

namespace Everest.Accessories
{
    public class WilliamAccessory : MonoBehaviour
    {
        [SerializeField]
        private Material _material;

#if PLUGIN
        private void Start()
        {
            var renderer = GetComponentInParent<SkinnedMeshRenderer>();
            if (renderer) renderer.material = _material;
        }
#endif
    }
}
