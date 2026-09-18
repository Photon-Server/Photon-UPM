using Fusion.Addons.AnchorsAddon;
using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.AnchorsAddon.Colocalization.Minimap
{
    /// <summary>
    /// Displays a minimap of the IRL rooms present in the scene.
    /// Allows to move the rooms, and provides a preview.
    /// 
    /// When moving the local user room, triggers a temporary hardware rig move
    /// </summary>
    public class IRLRoomsMinimap : MonoBehaviour, IRLRoomManager.IIRLRoomManagerPartListener
    {
        public static IRLRoomsMinimap SharedInstance;

        public float minimapScale = 0.03f;
        public Transform minimapReferential;
        public GameObject minimapVisual;
        public List<GameObject> minimapVisualAdditionalParts = new List<GameObject>();
        public GameObject minimapVisualPlate;

        public enum Status
        {
            NoInteraction,
            Interacting,
            PostInteractionCooldown
        }

        [Header("Activation")]
        public bool desactivateMinimapAtStart = true;
        public bool toggleOnMainButtonPress = true;
        public string controllerToggleButton = "menuButton";

        [Header("Interaction")]
        public bool isInteractive = true;
        public Status status = Status.NoInteraction;

        [Header("Feedback views")]
        public GameObject cooldownView;

        [Header("Options")]
        public bool shouldChangeAssociatedPartDisplayModeDuringInteraction = true;
        public IRLRoomManager.NetworkIRLRoomAssociatedPartDisplayMode associatedPartDisplayModeWithoutInteraction = IRLRoomManager.NetworkIRLRoomAssociatedPartDisplayMode.Never;
        public IRLRoomManager.NetworkIRLRoomAssociatedPartDisplayMode associatedPartDisplayModeDuringInteraction = IRLRoomManager.NetworkIRLRoomAssociatedPartDisplayMode.MainPlayerInRemoteRoomOnly;
        [HideInInspector] public List<MinimapRoom> _interactingMinimapRooms = new List<MinimapRoom>();
        [HideInInspector] public List<MinimapRoom> _interactionCooldownMinimapRooms = new List<MinimapRoom>();
        [Tooltip("If true, the display will be always adapted during update. Set it to false if you intenet de edit manually the settings and don't want your choices to be override (debug ...)")]
        public bool alwaysAdaptDisplayModeForInteractionStatus = true;
        public bool disableMinimapOnTeleport = false;
        public bool hideRoomRendererDuringCooldown = true;
        public bool hideRoomsRenderersDuringRemoteInteraction = true;
        public bool shouldCenterMinimapOnLocalRoom = true;
        public bool shouldScaleMinimapUnderRooms = true;
        public bool preventLocalRoomMove = false;
        public bool displayOnlyMainMemberAssociatedParts = true;

        [Header("MinimapElements")]
        public GameObject minimapRoomPrefab;
        public GameObject minimapCommonRoomPrefab;
        public GameObject minimapWallPrefab;
        public GameObject minimapTablePrefab;
        public GameObject minimapUserPrefab;
        public GameObject minimapLocalUserPrefab;

        [System.Serializable]
        public struct MaterialConfiguration
        {
            public Material minimapLocalRoomZoneMaterial;
            public Material minimapRemoteRoomZoneMaterial;
            public Material localWallMaterial;
            public Material remoteWallMaterial;
            public Material localUserMaterial;
            public Material remoteUserMaterial;
        }

        public MaterialConfiguration materialConfiguration;

        LocalInputTracker _minimapToggleInputTracker;
        Vector3 _rigPositionOnMinimapActivation;
        Vector3 _lastMinimapPositionOffsetToRig;
        Quaternion _lastMinimapRotationOffsetToRig;
        List<GameObject> _minimapGameObjects = new List<GameObject>();
        List<MinimapRoom> _minimapRooms = new List<MinimapRoom>();
        GameObject _minimapCommonRoom;
        float _lastAppliedMinimapScale = -1;
        bool _isStartActivationPending = false;
        Vector3 _lastHeadsetPosition = Vector3.zero;
        float _startHeadsetStabilizationWait = -1;
        float _endOfheadsetStabilizationWait = -1;
        MinimapRoom _localMinimapRoom = null;
        Vector3 _lastLocalMinimapRoomPosition = -1000 * Vector3.one;
        Vector3 _plateTopLocalPosition;
        Vector3 _plateCenterLocalPosition;
        Quaternion _plateTopLocalRotation;
        bool _plateUpdateRequired = false;
        Vector3 _defaultPlateWorldScale;
        List<SceneFocusedElement> _sceneFocusedElements = new List<SceneFocusedElement>();
        Vector3[] _roomLocalPointsToEncapsulate = new Vector3[] {
        0.5f * Vector3.left + 0.5f * Vector3.forward,
        0.5f * Vector3.left + 0.5f * Vector3.back,
        0.5f * Vector3.right + 0.5f * Vector3.forward,
        0.5f * Vector3.right + 0.5f * Vector3.back };
        bool _lastButtonState = false;
        bool _isCommonRoomtracked = false;
        bool _minimapGenerationRequested = false;

        bool IsMinimapActive => minimapReferential.gameObject.activeSelf;

        private void Awake()
        {
            if (SharedInstance == null)
                SharedInstance = this;

            _minimapToggleInputTracker = new LocalInputTracker(new List<string> { controllerToggleButton });
            _minimapToggleInputTracker.EnableActions();

            if (minimapReferential == null)
            {
                var minimapReferentialObject = new GameObject("MinimapReferential");
                minimapReferentialObject.transform.parent = transform;
                minimapReferential = minimapReferentialObject.transform;
                minimapReferential.transform.rotation = transform.rotation;
                minimapReferential.transform.position = transform.position;
            }

            if (_minimapCommonRoom == null && minimapCommonRoomPrefab != null)
            {
                _minimapCommonRoom = GameObject.Instantiate(minimapCommonRoomPrefab);
            }
            if (_minimapCommonRoom)
            {
                _minimapCommonRoom.transform.parent = minimapReferential.transform;
            }
            if (minimapVisualPlate)
            {
                var plateParentLossyScale = minimapVisualPlate.transform.parent.lossyScale;
                var plateLocalScale = minimapVisualPlate.transform.localScale;
                _defaultPlateWorldScale = new Vector3(plateLocalScale.x * plateParentLossyScale.x, plateLocalScale.y * plateParentLossyScale.y, plateLocalScale.z * plateParentLossyScale.z);

                _plateTopLocalPosition = transform.InverseTransformPoint(minimapVisualPlate.transform.TransformPoint(0.5f * Vector3.up));
                _plateCenterLocalPosition = transform.InverseTransformPoint(minimapVisualPlate.transform.position);
                _plateTopLocalRotation = Quaternion.Inverse(transform.rotation) * minimapVisualPlate.transform.rotation;
            }
        }

#if OCULUS_SDK_AVAILABLE
        bool _isOculusDevice = false;
#endif

        void Start()
        {
#if OCULUS_SDK_AVAILABLE
            string deviceModel = SystemInfo.deviceModel.ToLower();
            if (deviceModel.Contains("oculus") || deviceModel.Contains("meta"))
            {
                _isOculusDevice = true;
            }
#endif


            DesactivateMinimap();
            var rig = HardwareRigsRegistry.GetHardwareRig();

            if (preventLocalRoomMove == false)
            {
                // If we can move the local room, the minimap should be stored under the hardware rig, to avoid issue while moving our room that would move the rig, and so our hand, while grabbing
                transform.parent = rig?.transform;
            }

            if (desactivateMinimapAtStart == false)
            {
                _isStartActivationPending = true;
                // If the headset is not wore, and we want the minimap displayed, we end up displaying it anyway
                _startHeadsetStabilizationWait = Time.time + 0.1f;
                _endOfheadsetStabilizationWait = Time.time + 5;
            }

            IRLRoomManager.SharedInstance.listeners.Add(this);
            _rigPositionOnMinimapActivation = rig?.transform.position ?? Vector3.zero;

            MixedRealityRemoteUsersRoom.SharedInstance.onUpdateRemoteUsersRoom.AddListener(OnUpdateRemoteUsersRoom);
        }

        private void OnDestroy()
        {
            IRLRoomManager.SharedInstance?.listeners.Remove(this);
            if (SharedInstance == this)
                SharedInstance = null;

            MixedRealityRemoteUsersRoom.SharedInstance?.onUpdateRemoteUsersRoom.RemoveListener(OnUpdateRemoteUsersRoom);
        }

        [ContextMenu("ActivateMinimap")]
        public void ActivateMinimap()
        {
            minimapReferential.gameObject.SetActive(true);
            if (minimapVisual) minimapVisual.SetActive(true);
            foreach(var visual in minimapVisualAdditionalParts)
            {
                if (visual) visual.SetActive(true);
            }
            GenerateMinimap();
            _rigPositionOnMinimapActivation = HardwareRigsRegistry.GetHardwareRig()?.transform.position ?? Vector3.zero;
            AdaptDisplayModeForInteractionStatus();
        }

        [ContextMenu("DesactivateMinimap")]
        public void DesactivateMinimap()
        {
            ClearMinimap();

            minimapReferential.gameObject.SetActive(false);
            if (minimapVisual) minimapVisual.SetActive(false);
            foreach (var visual in minimapVisualAdditionalParts)
            {
                if (visual) visual.SetActive(false);
            }
            AdaptDisplayModeForInteractionStatus();
        }

        void OnRigTeleport(IHardwareRig rig)
        {
            transform.rotation = rig.transform.rotation * _lastMinimapRotationOffsetToRig;
            transform.position = rig.transform.TransformPoint(_lastMinimapPositionOffsetToRig);
            if (disableMinimapOnTeleport)
            {
                // Desactivate minimap on rig move
                DesactivateMinimap();
            }
            else
            {
                _rigPositionOnMinimapActivation = rig.transform.position;
            }
        }

        private void Update()
        {
            var rig = HardwareRigsRegistry.GetHardwareRig();
            if (_isStartActivationPending && Time.time > _startHeadsetStabilizationWait)
            {
                // We wait for one frame of head move to be sure that the headset is tracked properly
                var headsetPosition = rig?.Headset.transform.position ?? Vector3.zero;
                if (_lastHeadsetPosition != headsetPosition && _lastHeadsetPosition != Vector3.zero || _endOfheadsetStabilizationWait < Time.time)
                {
                    _isStartActivationPending = false;
                    ActivateMinimap();
                    PositionMinimapInFrontOfUser();
                }
                _lastHeadsetPosition = headsetPosition;
            }

            HandleRigTeleport(rig);


            if (_lastAppliedMinimapScale != minimapScale)
            {
                minimapReferential.transform.localScale = minimapScale * Vector3.one;
            }

            CheckToggleMinimapButton();

            UpdateInteractionStatus();


            if (cooldownView.activeSelf != (status == Status.PostInteractionCooldown))
                cooldownView.SetActive(status == Status.PostInteractionCooldown);

            if (cooldownView.activeSelf)
            {
                cooldownView.transform.LookAt(rig.Headset.transform.position);
            }

            if (alwaysAdaptDisplayModeForInteractionStatus)
            {
                AdaptDisplayModeForInteractionStatus();
            }

            if (IsMinimapActive && shouldCenterMinimapOnLocalRoom)
            {
                CenterMinimapOnLocalRoom();
            }
            if (IsMinimapActive && shouldScaleMinimapUnderRooms && minimapVisual)
            {
                ScaleMinimapPlateUnderRooms();
            }
        }

        void LateUpdate()
        {
            UpdateCommonRoomWhileInteracting();
            FollowCommonRoom();

            if (_minimapGenerationRequested)
            {
                _minimapGenerationRequested = false;
                GenerateMinimap();
            }
        }

        void HandleRigTeleport(IHardwareRig rig)
        {
            if (rig?.transform.position is Vector3 rigPosition)
            {
                if (IsMinimapActive && Vector3.Distance(_rigPositionOnMinimapActivation, rigPosition) > 0.1f)
                {
                    OnRigTeleport(rig);
                }
                _lastMinimapRotationOffsetToRig = Quaternion.Inverse(rig.transform.rotation) * transform.rotation;
                _lastMinimapPositionOffsetToRig = rig.transform.InverseTransformPoint(transform.position);
            }
        }

        void CheckToggleMinimapButton()
        {
            if (toggleOnMainButtonPress == false)
                return;

            bool minimapToggleState = false;
            if ((_minimapToggleInputTracker.ReadAnyButtonPressed() is bool currentState))
            {
                minimapToggleState = currentState;
            }

#if OCULUS_SDK_AVAILABLE
            if(_isOculusDevice)
            {
                var state = OVRPlugin.GetControllerState4((uint)OVRInput.Controller.LHand);
                bool menuGesture = (state.Buttons & (uint)OVRInput.RawButton.Start) > 0;
                minimapToggleState = minimapToggleState || menuGesture;
            }
#endif

            if (minimapToggleState != _lastButtonState)
            {
                _lastButtonState = minimapToggleState;
                if (minimapToggleState)
                {
                    if (IsMinimapActive)
                    {
                        DesactivateMinimap();
                    }
                    else
                    {
                        ActivateMinimap();
                        PositionMinimapInFrontOfUser();
                    }
                }
            }
        }

        void PositionMinimapInFrontOfUser()
        {
            var headsetTransform = HardwareRigsRegistry.GetHardwareRig().Headset.transform;
            transform.rotation = Quaternion.Euler(0, headsetTransform.eulerAngles.y, 0);
            transform.position = headsetTransform.TransformPoint(new Vector3(0, -0.3f, 0.5f));
        }

        void FollowCommonRoom()
        {
            if (_minimapCommonRoom != null && IsMinimapActive && MixedRealityRemoteUsersRoom.IsAvailable && MixedRealityRemoteUsersRoom.SharedInstance.remoteUsersRoom != null)
            {
                if (_isCommonRoomtracked == false)
                {
                    _isCommonRoomtracked = true;
                    var proxy = _minimapCommonRoom.AddComponent<LocalPoseProxy>();
                    proxy.source = MixedRealityRemoteUsersRoom.SharedInstance.remoteUsersRoom.transform;
                    proxy.target = _minimapCommonRoom.transform;
                    proxy.targetReferential = minimapReferential;
                }

                Vector3 scale = MixedRealityRemoteUsersRoom.SharedInstance.remoteUsersRoom.transform.localScale;
                if (scale.x != _minimapCommonRoom.transform.localScale.x || scale.z != _minimapCommonRoom.transform.localScale.z)
                {
                    //scale.y = 0.001f / minimapScale;
                    _minimapCommonRoom.transform.localScale = scale;
                }

                bool minimapCommonRoomShouldBeVisisble = MixedRealityRemoteUsersRoom.SharedInstance.isRemoteUsersRoomVisible;
                // Check that we're not in a case where the full room is included in the user room
                if (minimapCommonRoomShouldBeVisisble && _localMinimapRoom)
                {
                    bool isIncludedInLocalRoom =
                        IsPointInsideRoomBound(_minimapCommonRoom.transform.TransformPoint(new Vector3(-0.5f, 0, -0.5f)), _localMinimapRoom)
                        && IsPointInsideRoomBound(_minimapCommonRoom.transform.TransformPoint(new Vector3(-0.5f, 0, 0.5f)), _localMinimapRoom)
                        && IsPointInsideRoomBound(_minimapCommonRoom.transform.TransformPoint(new Vector3(0.5f, 0, 0.5f)), _localMinimapRoom)
                        && IsPointInsideRoomBound(_minimapCommonRoom.transform.TransformPoint(new Vector3(0.5f, 0, -0.5f)), _localMinimapRoom);
                    if (isIncludedInLocalRoom)
                    {
                        minimapCommonRoomShouldBeVisisble = false;
                    }

                }

                if (_minimapCommonRoom.activeSelf != minimapCommonRoomShouldBeVisisble)
                {
                    _minimapCommonRoom.SetActive(minimapCommonRoomShouldBeVisisble);
                }
            }
        }

        bool IsPointInsideRoomBound(Vector3 point, MinimapRoom room)
        {
            var localPoint = TransformManipulations.UnscaledOffset(room.transform.position, room.transform.rotation, point);
            var roomCorner = room.transform.TransformPoint(new Vector3(0.5f, 0, 0.5f));
            var localRoomCorner = TransformManipulations.UnscaledOffset(room.transform.position, room.transform.rotation, roomCorner);
            if (Mathf.Abs(localPoint.x) <= localRoomCorner.x && Mathf.Abs(localPoint.z) <= localRoomCorner.z)
            {
                return true;
            }
            return false;
        }

        void UpdateInteractionStatus()
        {
            if (_interactingMinimapRooms.Count > 0)
                status = Status.Interacting;
            else if (_interactionCooldownMinimapRooms.Count > 0)
                status = Status.PostInteractionCooldown;
            else
                status = Status.NoInteraction;
        }

        void AdaptDisplayModeForInteractionStatus()
        {
            if (shouldChangeAssociatedPartDisplayModeDuringInteraction)
            {
                var mode = status == Status.NoInteraction ? associatedPartDisplayModeWithoutInteraction : associatedPartDisplayModeDuringInteraction;
                if (IRLRoomManager.SharedInstance != null && IRLRoomManager.SharedInstance.roomAssociatedPartDisplayMode != mode)
                {
                    IRLRoomManager.SharedInstance.roomAssociatedPartDisplayMode = mode;
                }
            }
        }

        void UpdateCommonRoomWhileInteracting()
        {
            if (status != Status.NoInteraction)
            {
                MixedRealityRemoteUsersRoom.SharedInstance.UpdateRemoteUsersRoom();
            }
        }

        public void OnUpdateRemoteUsersRoom()
        {
            _plateUpdateRequired = true;
        }

        public void OnRoomAdaptRoomZoneBound()
        {
            _plateUpdateRequired = true;
        }

        public void OnRoomPreviewing()
        {
            _plateUpdateRequired = true;
        }

        public void OnRoomInteracting(MinimapRoom room)
        {
            if (_interactingMinimapRooms.Contains(room) == false)
                _interactingMinimapRooms.Add(room);
            AdaptDisplayModeForInteractionStatus();
        }

        public void OnRoomInteractionCooldownStart(MinimapRoom room)
        {
            if (_interactingMinimapRooms.Contains(room))
                _interactingMinimapRooms.Remove(room);
            if (_interactionCooldownMinimapRooms.Contains(room) == false)
                _interactionCooldownMinimapRooms.Add(room);
            AdaptDisplayModeForInteractionStatus();
        }

        public void OnRoomInteractionEnd(MinimapRoom room)
        {
            if (_interactingMinimapRooms.Contains(room))
                _interactingMinimapRooms.Remove(room);
            if (_interactionCooldownMinimapRooms.Contains(room))
                _interactionCooldownMinimapRooms.Remove(room);
            AdaptDisplayModeForInteractionStatus();
        }

        void ClearMinimap()
        {
            // We have to cancel preview on moverequesters
            for (int i = 0; i < _interactingMinimapRooms.Count; i++)
            {
                var interactingRoom = _interactingMinimapRooms[i];
                interactingRoom.CancelInteraction();
            }

            foreach (var o in _minimapGameObjects)
            {
                if (o != null)
                {
                    Destroy(o);
                }
            }
            _minimapGameObjects.Clear();
            _minimapRooms.Clear();
            _interactingMinimapRooms.Clear();
            _interactionCooldownMinimapRooms.Clear();
            _localMinimapRoom = null;
        }

        public void RequestGenerateMinimap()
        {
            _minimapGenerationRequested = true;
        }

        [ContextMenu("GenerateMinimap")]
        public void GenerateMinimap()
        {
            ClearMinimap();
            if (IRLRoomManager.IsAvailable == false) return;
            foreach (var r in IRLRoomManager.SharedInstance.knowRooms)
            {
                if (r == null) continue;

                List<MinimapRoomAssociatedPart> roomMinimapParts = new List<MinimapRoomAssociatedPart>();
                foreach (var p in r.associatedParts)
                {
                    var minimapPartObject = new GameObject($"MinimapPart-{r.roomId}-{p.name}");
                    minimapPartObject.transform.parent = minimapReferential;
                    if (p.partType == NetworkIRLRoomAssociatedPart.PartType.Wall)
                    {
                        // Wall z scale is not reliable (the actual z scale is handled by the wall visual)
                        var scale = p.transform.lossyScale;
                        scale.z = 0.01f;
                        minimapPartObject.transform.localScale = p.transform.lossyScale;
                    }
                    else
                    {
                        minimapPartObject.transform.localScale = p.transform.lossyScale;
                    }
                    var minimapPart = minimapPartObject.AddComponent<MinimapRoomAssociatedPart>();
                    minimapPart.ConfigurePositionProxy(roomPart: p, this);
                    minimapPart.displayOnlyMainMemberAssociatedParts = displayOnlyMainMemberAssociatedParts;
                    roomMinimapParts.Add(minimapPart);
                    _minimapGameObjects.Add(minimapPart.gameObject);
                }

                var minimapRoomGameObject = GameObject.Instantiate(minimapRoomPrefab);
                minimapRoomGameObject.name = $"MinimapRoom-{r.roomId}";
                minimapRoomGameObject.transform.parent = minimapReferential;
                MinimapRoom minimapRoom = minimapRoomGameObject.GetComponent<MinimapRoom>();
                if (minimapRoom == null)
                {
                    minimapRoom = minimapRoomGameObject.AddComponent<MinimapRoom>();
                }

                minimapRoom.minimapParts = roomMinimapParts;
                minimapRoom.hideRoomsRenderersDuringInteractionCooldown = hideRoomRendererDuringCooldown;
                minimapRoom.hideRoomsRenderersDuringRemoteInteraction = hideRoomsRenderersDuringRemoteInteraction;
                minimapRoom.minimap = this;
                minimapRoom.minimapReferential = minimapReferential;
                minimapRoom.preventLocalRoomMove = preventLocalRoomMove;

                minimapRoom.moveRequester = r.moveRequester;
                _minimapGameObjects.Add(minimapRoomGameObject);
                _minimapRooms.Add(minimapRoom);
                if (minimapRoom.moveRequester != null && minimapRoom.moveRequester.RoomId.ToString() == IRLRoomManager.SharedInstance.localNetworkIRLRoomMember.RoomId.ToString())
                {
                    _localMinimapRoom = minimapRoom;
                }
            }

            foreach (var element in _sceneFocusedElements)
            {
                var minimapPartObject = new GameObject($"MinimapSceneElement-{element.name}");
                minimapPartObject.transform.parent = minimapReferential;
                minimapPartObject.transform.localScale = element.transform.lossyScale;

                var minimapPart = minimapPartObject.AddComponent<MinimapSceneFocusedElement>();
                minimapPart.ConfigurePositionProxy(sceneFocusedElement: element, this);
                _minimapGameObjects.Add(minimapPart.gameObject);
            }
        }

        [ContextMenu("CenterMinimapOnLocalRoom")]
        public void CenterMinimapOnLocalRoom()
        {
            if (_localMinimapRoom == null || Vector3.Distance(_lastLocalMinimapRoomPosition, _localMinimapRoom.transform.position) < 0.01f)
            {
                return;
            }
            // We avoid recentering the map while an interaction is pending
            if (status != Status.NoInteraction)
                return;
            if (minimapVisualPlate == null || minimapReferential == null || _localMinimapRoom == null)
                return;

            var plateTopPosition = transform.TransformPoint(_plateTopLocalPosition);
            var plateTopRotation = transform.rotation * _plateTopLocalRotation;

            var localRoomOffsetPosition = minimapReferential.transform.InverseTransformPoint(_localMinimapRoom.transform.position);
            var localRoomOffsetRotation = Quaternion.Inverse(minimapReferential.transform.rotation) * _localMinimapRoom.transform.rotation;
            (var newMinimapReferentialPosition, var newMinimapReferentialRotation) = TransformManipulations.ReferentialPositionToRespectOffsetsOfPositionedObject(minimapReferential, positionedObjectPosition: plateTopPosition, positionedObjectRotation: plateTopRotation, localRoomOffsetPosition, localRoomOffsetRotation, acceptLossyScale: true);

            minimapReferential.transform.rotation = newMinimapReferentialRotation;
            minimapReferential.transform.position = newMinimapReferentialPosition + 0.5f * (newMinimapReferentialRotation * Vector3.up) * _localMinimapRoom.transform.lossyScale.y;
            _lastLocalMinimapRoomPosition = _localMinimapRoom.transform.position;
        }

        [ContextMenu("ScaleMinimapPlateUnderRooms")]
        public void ScaleMinimapPlateUnderRooms()
        {
            if (_plateUpdateRequired == false) return;
            _plateUpdateRequired = false;

            var plateTopPosition = transform.TransformPoint(_plateCenterLocalPosition);
            var plateTopRotation = transform.rotation * _plateTopLocalRotation;

            var orientedBounds = new OrientedBounds(initialCenter: plateTopPosition, plateTopRotation, initialSize: _defaultPlateWorldScale);
            foreach (var minimapRoom in _minimapRooms)
            {
                foreach (var roomLocalPoint in _roomLocalPointsToEncapsulate)
                {
                    var roomPoint = minimapRoom.transform.TransformPoint(roomLocalPoint);
                    orientedBounds.Encapsulate(roomPoint, ignoreYAxis: true);
                }
            }
            foreach (var commonRoomLocalPoint in _roomLocalPointsToEncapsulate)
            {
                var roomPoint = _minimapCommonRoom.transform.TransformPoint(commonRoomLocalPoint);
                orientedBounds.Encapsulate(roomPoint, ignoreYAxis: true);
            }
            orientedBounds.ApplyToTransform(minimapVisualPlate.transform, ignoreParentScale: false);
        }

        public void RegisterSceneFocusedElement(SceneFocusedElement element)
        {
            if (_sceneFocusedElements.Contains(element) == false)
            {
                _sceneFocusedElements.Add(element);
                if (IsMinimapActive)
                    GenerateMinimap();
            }
        }

        public void UnregisterSceneFocusedElement(SceneFocusedElement element)
        {
            if (_sceneFocusedElements.Contains(element))
            {
                _sceneFocusedElements.Remove(element);
                if (IsMinimapActive)
                    GenerateMinimap();
            }
        }

        #region IRLRoomManager.IIRLRoomManagerListener
        public void OnRoomCreate(string roomId)
        {
            if (IsMinimapActive)
                GenerateMinimap();
        }

        public void OnRoomDelete(string roomId)
        {
            if (IsMinimapActive)
                GenerateMinimap();
        }

        public void OnRoomMemberLeaving(string roomId)
        {
            if (IsMinimapActive)
                RequestGenerateMinimap();
        }

        public void OnAssociatedPartRegistration(NetworkIRLRoomAssociatedPart part)
        {
            if (IsMinimapActive)
                GenerateMinimap();
        }
        #endregion
    }
}

