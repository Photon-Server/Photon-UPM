using Fusion.Addons.Meta.HandsSync;
using Fusion.XR.Shared.Base;
using Fusion.XR.Shared.Core;
using UnityEngine;

namespace Fusion.Addons.Meta
{
#if OCULUS_SDK_AVAILABLE
    [RequireComponent(typeof(OVRSkeleton))]
    public class MetaBridgeHardwareHand : HardwareHand, IGrabbingProvider
    {
        [HideInInspector]
        public OVRSkeletonBonesCollecter ovrSkeletonBonesCollecter;
        [HideInInspector]
        public OVRSkeleton ovrSkeleton;
        [HideInInspector]
        public OVRHand ovrHand;

        public override Pose WorldIndexTipPose => ovrSkeletonBonesCollecter.IndexTipPose;
        public override Pose WorldWristPose => ovrSkeletonBonesCollecter.WristPose;

        #region IGrabbingProvider
        public bool IsGrabbing => ovrHand != null ? ovrHand.GetFingerIsPinching(OVRHand.HandFinger.Index) : false;
        #endregion

        [Header("Hand tracking loss handling")]
        public bool shouldFixTrackingLossJumps = true;
        public float invalidPositionJumpThreshold = 0.2f;
        public float maxInvalidPoseDuration = 0.15f;
        public bool logHandTrackingLossFixEvents = false; 

        bool lastPositionStored = false;
        Pose lastLocalPose = default;
        float invalidPoseDetectionTime = -1;
        float invalidPositionJumpThresholdSqr = 0f;

        public override Pose RigPartPose
        {
            get
            {
                var pose = ovrSkeletonBonesCollecter?.WristPose ?? base.RigPartPose;
                Pose localPose = new Pose(
                    position: Rig.transform.InverseTransformPoint(pose.position),
                    rotation: Quaternion.Inverse(Rig.transform.rotation)*pose.rotation
                    );

                if (shouldFixTrackingLossJumps == false || IsPoseValid(localPose) )
                {
                    lastLocalPose = localPose;
                    lastPositionStored = true;
                }
                else
                {
                    if(logHandTrackingLossFixEvents) Debug.LogError($"Invalid pose: keeping last pose {lastLocalPose.position} instead of {localPose.position}");
                    pose = new Pose(
                        position: Rig.transform.TransformPoint(lastLocalPose.position),
                        rotation: Rig.transform.rotation * lastLocalPose.rotation
                        );
                }
                return pose;
            }
        }
        
        protected bool IsPoseValid(Pose localPose)
        {
            // When an object is occluding the hand position, the hand can still be tracked officially, but start to jump toward Vector3.zero (in SDK v83 at least)
            // Detecting such cases
            bool isValid = true;
            if (invalidPositionJumpThresholdSqr == 0)
            {
                invalidPositionJumpThresholdSqr = invalidPositionJumpThreshold * invalidPositionJumpThreshold;
            }
            // Note: if TrackingStatus is already RigPartTrackingstatus.NotTracked, no need to try to fix the position
            if (lastPositionStored && (localPose.position - lastLocalPose.position).sqrMagnitude > invalidPositionJumpThresholdSqr)
            {
                isValid = false;
                if (invalidPoseDetectionTime == -1)
                {
                    if (logHandTrackingLossFixEvents) Debug.LogError("Hand position jump: recent loss of detection ?");
                    invalidPoseDetectionTime = Time.time;
                }
            }
            if (isValid)
            {
                if (invalidPoseDetectionTime != -1)
                {
                    if (logHandTrackingLossFixEvents) Debug.LogError("End of invalid pose detection");
                }
                invalidPoseDetectionTime = -1;
            }
            else if ((Time.time - invalidPoseDetectionTime) >= maxInvalidPoseDuration)
            {
                if (logHandTrackingLossFixEvents) Debug.LogError($"Invalid for too long: revalidate it anyway {lastLocalPose.position} => {localPose.position}");
                isValid = true;
            }
            return isValid;
        }

        public override void UpdateTrackingStatus()
        {
            base.UpdateTrackingStatus();
            var newTrackingStatus = ovrSkeletonBonesCollecter?.CurrentHandTrackingMode == Addons.HandsSync.HandTrackingMode.FingerTracking ? RigPartTrackingstatus.Tracked : RigPartTrackingstatus.NotTracked;
            
            if(newTrackingStatus == RigPartTrackingstatus.Tracked && TrackingStatus == RigPartTrackingstatus.NotTracked)
            {
                if (logHandTrackingLossFixEvents && invalidPoseDetectionTime != -1)
                {
                    Debug.LogError("Tracked received. Reset invalidPoseDetectionTime");
                }
                invalidPoseDetectionTime = -1;
                lastPositionStored = false;

            }
            TrackingStatus = newTrackingStatus;

            if (TrackingStatus == RigPartTrackingstatus.NotTracked)
            { 
                // Not tracked: if we where supposing a position jump, we can stop now (we were probably right then)
                if (logHandTrackingLossFixEvents && invalidPoseDetectionTime != -1)
                {
                    Debug.LogError("NotTracked received. Reset invalidPoseDetectionTime");
                }
                invalidPoseDetectionTime = -1;
            }
        }

        protected override void Awake()
        {
            base.Awake();
            // We let the meta rig deal with gameobject status
            disabledGameObjectWhenNotTracked = false;

            ovrSkeletonBonesCollecter = GetComponent<OVRSkeletonBonesCollecter>();
            ovrHand = GetComponent<OVRHand>();
            ovrSkeleton = GetComponent<OVRSkeleton>(); 

            switch (ovrSkeleton.GetSkeletonType())
            {
                case OVRSkeleton.SkeletonType.HandLeft:
                case OVRSkeleton.SkeletonType.XRHandLeft:
                    Side = RigPartSide.Left;
                    break;
                case OVRSkeleton.SkeletonType.HandRight:
                case OVRSkeleton.SkeletonType.XRHandRight:
                    Side = RigPartSide.Right;
                    break;
            }

            if (ovrSkeletonBonesCollecter == null)
            {
                ovrSkeletonBonesCollecter = gameObject.AddComponent<OVRSkeletonBonesCollecter>();
            }
        }

        protected override void Update()
        {
            base.Update();
            PositionHandBoneFollowers();
        }
    }
#else
    public class MetaBridgeHardwareHand : HardwareHand { }
#endif
}
