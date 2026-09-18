using Fusion;
using Fusion.Addons.AnchorsAddon.Colocalization;
using Fusion.XR.Shared.Core;
using System;
using UnityEngine;

public class GrabbableMoveRequester : NetworkBehaviour, IStateAuthorityChanged
{
    public bool blockRequestForLocalUserRoom = true;

    INetworkGrabbable grabbable;
    NetworkIRLRoomMoveRequester requester;
    IRLRoomManager roomManager;
    [SerializeField] TMPro.TMP_Text roomText;

    [SerializeField] float extendedPreviewDuration = 2;
    float endOfExtendedPreview = -1;

    private void Awake()
    {
        roomManager = FindAnyObjectByType<IRLRoomManager>();
        grabbable = GetComponentInChildren<INetworkGrabbable>();
        requester = GetComponent<NetworkIRLRoomMoveRequester>();
        grabbable.OnGrab.AddListener(OnGrab);
        grabbable.OnUngrab.AddListener(OnUngrab);
    }

    private void OnUngrab()
    {
        if (blockRequestForLocalUserRoom && requester.RoomId == roomManager.localNetworkIRLRoomMember?.RoomId)
        {
            requester.DidMoveWithoutRequest();
            requester.Status = NetworkIRLRoomMoveRequester.RequestStatus.RequestAllowed;
        }
        else
        {
            requester.Status = NetworkIRLRoomMoveRequester.RequestStatus.RequestAllowed;
            if (extendedPreviewDuration != 0)
            {
                endOfExtendedPreview = Time.time + extendedPreviewDuration;
            }
            else
            {
                requester.ShouldPreview = false;
            }
        }
    }

    private void OnGrab()
    {
        endOfExtendedPreview = -1;
        ConfigureRequesterWhileGrabbed();
    }

    void ConfigureRequesterWhileGrabbed()
    {
        if (blockRequestForLocalUserRoom && requester.RoomId == roomManager.localNetworkIRLRoomMember?.RoomId)
        {
            requester.Status = NetworkIRLRoomMoveRequester.RequestStatus.RequestsBlocked;
        }
        else
        {
            requester.Status = NetworkIRLRoomMoveRequester.RequestStatus.RequestPostponed;
            requester.ShouldPreview = true;
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
        if (grabbable.IsGrabbed)
        {
            ConfigureRequesterWhileGrabbed();
        }
    }

    public override void Render()
    {
        base.Render();
        if (Object.HasStateAuthority && requester.Status != NetworkIRLRoomMoveRequester.RequestStatus.RequestPostponed && endOfExtendedPreview  != -1)
        {
            if (endOfExtendedPreview <= Time.time)
            {
                // End of extended preview
                endOfExtendedPreview = -1;
                requester.ShouldPreview = false;
            }
        }
        
        var requesterRoomId = requester.RoomId.ToString();
        if (roomText)
        {
            if(requesterRoomId == roomManager.localNetworkIRLRoomMember?.RoomId.ToString())
            {
                roomText.text = "Move requester for\nthis room";
            }
            else 
            {
                // TODO Have a user readable text
                roomText.text = "Move requester for\n" + requesterRoomId;
            }
        }
    }

    public void StateAuthorityChanged()
    {
        endOfExtendedPreview = -1;
    }
}
