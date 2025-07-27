using UnityEngine;
#if PLUGIN
using Zorro.Core;
#endif

namespace Everest.Accessories
{
    internal class JuiceAccessory : SkeletonAccessory
    {
        [SerializeField]
        private GameObject _joelsephJuice;

        [SerializeField]
        private string _leftHandParent;
        [SerializeField]
        private Vector3 _leftHandPosition;
        [SerializeField]
        private Vector3 _leftHandRotation;

        [SerializeField]
        private string _rightHandParent;
        [SerializeField]
        private Vector3 _rightHandPosition;
        [SerializeField]
        private Vector3 _rightHandRotation;

        [SerializeField]
        private string _torsoParent;
        [SerializeField]
        private Vector3 _torsoPosition;
        [SerializeField]
        private Vector3 _torsoRotation;

#if PLUGIN
        private GameObject[] _instances = new GameObject[3];

        private void Start()
        {
            if (!_joelsephJuice) return;

            var leftHand = Instantiate(_joelsephJuice);
            leftHand.name = "LeftHandJuice";
            leftHand.transform.SetParent(transform.parent.FindChildRecursive(_leftHandParent));
            leftHand.transform.SetLocalPositionAndRotation(_leftHandPosition, Quaternion.Euler(_leftHandRotation));
            leftHand.transform.localScale = Vector3.one;
            _instances[0] = leftHand;

            var rightHand = Instantiate(_joelsephJuice);
            rightHand.name = "RightHandJuice";
            rightHand.transform.SetParent(transform.parent.FindChildRecursive(_rightHandParent));
            rightHand.transform.SetLocalPositionAndRotation(_rightHandPosition, Quaternion.Euler(_rightHandRotation));
            rightHand.transform.localScale = Vector3.one;
            _instances[1] = rightHand;

            var torso = Instantiate(_joelsephJuice);
            torso.name = "TorsoJuice";
            torso.transform.SetParent(transform.parent.FindChildRecursive(_torsoParent));
            torso.transform.SetLocalPositionAndRotation(_torsoPosition, Quaternion.Euler(_torsoRotation));
            torso.transform.localScale = Vector3.one;
            _instances[2] = torso;
        }

        private void OnDestroy()
        {
            foreach (var instance in _instances)
                if (instance) Destroy(instance);
        }
#endif
    }
}
