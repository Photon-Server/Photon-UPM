using Fusion.XR.Shared.Base;
using UnityEngine;
using System.Collections.Generic;
using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Tools;
using Fusion.XRShared.Tools;
using System;

#if XRHANDS_AVAILABLE
using UnityEngine.XR.Hands;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
#endif

#if XRHANDS_SYNCHRONIZATION_ADDON_AVAILABLE
using Fusion.Addons.XRHandsSync;
#endif

namespace Fusion.XR.Shared.XRHands
{
    /// <summary>
    /// Rig part to represent hand tracking, backed by Unity's XRHand. 
    /// If XRHands is not installed, the rigpart will be disabled
    /// </summary>
#if XRHANDS_AVAILABLE && XRHANDS_SYNCHRONIZATION_ADDON_AVAILABLE
    public class XRHandsHardwareHand : HardwareHand, IGrabbingProvider, ISkeletonDriverLogicOverride
    {
        [Header("Pinch detection")]
        [SerializeField] bool updateIsPinching = true;
        [SerializeField] float pinchThreshold = 0.02f;
        [SerializeField] bool usePinchForGrabbing = true;

        [Header("XRHands specific bones configuration (if used with Meta rig's OVRSkeleton, ...)")]
        public Transform handBonesRootOverride = null;
        public bool dontUpdateBonesTransforms = false;

        #region ISkeletonDriverLogicOverride
        public Transform SkeletonDriverRootOverride => handBonesRootOverride;
        public bool DontUpdateBonesTransforms => dontUpdateBonesTransforms;
        #endregion

#if XRHANDS_SYNCHRONIZATION_ADDON_AVAILABLE
        public override Pose WorldIndexTipPose => collectableSkeletonDriver.WorldIndexTipPose;
        public override Pose WorldIndexBasePose => collectableSkeletonDriver.BoneWorldPose(XRHandJointID.IndexProximal);
        public override Pose WorldWristPose => collectableSkeletonDriver.WorldWristPose;
        public override Pose WorldThumbTipPose => collectableSkeletonDriver.WorldThumbTipPose;
        public override Pose WorldMiddleTipPose => collectableSkeletonDriver.BoneWorldPose(XRHandJointID.MiddleTip);
        public override Pose WorldRingTipPose => collectableSkeletonDriver.BoneWorldPose(XRHandJointID.RingTip);
        public override Pose WorldLittleTipPose => collectableSkeletonDriver.BoneWorldPose(XRHandJointID.LittleTip);
        public override Pose WorldPalmPose => collectableSkeletonDriver.BoneWorldPose(XRHandJointID.Palm);


#endif
        public bool IsPinching { get; set; } = false;

        #region IGrabbingProvider
        public bool IsGrabbing => usePinchForGrabbing && IsPinching;
        #endregion

#if XRHANDS_SYNCHRONIZATION_ADDON_AVAILABLE
        protected XRHandCollectableSkeletonDriver collectableSkeletonDriver;
        public override Pose RigPartPose
        {
            get
            {
                if (Rig != null && collectableSkeletonDriver != null)
                {
                    return collectableSkeletonDriver.WorldWristPose;
                }

                return base.RigPartPose;
            }
        }


        protected override void RegisterToHardwareRig()
        {
            if (collectableSkeletonDriver == null && Side != RigPartSide.Undefined)
            {
                var leftHand = Side == RigPartSide.Left ? true : false;
                collectableSkeletonDriver = XRHandCollectableSkeletonDriverHelper.SetupXRHandsboneCollector(gameObject, leftHand: leftHand);
            }
            base.RegisterToHardwareRig();
        }
#endif

        #region Tracking status
#if XRHANDS_SYNCHRONIZATION_ADDON_AVAILABLE
#if XRHANDS_AVAILABLE
        XRHandSubsystem _handSubsystem;
        bool _noHandSubsystemErrorDisplayed = false;
#if UNITY_ANDROID
        bool _permissionRequested = false;
#endif
        protected virtual void DetectHandSubsystems()
        {
            if (_handSubsystem != null) return;

#if UNITY_ANDROID

            if (_permissionRequested == false)
            {
                PermissionsRequester.SharedInstance.AddPermissionRequest(GetHandTrackingPermission());
                _permissionRequested = true;
            }

            // Wait until permission is granted before looking for subsystem
            if (Permission.HasUserAuthorizedPermission(GetHandTrackingPermission()) == false)
                return;
#endif

            var handSubsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(handSubsystems);

            for (var i = 0; i < handSubsystems.Count; ++i)
            {
                var availableHandSubsystem = handSubsystems[i];
                if (availableHandSubsystem.running)
                {
                    _handSubsystem = availableHandSubsystem;
                    _handSubsystem.updatedHands += UpdatedHands;
                    Debug.Log("[DetectHandSubsystems] Hand tracking active");
                    break;
                }
            }
            if (handSubsystems.Count == 0 && _noHandSubsystemErrorDisplayed == false)
            {
                _noHandSubsystemErrorDisplayed = true;
                Debug.LogWarning("[DetectHandSubsystems] No subsystem found yet");
            }
        }

#if UNITY_ANDROID
        private string GetHandTrackingPermission()
        {

            string deviceModel = SystemInfo.deviceModel.ToLower();

            if (deviceModel.Contains("oculus") || deviceModel.Contains("meta"))
                return "com.oculus.permission.HAND_TRACKING";

            return "android.permission.HAND_TRACKING";
        }
#endif

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_handSubsystem != null)
            {
                _handSubsystem.updatedHands -= UpdatedHands;
            }
        }

        private void UpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags flags, XRHandSubsystem.UpdateType type)
        {
            IsPinching = false;
            if (updateIsPinching == false) return;
            var indexTipAvailable = collectableSkeletonDriver.TryGetBoneRigRelativePose(XRHandJointID.IndexTip, out var indexTipPose);
            var thumbTipAvailable = collectableSkeletonDriver.TryGetBoneRigRelativePose(XRHandJointID.ThumbTip, out var thumbTipPose);
            float indexDistance = 0;
            if (indexTipAvailable && thumbTipAvailable)
            {
                indexDistance = Vector3.Distance(indexTipPose.position, thumbTipPose.position);
                if (indexDistance < pinchThreshold)
                {
                    IsPinching = true;
                }
            }
        }
#endif
#endif

        public override void DoUpdateTrackingStatus()
        {
            base.DoUpdateTrackingStatus();
            TrackingStatus = RigPartTrackingstatus.NotTracked;
#if XRHANDS_SYNCHRONIZATION_ADDON_AVAILABLE
#if XRHANDS_AVAILABLE
            DetectHandSubsystems();
            if (_handSubsystem == null)
            {
                return;
            }
            var hand = Side == Core.RigPartSide.Left ? _handSubsystem.leftHand : _handSubsystem.rightHand;
            if (hand.isTracked)
            {
                TrackingStatus = RigPartTrackingstatus.Tracked;
            }
            else
            {
                IsPinching = false;
            }
#endif
#endif
        }
        #endregion

        private void OnValidate()
        {
#if !XRHANDS_AVAILABLE
            Debug.LogError("[XRHandsHardwareHand] Unity XR Hands package is required for finger tracking");
#endif
        }
    }
#else
    public class XRHandsHardwareHand : HardwareHand {}
#endif
}
