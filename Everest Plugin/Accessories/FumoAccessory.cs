using UnityEngine;

namespace Everest.Accessories
{
    public class FumoAccessory : SkeletonAccessory
    {
#if PLUGIN

        private void Start()
        {
            transform.parent.parent.parent.Find("MainMesh").gameObject.SetActive(false);

            var alignRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
            transform.rotation = alignRotation * transform.rotation;

            if (Physics.Raycast(transform.position + transform.up, Vector3.down, out var hit, 10f, LayerMask.GetMask("Terrain", "Map", "Vines")))
            {
                transform.position = hit.point;
            }
        }

        private void OnDestroy()
        {
            transform.parent.parent.parent.Find("MainMesh").gameObject.SetActive(true);
        }

#endif
    }
}
