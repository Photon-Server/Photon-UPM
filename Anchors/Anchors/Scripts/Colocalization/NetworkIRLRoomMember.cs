using Fusion;
using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Tools;
using System;
using System.Collections;
using UnityEngine;

/**
 * Handle IRL room presence of an user. Should be place on a network rig
 * 
 * Requires an IRLRoomManager in the scene
 */
public class NetworkIRLRoomMember : NetworkBehaviour, IColocalizationRoomProvider, IRLRoomMovingReferenceElement
{
    public const int MAX_ROOMID_LENGTH = 32;
    [Networked, OnChangedRender(nameof(OnRoomIdChange))]
    public NetworkString<_32> RoomId { get; set; }
    public INetworkRig rig;

    NetworkIRLRoomAnchor _roomAnchorToFollow = null;
    public NetworkIRLRoomAnchor RoomAnchorToFollow
    {
        get
        {
            return _roomAnchorToFollow;
        }

        set
        {
            _roomAnchorToFollowRigOffset = null;
            _roomAnchorToFollow = value;
        }
    }

    public enum RoomPresenceCause
    {
        ExplicitRoomIdChange,   // When the room id is simply changed
        InitializingRoomMerge,  // When the room id is changed due to 2 rooms being merged (during a colocation), and we are doing it
        FollowingRoomMerge,      // When the room id is changed due to 2 rooms being merged (during a colocation), and we are following someone else triggering it
    }

    [Networked]
    public RoomPresenceCause PresenceCause { get; set; }

    [Networked]
    public Vector3 PositionBeforeMergingRoom { get; set; }

    [Networked]
    public Quaternion RotationBeforeMergingRoom { get; set; }
    [Networked]
    public Vector3 PositionAfterMergingRoom { get; set; }

    [Networked]
    public Quaternion RotationAfterMergingRoom { get; set; }

    Pose? _roomAnchorToFollowRigOffset = null;

    [Tooltip("When an headset has been removed, its position in space when coming back might be erroneous. Check this to reset the user room (to prevent bad localization).  Should be set to true in most cases")]
    public bool leaveIRLRoomOnHDMReturn = true;

    #region IRLRoomMovingReferenceElement
    public Vector3 PositionBeforeMoveToPropagate => PositionBeforeMergingRoom;
    public Quaternion RotationBeforeMoveToPropagate => RotationBeforeMergingRoom;
    public Vector3 PositionAfterMoveToPropagate => PositionAfterMergingRoom;
    public Quaternion RotationAfterMoveToPropagate => RotationAfterMergingRoom;
    #endregion

    #region IColocalizationRoomProvider
    public string IRLRoomId => RoomId.ToString();
    #endregion

    IRLRoomManager _roomManager;
    Vector3 _realPosition;
    Quaternion _realRotation;
    bool _isHardwareRigPlacedAtTemporaryPosition = false;

#if OCULUS_SDK_AVAILABLE
    bool _roomResetRequired = false;
#endif

    private void Awake()
    {
        _roomManager = FindAnyObjectByType<IRLRoomManager>();
        rig = GetComponentInParent<INetworkRig>();
    }

    private void Start()
    {
#if OCULUS_SDK_AVAILABLE
        OVRManager.HMDMounted += OnOVRManagerHMDMounted;
        OVRManager.HMDUnmounted += OnOVRManagerHMDUnmounted;
#endif
    }

    public override void Spawned()
    {
        base.Spawned();
        if (Object.HasStateAuthority)
        {
            RoomId = GenerateGuid();
            PresenceCause = RoomPresenceCause.ExplicitRoomIdChange;
        }
        _roomManager.RegisterNetworkIRLRoomMember(this);
        _roomManager.OnNetworkIRLRoomMemberRoomChange(this, "");
    }

    public float minDistanceChangeToFollowAnchor = 0.01f;
    public float minAngleChangeToFollowAnchor = 1f;

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (RoomAnchorToFollow != null)
        {
            var rig = HardwareRigsRegistry.GetHardwareRig();
            if (rig != null)
            {
                if (RoomAnchorToFollow.ShouldNotBeFollowed)
                {
                    _roomAnchorToFollowRigOffset = null;
                }

                if (_roomAnchorToFollowRigOffset is Pose rigOffset)
                {
                    // Check distance, correct rig position only if error too large
                    var rigRotation = RoomAnchorToFollow.transform.rotation * rigOffset.rotation;
                    var rigPosition = RoomAnchorToFollow.transform.TransformPoint(rigOffset.position);
                    if (Vector3.Distance(rigPosition, rig.transform.position) > minDistanceChangeToFollowAnchor || Quaternion.Angle(rigRotation, rig.transform.rotation) > minAngleChangeToFollowAnchor)
                    {
                        _roomManager.LogEvent($"[NetworkIRLRoomMember] Reference anchor {RoomAnchorToFollow.AnchorId} " +
                            $"moved to {RoomAnchorToFollow.transform.position}/{RoomAnchorToFollow.transform.rotation}. " +
                            $"Changing rig position to follow it (ofset: {rigOffset.position}/{rigOffset.rotation})");
                        rig.transform.rotation = rigRotation;
                        rig.transform.position = rigPosition;
                    }
                }
                else
                {
                    //Store Rig offset to anchor
                    var positionOffset = RoomAnchorToFollow.transform.InverseTransformPoint(rig.transform.position);
                    var rotationOffset = Quaternion.Inverse(RoomAnchorToFollow.transform.rotation) * rig.transform.rotation;
                    _roomAnchorToFollowRigOffset = new Pose(positionOffset, rotationOffset);
                }
            }
        }

        UpdateRealPosition();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        _roomManager?.UnregisterNetworkIRLRoomMember(this);
    }

    public string GenerateGuid()
    {
        // 32 characters
        var guid = Guid.NewGuid().ToString("N");
        return guid;
    }

    void OnRoomIdChange(NetworkBehaviourBuffer previous)
    {
        string previousRoomId = GetPropertyReader<NetworkString<_32>>(nameof(RoomId)).Read(previous).ToString();

        _roomManager?.OnNetworkIRLRoomMemberRoomChange(this, previousRoomId);
    }

    #region Teporary moves handling
    public Vector3 LocalUserHardwareRigRealPosition
    {
        get
        {
            if (Object.HasStateAuthority == false)
            {
                throw new Exception("Should only be useed for the local user, not synchronized");
            }
            return _realPosition;
        }
    }

    public Quaternion LocalUserHardwareRigRealRotation
    {
        get
        {
            if (Object.HasStateAuthority == false)
            {
                throw new Exception("Should only be useed for the local user, not synchronized");
            }
            return _realRotation;
        }
    }

    void UpdateRealPosition()
    {
        if (_isHardwareRigPlacedAtTemporaryPosition == false)
        {
            var hardwareRig = HardwareRigsRegistry.GetHardwareRig();
            _realPosition = hardwareRig.transform.position;
            _realRotation = hardwareRig.transform.rotation;
        }
    }
    /// <summary>
    /// Move the hardware rig to a false position, storing the actual real position to restore it at the end of the frame. Useful for previewing local user move effects (while previewing room moves)
    /// </summary>
    public void TemporaryHardwareRigMove(Vector3 position, Quaternion rotation)
    {
        var hardwareRig = HardwareRigsRegistry.GetHardwareRig();
        StartCoroutine(RestoreRealPoseAtEndOfFrame(hardwareRig));
        hardwareRig.transform.rotation = rotation;
        hardwareRig.transform.position = position;
    }

    /// <summary>
    /// Store the desired hardware rig position. If a temporary move is running, make sure to restore the desired position at the end 
    /// </summary>
    public void OverrideHardwareRigPose(Vector3 position, Quaternion rotation)
    {
        if (_isHardwareRigPlacedAtTemporaryPosition)
        {
            _realPosition = position;
            _realRotation = rotation;
        }
        else
        {
            var hardwareRig = HardwareRigsRegistry.GetHardwareRig();
            hardwareRig.transform.rotation = rotation;
            hardwareRig.transform.position = position;
            _realPosition = position;
            _realRotation = rotation;
        }
    }

    IEnumerator RestoreRealPoseAtEndOfFrame(IHardwareRig hardwareRig)
    {
        if (_isHardwareRigPlacedAtTemporaryPosition == false)
        {
            _isHardwareRigPlacedAtTemporaryPosition = true;
            _realPosition = hardwareRig.transform.position;
            _realRotation = hardwareRig.transform.rotation;
        }
        yield return new WaitForEndOfFrame();
        hardwareRig.transform.rotation = _realRotation;
        hardwareRig.transform.position = _realPosition;
        _isHardwareRigPlacedAtTemporaryPosition = false;
    }
    #endregion

    #region Room change actions
    public void InitializingRoomMerge(string newRoomId, Vector3 positionBeforeMergingRoom, Quaternion rotationBeforeMergingRoom, Vector3 positionAfterMergingRoom, Quaternion rotationAfterMergingRoom)
    {
        PositionBeforeMergingRoom = positionBeforeMergingRoom;
        RotationBeforeMergingRoom = rotationBeforeMergingRoom;
        PositionAfterMergingRoom = positionAfterMergingRoom;
        RotationAfterMergingRoom = rotationAfterMergingRoom;
        // Will trigger on change, that may read merge position info, hence the need to do it before changing room
        ChangeRoomId(newRoomId, changeCause: NetworkIRLRoomMember.RoomPresenceCause.InitializingRoomMerge);
    }

    public void FollowingRoomMerge(string targetRoomId)
    {
        ChangeRoomId(targetRoomId, changeCause: NetworkIRLRoomMember.RoomPresenceCause.FollowingRoomMerge);

    }
    #endregion

    public void ChangeRoomId(NetworkString<_32> roomId, RoomPresenceCause changeCause = RoomPresenceCause.ExplicitRoomIdChange)
    {
        if (Object.HasStateAuthority == false)
        {
            Debug.LogError("Cannot set RoomId on anchors not owned");
            return;
        }

        var previousRoomId = RoomId;
        RoomId = roomId;

        PresenceCause = changeCause;

        // We immediatly notify the manager, without waiting for the change event
        // This way, it can move anchors only related to this user to the same room, to avoid, when all anchors are visible at the same time, going back and forth between a previous room (that should in fact be merged with the new one) and the new one
        _roomManager?.OnNetworkIRLRoomMemberRoomChange(this, previousRoomId.ToString());
    }


#if OCULUS_SDK_AVAILABLE
    [ContextMenu("OnOVRManagerHMDMounted")]
    private void OnOVRManagerHMDMounted()
    {
        if (_roomResetRequired && leaveIRLRoomOnHDMReturn)
        {
            if (Object && Object.HasStateAuthority)
            {
                var roomId = RoomId.ToString();
                Debug.Log("OnOVRManagerHMDMounted");
                _roomManager.LogEvent("The headset has been removed. reseting the user room (to prevent bad localization due to the headset recomputing its position)");
                RoomId = GenerateGuid();

                if (string.IsNullOrEmpty(roomId) == false)
                {
                    PresenceCause = RoomPresenceCause.ExplicitRoomIdChange;
                }
            }
        }
    }

    [ContextMenu("OnOVRManagerHMDUnmounted")]
    private void OnOVRManagerHMDUnmounted()
    {
        Debug.Log("OnOVRManagerHMDUnmounted: we will reset the user room on user return, as the MR positioning might be lost, hence invalidating the colocalization");
        _roomResetRequired = true;
    }
#endif
}
