using Fusion.XR.Shared.Utils;
using UnityEngine;
using UnityEngine.Events;

namespace Fusion.XR.Shared.Core.HardwareBasedGrabbing
{
    /**
     * Position based grabbable: will follow the grabber position accuratly while grabbed
     */
    public class Grabbable : MonoBehaviour, IGrabbable
    {
        [HideInInspector]
        public Vector3 localPositionOffset;
        [HideInInspector]
        public Quaternion localRotationOffset;
        public Grabber currentGrabber;
        [HideInInspector]
        public bool expectedIsKinematic = true;

        [Tooltip("Select a snapping point if you want to position the grabbable object at a specific position in the hand")]
        public Transform snappingPointTransform;

        [Tooltip("If false, it is only possible to grab a Grabbable previously hovered")]
        public bool allowedClosedHandGrabing = true;

        protected NetworkGrabbable networkGrabbable;
        [HideInInspector]
        public Rigidbody rb;

        [Tooltip("For object with a rigidbody, if true, apply hand velocity on ungrab")]
        public bool applyVelocityOnRelease = false;

        [Header("Events")]
        [Tooltip("Called only for the local grabber, when they may wait for authority before grabbing. onDidGrab will be called on all users")]
        public UnityEvent<GameObject> onWillGrab = new UnityEvent<GameObject>();
        [Tooltip("Called only for the local grabber, on ungrab")]
        public UnityEvent onUngrab = new UnityEvent();
        [Tooltip("Called only for the local grabber, on grab")]
        public UnityEvent onGrab = new UnityEvent();

        [Header("Advanced")]
        public bool pauseGrabbability = false;

        #region Velocity estimation
        // Velocity computation
        const int velocityBufferSize = 5;
        Vector3 lastPosition;
        Quaternion previousRotation;
        Vector3[] lastMoves = new Vector3[velocityBufferSize];
        Vector3[] lastAngularVelocities = new Vector3[velocityBufferSize];
        float[] lastDeltaTime = new float[velocityBufferSize];
        int lastMoveIndex = 0;

        protected virtual Vector3 Velocity
        {
            get
            {
                Vector3 move = Vector3.zero;
                float time = 0;
                for (int i = 0; i < velocityBufferSize; i++)
                {
                    if (lastDeltaTime[i] != 0)
                    {
                        move += lastMoves[i];
                        time += lastDeltaTime[i];
                    }
                }
                if (time == 0) return Vector3.zero;
                return move / time;
            }
        }

        protected virtual Vector3 AngularVelocity
        {
            get
            {
                Vector3 culmulatedAngularVelocity = Vector3.zero;
                int step = 0;
                for (int i = 0; i < velocityBufferSize; i++)
                {
                    if (lastDeltaTime[i] != 0)
                    {
                        culmulatedAngularVelocity += lastAngularVelocities[i];
                        step++;
                    }
                }
                if (step == 0) return Vector3.zero;
                return culmulatedAngularVelocity / step;
            }
        }

        #region IGrabbable
        public bool IsGrabbed => currentGrabber != null;

        public Vector3 LocalPositionOffset => localPositionOffset;

        public Quaternion LocalRotationOffset => localRotationOffset;

        public UnityEvent OnGrab => onGrab;

        public UnityEvent OnUngrab => onUngrab;

        public UnityEvent<GameObject> OnLocalUserGrab => onWillGrab;
        #endregion

        protected virtual void TrackVelocity()
        {
            lastMoves[lastMoveIndex] = transform.position - lastPosition;
            lastAngularVelocities[lastMoveIndex] = previousRotation.AngularVelocityChange(transform.rotation, Time.deltaTime);
            lastDeltaTime[lastMoveIndex] = Time.deltaTime;
            lastMoveIndex = (lastMoveIndex + 1) % 5;
            lastPosition = transform.position;
            previousRotation = transform.rotation;
        }

        void ResetVelocityTracking()
        {
            for (int i = 0; i < velocityBufferSize; i++) lastDeltaTime[i] = 0;
            lastMoveIndex = 0;
        }
        #endregion

        protected virtual void Awake()
        {
            networkGrabbable = GetComponent<NetworkGrabbable>();
            rb = GetComponent<Rigidbody>();
            if (networkGrabbable == null && rb != null)
            {
                expectedIsKinematic = rb.isKinematic;
            }
        }

        protected virtual void Update()
        {
            TrackVelocity();

            if (networkGrabbable == null || networkGrabbable.Object == null)
            {
                // We handle the following if we are not online (online, the Follow will be called by the NetworkGrabbable during FUN and Render)
                if (currentGrabber != null)
                {
                    // We use the rigPartPose instead of the transform, as in some cases (hand tracking), the hardware rig part root trasnform might not be properly positioned, but the RigPartPose garantee the position 
                    var grabberPose = currentGrabber.rigPart.RigPartPose;
                    Follow(followedTransformPosition: grabberPose.position, followedTransformRotation: grabberPose.rotation, localPositionOffset, localRotationOffset);
                }
            }
        }

        public virtual void Grab(Grabber newGrabber, Transform grabPointTransform = null)
        {
            if (onWillGrab != null) onWillGrab.Invoke(newGrabber.gameObject);

            if (snappingPointTransform != null)
            {
                transform.rotation = newGrabber.transform.rotation * Quaternion.Inverse(snappingPointTransform.localRotation);
                transform.position = newGrabber.transform.position - (snappingPointTransform.position - transform.position);
            }

            var grabberPose = newGrabber.rigPart.RigPartPose;
            // Find grabbable position/rotation in grabber referential
            (localPositionOffset, localRotationOffset) = TransformManipulations.UnscaledOffset(
                referenceTransformPosition: grabberPose.position, grabberPose.rotation,
                transformToOffset: transform);

            currentGrabber = newGrabber;

            if (networkGrabbable)
            {
                networkGrabbable.LocalGrab();
            }
            else
            {
                // We handle the following if we are not online (online, the DidGrab will be called by the NetworkGrabbable DidGrab, itself called on all clients by HandleGrabberChange when the grabber networked var has changed)
                LockObjectPhysics();
            }
            if (onGrab != null) onGrab.Invoke();
        }

        [SerializeField] bool deepDebug = false;
        public virtual void Ungrab()
        {
            if (deepDebug) Debug.LogError("G.Ungrab");
            currentGrabber = null;
            if (networkGrabbable)
            {
                networkGrabbable.LocalUngrab();
            }
            else
            {
                // We handle the following if we are not online (online, the DidGrab will be called by the NetworkGrabbable DidUngrab, itself called on all clients by HandleGrabberChange when the grabber networked var has changed)
                UnlockObjectPhysics();
            }
            if (onUngrab != null) onUngrab.Invoke();
        }

        public virtual void LockObjectPhysics()
        {
            // While grabbed, we disable physics forces on the object, to force a position based tracking
            if (rb) rb.isKinematic = true;
        }

        public virtual void UnlockObjectPhysics()
        {
            // We restore the default isKinematic state if needed
            if (rb) rb.isKinematic = expectedIsKinematic;

            // We apply release velocity if needed
            if (rb && rb.isKinematic == false && applyVelocityOnRelease)
            {
#if UNITY_6000_0_OR_NEWER
                rb.linearVelocity = Velocity;
#else
                rb.velocity = Velocity;
#endif
                rb.angularVelocity = AngularVelocity;
            }

            ResetVelocityTracking();
        }

        public void Follow(Transform followedTransform, Vector3 localPositionOffsetToFollowed, Quaternion localRotationOffsetTofollowed)
        {
            Follow(followedTransform.position, followedTransform.rotation, localPositionOffsetToFollowed, localRotationOffsetTofollowed);
        }

        public virtual void Follow(Vector3 followedTransformPosition, Quaternion followedTransformRotation, Vector3 localPositionOffsetToFollowed, Quaternion localRotationOffsetTofollowed)
        {
            if (pauseGrabbability) return;

            (transform.position, transform.rotation) = TransformManipulations.ApplyUnscaledOffset(
                referenceTransformPosition: followedTransformPosition, referenceTransformRotation: followedTransformRotation,
                offset: localPositionOffsetToFollowed,
                rotationOffset: localRotationOffsetTofollowed
            );
        }
    }

}
