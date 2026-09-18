using Fusion.XRShared.Tools;
using UnityEngine;
using UnityEngine.XR.ARFoundation;



namespace Fusion.Addons.AnchorsAddon.ARFoundation
{
    /// <summary>
    /// Prevent ARPlaneManager/ARFaceManager from launching without permission request grant.
    /// 
    /// Should be used alongside a PermissionsRequester, to align permissions on the desired timing (to avoid conflicting requests)
    /// </summary>
    [DefaultExecutionOrder(PermissionWaiter.EXECUTION_ORDER)]
    public class ARFPermissionWaiter : PermissionWaiter
    {
        ARPlaneManager arPlaneManager;
        ARFaceManager arFaceManager;

        #region PermissionWaiter override 
        protected override void Awake()
        {
            if (arPlaneManager == null)
                arPlaneManager = GetComponent<ARPlaneManager>();
            if (arFaceManager == null)
                arFaceManager = GetComponent<ARFaceManager>();


            base.Awake();
        }

        protected override void OnPermissionRequired()
        {
            base.OnPermissionRequired();
            if (arPlaneManager)
                arPlaneManager.enabled = false;
            if (arFaceManager)
                arFaceManager.enabled = false;
        }

        public override string PermissionName => GetSceneUnderstandingPermission();

        /// <summary>
        /// Should be called manually (for instance in a PermissionsRequester callback) if checkPermisisonGrantAutomatically is not checked
        /// </summary>
        public override void OnPermissionGranted()
        {
            base.OnPermissionGranted();
            if (arPlaneManager)
                arPlaneManager.enabled = true;
            if (arFaceManager)
                arFaceManager.enabled = true;
        }

        private string GetSceneUnderstandingPermission()
        {
            string deviceModel = SystemInfo.deviceModel.ToLower();

            if (deviceModel.Contains("oculus") || deviceModel.Contains("meta"))
                return "com.oculus.permission.USE_SCENE";

            return "android.permission.SCENE_UNDERSTANDING_COARSE";
        }
        #endregion
    }
}


