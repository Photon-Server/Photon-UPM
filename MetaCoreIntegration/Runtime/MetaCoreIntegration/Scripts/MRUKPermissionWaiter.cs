using Fusion.XRShared.Tools;
#if MRUK_AVAILABLE
using Meta.XR.MRUtilityKit;
using System.Reflection;

#endif
using UnityEngine;

namespace Fusion.Addons.Meta
{
#if MRUK_AVAILABLE
    /// <summary>
    /// Prevent MRUK from launching permission request.
    /// Provide a callback to load scene on MRUK.
    /// 
    /// Should be used alongside a PermissionsRequester, to align permissions on the desired timing (to avoid conflicting requests)
    /// </summary>
    [RequireComponent(typeof(MRUK))]
#endif
    [DefaultExecutionOrder(PermissionWaiter.EXECUTION_ORDER)]
    public class MRUKPermissionWaiter : PermissionWaiter
    {
#if MRUK_AVAILABLE
        MRUK mruk;

        #region PermissionWaiter override 
        protected override void Awake()
        {
            mruk = GetComponent<MRUK>();
            base.Awake();
        }
        protected override void OnPermissionRequired()
        {
            base.OnPermissionRequired();
            PreventOVRManagerStartupPermissions();
            mruk.SceneSettings.LoadSceneOnStartup = false;
        }

        public override string PermissionName => OVRPermissionsRequester.ScenePermission;

        /// <summary>
        /// Should be called manually (for instance in a PermissionsRequester callback) if checkPermisisonGrantAutomatically is not checked
        /// </summary>
        public override void OnPermissionGranted()
        {
            base.OnPermissionGranted();
            MRUKLoadScene();
        }
        #endregion

        /// <summary>
        /// Should be called when proper permissions are granted
        /// </summary>
        public async void MRUKLoadScene()
        {
            if(IsPermissionGranted == false)
            {
                Debug.LogError($"Should be called when {PermissionName} permission is granted only");
                return;
            }

            try
            {
                var sceneModel = MRUK.SceneModel.V1;
                if (mruk.SceneSettings.EnableHighFidelityScene)
                {
                    sceneModel = MRUK.SceneModel.V2FallbackV1;
                }
                await mruk.LoadSceneFromDevice(sceneModel: sceneModel);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Unable to load scene with MRUK");
                Debug.LogException(e);
            }
        }

        void PreventOVRManagerStartupPermissions()
        {
            var fieldToNeutralize = "requestScenePermissionOnStartup";
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
            catch (System.Exception e)
            {
                Debug.LogError($"[PermissionsRequester] Error while trying to disable OVRManager.{fieldToNeutralize}. Should adapt {GetType().Name} to the new OVRManager interface");
                Debug.LogException(e);
            }
        }
#endif
    }
}
