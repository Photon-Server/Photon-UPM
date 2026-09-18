using Fusion.XR.Shared.Automatization;
using Fusion.XR.Shared.Core;
using Fusion.XR.Shared.Tools;
using Fusion.XR.Shared.Utils;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace Fusion.Addons.AnchorsAddon
{
    /// <summary>
    /// Resize a virtual room gameobject, to contain all the remote users walls, as well as portals on the intersection between the local user room and this virtual room
    /// Relevant to have a visibility on where remote users could move (to provide a visual context when they are inside the local user walls for instance)
    /// 
    /// Usually, the portal have a stencil buffer shader being the only way to see either:
    /// - the virtual room (DisplayLocalWallIntersectionPortalsAndRemoteUserRoom mode)
    /// - some non related distant background (DisplayLocalWallIntersectionPortalsOnly mode)
    /// 
    /// The layerVisibleThroughPortalStencilBufferOnly layer should be configured in the render pipeline settings to automatically require the stencil set by the portal shader (default is 1 for the material used in the MRRoomPortal prefab)
    /// </summary>
    public class MixedRealityRemoteUsersRoom : MonoBehaviour, IRLRoomManager.IIRLRoomManagerPartListener
    {
        public static MixedRealityRemoteUsersRoom SharedInstance;
        public static bool IsAvailable => SharedInstance != null;

        bool irlRoomManagerRegistered = false;

        [Header("Remote room configuration")]
        public GameObject remoteUsersRoom = null;
        const string PortalPrefabName = "MRRoomPortal";
        public Material remoteUsersRoomMaterial = null;
        public GameObject portalPrefab;

        public enum RemoteUsersRoomDisplayMode
        {
            DisplayLocalWallIntersectionPortalsOnly, // Allow to provide portal only, to show a remote background
            DisplayLocalWallIntersectionPortalsAndRemoteUserRoom, // Show portals alllowing to see the remote users rooms
            DisplayRemoteUserRoomOnly // Display the remote users room only (usual, only relevant for debugging)
        }

        [System.Serializable]
        public struct RemoteUsersRoomIntersection
        {
            public GameObject planeGameObject;
        }

        [Header("Display mode")]
        public RemoteUsersRoomDisplayMode remoteUsersRoomDisplayMode = RemoteUsersRoomDisplayMode.DisplayLocalWallIntersectionPortalsAndRemoteUserRoom;
        [Tooltip("Object that will be only activated when the display mode is set to RemoteUsersRoomDisplayMode.DisplayLocalWallIntersectionPortalsOnly. Should usually be only visible when seen after the portal shader is renderered - by default it is done by requiring a stencil, of value 1 in the default portal prefab material")]
        public GameObject backgroundObjectForDisplayLocalWallIntersectionPortalsOnly;
        [Tooltip("Layer that will be only visible when seen after the portal shader is renderered - by default it is done by requiring a stencil, of value 1 in the default portal prefab material")]
        public string remoteUsersRoomLayerForDisplayLocalWallIntersectionPortalsAndRemoteUserRoom = "VisibleThroughLocalWallsPortals";
        public string remoteUsersRoomLayerForDisplayRemoteUserRoomOnly = "Default";
        [Tooltip("If remote rooms have different ceiling/ground positioning than the local user, the remote room won't be align with the local user room. " +
            "To avoid that, this option, if true, makes sure du user the ceiling and ground position of the local user for the remote rooms, faking a more natural continuity")]
        public bool userLocalUserRoomVerticalPosition = true;

        [Header("Local user position handling - Room extension")]
        [Tooltip("If addLocalUserHeadsetToVirtualRoom add the user headset is out of the room, the room will be extended to include it. Relevant when the remote rooms are totally out of the local user room, and/or when the remote room external faces shader let them visible")]
        public bool addLocalUserHeadsetToVirtualRoom = true;
        public float localUserHeadsetMargin = 0.3f;
        [Tooltip("If addLocalUserHeadsetToVirtualRoom add the user headset is out of the room, the room will be extended to include it. After localUserOutRoomRefreshDelay, we check if we could decrease this room artificial extension by at least artificialRoomExtensionDecreaseTriggeringRoomresizing meter: if it is the case, we resize")]
        public float artificialRoomExtensionDecreaseTriggeringRoomresizing = 1f;
        [Tooltip("If addLocalUserHeadsetToVirtualRoom add the user headset is out of the room, the room will be extended to include it. After localUserOutRoomRefreshDelay, we check if we could decrease this room artificial extension")]
        public float localUserOutRoomRefreshDelay = 5;
        [Header("Local user position handling - Display")]
        public bool forceDisplayRemoteRoomWhenNoPortals = false;

        public bool isRemoteUsersRoomVisible = true;

        public interface IRemoteRoomCreationCustomizer
        {
            bool ShouldIncludeRemoteRoom(string roomId);
            Quaternion? RemoteUsersRoomRotation();
        }

        public IRemoteRoomCreationCustomizer creationCustomizer = null;

        [Header("Portals")]
        public List<RemoteUsersRoomIntersection> localWallsIntersectionPortals = new List<RemoteUsersRoomIntersection>();
        [Header("Automatically set")]
        public IRLRoomManager irlRoomManager = null;

        [Header("Event")]
        public UnityEvent onUpdateRemoteUsersRoom;

        IVisibility _remoteUsersRoomRenderersVisibility = null;
        RemoteUsersRoomDisplayMode _lastRemoteUsersRoomDisplayMode;

        // TODO Axis might be different for other wall detection system (ARFundation ,...)
        Vector3[] _wallEdges = new Vector3[] {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0)
        };

        bool _anyRemoteRoomPresent = false;
        bool _anyPortalsPresent = false;
        bool _localUserOutOfRoom = false;
        float _lastRoomUpdate = -1;
        public List<Transform> remoteUsersRoomAdditionalTransforms = new List<Transform>();
        bool _updateRemoteUsersRoomRequired = false;

        const int preconfiguredURPConfigPortalLayer = 11;

        private void Awake()
        {
            if (SharedInstance == null) 
                SharedInstance = this;

            if (string.IsNullOrEmpty(remoteUsersRoomLayerForDisplayLocalWallIntersectionPortalsAndRemoteUserRoom) == false)
            {
                int layer = LayerMask.NameToLayer(remoteUsersRoomLayerForDisplayLocalWallIntersectionPortalsAndRemoteUserRoom);
                if (layer == -1)
                {
                    Debug.LogError($"[MixedRealityRemoteUsersRoom] Please add a {remoteUsersRoomLayerForDisplayLocalWallIntersectionPortalsAndRemoteUserRoom} layer. If you want to use the Anchors add-on preconfigured URP files, please set this layer as the layer {preconfiguredURPConfigPortalLayer} (otherwise, change the layer mask of the layer requiring the stencil id set by the portal prefab shader, in your Universal Render Data config");
                }
            }

            FindIRLRoomManager();
            if(remoteUsersRoom == null)
            {
                Debug.Log("Missing remoteUsersRoom game object. Creating one.");
                remoteUsersRoom = GameObject.CreatePrimitive(PrimitiveType.Cube);
                remoteUsersRoom.name = "RemoteRoom";
                remoteUsersRoom.transform.parent = transform;
                remoteUsersRoom.transform.rotation = transform.rotation;
                remoteUsersRoom.transform.position = transform.position;
                remoteUsersRoom.AddComponent<Visibility>();
            }

            if (remoteUsersRoomMaterial)
            {
                foreach(var r in remoteUsersRoom.GetComponentsInChildren<Renderer>(true))
                {
                    r.material = remoteUsersRoomMaterial;
                }
            }
            if (_remoteUsersRoomRenderersVisibility == null && remoteUsersRoom)
            {
                _remoteUsersRoomRenderersVisibility = remoteUsersRoom.GetComponentInChildren<IVisibility>();
            }

            _lastRemoteUsersRoomDisplayMode = remoteUsersRoomDisplayMode;
            ChangeRemoteUsersRoomVisibility(false);
        }

        private void OnDestroy()
        {
            if(SharedInstance == this)
                SharedInstance = null;
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (portalPrefab == null)
            {
                if (AssetLookup.TryFindAsset(new XR.Shared.Automatization.AssetLookup.AssetLookupCriteria(PortalPrefabName, extension: "prefab", requiredPathElements: new string[] { "Anchors" }), out GameObject prefab))
                {
                    portalPrefab = prefab;
                }
            }
#endif
        }

        private void Update()
        {
            if (_lastRemoteUsersRoomDisplayMode != remoteUsersRoomDisplayMode)
            {
                _lastRemoteUsersRoomDisplayMode = remoteUsersRoomDisplayMode;
                RequestUpdateRemoteUsersRoom();
            }

            if (_anyRemoteRoomPresent && addLocalUserHeadsetToVirtualRoom)
            {
                var rig = HardwareRigsRegistry.GetHardwareRig();
                if (rig != null)
                {
                    var localPos = remoteUsersRoom.transform.InverseTransformPoint(rig.Headset.transform.position);
                    if (Mathf.Abs(localPos.x) > 0.5f || Mathf.Abs(localPos.y) > 0.5f || Mathf.Abs(localPos.z) > 0.5f)
                    {
                        // User head is out of the remote users room
                        RequestUpdateRemoteUsersRoom();
                    }

                    if (_localUserOutOfRoom && (Time.time - _lastRoomUpdate) > localUserOutRoomRefreshDelay)
                    {
                        // We check if the headset position change enough to justify changing the room size (the room was "extended" to include the headset position, but maybe now a smaller extension could be enough)
                        var scale = remoteUsersRoom.transform.lossyScale;
                        float maxMargin = 0;
                        if (Mathf.Abs(localPos.x) < 0.5f)
                        {
                            var margin = scale.x / 2f - Mathf.Abs(localPos.x) * scale.x;
                            if (margin > maxMargin) maxMargin = margin;
                        }
                        if (Mathf.Abs(localPos.z) < 0.5f)
                        {
                            var margin = scale.z / 2f - Mathf.Abs(localPos.z) * scale.z;
                            if (margin > maxMargin) maxMargin = margin;
                        }
                        // We recompute if 
                        if(maxMargin > artificialRoomExtensionDecreaseTriggeringRoomresizing)
                        {
                            RequestUpdateRemoteUsersRoom();
                        }
                    }
                }
            }

            if (_updateRemoteUsersRoomRequired)
            {
                UpdateRemoteUsersRoom();
                _updateRemoteUsersRoomRequired = false;
            }
        }

        void FindIRLRoomManager()
        {
            if (irlRoomManager == null)
            {
                irlRoomManager = FindAnyObjectByType<IRLRoomManager>();
            }
            if (irlRoomManager && irlRoomManagerRegistered == false)
            {
                irlRoomManagerRegistered = true;
                irlRoomManager.listeners.Add(this);
            }
        }

        bool ShouldIncludeRemoteRoom(string roomId)
        {
            bool shouldInclude = true;
            if (creationCustomizer != null)
            {
                shouldInclude = creationCustomizer.ShouldIncludeRemoteRoom(roomId);
            }
            return shouldInclude && irlRoomManager.knowRoomByRoomIds.ContainsKey(roomId);
        }

        Quaternion RemoteUsersRoomRotation()
        {
            if (creationCustomizer != null && creationCustomizer.RemoteUsersRoomRotation() is Quaternion rotation)
            {
                return rotation;
            }

            // Use closest local user wall rotation
            NetworkIRLRoomAssociatedPart closestWall = ClosestWall();

            return closestWall != null ? closestWall.transform.rotation : Quaternion.identity;
        }

        NetworkIRLRoomAssociatedPart ClosestWall()
        {
            List<NetworkIRLRoomAssociatedPart> localUserParts = LocalUserParts();
            var rig = HardwareRigsRegistry.GetHardwareRig();
            if (localUserParts == null || rig == null)
            {
                return null;
            }
            
            float minDistance = float.MaxValue;
            NetworkIRLRoomAssociatedPart closestWall = null;

            foreach (var wall in localUserParts)
            {
                if (wall.partType != NetworkIRLRoomAssociatedPart.PartType.Wall) continue;
                var distance = Vector3.Distance(rig.Headset.transform.position, wall.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestWall = wall;
                }
            }
            return closestWall;
        }

        [ContextMenu("UpdateRemoteUsersRoom")]
        public void UpdateRemoteUsersRoom()
        {
            var roomIds = new List<string>(irlRoomManager.knowRoomByRoomIds.Keys);

            var rotation = RemoteUsersRoomRotation();
            OrientedBounds roomBound = new OrientedBounds(rotation, 0.05f * Vector3.one);

            _anyRemoteRoomPresent = false;
            _anyPortalsPresent = false;

            foreach (var roomId in roomIds)
            {
                if (ShouldIncludeRemoteRoom(roomId) == false) continue;

                bool isLocalRoom = irlRoomManager.localNetworkIRLRoomMember && irlRoomManager.localNetworkIRLRoomMember.Object != null && irlRoomManager.localNetworkIRLRoomMember.RoomId.ToString() == roomId;
                
                // Find walls
                foreach (var part in irlRoomManager.knowRoomByRoomIds[roomId].associatedParts)
                {
                    if (part.partType != NetworkIRLRoomAssociatedPart.PartType.Wall) continue;
                    if (part.RoomId != roomId) continue;
                    if (part.IsOwnedByRoomMainMember == false) continue;
                    foreach (var wallEdge in _wallEdges)
                    {
                        var point = part.transform.TransformPoint(wallEdge);

                        if (isLocalRoom == false)
                        {
                            roomBound.Encapsulate(point);
                            _anyRemoteRoomPresent = true;
                        }
                    }
                }
            }

            foreach(var additionTransform in remoteUsersRoomAdditionalTransforms)
            {
                roomBound.Encapsulate(additionTransform.position);
                _anyRemoteRoomPresent = true;
            }

            roomBound.ApplyToTransform(remoteUsersRoom.transform);
            _localUserOutOfRoom = false;

            if (_anyRemoteRoomPresent && addLocalUserHeadsetToVirtualRoom)
            {
                var rig = HardwareRigsRegistry.GetHardwareRig();
                if (rig != null)
                {
                    var localPos = remoteUsersRoom.transform.InverseTransformPoint(rig.Headset.transform.position);
                    if (Mathf.Abs(localPos.x) > 0.5f || Mathf.Abs(localPos.y) > 0.5f || Mathf.Abs(localPos.z) > 0.5f)
                    {
                        roomBound.Encapsulate(rig.Headset.transform.position);
                        roomBound.Encapsulate(rig.Headset.transform.position - localUserHeadsetMargin * remoteUsersRoom.transform.right - localUserHeadsetMargin * remoteUsersRoom.transform.forward);
                        roomBound.Encapsulate(rig.Headset.transform.position + localUserHeadsetMargin * remoteUsersRoom.transform.right + localUserHeadsetMargin * remoteUsersRoom.transform.forward);
                        roomBound.Encapsulate(rig.Headset.transform.position);
                        roomBound.ApplyToTransform(remoteUsersRoom.transform);
                        _localUserOutOfRoom = true;
                    }
                }
            }

            if (userLocalUserRoomVerticalPosition)
            {
                NetworkIRLRoomAssociatedPart closestWall = ClosestWall();
                if (closestWall)
                {
                    remoteUsersRoom.transform.position = new Vector3(remoteUsersRoom.transform.position.x, closestWall.transform.position.y, remoteUsersRoom.transform.position.z);
                    remoteUsersRoom.transform.localScale = new Vector3(remoteUsersRoom.transform.localScale.x, closestWall.transform.localScale.y, remoteUsersRoom.transform.localScale.z);
                }
            }

            _lastRoomUpdate = Time.time;
            DisplayRemoteUsersRoom();

            if(onUpdateRemoteUsersRoom != null)
            {
                onUpdateRemoteUsersRoom.Invoke();
            }
        }

        void ChangeRemoteUsersRoomVisibility(bool visible)
        {
            isRemoteUsersRoomVisible = visible;
            if (_remoteUsersRoomRenderersVisibility != null)
            {
                _remoteUsersRoomRenderersVisibility.ChangeVisibility(visible);
            }
        }

        void DisplayRemoteUsersRoom()
        {
            if(_anyRemoteRoomPresent == false)
            {
                ClearIRLRoomIntersectionWithRemoteUsersRoom();
                ChangeRemoteUsersRoomVisibility(false);
                return;
            }

            switch (remoteUsersRoomDisplayMode)
            {
                case RemoteUsersRoomDisplayMode.DisplayLocalWallIntersectionPortalsOnly:
                    // We only display IRL room walls intersection with the remote users room. The remote users room is invisible
                    // Relevant to use this intersection as portals to a remote background
                    CreateIRLRoomIntersectionWithRemoteUsersRoom();
                    ChangeRemoteUsersRoomVisibility(false);
                    if (backgroundObjectForDisplayLocalWallIntersectionPortalsOnly)
                    {
                        backgroundObjectForDisplayLocalWallIntersectionPortalsOnly.SetActive(true);
                    }
                    break;

                case RemoteUsersRoomDisplayMode.DisplayLocalWallIntersectionPortalsAndRemoteUserRoom:
                    // We display both:
                    // - IRL room walls intersection with the remote users room
                    // - the remote users room
                    // This is relevant to see the remote users room only through those intersection sections (where it is needed)
                    CreateIRLRoomIntersectionWithRemoteUsersRoom();
                    ChangeRemoteUsersRoomVisibility(true);
                    if (string.IsNullOrEmpty(remoteUsersRoomLayerForDisplayLocalWallIntersectionPortalsAndRemoteUserRoom) == false)
                    {
                        LayerUtils.ApplyLayer(remoteUsersRoom.gameObject, remoteUsersRoomLayerForDisplayLocalWallIntersectionPortalsAndRemoteUserRoom, applyLayerToChildren: true);
                    }
                    if (backgroundObjectForDisplayLocalWallIntersectionPortalsOnly)
                    {
                        backgroundObjectForDisplayLocalWallIntersectionPortalsOnly.SetActive(false);
                    }
                    break;

                case RemoteUsersRoomDisplayMode.DisplayRemoteUserRoomOnly:
                    // We only display the remote users room (no wall intersections acting as portals)
                    DisplayRemoteUserRoomOnly();
                    break;
            }

            if (forceDisplayRemoteRoomWhenNoPortals && _anyRemoteRoomPresent == true && _anyPortalsPresent == false && remoteUsersRoomDisplayMode != RemoteUsersRoomDisplayMode.DisplayRemoteUserRoomOnly)
            {
                // No portals, but remote rooms are present: as forceDisplayRemoteRoomWhenNoPortals is true, we force display of remote users room
                DisplayRemoteUserRoomOnly();
            }
        }

        void DisplayRemoteUserRoomOnly()
        {
            ClearIRLRoomIntersectionWithRemoteUsersRoom();
            ChangeRemoteUsersRoomVisibility(true);
            if (string.IsNullOrEmpty(remoteUsersRoomLayerForDisplayRemoteUserRoomOnly) == false)
            {
                LayerUtils.ApplyLayer(remoteUsersRoom.gameObject, remoteUsersRoomLayerForDisplayRemoteUserRoomOnly, applyLayerToChildren: true);
            }
            if (backgroundObjectForDisplayLocalWallIntersectionPortalsOnly)
            {
                backgroundObjectForDisplayLocalWallIntersectionPortalsOnly.SetActive(false);
            }
        }

        void ClearIRLRoomIntersectionWithRemoteUsersRoom()
        {
            foreach (var intersection in localWallsIntersectionPortals)
            {
                if (intersection.planeGameObject)
                {
                    Destroy(intersection.planeGameObject);
                }
            }
            localWallsIntersectionPortals.Clear();
        }

        /// <summary>
        /// Return NetworkIRLRoomAssociatedPart that are corresponding to the local user
        /// </summary>
        List<NetworkIRLRoomAssociatedPart> LocalUserParts()
        {
            if (irlRoomManager == null || irlRoomManager.localNetworkIRLRoomMember == null)
            {
                return null;
            }
            var localNetworkIRLRoomMember = irlRoomManager.localNetworkIRLRoomMember;
            if (localNetworkIRLRoomMember.Object == null || irlRoomManager.knowRoomByRoomIds.ContainsKey(localNetworkIRLRoomMember.RoomId.ToString()) == false)
            {
                return null;
            }
            string localUserRoomId = localNetworkIRLRoomMember.RoomId.ToString();
            var localUserParts = new List<NetworkIRLRoomAssociatedPart>();

            foreach(var localRoomPart in irlRoomManager.knowRoomByRoomIds[localUserRoomId].associatedParts)
            {
                if (localRoomPart.ReferenceRoomMember == localNetworkIRLRoomMember)
                {
                    localUserParts.Add(localRoomPart);
                }
            }
            return localUserParts;
        }

        /// <summary>
        /// Create portals matching places where we want to see the common room
        /// </summary>
        void CreateIRLRoomIntersectionWithRemoteUsersRoom()
        {
            _anyPortalsPresent = false;

            if (portalPrefab == null)
            {
                Debug.LogError("Missing portal prefab");
            }

            List<NetworkIRLRoomAssociatedPart> localUserParts = LocalUserParts();
            if (localUserParts == null)
            {
                //Debug.LogError("[MixedRealityRemoteUsersRoom.CreateIRLRoomIntersectionWithRemoteUsersRoom] No local user walls: impossible to create wall/remote users room intersection portals");
                return;
            }

            ClearIRLRoomIntersectionWithRemoteUsersRoom();

            Vector3[] remoteUsersRoomPlaneCenters = new Vector3[] {
                remoteUsersRoom.transform.position + (remoteUsersRoom.transform.lossyScale.x / 2f) * remoteUsersRoom.transform.right,
                remoteUsersRoom.transform.position - (remoteUsersRoom.transform.lossyScale.x / 2f) * remoteUsersRoom.transform.right,
                remoteUsersRoom.transform.position + (remoteUsersRoom.transform.lossyScale.z / 2f) * remoteUsersRoom.transform.forward,
                remoteUsersRoom.transform.position - (remoteUsersRoom.transform.lossyScale.z / 2f) * remoteUsersRoom.transform.forward
            };

            Vector3[] remoteUsersRoomPlaneNormals = new Vector3[] {
                remoteUsersRoom.transform.right,
                -remoteUsersRoom.transform.right,
                remoteUsersRoom.transform.forward,
                -remoteUsersRoom.transform.forward
            };

            var remoteUsersRoomXWidth = remoteUsersRoom.transform.lossyScale.x;
            var rRemoteUsersRoomZWidth = remoteUsersRoom.transform.lossyScale.z;

            Dictionary<GameObject, Vector3> positionOfEdges = new Dictionary<GameObject, Vector3>();
            foreach (var wall in localUserParts)
            {
                if (wall.partType != NetworkIRLRoomAssociatedPart.PartType.Wall) continue;

                var intersection = new RemoteUsersRoomIntersection();
                if (wall.RoomId != irlRoomManager.localNetworkIRLRoomMember.RoomId.ToString())
                {
                    continue;
                }

                var wallWidth = wall.transform.lossyScale.x;
                var pointA = wall.transform.position - (wallWidth / 2f) * wall.transform.right;
                var pointB = wall.transform.position + (wallWidth / 2f) * wall.transform.right;

                var pointAProj = pointA;
                var pointBProj = pointB;
                float aProjDistance = float.MaxValue;
                float bProjDistance = float.MaxValue;

                bool pointAInsideRemoteUsersRoom = IsInsideRemoteUsersRoom(pointA);
                bool pointBInsideRemoteUsersRoom = IsInsideRemoteUsersRoom(pointB);


                for (int i = 0; i < remoteUsersRoomPlaneCenters.Length; i++)
                {
                    var remoteUsersRoomPlane = new Plane(remoteUsersRoomPlaneNormals[i], remoteUsersRoomPlaneCenters[i]);
                    if (pointAInsideRemoteUsersRoom == false && remoteUsersRoomPlane.Raycast(new Ray(pointA, pointB - pointA), out var distanceA))
                    {
                        // We check if this cnadidate could be a closer projection of AB
                        if (distanceA < aProjDistance)
                        {
                            // We check if the projection is inside the remote users room boundaries
                            var candidatePointAProj = pointA + (pointB - pointA).normalized * distanceA;
                            if (IsInsideRemoteUsersRoom(candidatePointAProj))
                            {
                                aProjDistance = distanceA;
                                pointAProj = candidatePointAProj;
                            }
                        }
                    }
                    if (pointBInsideRemoteUsersRoom == false && remoteUsersRoomPlane.Raycast(new Ray(pointB, pointA - pointB), out var distanceB))
                    {
                        // We check if this cnadidate could be a closer projection of BA
                        if (distanceB < bProjDistance)
                        {
                            var candidatePointBProj = pointB + (pointA - pointB).normalized * distanceB;
                            if (IsInsideRemoteUsersRoom(candidatePointBProj))
                            {
                                bProjDistance = distanceB;
                                pointBProj = candidatePointBProj;
                            }
                        }
                    }
                }

                if (IsInsideRemoteUsersRoom(pointAProj) && IsInsideRemoteUsersRoom(pointBProj))
                {
                    var wallIndex = localWallsIntersectionPortals.Count;

                    var planeCenter = pointAProj + (pointBProj - pointAProj) / 2f;
                    var planeScale = new Vector3(Vector3.Magnitude((pointBProj - pointAProj)), wall.transform.lossyScale.y, wall.transform.lossyScale.z);

                    var planeObj = GameObject.Instantiate(portalPrefab);
                    planeObj.name += $"-PortalToRemoteUsersRoom";
                    planeObj.transform.rotation = wall.transform.rotation;
                    planeObj.transform.position = planeCenter;
                    planeObj.transform.localScale = planeScale;

                    intersection.planeGameObject = planeObj;
                    _anyPortalsPresent = true;

                    if (pointAProj == pointA)
                    {
                        AddEdge(pointA, planeObj, ref positionOfEdges, isRightSide: true);
                    }
                    if (pointBProj == pointB)
                    {
                        AddEdge(pointB, planeObj, ref positionOfEdges, isRightSide: false);
                    }
                }
                localWallsIntersectionPortals.Add(intersection);
            }            
        }

        void AddEdge(Vector3 position, GameObject portalObj, ref Dictionary<GameObject, Vector3> positionOfEdges, bool isRightSide)
        {
            var portal = portalObj.GetComponentInChildren<MRRoomPortal>();
            if (portal == null) return;

            // We check if the new edge position matches a previous edge position: in this case, we "merge" them (by hiding the border game objects, both for this new edge, and for the existing one that was at this position)
            var borderObjects = isRightSide?portal.rightSideBorderObjects:portal.leftSideBorderObjects;
            bool isMerged = false;
            position = new Vector3(position.x, 0, position.z);
            foreach(var edge in positionOfEdges.Keys)
            {
                var edgePos = positionOfEdges[edge];

                var distance = Vector3.Distance(position, edgePos);
                if(distance < 0.01f)
                {
                    isMerged = true;
                    // Hide existing edge
                    edge.SetActive(false);
                }
            }
            if(isMerged == false)
            {
                // Not merge, we add it to the list to double check later on if new edges could be at the same position, needing to be merged
                foreach (var borderObject in borderObjects)
                {
                    positionOfEdges.Add(borderObject, position);
                }
            }
            else
            {
                foreach (var borderObject in borderObjects)
                {
                    // Hide new edge
                    borderObject.SetActive(false);
                }
            }
        }

        bool IsInsideRemoteUsersRoom(Vector3 pos)
        {
            var relativePos = remoteUsersRoom.transform.InverseTransformPoint(pos);
            bool insideRemoteUsersRoom = Mathf.Abs(relativePos.x) < 0.51f && Mathf.Abs(relativePos.z) < 0.51f;
            return insideRemoteUsersRoom;
        }

        public void RequestUpdateRemoteUsersRoom()
        {
            _updateRemoteUsersRoomRequired = true;

        }

        public void RegisterRemoteUsersRoomAdditionalTransforms(Transform t)
        {
            if (remoteUsersRoomAdditionalTransforms.Contains(t) == false)
            {
                remoteUsersRoomAdditionalTransforms.Add(t);
                RequestUpdateRemoteUsersRoom();
            }
        }
        public void UnregisterRemoteUsersRoomAdditionalTransforms(Transform t)
        {
            if (remoteUsersRoomAdditionalTransforms.Contains(t) )
            {
                remoteUsersRoomAdditionalTransforms.Remove(t);
                RequestUpdateRemoteUsersRoom();
            }
        }

        #region IRLRoomManager.IIRLRoomManagerPartListener
        public void OnAssociatedPartPoseChange(NetworkIRLRoomAssociatedPart part)
        {
            if (part.partType == NetworkIRLRoomAssociatedPart.PartType.Wall && (part.IsOwnedByRoomMainMember || part.ReferenceRoomMember == irlRoomManager.localNetworkIRLRoomMember))
            {
                UpdateRemoteUsersRoom();
            }
        }

        public void OnRoomCreate(string roomId) {
            UpdateRemoteUsersRoom();
        }

        public void OnRoomDelete(string roomId) {
            UpdateRemoteUsersRoom();
        }
        #endregion

    }
}


