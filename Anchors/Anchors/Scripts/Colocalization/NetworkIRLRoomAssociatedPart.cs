using Fusion;
using Fusion.Addons.AnchorsAddon.Colocalization;
using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Represent an element in the room associated to a specific user (typically, a piece of furniture detected)
/// 
/// Allow to preview a room move
/// This class does not handle the actual room move applications, just the preview:
/// - members should be moved by IRLRoomManager colocalization or followed anchors
/// - anchors should be moved by IRLRoomManager
/// - IRLRoom furniture should be despawned and respawned on colocalization, or attachedToLocalUser should be set to true so that they will follow the user
/// </summary>
[DefaultExecutionOrder(NetworkIRLRoomAssociatedPart.EXECUTION_ORDER)]
public class NetworkIRLRoomAssociatedPart : NetworkBehaviour
{
    public const int EXECUTION_ORDER = 10_000;

    public enum PartType
    {
        Undefined,
        Table,
        Wall,
        Ceiling,
        Floor,
        Screen,
        Bed,
        Other20, Other19, Other18, Other17, Other16, Other15, Other14, Other13, Other12, Other11,
        Other10, Other9, Other8, Other7, Other6, Other5, Other4, Other3, Other2, Other1,
        User,
        Other
    }

    public PartType partType;
    [Tooltip("If true, and if previewRoomRequesterMoves is true, and if this is owned by the local user, the hardware rig will follow the preview movements (and be reset to its position after the frame). Avoid enabling it if you have other locomotion logic")]
    [DrawIf(nameof(partType), (long)PartType.User, Hide = true)]
    public bool moveHardwareRigForLocalUserPreview = true;

    [Header("Position synchronisation configuration")]
    [Tooltip("If true, will ensure to preserve the offset with its local user")]
    public bool attachedToLocalUser = false;
    public bool previewRoomRequesterMoves = true;

    [Header("Layer")]
    public bool applyLayerIfNotStateAuthority = false;
    public string layerToApplyName = "";

    [Networked]
    public NetworkIRLRoomMember ReferenceRoomMember { get; set; }
    protected IRLRoomManager roomManager;

    [Header("Visualisation")]
    public bool adaptRendersToRoomManagerMode = false;
    public List<Renderer> renderers = new List<Renderer>();

    [Header("Debug - Simulated rooms")]
    [Tooltip("Fake room id to simulate additional rooms")]
    public string fakeRoomId = null;
    public bool fakeOwnedByMainRoomMember = false;



    public string RoomId => string.IsNullOrEmpty(fakeRoomId) == false ? fakeRoomId : ReferenceRoomMember?.RoomId.ToString();

    public bool IsOwnedByRoomMainMember => fakeOwnedByMainRoomMember || (roomManager != null && ReferenceRoomMember == roomManager.RoomMainMember(RoomId));

    protected virtual void Awake()
    {
        roomManager = FindAnyObjectByType<IRLRoomManager>();
        if (roomManager == null)
        {
            Debug.LogError("Missing IRLRoomManager");
        }
        if (renderers == null || renderers.Count == 0)
        {
            renderers = new List<Renderer>(GetComponentsInChildren<Renderer>());
        }
        _nt = GetComponent<NetworkTransform>();
    }


    public UnityEvent<NetworkIRLRoomAssociatedPart> onPoseUpdate = new UnityEvent<NetworkIRLRoomAssociatedPart>();
    public UnityEvent onPreviewing = new UnityEvent();
    public UnityEvent onStopPreviewing = new UnityEvent();

    bool _lastDisplayState = true;
    NetworkTransform _nt;
    Vector3 _lastPosition;
    Quaternion _lastRotation;
    bool _isLocalUserNetworkRig = false; 
    NetworkIRLRoomMember _localMember = null;
    Vector3 _offsetPositionToLocalMember;
    Quaternion _offsetRotationToLocalMember;

    public override void Spawned()
    {
        base.Spawned();
        if (Object.HasStateAuthority == false && applyLayerIfNotStateAuthority)
        {
            LayerUtils.ApplyLayer(gameObject, layerToApplyName, true);
        }
        RegisterToIRLRoomManager();

        if(string.IsNullOrEmpty(fakeRoomId) == false && attachedToLocalUser)
        {
            Debug.LogError("Fake rooms' associated parts should usally not be attached to local user");
        }
        if (partType == PartType.User && HasStateAuthority)
        {
            var networkRig = GetComponent<NetworkRig>();
            if (networkRig)
            {
                _isLocalUserNetworkRig = true;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        bool localUserDetectedThisFrame = false;
        if (Object.HasStateAuthority)
        {
            if (_localMember == null)
            {
                if (roomManager == null)
                {
                    Debug.LogError($"[{name}] No room manager");
                    return;
                }
                _localMember = roomManager.localNetworkIRLRoomMember;
                if (_localMember != null)
                {
                    localUserDetectedThisFrame = true;
                }
            }
        }

        if (Object.HasStateAuthority)
        {
            if (localUserDetectedThisFrame)
            {
                ReferenceRoomMember = _localMember;
                RegisterToIRLRoomManager();
            }
        }

        if (Object.HasStateAuthority && attachedToLocalUser)
        {
            if (localUserDetectedThisFrame)
            {
                _offsetPositionToLocalMember = _localMember.transform.InverseTransformPoint(transform.position);
                _offsetRotationToLocalMember = Quaternion.Inverse(_localMember.transform.rotation) * transform.rotation;
            }
            if (_localMember)
            {
                var expectedPosition = _localMember.transform.TransformPoint(_offsetPositionToLocalMember);
                var expectedRotation = _localMember.transform.rotation * _offsetRotationToLocalMember;
                if ((expectedPosition - transform.position).sqrMagnitude > 0.01f)
                {
                    // We teleport to ensure that it is a direct move, without intermediary positions
                    //Debug.LogError($"Teleport NetworkIRLRoomAssociatedPart {name} {transform.position} -> {expectedPosition}");
                    _nt.Teleport(expectedPosition, expectedRotation);
                    OnPoseUpdate();
                }
                else
                {
                    // In case of just a rotation or a very small move
                    transform.position = expectedPosition;
                    transform.rotation = expectedRotation;
                }
            }
        }
    }

    void OnPoseUpdate()
    {
        _lastPosition = transform.position;
        _lastRotation = transform.rotation;
        if (onPoseUpdate != null) onPoseUpdate.Invoke(this);
        roomManager.OnAssociatedPartPoseChange(this);
    }

    public enum PreviewState
    {
        NoPreview,
        Previewing,
        PreviewCancelledAsMoveAlreadyOccured,
    }

    public PreviewState previewState = PreviewState.NoPreview;
    Vector3 positionAtPreviewStart;
    Quaternion rotationAtPreviewStart;

    string registeredRoomId = null;

    void RegisterToIRLRoomManager()
    {
        string roomId = null;
        if (string.IsNullOrEmpty(fakeRoomId) == false)
        {
            roomId = fakeRoomId;
        }
        else if(ReferenceRoomMember != null)
        {
            roomId = ReferenceRoomMember.RoomId.ToString();
        }
        if (string.IsNullOrEmpty(roomId)) return;
        if (registeredRoomId == roomId) return;
        UnregisterToIRLRoomManager();
        registeredRoomId = roomId;
        roomManager.OnAssociatedPartJoiningRoom(this, roomId);
    }

    void UnregisterToIRLRoomManager()
    {
        if (string.IsNullOrEmpty(registeredRoomId)) return;
        roomManager.OnAssociatedPartLeavingRoom(this, registeredRoomId);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);
        if (registeredRoomId != null) UnregisterToIRLRoomManager();
    }

    public override void Render()
    {
        base.Render();

        if ((_lastPosition - transform.position).sqrMagnitude > 0.01f)
        {
            OnPoseUpdate();
        }

        RegisterToIRLRoomManager();

        if (ReferenceRoomMember == null)
        {
            if (string.IsNullOrEmpty(fakeRoomId) && registeredRoomId != null)
            {
                UnregisterToIRLRoomManager();
            }
        }
        else
        {
            var roomId = ReferenceRoomMember.RoomId.ToString();
            if (roomId != registeredRoomId)
            {
                RegisterToIRLRoomManager();
            }
            HandleRoomMovePreview();
        }

        if (adaptRendersToRoomManagerMode)
        {
            AdaptDisplay();
        }
    }

    void Previewing(NetworkIRLRoomMoveRequester moveRequester)
    {
        StartCoroutine(RestoreState(transform, transform.position, transform.rotation));
        roomManager.MoveTransformToFollowSameRoomReferenceElementMove(moveRequester, transform);
        if (onPreviewing != null) onPreviewing.Invoke();

        if (moveHardwareRigForLocalUserPreview && _isLocalUserNetworkRig)
        {
            roomManager.localNetworkIRLRoomMember.TemporaryHardwareRigMove(transform.position, transform.rotation);
        }
    }

    void StopPreviewing()
    {
        if (onStopPreviewing != null) onStopPreviewing.Invoke();
    }

    void HandleMoveRequesterPreview(NetworkIRLRoomMoveRequester moveRequester)
    {
        if (previewState == PreviewState.NoPreview)
        {
            previewState = PreviewState.Previewing;
            positionAtPreviewStart = transform.position;
            rotationAtPreviewStart = transform.rotation;
        }
        else if (previewState == PreviewState.Previewing)
        {
            // Maybe the request has already been applied: no need to apply an addition preview anymore then
            if (positionAtPreviewStart != transform.position || rotationAtPreviewStart != transform.rotation)
            {
                StopPreviewing();
                previewState = PreviewState.PreviewCancelledAsMoveAlreadyOccured;
            }
        }

        if (previewState == PreviewState.Previewing)
        {
            // Preview running
            Previewing(moveRequester);
        }
    }

    void HandleRoomMovePreview()
    {
        if (ReferenceRoomMember == null) return;

        var roomId = ReferenceRoomMember.RoomId.ToString();

        if (roomManager.knowRoomByRoomIds.ContainsKey(roomId) && roomManager.knowRoomByRoomIds[roomId].moveRequester != null)
        {
            var moveRequester = roomManager.knowRoomByRoomIds[roomId].moveRequester;

            if (previewRoomRequesterMoves && moveRequester.ShouldPreview)
            {
                HandleMoveRequesterPreview(moveRequester);
            }
            else
            {
                if (previewState == PreviewState.Previewing)
                {
                    StopPreviewing();
                }
                previewState = PreviewState.NoPreview;

                // No preview of a pending move. If the element is attached to the local user, we can extrapolate its move normally
                if (Object.HasStateAuthority && attachedToLocalUser && _localMember)
                {
                    transform.position = _localMember.transform.TransformPoint(_offsetPositionToLocalMember);
                    transform.rotation = _localMember.transform.rotation * _offsetRotationToLocalMember;
                }
            }
        }
        else
        {
            previewState = PreviewState.NoPreview;
        }
    }

    void AdaptDisplay()
    {
        bool shoulDisplay = true;

        var referenceRoomMemberRoomId = RoomId;
        var localUserRoomId = roomManager.localNetworkIRLRoomMember?.RoomId.ToString() ?? "";
        var isRemoteRoom = referenceRoomMemberRoomId != localUserRoomId;

        if (roomManager.roomAssociatedPartDisplayMode == IRLRoomManager.NetworkIRLRoomAssociatedPartDisplayMode.Never)
        {
            shoulDisplay = false;
        }
        else if (roomManager.roomAssociatedPartDisplayMode != IRLRoomManager.NetworkIRLRoomAssociatedPartDisplayMode.Always && roomManager.roomAssociatedPartDisplayMode != IRLRoomManager.NetworkIRLRoomAssociatedPartDisplayMode.LocalRoomOnly && isRemoteRoom == false)
        {
            shoulDisplay = false;
        }
        else if (roomManager.roomAssociatedPartDisplayMode == IRLRoomManager.NetworkIRLRoomAssociatedPartDisplayMode.RemoteRoomOnly && isRemoteRoom == false)
        {
            shoulDisplay = false;
        }
        else if (roomManager.roomAssociatedPartDisplayMode == IRLRoomManager.NetworkIRLRoomAssociatedPartDisplayMode.LocalRoomOnly && isRemoteRoom == true)
        {
            shoulDisplay = false;
        }
        else if (roomManager.roomAssociatedPartDisplayMode == IRLRoomManager.NetworkIRLRoomAssociatedPartDisplayMode.MainPlayerInRemoteRoomOnly && isRemoteRoom)
        {
            if (ReferenceRoomMember != roomManager.RoomMainMember(referenceRoomMemberRoomId) && string.IsNullOrEmpty(fakeRoomId) == true)
            {
                shoulDisplay = false;
            }
        }

        if (shoulDisplay != _lastDisplayState)
        {
            foreach (var r in renderers) r.enabled = shoulDisplay;
            _lastDisplayState = shoulDisplay;
        }
    }

    IEnumerator RestoreState(Transform transformToRestore, Vector3 p, Quaternion r)
    {
        yield return new WaitForEndOfFrame();
        transformToRestore.rotation = r;
        transformToRestore.position = p;
    }
}
