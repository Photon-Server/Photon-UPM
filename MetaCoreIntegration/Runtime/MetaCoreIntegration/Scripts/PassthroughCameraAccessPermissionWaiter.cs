using Fusion.XRShared.Tools;
using System.Reflection;


#if MRUK_AVAILABLE
using Meta.XR.MRUtilityKit;
using Meta.XR;
#endif
using UnityEngine;

namespace Fusion.Addons.Meta
{
#if MRUK_AVAILABLE
    /// <summary>
    /// Launch appropriate request for PassthroughCameraAccess 
    /// 
    /// Should be used alongside a PermissionsRequester, to align permissions on the desired timing (to avoid conflicting requests)
    /// </summary>
    [RequireComponent(typeof(PassthroughCameraAccess))]
#endif
    [DefaultExecutionOrder(PermissionWaiter.EXECUTION_ORDER)]
    public class PassthroughCameraAccessPermissionWaiter : PermissionWaiter
    {
#if MRUK_AVAILABLE
        PassthroughCameraAccess passthroughCameraAccess;
        public bool disablePassthroughCameraAccessWhileWaitingForPermission = false;

        #region PermissionWaiter override 
        protected override void Awake()
        {
            passthroughCameraAccess = GetComponent<PassthroughCameraAccess>();
            base.Awake();
        }
        protected override void OnPermissionRequired()
        {
            base.OnPermissionRequired();
            if(disablePassthroughCameraAccessWhileWaitingForPermission) passthroughCameraAccess.enabled = false;
            PreventOVRManagerStartupPermissions();
        }

        public override string PermissionName => OVRPermissionsRequester.PassthroughCameraAccessPermission;

        /// <summary>
        /// Should be called manually (for instance in a PermissionsRequester callback) if checkPermisisonGrantAutomatically is not checked
        /// </summary>
        public override void OnPermissionGranted()
        {
            base.OnPermissionGranted();
            if (disablePassthroughCameraAccessWhileWaitingForPermission) passthroughCameraAccess.enabled = true;
        }
        #endregion

        void PreventOVRManagerStartupPermissions()
        {
            var fieldToNeutralize = "requestPassthroughCameraAccessPermissionOnStartup";
            try
            {
                var ovrManager = FindAnyObjectByType<OVRManager>(FindObjectsInactive.Include);
                if (ovrManager == null) return;

                var field = ovrManager.GetType().GetField(fieldToNeutralize, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    bool value = (bool)field.GetValue(ovrManager);
                    if (value == true)
                    {
                        Debug.Log($"[PermissionsRequester-{GetType().Name}] Disabling OVRManager.{fieldToNeutralize} (would overlap with PermissionsRequester permissions handling)");
                        field.SetValue(ovrManager, false);
                    }
                }
            }
            catch (System.Exception e) {
                Debug.LogError($"[PermissionsRequester] Error while trying to disable OVRManager.{fieldToNeutralize}. Should adapt {GetType().Name} to the new OVRManager interface");
                Debug.LogException(e);
            }
        }
#endif
    }
}
