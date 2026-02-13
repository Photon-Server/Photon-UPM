using System;
using UnityEngine;
using UnityEngine.Android;

namespace Fusion.Addons.ScreenSharing
{    public class MetaWebcamPermissionRequester : MonoBehaviour
    {
        public static MetaWebcamPermissionRequester SharedInstance = null;

        public static string MetaWebcamPermissionName = "android.permission.CAMERA";
        public bool isRequesting;
        private bool hasPermission;

        public static event Action<bool> WebcamPermissionCallback;

        public bool HasPermission
        {
            get
            {
                return this.hasPermission;
            }
            private set
            {
                Debug.Log($"Webcam Permission Granted: {value}");
                WebcamPermissionCallback?.Invoke(value);
                if (this.hasPermission != value)
                {
                    this.hasPermission = value;
                }
            }
        }

        void Awake()
        {
            SharedInstance = this;
            this.CheckPermissions();
        }

        private void OnDestroy()
        {
            if(SharedInstance == this)
            {
                SharedInstance = null;
            }
        }
        public void CheckPermissions()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(MetaWebcamPermissionName))
            {
                this.HasPermission = true;
            }
            else
            {
                Debug.Log($"Android Webcam Permission ({MetaWebcamPermissionName}) Request");
                var callbacks = new PermissionCallbacks();
                callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
                callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
                Permission.RequestUserPermission(MetaWebcamPermissionName, callbacks);

                this.isRequesting = true;
            }
#else
            this.HasPermission = true;
#endif
        }

        internal void PermissionCallbacks_PermissionGranted(string permissionName)
        {
            this.isRequesting = false;
            this.HasPermission = true;
            Debug.Log($"{permissionName} PermissionGranted");
        }

        internal void PermissionCallbacks_PermissionDenied(string permissionName)
        {
            this.isRequesting = false;
            this.HasPermission = false;
            Debug.Log($"{permissionName} PermissionDenied");
        }
    }
}
