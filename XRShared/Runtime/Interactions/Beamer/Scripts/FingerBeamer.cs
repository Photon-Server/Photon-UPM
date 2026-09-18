using Fusion.XR.Shared.Base;
using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Locomotion;
using UnityEngine;
using static Fusion.XR.Shared.FingerBeamer;

namespace Fusion.XR.Shared
{
    /// <summary>
    /// Control a beamer with finger tracking
    /// User should form a "L" shape with their thumb and index, with the fist closed, to enabled the beam.
    /// It will stay activated while the thumb is up (the index can be closed if needed once the beam is active)
    /// If the beam was active when the thumb is lowered, the beamer can trigger a teleport (otherwise, a CancelHit is called to prevent it - for instance if the fist is opened)
    /// </summary>
    public class FingerBeamer : MonoBehaviour
    {
        HardwareHand hardwareHand;
        RayBeamer beamer;

        public float poseDurationBeforeBeamActivation = 0;
        public float cooldownBeforeBeamReactivation = 0.3f;
        [Tooltip("Offset for the beam source to the index finger base (for left hand, for right hand, the x axis value will be reversed)")]
        public Vector3 offsetInIndexBaseReferential = new Vector3(0.015f,0.01f,0);

        public float maxThumbToWristSideAngle = 60;
        public float maxIndexToWristAngle = 30;
        public float maxOtherFingerTipsDistanceToPalm = 0.07f;
        [Tooltip("If false, the index base direction will be use - can lead to small angle offset if the index is raised slowly")]
        public bool  useWristDirection = true;

        public enum FingersShape
        {
            Unidentified,
            FistOpened,
            Thumbup,
            IndexPointing,
            Lshape
        }

        public FingersShape fingersShape = FingersShape.Unidentified;

        [Header("Debug")]
        public GameObject debugPrefab;
        float _thumbToWristSideAngle;
        float _indexToWristAngle;
        GameObject _thumbDebug;
        GameObject _indexDebug;
        GameObject _wristDebug;
        GameObject _indexBaseDebug;
        Pose _indexPose;
        Pose _indexBasePose;
        Pose _wristPose;

        Quaternion _beamRotationToWrist;


        private void Awake()
        {
            hardwareHand = GetComponent<HardwareHand>();
            if (hardwareHand == null)
            {
                Debug.LogError("FingerBeamer should be placed next to a Hardwarehand component (typically XRHandsHarwareHand)");
                enabled = false;
            }

            beamer = GetComponentInParent<RayBeamer>();
            if (beamer == null)
            {
                beamer = GetComponentInChildren<RayBeamer>();
            }

            if (beamer == null)
            {
                Debug.LogError("Missing beamer");
            }
            else
            {
                beamer.useRayActionInput = false;
            }

            if (debugPrefab)
            {
                _thumbDebug = GameObject.Instantiate(debugPrefab);
                _indexDebug = GameObject.Instantiate(debugPrefab);
                _wristDebug = GameObject.Instantiate(debugPrefab);
                _indexBaseDebug = GameObject.Instantiate(debugPrefab);
            }
        }

        float _wasActiveStart = -1;
        bool _wasActive = false;
        float _lastBeamdesactivation = -1;
        private void Update()
        {
            DetermineFingerState();
            AdaptBeamer();
        }

        void AdaptBeamer() {
            bool isBeamActive = false;

            // If the hand is not tracked, or the fist is not closed, we cancel the beam
            if (hardwareHand.TrackingStatus != Core.RigPartTrackingstatus.Tracked || (fingersShape != FingersShape.IndexPointing && fingersShape != FingersShape.Lshape))
                {
                if (beamer)
                {
                    beamer.CancelHit();
                    beamer.isRayEnabled = false;
                }
            }
            else if (fingersShape == FingersShape.Unidentified)
            {
                if (beamer)
                {
                    beamer.isRayEnabled = false;
                }
            }
            else
            {
                bool shouldActivateBeam = fingersShape == FingersShape.Lshape;
                if (shouldActivateBeam)
                {
                    bool isPoseActiveForLongEnough = poseDurationBeforeBeamActivation == 0 || _wasActiveStart == -1 || (Time.time - _wasActiveStart) > poseDurationBeforeBeamActivation;
                    bool isDesactivationOldEnough = cooldownBeforeBeamReactivation == 0 || _lastBeamdesactivation == -1 ||(Time.time - _lastBeamdesactivation) > cooldownBeforeBeamReactivation;
                    if (isPoseActiveForLongEnough && isDesactivationOldEnough)
                    {
                        isBeamActive = true;
                    }
                }

                if (isBeamActive && _wasActive == false)
                {
                    // save the beam rotation, to keep it stable
                    _beamRotationToWrist = Quaternion.Inverse(_wristPose.rotation) * _indexBasePose.rotation;
                }
                var beamerPose = new Pose(_indexBasePose.position, _wristPose.rotation);

                if(useWristDirection == false)
                {
                    beamerPose.rotation = beamerPose.rotation * _beamRotationToWrist;
                }

                if (isBeamActive)
                {
                    var offsetToApply = offsetInIndexBaseReferential;
                    var isRightSideHand = hardwareHand.Side == RigPartSide.Right;
                    if (isRightSideHand)
                    {
                        offsetToApply.x = -offsetToApply.x;
                    }
                    beamer.isRayEnabled = true;
                    beamer.origin.position = beamerPose.position + beamerPose.rotation * offsetToApply;
                    beamer.origin.rotation = beamerPose.rotation;
                }
                else
                {
                    beamer.isRayEnabled = false;
                }
                                


                if (shouldActivateBeam)
                {
                    if (_wasActiveStart == -1)
                    {
                        _wasActiveStart = Time.time;
                    }
                }
                else
                {
                    _wasActiveStart = -1;
                }
            }
            if(_wasActive && isBeamActive == false)
            {
                _lastBeamdesactivation = Time.time;
            }
            else if (isBeamActive)
            {
                _lastBeamdesactivation = -1;
            }
            _wasActive = isBeamActive;
        }


        void DetermineFingerState() {
            fingersShape = FingersShape.Unidentified;
            if (hardwareHand.TrackingStatus != Core.RigPartTrackingstatus.Tracked)
            {
                return;
            }
            var isRightSideHand = hardwareHand.Side == RigPartSide.Right;

            var thumbPose = hardwareHand.WorldThumbTipPose;
            _wristPose = hardwareHand.WorldWristPose;
            _indexBasePose = hardwareHand.WorldIndexBasePose;
            _indexPose = hardwareHand.WorldIndexTipPose;

            // Debug 
            if (_thumbDebug) _thumbDebug.transform.SetPositionAndRotation(thumbPose.position, thumbPose.rotation);
            if (_indexDebug) _indexDebug.transform.SetPositionAndRotation(_indexPose.position, _indexPose.rotation);
            if (_indexBaseDebug) _indexBaseDebug.transform.SetPositionAndRotation(_indexBasePose.position, _indexBasePose.rotation);
            if (_wristDebug) _wristDebug.transform.SetPositionAndRotation(_wristPose.position, _wristPose.rotation);

            // Check if the fist is closed
            if (IsFistClosed() == false)
            {
                fingersShape = FingersShape.FistOpened;
                return;
            }

            _thumbToWristSideAngle = NormalisedAngle(Vector3.Angle(thumbPose.rotation * Vector3.forward, _wristPose.rotation * Vector3.right));
            _indexToWristAngle = NormalisedAngle(Vector3.Angle(_indexPose.rotation * Vector3.forward, _wristPose.rotation * Vector3.forward));

            bool indexPointing = Mathf.Abs(_indexToWristAngle) < maxIndexToWristAngle;

            if (indexPointing)
            {
                fingersShape = FingersShape.IndexPointing;
            }

            bool thumbIsup = isRightSideHand ? Mathf.Abs(Mathf.Abs(_thumbToWristSideAngle) - 180) < maxThumbToWristSideAngle : Mathf.Abs(Mathf.Abs(_thumbToWristSideAngle)) < maxThumbToWristSideAngle;
            if (thumbIsup)
            {
                fingersShape = indexPointing ? FingersShape.Lshape : FingersShape.Thumbup;
            }
        }

        float NormalisedAngle(float angle)
        {
            return Mathf.Repeat(angle + 180f, 360f) - 180f;
        }

        bool IsFistClosed()
        {
            var palmPose = hardwareHand.WorldPalmPose;
            if (Vector3.Distance(hardwareHand.WorldMiddleTipPose.position, palmPose.position) > maxOtherFingerTipsDistanceToPalm)
                return false;
            if (Vector3.Distance(hardwareHand.WorldLittleTipPose.position, palmPose.position) > maxOtherFingerTipsDistanceToPalm)
                return false;
            if (Vector3.Distance(hardwareHand.WorldRingTipPose.position, palmPose.position) > maxOtherFingerTipsDistanceToPalm)
                return false;
            return true;
        }
    }
}
