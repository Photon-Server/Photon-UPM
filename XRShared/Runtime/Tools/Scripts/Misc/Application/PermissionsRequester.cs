using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

#if (UNITY_IOS || UNITY_VISIONOS) && !UNITY_EDITOR
using UnityEngine.iOS;
#endif

namespace Fusion.XRShared.Tools
{
    /// <summary>
    /// Launch successively permission requests (Android only for now)
    /// </summary>
    [DefaultExecutionOrder(PermissionsRequester.EXECUTION_ORDER)]
    public class PermissionsRequester : MonoBehaviour
    {
        public const int EXECUTION_ORDER = -20_000;
        public static PermissionsRequester _sharedInstance;
        

        public static PermissionsRequester SharedInstance { 
            get
            {
                if (_sharedInstance == null)
                {
                    var permissionRequesterGameObject = new GameObject("PermissionsRequester");
                    permissionRequesterGameObject.AddComponent<PermissionsRequester>();
                }
                return _sharedInstance;
            }
        
        }

        /// <summary>
        /// Launch a permission request (Android only for now)
        /// </summary>
        [System.Serializable]
        public class PermissionRequest
        {
            public const float DefaultDelayBeforeRequest = 0.05f;
            public string permissionName = "";
            public bool hasBeenChecked = false;
            public bool isRequesting = false;
            public float delayBeforeRequest = DefaultDelayBeforeRequest;
            public float delayBeforeCallback = 0;
            [Tooltip("Has the permission been received (do not edit in the inspector)")]
            [SerializeField] private bool hasPermission = false;
            public UnityEvent<bool> permissionCallback = new UnityEvent<bool>();
            public UnityEvent permissionGrantedCallback = new UnityEvent();

            public bool HasPermission
            {
                get
                {
                    return this.hasPermission;
                }
                private set
                {
                    Debug.Log($"[PermissionsRequester] {permissionName} permission Granted: {value}");
                    this.hasBeenChecked = true;
                    this.isRequesting = false;
                    if (this.hasPermission != value)
                    {
                        this.hasPermission = value;
                    }
                    NotifyPermissionChange(value);
                }
            }

            async void NotifyPermissionChange(bool value)
            {
                if(delayBeforeCallback > 0)
                {
                    await Task.Delay((int)(1000 * delayBeforeCallback));
                }
                permissionCallback?.Invoke(value);
                if (value)
                {
                    permissionGrantedCallback?.Invoke();
                }
            }

            public PermissionRequest(string permissionName, float delayBeforeRequest = DefaultDelayBeforeRequest)
            {
                this.permissionName = permissionName;
                this.delayBeforeRequest = delayBeforeRequest;
            }

            public async void CheckPermission()
            {
                if (delayBeforeRequest > 0)
                {
                    await Task.Delay((int)(1000 * delayBeforeRequest));
                }
                Debug.Log($"[PermissionsRequester] Checking permission {permissionName} (RequestUserPermission) ...");
#if UNITY_ANDROID && !UNITY_EDITOR
                if (Permission.HasUserAuthorizedPermission(permissionName))
                {
                    this.HasPermission = true;
                }
                else
                {
                    Debug.Log($"[PermissionsRequester] Permission {permissionName} Request");
                    var callbacks = new PermissionCallbacks();
                    callbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
                    callbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
                    Permission.RequestUserPermission(permissionName, callbacks);

                    this.isRequesting = true;
                }
#else
                this.HasPermission = true;
#endif
            }

            public void UpdatePermission()
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (hasBeenChecked && HasPermission == false && Permission.HasUserAuthorizedPermission(permissionName)){
                    Debug.LogError($"[PermissionsRequester] Permission {permissionName} granted, after first rejection (several conflicting request were initially pending probably)");
                    this.HasPermission = true;
                }
#endif
            }

            internal void PermissionCallbacks_PermissionGranted(string permissionName)
            {
                this.HasPermission = true;
                Debug.Log($"[PermissionsRequester] {permissionName} PermissionGranted");
            }

            internal void PermissionCallbacks_PermissionDenied(string permissionName)
            {
                this.HasPermission = false;
                Debug.Log($"[PermissionsRequester] {permissionName} PermissionDenied");
            }
        }


        public List<PermissionRequest> permissionRequests = new List<PermissionRequest>();
        public UnityEvent allPermissionCallback;
        public List<GameObject> objectsToActivateAfterPermissionChecks = new List<GameObject>();
        public bool checkPermissionsBeforeFusionStart = false;

        public bool requestOnStart = true;
        public bool isLaunchingRequests = false;
        bool launchingRequestAlreadyStarted = false;
        bool shouldLaunchPermissionRequests = false;

        void Awake()
        {
#if (UNITY_IOS || UNITY_VISIONOS)
            Debug.LogError("PermissionsRequester does not support iOS/visionOS yet");
#endif
            _sharedInstance = this;
            if (checkPermissionsBeforeFusionStart)
            {
                var networkRunner = FindAnyObjectByType<NetworkRunner>();
                if (networkRunner)
                {
                    networkRunner.gameObject.SetActive(false);
                    objectsToActivateAfterPermissionChecks.Add(networkRunner.gameObject);
                }
                var fusionbootstrap = FindAnyObjectByType<FusionBootstrap>();
                if (fusionbootstrap)
                {
                    fusionbootstrap.gameObject.SetActive(false);
                    objectsToActivateAfterPermissionChecks.Add(fusionbootstrap.gameObject);
                }
            }
        }

        private void Start()
        {
            if (requestOnStart)
            {
                shouldLaunchPermissionRequests = true;
            }
        }

        async Task WaitForRequestLaunchFinished()
        {
            while (isLaunchingRequests)
            {
                await Task.Delay(50);
            }
        }

        public async Task LaunchPermissionRequests()
        {
            await WaitForRequestLaunchFinished();
            isLaunchingRequests = true;
            launchingRequestAlreadyStarted = true;
            Debug.Log("[PermissionsRequester] LaunchPermissionRequests");
            int i = 1;
            foreach (var requester in permissionRequests) {
                Debug.Log($"[PermissionsRequester] LaunchPermissionRequests: {i}/{permissionRequests.Count} ({requester.permissionName})");
                if (requester.hasBeenChecked == false && requester.isRequesting == false)
                {
                    requester.CheckPermission();
                }
                while (requester.hasBeenChecked == false)
                {
                    await Task.Delay(50);
                }
                i++;
            }
            Debug.Log("[PermissionsRequester] All permissions checked");
            foreach(var objectToActivateAfterPermissionChecks in objectsToActivateAfterPermissionChecks)
            {
                objectToActivateAfterPermissionChecks.SetActive(true);
            }
            allPermissionCallback?.Invoke();
            isLaunchingRequests = false;
        }

        private void Update()
        {
            if (shouldLaunchPermissionRequests)
            {
                shouldLaunchPermissionRequests = false;
                StartPermissionRequests();
            }

            foreach (var requester in permissionRequests)
            {
                requester.UpdatePermission();
            }
        }

        async void StartPermissionRequests()
        {
            await LaunchPermissionRequests();
        }

        private void OnDestroy()
        {
            if(SharedInstance == this)
            {
                _sharedInstance = null;
            }
        }

        public bool TryFindConfiguredPermissionRequest(string permission, out PermissionRequest requester)
        {
            bool found = false;
            requester = null;
            foreach (var r in permissionRequests)
            {
                if(r.permissionName == permission)
                {
                    requester = r;
                    found = true;
                    break;
                }
            }
            return found;
        }

        public async void AddPermissionRequest(string permission, UnityAction<bool> permissionCallback = null)
        {
            PermissionRequest request = null;
            if (launchingRequestAlreadyStarted)
            {
                // Permisison checks had already started: making sure it is finished before adding a new one (to avoid changing the list while read)");
                await WaitForRequestLaunchFinished();
            }
            if (TryFindConfiguredPermissionRequest(permission, out request) == false)
            {
                Debug.Log($"[PermissionsRequester] Adding permission request for {permission}");
                request = new PermissionRequest(permission);
                if(permissionCallback != null) request.permissionCallback.AddListener(permissionCallback);
                permissionRequests.Add(request);
                if (launchingRequestAlreadyStarted)
                {
                    // Permisison checks had already started: we have to run the permissions again for this one to be checked
                    Debug.Log("[PermissionsRequester] Launching permissions requests due to late AddPermissionRequest");
                    await LaunchPermissionRequests();
                }
            }
            else
            {
                // The request already exists. Set the callback, if any
                if (permissionCallback != null)
                {
                    request.permissionCallback.AddListener(permissionCallback);
                    if (request.hasBeenChecked)
                    {
                        // The request has already been requested, and a permission result has been set. Manually call the callback
                        permissionCallback(request.HasPermission);
                    }
                }
            }
        }
    }
}
