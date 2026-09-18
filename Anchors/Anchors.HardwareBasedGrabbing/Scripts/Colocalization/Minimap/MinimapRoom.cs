using Fusion.Addons.AnchorsAddon.Colocalization;
using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Core.HardwareBasedGrabbing;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Fusion.Addons.AnchorsAddon.Colocalization.Minimap
{
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    public class MinimapRoom : MonoBehaviour, IRLRoomManager.IIRLRoomManagerPartListener
    {
        public const int EXECUTION_ORDER = MinimapRoomAssociatedPart.EXECUTION_ORDER + 10;
        public NetworkIRLRoomMoveRequester moveRequester;
        public GameObject minimapMoveRequester;
        public Transform minimapReferential;
        public List<MinimapRoomAssociatedPart> minimapParts = new List<MinimapRoomAssociatedPart>();
        public Dictionary<MinimapRoomAssociatedPart, Pose> _localPoseForFakeMinimapParts = new Dictionary<MinimapRoomAssociatedPart, Pose>();
        public IRLRoomsMinimap minimap;
        LocalPoseProxy _moveRequesterLocalPoseProxy;
        Grabbable _grabbable;
        Collider _interactionCollider;
        IRLRoomsMinimap.Status _status = IRLRoomsMinimap.Status.NoInteraction;
        public TMPro.TMP_Text roomNameText;
        public TMPro.TMP_Text roomDescriptionText;

        const float ExtendedPreviewDuration = 3;
        float _endOfExtendedPreview = -1;
        public float delayBeforeConsideringUngrabAsARequest = 0.5f;        


        // For now, we don't allow local user room move (as the move requester won't trigger a move in the current implementation)
        public bool preventLocalRoomMove = true;
        public bool isMovingFakeRoom = false;
        public bool hideRoomsRenderersDuringInteractionCooldown = true;
        public bool hideRoomsRenderersDuringRemoteInteraction = true;
        Renderer _roomRenderer;
        bool _isTakingMoveRequesterAuthority = false;
        bool _unappliedUngrab = false;
        float _unappliedUngrabTime = -1;

        bool IsLocalRoom => moveRequester != null && moveRequester.RoomId == IRLRoomManager.SharedInstance.localNetworkIRLRoomMember?.RoomId;

        private void Start()
        {
            _roomRenderer = GetComponentInChildren<Renderer>();
            if (roomNameText)
            {
                roomNameText.text = $"";
            }
            if (roomDescriptionText)
            {
                roomDescriptionText.text = $"";
            }

            _interactionCollider = gameObject.GetComponent<Collider>();
            // For now, we don't allow local user room move (as the move requester won't trigger a move in the current implementation)
            if (minimap.isInteractive == false || preventLocalRoomMove == false || IsLocalRoom == false)
            {
                _grabbable = gameObject.AddComponent<Grabbable>();
                _grabbable.onGrab.AddListener(OnGrab);
                _grabbable.onUngrab.AddListener(OnUngrab);
            }
            else
            {
                if (_interactionCollider)
                {
                    _interactionCollider.enabled = false;
                }
            }

            var material = IsLocalRoom ? minimap.materialConfiguration.minimapLocalRoomZoneMaterial : minimap.materialConfiguration.minimapRemoteRoomZoneMaterial;
            if (material && gameObject.TryGetComponent<Renderer>(out var r))
            {
                r.material = material;
            }

            CreateMinimapMoveRequester();

            AdaptRoomZoneBound();

            IRLRoomManager.SharedInstance.listeners.Add(this);
        }

        public void CancelInteraction()
        {
            if (_grabbable != null && _grabbable.currentGrabber != null)
            {
                _grabbable.currentGrabber.Ungrab(_grabbable);
            }
            moveRequester.DidMoveWithoutRequest();
            if (moveRequester.HasStateAuthority) moveRequester.Status = NetworkIRLRoomMoveRequester.RequestStatus.RequestsBlocked;
            InteractionEnd();
        }

        void OnGrab()
        {
            if (_unappliedUngrab)
            {
                _unappliedUngrab = false;
                _unappliedUngrabTime = -1;
            }
            else { 
                StartMovingRoom();
            }
        }

        [ContextMenu("StartMovingRoom")]
        async void StartMovingRoom()
        {
            if (minimap.status != IRLRoomsMinimap.Status.NoInteraction)
            {
                // We only allow one interaction at a time
                Debug.Log("Ungrab due to already interacting");
                _grabbable.Ungrab();
                return;
            }

            if (preventLocalRoomMove && moveRequester != null && moveRequester.RoomId.ToString() == IRLRoomManager.SharedInstance?.localNetworkIRLRoomMember.RoomId.ToString())
            {
                // For now, we don't allow local user room move (as we won't have previews in the current implementation)
                Debug.Log("Ungrab due to local room");
                _grabbable.Ungrab();
                return;
            }

            if (moveRequester != null && moveRequester.HasStateAuthority == false)
            {
                if (moveRequester.Status != NetworkIRLRoomMoveRequester.RequestStatus.RequestsBlocked && moveRequester.Status != NetworkIRLRoomMoveRequester.RequestStatus.RequestAllowed)
                {
                    // The current state authority is doing a move, we cancel the grab instead of trying to take the authority
                    Debug.Log("Ungrab due to remote " + moveRequester.Status);
                    _grabbable.Ungrab();
                    return;
                }

                // We can't sync the move requester position with its counterpart version, until we have state auth on it, but we are moving the minimap version (grabbing, and taking authority will take some time): disabling the position synchronization during authority trnasfer
                _moveRequesterLocalPoseProxy.enabled = false;
                _isTakingMoveRequesterAuthority = true;
                await moveRequester.Object.WaitForStateAuthority();
                _isTakingMoveRequesterAuthority = false;
                // restore position synchornization (the minimap version will modify the actual move requester position now that we have authority on it)
                _moveRequesterLocalPoseProxy.enabled = true;
            }

            if (moveRequester)
            {
                // Ensure that the grabbing starting point is considered as the reference point for the move
                moveRequester.DidMoveWithoutRequest();

                // Prevent immediate moves, and activate preview mode
                moveRequester.Status = NetworkIRLRoomMoveRequester.RequestStatus.RequestPostponed;
            }
            else
            {
                StartMovingFakeRoom();
            }

            InteractionStart();
        }

        void InteractionStart()
        {
            if (moveRequester) moveRequester.ShouldPreview = true;
            OnInteractionStart();
        }

        void OnInteractionStart()
        {
            minimap.OnRoomInteracting(this);
            _status = IRLRoomsMinimap.Status.Interacting;
        }

        void InteractionCooldownStart()
        {
            _status = IRLRoomsMinimap.Status.PostInteractionCooldown;
            minimap.OnRoomInteractionCooldownStart(this);

            // Hnadling of fake rooms
            if (moveRequester == null)
            {
                ApplyMoveToFakeRoom();
            }
            if (hideRoomsRenderersDuringInteractionCooldown && _roomRenderer != null)
                _roomRenderer.enabled = false;
        }

        void InteractionEnd()
        {
            if (moveRequester) moveRequester.ShouldPreview = false;
            OnInteractionEnd();
        }

        void OnInteractionEnd()
        {
            _status = IRLRoomsMinimap.Status.NoInteraction;
            _endOfExtendedPreview = -1;
            minimap.OnRoomInteractionEnd(this);
            AdaptRoomZoneBound();
            if (hideRoomsRenderersDuringInteractionCooldown && _roomRenderer != null)
                _roomRenderer.enabled = true;
        }

        void OnUngrab()
        {
            if(delayBeforeConsideringUngrabAsARequest > 0)
            {
                _unappliedUngrab = true;
                _unappliedUngrabTime = Time.time;
            }
            else
            {
                EndMovingRoom();
            }                
        }

        void CheckUnAppliedUngrab()
        {
            // Check if cooldown (that allows to quickly ungrag/re-grab) is over
            if (_unappliedUngrab && (Time.time - _unappliedUngrabTime) > delayBeforeConsideringUngrabAsARequest)
            {
                _unappliedUngrab = false;
                _unappliedUngrabTime = -1;
                EndMovingRoom();
            }
        }

        [ContextMenu("EndMovingRoom")]
        void EndMovingRoom()
        {
            bool wasInteracting = _status != IRLRoomsMinimap.Status.NoInteraction;
            if (wasInteracting && moveRequester)
            {
                moveRequester.Status = NetworkIRLRoomMoveRequester.RequestStatus.RequestAllowed;
            }
            if (wasInteracting && ExtendedPreviewDuration != 0)
            {
                _endOfExtendedPreview = Time.time + ExtendedPreviewDuration;
                InteractionCooldownStart();
                if (moveRequester == null)
                {
                    InteractionEnd();
                }
            }
            else
            {
                InteractionEnd();
            }
        }

        void OnPlannedRequestApplied()
        {
            // We will only allow moves while grabbed
            if (moveRequester.HasStateAuthority) moveRequester.Status = NetworkIRLRoomMoveRequester.RequestStatus.RequestsBlocked;
        }

        private void Update()
        {
            // Relevant if the move requester joined the room later on
            CreateMinimapMoveRequester();

            CheckUnAppliedUngrab();

            if (moveRequester != null)
            {
                if (moveRequester.HasStateAuthority && _endOfExtendedPreview != -1)
                {
                    if (_endOfExtendedPreview <= Time.time)
                    {
                        // End of extended preview
                        InteractionEnd();
                    }
                }

                if (moveRequester && moveRequester.Object && moveRequester.Object.IsValid && IRLRoomManager.SharedInstance.knowRoomByRoomIds.ContainsKey(moveRequester.RoomId.ToString()))
                {
                    var room = IRLRoomManager.SharedInstance.knowRoomByRoomIds[moveRequester.RoomId.ToString()];
                    if (roomNameText)
                    {
                        roomNameText.text = $"Room {moveRequester.RoomId}";
                    }
                    if (roomDescriptionText)
                    {
                        roomDescriptionText.text = IsLocalRoom ? $"(your room, {room.members.Count} user(s) )" : $"({room.members.Count} user(s) )";
                    }
                }
            }

            MoveFakeRooms();

            if (moveRequester != null && moveRequester.Object && moveRequester.Object.IsValid && moveRequester.ShouldPreview)
            {
                minimap.OnRoomPreviewing();
            }

            // Handle minimap messaging when a preview is triggered by a remote user (to allow common room updating)
            if (moveRequester != null && moveRequester.HasStateAuthority == false && _isTakingMoveRequesterAuthority == false)
            {
                if (moveRequester.ShouldPreview && _status == IRLRoomsMinimap.Status.NoInteraction)
                {
                    OnInteractionStart();
                }
                else if (moveRequester.ShouldPreview == false && _status == IRLRoomsMinimap.Status.Interacting)
                {
                    OnInteractionEnd();
                }
                if (_status == IRLRoomsMinimap.Status.Interacting && hideRoomsRenderersDuringRemoteInteraction && _roomRenderer)
                {
                    _roomRenderer.enabled = false;
                }
            }
        }

        void CreateMinimapMoveRequester()
        {
            if (minimapMoveRequester != null)
            {
                return;
            }
            if (moveRequester == null)
            {
                return;
            }

            // We will only allow moves while grabbed
            if (moveRequester.HasStateAuthority)
            {
                moveRequester.Status = NetworkIRLRoomMoveRequester.RequestStatus.RequestsBlocked;
            }

            minimapMoveRequester = new GameObject($"MinimapMoveRequester-{moveRequester.RoomId}");
            minimapMoveRequester.transform.parent = transform;
            moveRequester.onPlannedRequestApplied.AddListener(OnPlannedRequestApplied);

            // We place the minimap version of the world requester at its matching starting position in real world
            // We use the world position as local position here for the minimap version
            if (moveRequester == null) { Debug.LogError("No move requester"); return; }
            if (minimapReferential == null) { Debug.LogError("No minimapReferential"); return; }
            minimapMoveRequester.transform.rotation = minimapReferential.rotation * moveRequester.transform.rotation;
            minimapMoveRequester.transform.position = minimapReferential.transform.TransformPoint(moveRequester.transform.position);

            // The minimap version of the move requester will move the actual one, if we have state authority on it (we will take it when grabbing the minimap preview, unless it is already grabbed)
            _moveRequesterLocalPoseProxy = gameObject.AddComponent<LocalPoseProxy>();
            _moveRequesterLocalPoseProxy.source = minimapMoveRequester.transform;
            _moveRequesterLocalPoseProxy.sourceReferential = minimapReferential;
            _moveRequesterLocalPoseProxy.target = moveRequester.transform;
            _moveRequesterLocalPoseProxy.swapLogicIfTargetNotStateAuthority = true;
        }

        private void OnDestroy()
        {
            IRLRoomManager.SharedInstance?.listeners.Remove(this);
            if (_status == IRLRoomsMinimap.Status.PostInteractionCooldown || _status == IRLRoomsMinimap.Status.Interacting)
            {
                InteractionEnd();
            }
        }

        [ContextMenu("AdaptRoomZoneBound")]
        void AdaptRoomZoneBound()
        {
            var roomRotation = Quaternion.identity;
            foreach (var p in minimapParts)
            {
                if (p.roomPart.partType == NetworkIRLRoomAssociatedPart.PartType.Wall)
                {
                    roomRotation = p.roomPart.transform.rotation;
                    break;
                }
            }
            var orientedBound = new OrientedBounds(roomRotation);
            foreach (var p in minimapParts)
            {
                // We only use walls to define the room zone
                if (p.roomPart.partType != NetworkIRLRoomAssociatedPart.PartType.Wall) continue;

                // Make sure the position is up to date
                p.localPoseProxy.AdaptPosition();
                var originalTransform = p.roomPart.transform;
                orientedBound.Encapsulate(originalTransform.position);
                orientedBound.Encapsulate(originalTransform.TransformPoint(new Vector3(-0.5f, -0.5f, 0f)));
                orientedBound.Encapsulate(originalTransform.TransformPoint(new Vector3(-0.5f, 0.5f, 0f)));
                orientedBound.Encapsulate(originalTransform.TransformPoint(new Vector3(0.5f, 0.5f, 0f)));
                orientedBound.Encapsulate(originalTransform.TransformPoint(new Vector3(0.5f, -0.5f, 0f)));
            }

            // We applied the bound to find the world scale (that will be applied as local scale thanks to ignoreParentScale), and world position. We will them adapt them to the minimap referential
            orientedBound.ApplyToTransform(transform, ignoreParentScale: true);
            // The position did not took into account the minimap referantial: ApplyToTransform applied in world space the changes
            var localPosition = transform.position;
            var localRotation = transform.rotation;
            transform.rotation = minimapReferential.rotation * localRotation;
            transform.position = minimapReferential.TransformPoint(localPosition);
            minimap.OnRoomAdaptRoomZoneBound();
        }

        #region IIRLRoomManagerPartListener
        public void OnRoomCreate(string roomId) { }

        public void OnRoomDelete(string roomId) { }

        public void OnAssociatedPartPoseChange(NetworkIRLRoomAssociatedPart part)
        {
            if (moveRequester != null && part && moveRequester && part.RoomId.ToString() == moveRequester.RoomId.ToString())
            {
                AdaptRoomZoneBound();
                if (_status == IRLRoomsMinimap.Status.PostInteractionCooldown)
                {
                    Debug.Log("Move applied before cooldown end. Stopping cooldown");
                    InteractionEnd();
                }
            }
        }
        #endregion

        #region Fake rooms handling
        void StartMovingFakeRoom()
        {
            _localPoseForFakeMinimapParts.Clear();

            foreach (var part in minimapParts)
            {
                bool isFakeRoomPart = string.IsNullOrEmpty(part.roomPart.fakeRoomId) == false;
                if (isFakeRoomPart)
                {
                    // There is no move requester as it is a fake room. We need to move it manually. We revert the local pose proxy logic for one adaptation for that
                    _localPoseForFakeMinimapParts[part] = new Pose(transform.InverseTransformPoint(part.transform.position), Quaternion.Inverse(transform.rotation) * part.transform.rotation);
                    part.localPoseProxy.forceLogicSwap = true;
                    isMovingFakeRoom = true;
                }
            }
        }

        void MoveFakeRooms()
        {
            if (isMovingFakeRoom)
            {
                foreach (var part in minimapParts)
                {
                    bool isFakeRoomPart = string.IsNullOrEmpty(part.roomPart.fakeRoomId) == false;
                    if (isFakeRoomPart && _localPoseForFakeMinimapParts.ContainsKey(part))
                    {
                        part.transform.rotation = transform.rotation * _localPoseForFakeMinimapParts[part].rotation;
                        part.transform.position = transform.TransformPoint(_localPoseForFakeMinimapParts[part].position);
                    }
                }
            }
        }

        void ApplyMoveToFakeRoom()
        {
            isMovingFakeRoom = false;
            foreach (var p in minimapParts)
            {
                bool isFakeRoomPart = string.IsNullOrEmpty(p.roomPart.fakeRoomId) == false;
                if (isFakeRoomPart)
                {
                    // There is no move requester as it is a fake room. We need to move it manually. We revert the local pose proxy logic for one adaptation for that
                    p.localPoseProxy.forceLogicSwap = false;
                }
            }
        }
        #endregion
    }
}


