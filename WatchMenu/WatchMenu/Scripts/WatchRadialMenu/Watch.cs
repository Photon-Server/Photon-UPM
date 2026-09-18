using UnityEngine;
using Fusion.XR.Shared.Core.Interaction;
using Fusion.XR.Shared.Core.Touch;

namespace Fusion.Addons.WatchMenu
{
    public class Watch : MonoBehaviour, ITouchableListener
    {
        [SerializeField] WatchAim topWatchAim;
        [SerializeField] WatchAim backWatchAim;
        [SerializeField] IRegisterableTouchable topWatchAimTouchable;
        [SerializeField] IRegisterableTouchable backWatchAimTouchable;
        [SerializeField] Collider topWatchAimTouchableCollider;
        [SerializeField] Collider backWatchAimTouchableCollider;
        WatchManager currentManager;


        [Header("Debug")]
        [SerializeField] WatchManager.WristConfiguration lastAppliedConfiguration;
        [SerializeField] WatchManager.WatchOption lastAppliedWatchOptions;
        [SerializeField] WatchManager.WatchOption lastAppliedWatchBackOptions;

        [Header("Legacy")]
        public bool resetVisualPositionOnConfigurationReception = true;
        [SerializeField] Transform visual;

        private void Awake()
        {
            if (topWatchAim == null)
            {
                var watchAims = GetComponentsInChildren<WatchAim>();
                if (watchAims.Length == 1)
                {
                    topWatchAim = watchAims[0];
                }
                else if (watchAims.Length > 1)
                {
                    Debug.LogError("[Watch] Error: Top watch aim not specified in Watch, and several watch aim found: unable to determine which one is top, and which one is bottom");
                }
            }
        }

        bool topWatchAimConfigured = false;
        bool backWatchAimConfigured = false;

        private void Start()
        {
            ConfigureTopWatchAim();
            ConfigureBackWatchAim();
        }

        void ConfigureTopWatchAim() {
            if (topWatchAimConfigured) return;
            if (topWatchAim == null) return;
            topWatchAimConfigured = true;
            if (topWatchAim) topWatchAimTouchable = topWatchAim.GetComponentInChildren<IRegisterableTouchable>(true);
            if (topWatchAimTouchable != null) topWatchAimTouchable.RegisterListener(this);
            if (topWatchAimTouchable != null) topWatchAimTouchableCollider = topWatchAimTouchable.transform.GetComponentInChildren<Collider>(true);
        }

        void ConfigureBackWatchAim()
        {
            if (backWatchAimConfigured) return;
            if (backWatchAim == null) return;
            backWatchAimConfigured = true;
            if (backWatchAim) backWatchAimTouchable = backWatchAim.GetComponentInChildren<IRegisterableTouchable>(true);
            if (backWatchAimTouchable != null) backWatchAimTouchable.RegisterListener(this);
            if (backWatchAimTouchable != null) backWatchAimTouchableCollider = backWatchAimTouchable.transform.GetComponentInChildren<Collider>(true);
        }

        public void ActivateWristConfiguration(WatchManager.WristConfiguration configuration, WatchManager.WatchOption watchOptions, WatchManager.WatchOption watchBackOptions, WatchManager manager)
        {
            if (topWatchAim == null && configuration.wrist != null)
            {
                topWatchAim = configuration.wrist.WristTransform.GetComponentInChildren<WatchAim>(true);
            }
            if (backWatchAim == null && configuration.wrist is IDetailedWrist detailedWrist)
            {
                backWatchAim = detailedWrist.WristBottomTransform.GetComponentInChildren<WatchAim>(true);
            }
            ConfigureTopWatchAim();
            ConfigureBackWatchAim();

            lastAppliedConfiguration = configuration; 
            lastAppliedWatchOptions = watchOptions;                
            lastAppliedWatchBackOptions = watchBackOptions;

            currentManager = manager;
            if (visual && resetVisualPositionOnConfigurationReception)
            {
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }
            if (topWatchAim)
            {
                topWatchAim.aimObject = configuration.wrist.WristTransform;
                topWatchAim.ChangeRigPartVisualizer(configuration.RigPartVisualizer);
                topWatchAim.radialMenu = configuration.topRadialMenu;
                if (watchOptions != null)
                {
                    topWatchAim.radialMenuFollowWatchPosition = watchOptions.radialMenuFollowWatchPosition;
                    topWatchAim.timerBeforeOpeningTheMenu = watchOptions.timerBeforeOpeningTheMenu;
                    topWatchAim.radialMenuTranslationOffset = watchOptions.radialMenuTranslationOffset;
                    topWatchAim.radialMenuRotationOffset = watchOptions.radialMenuRotationOffset;
                    topWatchAim.ChangeApplyUnscaledOffset(watchOptions.applyUnscaledOffset);
                }
            }
            if (backWatchAim && configuration.wrist is IDetailedWrist wristDetails)
            {
                backWatchAim.aimObject = wristDetails.WristBottomTransform;
                backWatchAim.ChangeRigPartVisualizer(configuration.RigPartVisualizer);
                backWatchAim.radialMenu = configuration.bottomRadialMenu;
                if (watchBackOptions != null)
                {
                    backWatchAim.radialMenuFollowWatchPosition = watchBackOptions.radialMenuFollowWatchPosition;
                    backWatchAim.timerBeforeOpeningTheMenu = watchBackOptions.timerBeforeOpeningTheMenu;
                    backWatchAim.radialMenuTranslationOffset = watchBackOptions.radialMenuTranslationOffset;
                    backWatchAim.radialMenuRotationOffset = watchBackOptions.radialMenuRotationOffset;
                    backWatchAim.ChangeApplyUnscaledOffset(watchBackOptions.applyUnscaledOffset);
                }
            }
            if (topWatchAimTouchable != null) topWatchAimTouchable.enabled = watchOptions.enableTouch;
            if (topWatchAimTouchableCollider != null) topWatchAimTouchableCollider.enabled = watchOptions.enableTouch;
            if (backWatchAimTouchable != null) backWatchAimTouchable.enabled = watchBackOptions.enableTouch;
            if (backWatchAimTouchableCollider != null) backWatchAimTouchableCollider.enabled = watchBackOptions.enableTouch;
        }

        #region ITouchableListener
        public void OnToucherContactStart(ITouchable touchable, Toucher toucher)
        {
            if (touchable == topWatchAimTouchable)
            {
                currentManager?.OnWatchTouchStart();
            }

            if (touchable == backWatchAimTouchable)
            {
                currentManager?.OnWatchBackTouchStart();
            }
        }

        public void OnToucherStay(ITouchable touchable, Toucher toucher)
        {
        }

        public void OnToucherContactEnd(ITouchable touchable, Toucher toucher)
        {
        }
        #endregion
    }
}
