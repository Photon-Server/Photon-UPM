using UnityEngine;

namespace Fusion.Addons.AnchorsAddon.Colocalization.Minimap
{
    public class CompensateParentScale : MonoBehaviour
    {
        Vector3 _initialScale = Vector3.one;
        Vector3 _previousParentLocalScale;
        [SerializeField] bool compensateLocalScale = true;
        [SerializeField] Transform parentToCompensate = null;

        private void Awake()
        {
            _initialScale = transform.localScale;
            if (parentToCompensate == null)
            {
                parentToCompensate = transform.parent;
            }
        }

        void Update()
        {
            AdaptScale();
        }

        void AdaptScale()
        {
            if (parentToCompensate == null || parentToCompensate.localScale == _previousParentLocalScale)
                return;
            _previousParentLocalScale = parentToCompensate.localScale;

            Vector3 parentScale;
            if (compensateLocalScale)
            {
                parentScale = _previousParentLocalScale;
            }
            else
            {
                parentScale = parentToCompensate.lossyScale;
            }

            transform.localScale = new Vector3(_initialScale.x / parentScale.x, _initialScale.y / parentScale.y, _initialScale.z / parentScale.z);
        }
    }
}


