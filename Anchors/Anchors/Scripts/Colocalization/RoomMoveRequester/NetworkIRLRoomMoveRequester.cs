using Fusion;
using Fusion.XR.Shared.Core;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Fusion.Addons.AnchorsAddon.Colocalization
{
    /// <summary>
    /// Allow to move a whole IRL room.
    /// This is done through the InitializingRoomMove call, that plan a room move, so that this member rig will have a new position/rotation. All members and anchors in the room will then be moved by the IRLRoomManager (first the anchors, then the members will follow the anchors)
    /// 
    /// This method can be either:
    /// - called manually from another script
    /// - or the gameobject containing this component can be moved to trigger the room movement, once the position has not changed for delayBeforeStabilizingIncomingPosition seconds.
    /// 
    /// If the object needs to be moved without triggering a room locomotion, set Status to RequestsBlocked first.
    /// 
    /// It is also possible to prevent the room to move immediatly, while allowing the NetworkIRLRoomAssociatedPart of the room (where previewRoomRequesterMoves is true) to show a preview position of their future possible when the move will be triggered.
    /// To do that:
    /// - first set the status to RequestPostponed
    /// - you can now move the requester, and the room associated parts will appear at their future position
    /// - then, to trigger the actual move, set the Status to RequestAllowed
    /// See GrabbableMoveRequester for an example of this usage.
    /// </summary>
    public class NetworkIRLRoomMoveRequester : NetworkBehaviour, IRLRoomMovingReferenceElement, IStateAuthorityChanged
    {
        [Networked, OnChangedRender(nameof(OnRoomIdChange))]
        public NetworkString<_32> RoomId { get; set; }

        [Networked]
        public Vector3 PositionBeforeMoveToPropagate { get; set; }

        [Networked]
        public Quaternion RotationBeforeMoveToPropagate { get; set; }
        [Networked]
        public Vector3 PositionAfterMoveToPropagate { get; set; }

        [Networked]
        public Quaternion RotationAfterMoveToPropagate { get; set; }

        [Networked, OnChangedRender(nameof(OnMoveCounterChange))]
        public int MoveCounter { get; set; } = 0;

        [Tooltip("If an axis value is not zero, the requested position won't change on this axis")]
        public Vector3 positionLockAxis = new Vector3(0, 1, 0);
        [Tooltip("If an axis value is not zero, the requested position won't change on this axis")]
        public Vector3 rotationLockAxis = new Vector3(1, 0, 1);

        public enum RequestStatus { 
            RequestAllowed,                 // Normal state: watches for target moves to potential trigger a request    
            RequestPostponed,               // Expected request position are saved, but the actual move request is not sent to move the room (useful for previews)
            RequestPlanned,               // A request should be send to move the room, we are first waiting a bit (delayBeforeStabilizingIncomingPosition) to be sure that the requester is not currently moving
            RequestSent,                    // The move request has just been sent
            RequestsBlocked                 // This component should not allow moves (relevant if an user in the room could move it, and it is not desired)
        }

        [Networked]
        public NetworkBool ShouldPreview { get; set; } = false;

        [Networked, OnChangedRender(nameof(OnStatusChange))]
        public RequestStatus Status { get; set; } = RequestStatus.RequestAllowed;

        [Tooltip("Transform to easily pass the target position and rotation (optional, will be automatically set)")]
        public Transform target;

        [Tooltip("A move requester position change will be consider stabilized after this delay, and will trigger its room's move")]
        public float delayBeforeStabilizingIncomingPosition = 1;

        public bool despawnWhenRoomAlreadycontainsARequester = true;
        public bool despawnWhenRoomIsEmpty = true;


        IRLRoomManager roomManager;

        Vector3 lastSentTargetPosition;
        Quaternion lastSentTargetRotation;
        float incomingPositionStabilisationTimeout = -1;
        float lastRequestSentTime = -1;

        string baseName = "";

        public UnityEvent onPlannedRequestApplied = new UnityEvent();

        private void Awake()
        {
            baseName = name;
            roomManager = FindAnyObjectByType<IRLRoomManager>();
            if (target == null) target = transform;
        }

        /// <summary>
        /// Plan a room move, so that this room's members rigs will have a new position/rotation
        /// All members and anchors in the room will then be moved by the IRLRoomManager (first the anchors, then the members will follow the anchors - the member will move directly if they don't follow an anchor)
        /// </summary>
        public void InitializingRoomMove(Vector3 positionBeforeMoveToPropagate, Quaternion rotationBeforeMoveToPropagate, Vector3 positionAfterMoveToPropagate, Quaternion rotationAfterMoveToPropagate)
        {
            PositionBeforeMoveToPropagate = positionBeforeMoveToPropagate;
            RotationBeforeMoveToPropagate = rotationBeforeMoveToPropagate;
            PositionAfterMoveToPropagate = positionAfterMoveToPropagate;
            RotationAfterMoveToPropagate = rotationAfterMoveToPropagate;
            MoveCounter = roomManager.MaxMoveCounterForRoomId(RoomId.ToString()) + 1;
        }

        void OnStatusChange()
        {
            roomManager?.ConsoleLog($"[NetworkIRLRoomMoveRequester] OnStatusChange {RoomId}: {Status} ({MoveCounter})");
        }

        void OnMoveCounterChange()
        {
            roomManager?.NetworkIRLRoomMoveRequesterMoveCounterChange(this);
        }

        public override void Spawned()
        {
            base.Spawned();
            if (Object.HasStateAuthority)
            {
                DidMoveWithoutRequest();
            }
            name = $"{baseName}-{RoomId}";
            OnStatusChange();

            roomManager.RegisterNetworkIRLMoveRequester(this);
            roomManager.OnNetworkIRLRoomMoveRequesterRoomChange(this, "");

            // Not need to call roomManager.NetworkIRLRoomMoveRequesterMoveCounterChange(this),
            // as a new requester should not trigger and move, and as for late joining users, there have no move to propagate
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            roomManager?.UnregisterNetworkIRLMoveRequester(this);
        }

        // Prevent triggering moves
        public void DidMoveWithoutRequest()
        {
            var position = target.position;
            var rotation = target.rotation;
            DidMoveWithoutRequest(position, rotation);
        }

        public void DidMoveWithoutRequest(Vector3 position, Quaternion rotation)
        {
            lastSentTargetPosition = target.position;
            lastSentTargetRotation = target.rotation;
            PositionBeforeMoveToPropagate = lastSentTargetPosition;
            RotationBeforeMoveToPropagate = lastSentTargetRotation;
            PositionAfterMoveToPropagate = lastSentTargetPosition;
            RotationAfterMoveToPropagate = lastSentTargetRotation;
        }

        public Vector3 ApplyFilter(Vector3 originalValue, Vector3 targetValue, Vector3 filter)
        {
            var result = targetValue;
            if (filter.x > 0) result.x = originalValue.x;
            if (filter.y > 0) result.y = originalValue.y;
            if (filter.z > 0) result.z = originalValue.z;
            return result;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            CheckSentRequestState();

            // If request are not bloqued, we check if the target has been moved, and is stabilized
            if (Status != RequestStatus.RequestsBlocked)
            {
                CheckTargetMoveBasedPositionRequest();
            }
        }

        void CheckTargetMoveBasedPositionRequest()
        {
            // We check that a request is not pending application before analysis a temporary move
            if (Status != RequestStatus.RequestSent && target != null && string.IsNullOrEmpty(RoomId.ToString()) == false)
            {
                const float minPositionChange = 0.001f;
                const float minRotationChange = 0.1f;
                // We check if the move requester has been moved from its last position during a request
                var filteredTargetPosition = ApplyFilter(lastSentTargetPosition, target.position, positionLockAxis);
                var filteredTargetRotation = Quaternion.Euler(ApplyFilter(lastSentTargetRotation.eulerAngles, target.rotation.eulerAngles, rotationLockAxis));

                var distanceFromLastRequest = Vector3.Distance(filteredTargetPosition, lastSentTargetPosition);
                var rotationFromLastRequest = Quaternion.Angle(lastSentTargetRotation, filteredTargetRotation);

                if (distanceFromLastRequest > minPositionChange || rotationFromLastRequest > minRotationChange)
                {
                    // The move requester has moved, we may send a request. Waiting for the position stabilization (the position/rotation requested is stored in PositionAfterMoveToPropagate/RotationAfterMoveToPropagate, to see if it stayed stable long enough to be considered stable)
                    float positionChange = (PositionAfterMoveToPropagate - filteredTargetPosition).magnitude;
                    float angleChange = Quaternion.Angle(RotationAfterMoveToPropagate, filteredTargetRotation);
                    if (incomingPositionStabilisationTimeout == -1 || positionChange > minPositionChange || angleChange > minRotationChange)
                    {
                        // The position/rotation changed (or is new), so the request is not yet stable
                        PositionBeforeMoveToPropagate = lastSentTargetPosition;
                        RotationBeforeMoveToPropagate = lastSentTargetRotation;
                        PositionAfterMoveToPropagate = filteredTargetPosition;
                        RotationAfterMoveToPropagate = filteredTargetRotation;

                        incomingPositionStabilisationTimeout = Time.time + delayBeforeStabilizingIncomingPosition;
                        //roomManager?.ConsoleLog($"Starting/updating stabilisation timer: positionChangeInRequest:{positionChange} -> angleChangeInRequest: {angleChange} (incomingPositionStabilisationTimeout: {incomingPositionStabilisationTimeout} / time: {Time.time} / Status: {Status})");
                    }

                    // We check that the request is not postponed (preventing stabilization - for preview only modes)
                    if (Status != RequestStatus.RequestPostponed)
                    {
                        var previousStatus = Status;
                        Status = RequestStatus.RequestPlanned;
                        if (delayBeforeStabilizingIncomingPosition == 0 || (incomingPositionStabilisationTimeout < Time.time))
                        {
                            // The request is not postponed, and is stabilized: we launch it
                            roomManager?.ConsoleLog($"InitializingRoomMove: {lastSentTargetPosition} -> {PositionAfterMoveToPropagate} (incomingPositionStabilisationTimeout: {incomingPositionStabilisationTimeout} / time: {Time.time} / previousStatus: {previousStatus})");
                            InitializingRoomMove(lastSentTargetPosition, lastSentTargetRotation, PositionAfterMoveToPropagate, RotationAfterMoveToPropagate);
                            incomingPositionStabilisationTimeout = -1;
                            lastSentTargetPosition = PositionAfterMoveToPropagate;
                            lastSentTargetRotation = RotationAfterMoveToPropagate;
                            target.rotation = lastSentTargetRotation;
                            target.position = lastSentTargetPosition;
                            lastRequestSentTime = Time.time;
                            Status = RequestStatus.RequestSent;
                        }
                    }
                }
            }
        }

        void CheckSentRequestState()
        {
            // Check if sent request have been applied
            if (Status == RequestStatus.RequestSent && Object.HasStateAuthority && roomManager.MaxMoveCounterForRoomId(RoomId.ToString()) >= MoveCounter)
            {
                Status = RequestStatus.RequestAllowed;
                if (onPlannedRequestApplied != null) onPlannedRequestApplied.Invoke();
            }

            if (Status == RequestStatus.RequestSent && lastRequestSentTime != -1 && (Time.time - lastRequestSentTime) > 5)
            {
                // Watchdog timeout. Unexpected, log an error, but consider the request sent to recover
                Debug.LogError("[Error] Timeout on MoveRequester request");
                Status = RequestStatus.RequestAllowed;
                if (onPlannedRequestApplied != null) onPlannedRequestApplied.Invoke();
            }
        }

        public override void Render()
        {
            base.Render();
            Object.AffectStateAuthorityIfNone();
        }

        public void ChangeRoomId(string roomId)
        {
            var previousRoomId = RoomId.ToString();
            if (string.IsNullOrEmpty(roomId)) return;

            if (Object.HasStateAuthority == false)
            {
                Debug.LogError("Cannot set RoomId on move requester not owned");
                return;
            }

            if (roomId.Length > NetworkIRLRoomMember.MAX_ROOMID_LENGTH)
            {
                roomId = roomId.Substring(0, NetworkIRLRoomMember.MAX_ROOMID_LENGTH);
            }
            RoomId = roomId;

            roomManager?.OnNetworkIRLRoomMoveRequesterRoomChange(this, previousRoomId);
        }

        public virtual void OnRoomAlreadyContainsARequester(NetworkIRLRoomMoveRequester existingRequester) {
            if(despawnWhenRoomAlreadycontainsARequester && Object.HasStateAuthority)
            {
                Debug.Log($"Only one move requester should be associated to a given room: {existingRequester} ({existingRequester.RoomId}=");
                Runner.Despawn(Object);
            }
                
        }

        public virtual void OnRoomEmpty()
        {
            if (despawnWhenRoomIsEmpty && Object.HasStateAuthority)
            {
                Debug.Log($"Room {RoomId} empty, destroying move requester");
                Runner.Despawn(Object);
            } 
        }

        void OnRoomIdChange(NetworkBehaviourBuffer previous)
        {
            name = $"{baseName}-{RoomId}";

            string previousRoomId = GetPropertyReader<NetworkString<_32>>(nameof(RoomId)).Read(previous).ToString();
            Debug.Log($"Move requester room changed: {RoomId}, prev: {previousRoomId}");

            roomManager?.OnNetworkIRLRoomMoveRequesterRoomChange(this, previousRoomId);
        }

        #region IStateAuthorityChanged
        public void StateAuthorityChanged()
        {
            if (Object.HasStateAuthority)
            {
                // Make sure to store the initial position on state auth change, to prevent triggering a move based on previously known position
                DidMoveWithoutRequest();
            }

            var roomId = RoomId.ToString();
            if (Object.HasStateAuthority && roomManager.knowRoomByRoomIds.ContainsKey(roomId))
            {
                // Our room exists, and we are state auth on this component
                if (roomManager.knowRoomByRoomIds[roomId].members.Count == 0)
                {
                    Runner.Despawn(Object);
                }
            }
            if (Object && Object.HasStateAuthority && roomManager.knowRoomByRoomIds.ContainsKey(roomId) == false)
            {
                // Our room does not exist anymore
                Runner.Despawn(Object);
            }
        }
        #endregion
    }
}
