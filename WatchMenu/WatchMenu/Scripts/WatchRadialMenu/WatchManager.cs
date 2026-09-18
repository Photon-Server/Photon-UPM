using Fusion.Addons.WatchMenu;
using Fusion.XR.Shared.Automatization;
using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Core.Interaction;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Fusion.Addons.WatchMenu
{
    public class WatchManager : MonoBehaviour, IWristTracker
    {
        const string WATCHFACE_PREFAB = "WatchFace";
        const string WATCHBACKFACE_PREFAB = "WatchBackFace";

        [Header("Watch interaction")]
        [SerializeField] RadialMenu topRadialMenu;
        [SerializeField] RadialMenu bottomRadialMenu;

        [Header("Watch detection/creation")]
        [SerializeField] Watch watch;
        [SerializeField] WatchBackFace watchBackFace;
        public Watch watchPrefab;
        public WatchBackFace watchBackFacePrefab;
        [SerializeField] RigPartSide expectedWristSide = RigPartSide.Left;

        [Header("Watch options")]

        public WatchOption controllerWatchOption = new WatchOption
        {
        }; 
        public WatchOption controllerWatchBackFaceOption = new WatchOption
        {
            radialMenuRotationOffset = new Vector3(0, 0, 260),
        };
        public WatchOption handTrackingWatchOption = new WatchOption
        {
            radialMenuFollowWatchPosition = false,
            timerBeforeOpeningTheMenu = 0.75f
        };
        public WatchOption handTrackingWatchBackFaceOption = new WatchOption
        {
            radialMenuFollowWatchPosition = false,
            radialMenuRotationOffset = new Vector3(0, 0, 260),
            timerBeforeOpeningTheMenu = 0.75f,
        };

        [Header("Main buttons events")]
        [SerializeField] UnityEvent onWatchTouchStart = new UnityEvent();
        [SerializeField] UnityEvent onWatchBackTouchStart = new UnityEvent();

        [Header("Debug")]
        [SerializeField] bool debugLog = false;

        List<IWrist> wrists = new List<IWrist>();
        List<WristConfiguration> wristConfigurations = new List<WristConfiguration>();
        WristConfiguration activeConfiguration;

        public bool dontMoveWatchBackWhenParentedUnderWatch = false;
        //  We won't move the backface if isWatchBackFaceChildOfWatch is true, as it means then that the back face has been designed to follow the front face
        bool isWatchBackFaceChildOfWatch = false;

        [System.Serializable]
        public class WristConfiguration
        {
            public IWrist wrist;
            public RadialMenu topRadialMenu;
            public RadialMenu bottomRadialMenu;
            RigPartVisualizer _rigPartVisualizer;
            public RigPartVisualizer RigPartVisualizer
            {
                get
                {
                    if(_rigPartVisualizer == null)
                    {
                        _rigPartVisualizer = wrist.WristTransform.GetComponentInParent<RigPartVisualizer>();
                    }
                    return _rigPartVisualizer;
                }
            }
        }

        [System.Serializable]
        public class WatchOption {
            public bool radialMenuFollowWatchPosition = true;
            public float timerBeforeOpeningTheMenu = 0.5f;
            public Vector3 radialMenuTranslationOffset = new Vector3(0f, 0.015f, -0.0225f);
            public Vector3 radialMenuRotationOffset = new Vector3(0f, 0f, 90f);
            public bool enableTouch = true;
            public bool applyUnscaledOffset = true;
        }

        void Start()
        {
            foreach (var wrist in GetComponentsInChildren<IWrist>())
            {
                RegisterWrist(wrist);
            }
            PrepareWatch();
        }

        void PrepareWatch()
        {
            if (watch == null)
            {
                foreach(var w in GetComponentsInChildren<Watch>(true))
                {
                    if(watch == null)
                    {
                        if(debugLog) Debug.Log($"[WatchManager] Watch found {w}, under {w.transform.parent}");
                        watch = w;
                    }
                    else
                    {
                        if (debugLog) Debug.Log($"[WatchManager] Duplicate watch, not needed anymore: removing {w}, under {w.transform.parent}");
                        Destroy(w.gameObject);
                    }
                }
            }
            if (watchBackFace == null)
            {
                watchBackFace = GetComponentInChildren<WatchBackFace>(true);
            }
            CheckBackFaceParent();
            if (watch == null || watchBackFace == null)
            {
                CreateWatch();
            }
        }

        void CheckBackFaceParent()
        {
            if (watchBackFace == null) return;
            var t = watchBackFace.transform;
            while(t != null)
            {
                if (watch && t == watch.transform)
                {
                    isWatchBackFaceChildOfWatch = true;
                    break;
                }
                t = t.parent;
            }
        }

        void CreateWatch() {
            if (TryUpdateActiveConfiguration())
            {
                if (watch == null)
                {
                    if(watchPrefab != null)
                    {
                        if (debugLog) Debug.Log("[WatchManager] Create watch");
                        watch = GameObject.Instantiate(watchPrefab);
                        if (watchBackFace == null)
                        {
                            watchBackFace = watch.GetComponentInChildren<WatchBackFace>(true);
                            CheckBackFaceParent();
                        }
                    }
                    else
                    {
                        Debug.LogError("[WatchManager] Error: Unable to create watch, no watchPrefab");
                    }
                }
                if (watchBackFace == null && watchBackFacePrefab != null)
                {
                    if (debugLog) Debug.Log("[WatchManager] Create watch back");
                    watchBackFace = GameObject.Instantiate(watchBackFacePrefab);
                }
                ActivateWristConfiguration(activeConfiguration);
            }
        }

        WristConfiguration BuildWristConfiguration(IWrist wrist)
        {
            var config = new WristConfiguration();
            config.wrist = wrist;
            config.topRadialMenu = topRadialMenu; 
            config.bottomRadialMenu = bottomRadialMenu;
            return config;
        }

        bool TryUpdateActiveConfiguration()
        {
            bool configChanged = false;
            foreach (var configuration in wristConfigurations)
            {
                if (configuration.RigPartVisualizer && configuration.RigPartVisualizer.lastAppliedShouldDisplay)
                {
                    if (activeConfiguration?.wrist != configuration?.wrist)
                    {
                        if (debugLog) Debug.Log($"[WatchManager] Config change {activeConfiguration?.wrist} ({activeConfiguration?.RigPartVisualizer}) -> {configuration?.wrist} ({configuration?.RigPartVisualizer})");
                        activeConfiguration = configuration;
                        configChanged = true;
                    }
                    break;
                }
                else if (activeConfiguration == null)
                {
                    // We set the first config as the default active one if none is set (and continue to look for an active one)
                    activeConfiguration = configuration;
                    if (debugLog) Debug.Log("[WatchManager] We set the first config as the default active one if none is set (and continue to look for an active one)");
                    configChanged = true;
                }
            }
            return configChanged;
        }

        void CheckActiveWrist()
        {
            if (TryUpdateActiveConfiguration())
            {
                ActivateWristConfiguration(activeConfiguration);
            }
        }

        void ActivateWristConfiguration(WatchManager.WristConfiguration configuration)
        {
            if (configuration == null)
            {
                return;
            }

            // We don't move the backface if isWatchBackFaceChildOfWatch is true, as it means then that the back face has been designed to follow the front face
            if ((dontMoveWatchBackWhenParentedUnderWatch == false ||isWatchBackFaceChildOfWatch == false) && watchBackFace && configuration.wrist is IDetailedWrist wristDetails)
            {
                if (debugLog) Debug.Log($"[WatchManager] Anchor watch back {watchBackFace} to wrist back {wristDetails.WristBottomTransform}");

                watchBackFace.transform.parent = wristDetails.WristBottomTransform;
                watchBackFace.transform.localPosition = Vector3.zero;
                watchBackFace.transform.localRotation = Quaternion.identity;
            }
            if (watch)
            {
                if (debugLog) Debug.Log($"[WatchManager] Anchor watch {watch} to wrist {configuration.wrist.WristTransform}");

                if (configuration.RigPartVisualizer && configuration.RigPartVisualizer.rigPart != null)
                {
                    watch.name = "Watch-" + configuration.RigPartVisualizer.rigPart.GetType().Name;
                }
                //Debug.LogError($"[WatchManager] Anchor watch {watch.transform.name} to wrist {configuration.wrist.WristTransform}");
                watch.transform.parent = configuration.wrist.WristTransform;
                watch.transform.localPosition = Vector3.zero;
                watch.transform.localRotation = Quaternion.identity;

                bool isHandTracking = configuration.RigPartVisualizer?.rigPart is IHand;
                WatchOption watchOptions = isHandTracking ? handTrackingWatchOption : controllerWatchOption;
                WatchOption watchBackOptions = isHandTracking ? handTrackingWatchBackFaceOption : controllerWatchBackFaceOption;

                watch.ActivateWristConfiguration(activeConfiguration, watchOptions, watchBackOptions, this);
            }
        }

        private void Update()
        {
            PrepareWatch();
            CheckActiveWrist();
        }

        #region IWristTracker
        public void RegisterWrist(IWrist wrist)
        {
            var rigPart = wrist.WristTransform.GetComponentInParent<ILateralizedRigPart>();
            if (expectedWristSide != RigPartSide.Undefined && (rigPart == null || rigPart.Side != expectedWristSide))
            {
                if (debugLog) Debug.Log($"[WatchManager] Ignore wrist {wrist} not on expected side {expectedWristSide} (was on {rigPart?.Side} of {rigPart})");
                return;
            }
            if (wrists.Contains(wrist) == false)
            {
                wrists.Add(wrist);
                wristConfigurations.Add(BuildWristConfiguration(wrist));
            }
        }

        public void UnregisterWrist(IWrist wrist)
        {
            if (wrists.Contains(wrist))
            {
                wrists.Remove(wrist);
                foreach(var wristConfiguration in wristConfigurations)
                {
                    if (wristConfiguration.wrist == wrist)
                    {
                        wristConfigurations.Remove(wristConfiguration);
                        break;
                    }
                }
            }
        }
        #endregion

        public void OnWatchTouchStart()
        {
            if(onWatchTouchStart != null) onWatchTouchStart.Invoke();
        }

        public void OnWatchBackTouchStart()
        {
            if (onWatchBackTouchStart != null) onWatchBackTouchStart.Invoke();
        }


        private void OnValidate()
        {
#if UNITY_EDITOR

            if (watchPrefab == null)
            {
                if (AssetLookup.TryFindAsset(WATCHFACE_PREFAB, out GameObject availableWatchFacePrefabGo, requiredPathElement: "WatchMenu"))
                {
                    var availableWatchFacePrefab = availableWatchFacePrefabGo.GetComponent<Watch>();
                    if (availableWatchFacePrefab) watchPrefab = availableWatchFacePrefab;
                }
            }
            if (watchBackFacePrefab == null)
            {
                if (AssetLookup.TryFindAsset(WATCHBACKFACE_PREFAB, out GameObject availableWatchBackFacePrefabGo, requiredPathElement: "WatchMenu"))
                {
                    var availableWatchBackFacePrefab = availableWatchBackFacePrefabGo.GetComponent<WatchBackFace>();
                    if (availableWatchBackFacePrefab) watchBackFacePrefab = availableWatchBackFacePrefab;
                }
            }
#endif
        }

    }

}
