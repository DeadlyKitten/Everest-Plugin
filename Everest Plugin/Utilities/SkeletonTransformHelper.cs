using UnityEngine;

namespace Everest.Utilities
{
    public class SkeletonTransformHelper
    {
        private static readonly Vector3 _scoutLocalPosition = Vector3.zero;
        private static readonly Quaternion _scoutLocalRotation = Quaternion.Euler(0, -1.707547e-06f, 0);
        private static readonly Vector3 _scoutLocalScale = Vector3.one * 0.3307868f;
        private static readonly Vector3 _armatureLocalPosition = Vector3.zero;
        private static readonly Quaternion _armatureLocalRotation = Quaternion.Euler(-90f, 0, 0);

        private static Matrix4x4 scoutMatrix = Matrix4x4.TRS(_scoutLocalPosition, _scoutLocalRotation, _scoutLocalScale);
        private static Matrix4x4 armatureMatrix = Matrix4x4.TRS(_armatureLocalPosition, _armatureLocalRotation, Vector3.one);
        private static Matrix4x4 childMatrix = scoutMatrix * armatureMatrix;

        public static Vector3 GetHipWorldPosition(Vector3 parentWorldPosition, Vector3 parentWorldRotation, Vector3 hipLocalPosition)
        {
            var parentMatrix = Matrix4x4.TRS(parentWorldPosition, Quaternion.Euler(parentWorldRotation), Vector3.one);
            var armatureWorldMatrix = parentMatrix * childMatrix;
            var hipWorldPosition = armatureWorldMatrix.MultiplyPoint3x4(hipLocalPosition);
            return hipWorldPosition;
        }
    }
}
