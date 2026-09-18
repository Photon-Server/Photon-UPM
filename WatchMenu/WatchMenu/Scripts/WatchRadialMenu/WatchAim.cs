using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Utils;
using UnityEngine;

namespace Fusion.Addons.WatchMenu
{
    /// <summary>
    /// This component, placed on the watch, checks whether the user is looking at it.
    /// When they are, it instructs the configured radial menu to display its buttons.
    /// Please note this class adds itself to the `Game Objects To Adapt` list of the `RigPartVisualizer`
    /// </summary>


    [DefaultExecutionOrder(WatchAim.EXECUTION_ORDER)]
    public class WatchAim : MonoBehaviour
    {
        // We move the menu late, to be sure to follow components after their final moves for the frame (rig parts, ...)
        const int EXECUTION_ORDER = 100_000;
        [SerializeField] Transform radialMenuCenterPosition;
        public Transform aimObject;
        public Vector3 radialMenuTranslationOffset = new Vector3(0f, 0.015f, 0f);
        public Vector3 radialMenuRotationOffset = new Vector3(0f, 0f, 90f);
        public RadialMenu radialMenu;
        [SerializeField] float acceptedAngleBetweenAimObjectAndHeadset = 32f;
        [SerializeField] bool disableWhenOnline = false;
        public bool radialMenuFollowWatchPosition = true;
        public float timerBeforeOpeningTheMenu = 0.5f;
        [SerializeField] float timerBeforeClosingTheMenu = 1f;

        float headToTargetObjectDotThreshold = -1;
        float lastWatchingTime = -1;
        float startWatchingTime = -1;

        [Header("Set automatically")]
        [SerializeField] Transform headsetTransform;
        IHardwareRig hardwareRig;
        NetworkObject networkObject;
        [SerializeField] RigPartVisualizer rigPartVisualizer;

        bool applyUnscaledOffset = false;
        bool shouldUseAimObjectForMenuCenterPosition = false;

        private void Awake()
        {
            networkObject = GetComponentInParent<NetworkObject>();
        }

        public void ChangeApplyUnscaledOffset(bool applyUnscaledOffset)
        {
            this.applyUnscaledOffset = applyUnscaledOffset;
        }

        private void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }

        private void OnDestroy()
        {
            ChangeRigPartVisualizer(null);
        }

        private void Start()
        {
            if (radialMenu == null)
            {
                Debug.LogError($"RadialMenu not set {name} ({transform.root?.name ?? ""})");
            }
            if (aimObject == null)
            {
                aimObject = transform;
            }
            SetHeadset();

            headToTargetObjectDotThreshold = Mathf.Cos(acceptedAngleBetweenAimObjectAndHeadset * Mathf.Deg2Rad);
        }

        [BeforeRenderOrder(WatchAim.EXECUTION_ORDER)]
        void OnBeforeRender()
        {
            WatchMenuHandling();
        }

        void SetHeadset()
        {
            if (hardwareRig == null)
            {
                hardwareRig = HardwareRigsRegistry.GetHardwareRig();
            }
            if (hardwareRig == null)
            {
                Debug.LogError("No hardwareRig");
            }
            else
            {
                if (headsetTransform == null && hardwareRig.Headset != null)
                {
                    headsetTransform = hardwareRig.Headset.transform;
                }
                if (headsetTransform == null)
                {
                    Debug.LogError("headsetTransform not set and Headset not found");
                }
            }
        }

        public void ChangeRigPartVisualizer(RigPartVisualizer visualizer)
        {

            if (rigPartVisualizer != null)
            {
                rigPartVisualizer.RemoveObjectContentToAdapt(gameObject, shouldAdaptGameObject: true, includeDisabledComponents: true);
                if(aimObject) rigPartVisualizer.RemoveObjectContentToAdapt(aimObject.gameObject, shouldAdaptGameObject: true, includeDisabledComponents: true);
            }
            rigPartVisualizer = visualizer;
            if (rigPartVisualizer != null)
            {
                rigPartVisualizer.AddObjectContentToAdapt(gameObject, shouldAdaptGameObject: true, includeDisabledComponents: true);
                if (aimObject) rigPartVisualizer.AddObjectContentToAdapt(aimObject.gameObject, shouldAdaptGameObject: true, includeDisabledComponents: true);
                rigPartVisualizer.ReApplyAdapt();
            }
        }

        void WatchMenuHandling()
        {

            if (radialMenu == null) return;
            if (networkObject && networkObject.HasStateAuthority == false) return;

            if (rigPartVisualizer == null)
            {
                ChangeRigPartVisualizer(GetComponentInParent<RigPartVisualizer>());
            }

            if (rigPartVisualizer && rigPartVisualizer.ShouldDisplay() == false)
            {
                radialMenu.CloseRadialMenu();
                return;
            }

            if (disableWhenOnline && hardwareRig.LocalUserNetworkRig != null && (hardwareRig.LocalUserNetworkRig.Object?.Runner?.IsRunning ?? false))
            {
                radialMenu.CloseRadialMenu();
                return;
            }

            if (hardwareRig == null) return;

            if (radialMenuFollowWatchPosition)
            {
                ComputeRadialMenuPosition();
            }

            if (headsetTransform == null || radialMenu == null || aimObject == null) return;

            // open the menu, after a timer, if the user is looking toward the watch 
            if (radialMenu.menuIsDisplayed == false && IsHeadsetLookingAtTargetObject(aimObject.gameObject))
            {
                if (startWatchingTime == -1)
                {
                    startWatchingTime = Time.time;
                }

                if (Time.time - startWatchingTime > timerBeforeOpeningTheMenu)
                {
                    ComputeRadialMenuPosition();
                    radialMenu.OpenRadialMenu();
                }
            }

            // Check if the menu should be closed 
            bool menuShouldBeClosed = false;
            if (radialMenu.menuIsDisplayed)
            {
                // If the menu follow the watch, the menu must be closed if the headset it not toward the watch
                if (radialMenuFollowWatchPosition)
                {
                    if (IsHeadsetLookingAtTargetObject(aimObject.gameObject))
                    {
                        lastWatchingTime = Time.time;
                    }
                    else
                    {
                        menuShouldBeClosed = true;
                    }
                }
                // If the menu doesn't follow the watch, the menu must be closed if the headset it not toward the menu
                else
                {
                    if (IsHeadsetLookingAtTargetObject(radialMenu.gameObject))
                    {
                        lastWatchingTime = Time.time;
                    }
                    else
                    {
                        menuShouldBeClosed = true;
                    }
                }
            }

            // Close the menu if required
            if (menuShouldBeClosed)
            {
                if (Time.time - lastWatchingTime > timerBeforeClosingTheMenu)
                {
                    radialMenu.CloseRadialMenu();
                    startWatchingTime = -1;
                }
            }
        }

        void ComputeRadialMenuPosition()
        {
            if (radialMenuCenterPosition == null)
            {
                radialMenuCenterPosition = aimObject;
                shouldUseAimObjectForMenuCenterPosition = true;
            }
            if (shouldUseAimObjectForMenuCenterPosition)
            {
                radialMenuCenterPosition = aimObject;
            }
            if (applyUnscaledOffset)
            {
                (var position, var rotation) = TransformManipulations.ApplyUnscaledOffset(radialMenuCenterPosition.transform.position, radialMenuCenterPosition.transform.rotation, radialMenuTranslationOffset, Quaternion.Euler(radialMenuRotationOffset));
                radialMenu.transform.rotation = rotation;
                radialMenu.transform.position = position;
            }
            else
            {
                radialMenu.transform.rotation = radialMenuCenterPosition.rotation * Quaternion.Euler(radialMenuRotationOffset);
                radialMenu.transform.position = radialMenuCenterPosition.transform.TransformPoint(radialMenuTranslationOffset);
            }
        }

        private bool IsHeadsetLookingAtTargetObject(GameObject targetObject)
        {
            // Check if headset is looking at target object
            Vector3 headToTargetObjectVector = targetObject.transform.position - headsetTransform.position;
            Vector3 directionHeadToTargetObject = headToTargetObjectVector.normalized;
            float headToTargetObjectDot = Vector3.Dot(headsetTransform.forward, directionHeadToTargetObject);

            if (headToTargetObjectDot < headToTargetObjectDotThreshold)
            {
                //Debug.LogError($"IsHeadsetLookingAtTargetObject KO1 (Check if headset is looking at target object) {headToTargetObjectDot} < {headToTargetObjectDotThreshold}");
                return false;
            }

            // Check if target object if oriented toward the head
            Vector3 targetObjecttoHeadVector = headsetTransform.position - targetObject.transform.position;
            Vector3 directionAimObjectToHead = targetObjecttoHeadVector.normalized;
            float targetObjectToHeadDot = -Vector3.Dot(directionAimObjectToHead, targetObject.transform.forward);

            if (targetObjectToHeadDot < headToTargetObjectDotThreshold)
            {
                //Debug.LogError($"IsHeadsetLookingAtTargetObject KO2 (Check if target object {targetObject} if oriented toward the head) {targetObjectToHeadDot} < {headToTargetObjectDotThreshold}");
                return false;
            }

            return true;
        }
    }
}
