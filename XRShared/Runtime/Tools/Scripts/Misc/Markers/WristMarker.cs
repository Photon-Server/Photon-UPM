using Fusion.XR.Shared.Core.Interaction;
using UnityEngine;

namespace Fusion.XR.Shared.Core.Tools
{
    public class WristMarker : MonoBehaviour, IDetailedWrist
    {
        [Tooltip("Used when bottom is not defined")]
        public float defaultWristWidth = 0.049191f;

        public Transform top;
        public Transform bottom;

        private void Start()
        {
            foreach(var tracker in GetComponentsInParent<IWristTracker>())
            {
                tracker.RegisterWrist(this);
            }
        }

        private void OnDestroy()
        {
            foreach (var tracker in GetComponentsInParent<IWristTracker>())
            {
                tracker.UnregisterWrist(this);
            }
        }

        #region IWristTop
        public Transform WristTransform => top;
        #endregion

        #region IWristBottom
        public Transform WristBottomTransform => bottom;

        private void Awake()
        {
            if (top == null)
            {
                top = transform;
            }
            if(bottom == null)
            {
                var bottomGameObject = new GameObject("WatchBottom");
                bottomGameObject.transform.parent = top;
                bottomGameObject.transform.localPosition = new Vector3(0, 0, defaultWristWidth / top.lossyScale.z );
                bottomGameObject.transform.localRotation = Quaternion.Euler(180, 0, 0);
                bottom = bottomGameObject.transform;
            }
        }
        #endregion 
    }
}

