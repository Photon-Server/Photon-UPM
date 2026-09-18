using UnityEngine;

namespace Fusion.XR.Shared.Utils
{
    public class TransformFollow : MonoBehaviour
    {
        public Transform target;
        public bool automaticallySetOffset = true;
        public Vector3 positionOffset = Vector3.zero;
        public Vector3 eulerRotationOffset = Vector3.zero;

        private void Awake()
        {
            if (automaticallySetOffset && target != null)
            {
                positionOffset = target.transform.InverseTransformPoint(transform.position);
                eulerRotationOffset = (Quaternion.Inverse(target.transform.rotation) * transform.rotation).eulerAngles;
            }
        }
        private void Update()
        {
            if(target != null)
            {
                transform.rotation = target.transform.rotation * Quaternion.Euler(eulerRotationOffset);
                transform.position = target.transform.TransformPoint(positionOffset);
            }
        }
    }
}
