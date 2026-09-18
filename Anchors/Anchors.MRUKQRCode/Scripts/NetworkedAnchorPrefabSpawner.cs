using Fusion;
#if MRUK_AVAILABLE
using Fusion.Addons.Meta;
#endif
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Meta.XR.MRUtilityKit
{
#if MRUK_AVAILABLE
    /// <summary>
    /// Subclass of AnchorPrefabSpawner that wait for the local user to be connected to Fusion before spawning the prefab for the various room elements.
    /// The spawned prefabs are expected to be NetworkedObjects, that will be spawned on Fusion
    /// </summary>
    public class NetworkedAnchorPrefabSpawner : AnchorPrefabSpawner, INetworkRunnerCallbacks
    {
        public static NetworkedAnchorPrefabSpawner SharedInstance = null;
        public static bool IsAvailable => SharedInstance != null;

        [Tooltip("If true, the renderer in the spawned object won't be visible for the locla user (relevant if the room visulisation is relevant for remote users only. For more control, use NetworkIRLRoomassociatedpart on spawned furniture parts, and configure visiiblity with IRLRoomManager.RoomAssociatedPartDisplayMode")]
        [SerializeField] private bool hideRenderForLocalUser = true;
        [SerializeField] private NetworkObject MRUKRoomNetworkObjectPrefab;

        [Header("Automatically set")]
        [SerializeField] private NetworkRunner runner;
        public Dictionary<MRUKAnchor, NetworkObject> AnchorPrefabSpawnerNetworkObjects { get; } = new();
        public Dictionary<MRUKRoom, NetworkObject> MRUKRoomNetworkObjects { get; } = new();

        bool _callBackRegistered = false;
        private static readonly string Suffix = "(PrefabSpawner Clone)";

        [Header("Debug")]
        [Tooltip("Specific non networked spawnable object, for MRUKAnchor.SceneLabels.WALL_FACE elements.")]
        public GameObject localWallPrefab = null;

        [System.Serializable]
        public class FallbackFurniture
        {
            public bool shouldSpawn = true;
            public MRUKAnchor.SceneLabels labels;
            [Tooltip("All fakePositionTransformsRoot child Trasnforms will be added to fakePositionTransforms")]
            public Transform fakePositionTransformsRoot;
            public List<Transform> fakePositionTransforms = new List<Transform>();
        }

        [Header("Issue handling")]
        [Tooltip("If true, after removing the headset, when putting it back, a scene scan clear and reload is triggered, to counter headset orientation loss. Note that all previous room element will be despawned")]
        public bool resetRoomScanOnHMDReturn = true;

        [Tooltip("Used when there is no MRUK room")]
        public List<FallbackFurniture> fallbackFurnituresPositions = new List<FallbackFurniture>();

        bool _shouldSpawnPrefabs = false;
        MRUKPermissionWaiter _mrukPermissionWaiter;
        bool _isPaused = false;
        bool _isFocused = true;
        float _endOfMRUKLoadSceneTimeout = -1;
        float _mrukLoadSceneDelay = 1f;
        bool _hasBeenWaitingForPermissions = false;
        bool _roomResetRequired = false;

        public enum Status
        {
            NotSpawned,
            Spawned,
            SpawnedWithFallbackFurniturePositions,
            WaitingForPermissions,
            WaitingForMRUKSceneLoadedCallback,
            WaitingForAppFocus,
            WaitingForAppOtherPotentialPermissionRequests
        }

        public Status status = Status.NotSpawned;

        private void Awake()
        {
            SpawnOnStart = MRUK.RoomFilter.None;
            _mrukPermissionWaiter = GetComponent<MRUKPermissionWaiter>();
        }

        private void OnEnable()
        {
            if (SharedInstance == null)
                SharedInstance = this;
#if OCULUS_SDK_AVAILABLE
            OVRManager.HMDMounted += OnOVRManagerHMDMounted;
            OVRManager.HMDUnmounted += OnOVRManagerHMDUnmounted;
#endif    
        }

        private void OnDisable()
        {
            if (SharedInstance == this)
                SharedInstance = null;
#if OCULUS_SDK_AVAILABLE
            OVRManager.HMDMounted -= OnOVRManagerHMDMounted;
            OVRManager.HMDUnmounted -= OnOVRManagerHMDUnmounted;
#endif      
        }

        private void Update()
        {
            FindRunner();
            if (_shouldSpawnPrefabs)
            {
                TryToSpawnPrefabs();
            }
        }

        protected virtual void FindRunner()
        {
            if (_callBackRegistered) return;

            if (runner == null || runner.IsRunning == false)
            {
                // Try to find a runner
                runner = NetworkRunner.GetRunnerForGameObject(gameObject);
            }

            if (runner != null && runner.IsRunning)
            {
                runner.AddCallbacks(this);
                _callBackRegistered = true;
            }
        }

        #region AnchorPrefabSpawner overrides
        protected override void SpawnPrefab(MRUKAnchor anchorInfo)
        {
            if (_callBackRegistered)
            {
                SpawnNetworkedPrefab(anchorInfo);
            }
            else
            {
                Debug.LogError("Issue on callback registration");
            }
        }
        #endregion

        private void SpawnNetworkedPrefab(MRUKAnchor anchorInfo)
        {
            NetworkObject roomNetworkObject = null;

            var prefabToCreate = LabelToPrefab(anchorInfo.Label, anchorInfo, out var prefabGroup);
            if (prefabToCreate == null)
            {
                return;
            }

            if (AnchorPrefabSpawnerNetworkObjects.ContainsKey(anchorInfo))
            {
                Debug.LogWarning("Anchor already associated with a gameobject spawned from this AnchorPrefabSpawner");
                return;
            }

            // Spawing MRUKRoom NetworkObject
            //Debug.LogError($"anchorInfo.Room = {anchorInfo.Room}");
            if (MRUKRoomNetworkObjects.ContainsKey(anchorInfo.Room))
            {
                roomNetworkObject = MRUKRoomNetworkObjects[anchorInfo.Room];
            }
            else
            {
                if (runner == null || runner.IsRunning == false) Debug.LogError("Runner is null or not running");
                if (MRUKRoomNetworkObjectPrefab)
                {
                    roomNetworkObject = runner.Spawn(MRUKRoomNetworkObjectPrefab, Vector3.zero, Quaternion.identity);
                    MRUKRoomNetworkObjects.Add(anchorInfo.Room, roomNetworkObject);
                    roomNetworkObject.name = "MRUK NetWorkRoom " + anchorInfo.Room.ToString();
                }
            }

            // Spawning hardware room element
            Vector3 localPosition = Vector3.zero;
            Quaternion localRotation = Quaternion.identity;

            // Create a new instance of the prefab
            // We will translate location and scale differently depending on the label.
            //var prefab = Instantiate(prefabToCreate, anchorInfo.transform);

            var roomElement = runner.Spawn(prefabToCreate, onBeforeSpawned: (runner, spawnedRoomElement) => {
                spawnedRoomElement.name = string.Concat(prefabToCreate.name, Suffix);
                spawnedRoomElement.name = prefabToCreate.name + Suffix;

                // prefab.transform.parent = anchorInfo.transform;
                if (roomNetworkObject) spawnedRoomElement.transform.parent = roomNetworkObject.transform;

                var prefabBounds = prefabGroup.IgnorePrefabSize ? null : Utilities.GetPrefabBounds(prefabToCreate);

                var resizer = spawnedRoomElement.GetComponentInChildren<GridSliceResizer>(true);
                if (!prefabBounds.HasValue && resizer)
                {
                    prefabBounds = resizer.OriginalMesh.bounds;
                }

                var prefabSize = prefabBounds?.size ?? Vector3.one;

                if (anchorInfo.VolumeBounds.HasValue)
                {
                    var cardinalAxisIndex = 0;
                    if (prefabGroup.CalculateFacingDirection && !prefabGroup.MatchAspectRatio)
                    {
                        GetDirectionAwayFromClosestWall(MRUK.Instance.GetCurrentRoom(), anchorInfo, out cardinalAxisIndex);
                    }

                    var volumeBounds = RotateVolumeBounds(anchorInfo.VolumeBounds.Value,
                        cardinalAxisIndex);

                    var volumeSize = volumeBounds.size;
                    var scale = new Vector3(volumeSize.x / prefabSize.x, volumeSize.z / prefabSize.y,
                        volumeSize.y / prefabSize.z); // flipped z and y to correct orientation

                    if (prefabGroup.MatchAspectRatio)
                    {
                        MatchAspectRatio(anchorInfo, prefabGroup.CalculateFacingDirection,
                            prefabSize, volumeSize, ref cardinalAxisIndex, ref volumeBounds, ref scale);
                    }

                    scale = prefabGroup.Scaling == ScalingMode.Custom
                        ? CustomPrefabScaling(scale)
                        : AnchorPrefabSpawnerUtilities.ScalePrefab(scale, prefabGroup.Scaling);

                    localPosition = prefabGroup.Alignment == AlignMode.Custom
                         ? CustomPrefabAlignment(volumeBounds, prefabBounds)
                         : AnchorPrefabSpawnerUtilities.AlignPrefabPivot(volumeBounds, prefabBounds, scale,
                             prefabGroup.Alignment);

                    // scene geometry is unusual, we need to swap Y/Z for a more standard prefab structure
                    localRotation = Quaternion.Euler((cardinalAxisIndex - 1) * 90, -90, -90); ;
                    spawnedRoomElement.transform.localScale = scale;
                }
                else if (anchorInfo.PlaneRect.HasValue)
                {
                    var planeSize = anchorInfo.PlaneRect.Value.size;
                    var scale = new Vector2(planeSize.x / prefabSize.x, planeSize.y / prefabSize.y);

                    spawnedRoomElement.transform.localScale = prefabGroup.Scaling == ScalingMode.Custom
                        ? CustomPrefabScaling(scale)
                        : AnchorPrefabSpawnerUtilities.ScalePrefab(scale, prefabGroup.Scaling);

                    spawnedRoomElement.transform.localPosition = prefabGroup.Alignment == AlignMode.Custom
                        ? CustomPrefabAlignment(anchorInfo.PlaneRect.Value, prefabBounds)
                        : AnchorPrefabSpawnerUtilities.AlignPrefabPivot(anchorInfo.PlaneRect.Value, prefabBounds, scale,
                            prefabGroup.Alignment);
                }

                MeshRenderer prefabRenderer = spawnedRoomElement.GetComponentInChildren<MeshRenderer>();
                if (prefabRenderer != null)
                {
                    prefabRenderer.enabled = !hideRenderForLocalUser;
                }

                var roomElementPosition = anchorInfo.transform.TransformPoint(localPosition);
                var roomElementRotation = anchorInfo.transform.rotation * localRotation;
                spawnedRoomElement.transform.position = roomElementPosition;
                spawnedRoomElement.transform.rotation = roomElementRotation;

                if (anchorInfo.Label == MRUKAnchor.SceneLabels.WALL_FACE)
                {
                    CreateLocalWallPrefab(spawnedRoomElement.gameObject, roomElementPosition, roomElementRotation);
                }
            });

            AnchorPrefabSpawnerNetworkObjects.Add(anchorInfo, roomElement);
        }

        void CreateLocalWallPrefab(GameObject referenceObjectToCopy, Vector3 position, Quaternion rotation)
        {
            if (localWallPrefab == null) return;

            var wall = GameObject.Instantiate(localWallPrefab, referenceObjectToCopy.transform.position, referenceObjectToCopy.transform.rotation);
            wall.transform.localScale = referenceObjectToCopy.transform.lossyScale;
        }

        private void UpdateNetworkObjectVisibility()
        {

            foreach (var anchor in AnchorPrefabSpawnerNetworkObjects)
            {
                NetworkObject no = anchor.Value;
                if (no == null) continue;

                MeshRenderer renderer = no.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = !hideRenderForLocalUser;
                }
            }
        }

        [ContextMenu("Reload scene")]
        public async void ReloadScene()
        {
            Debug.Log("[NetworkedAnchorPrefabSpawner] ReloadScene");
            ClearPrefabs();
            MRUK.Instance.ClearScene();
            await MRUK.Instance.LoadSceneFromDevice();
            SpawnPrefabs();
        }

        #region Deletion
        protected override void ClearPrefabs()
        {
            foreach (var kv in AnchorPrefabSpawnerNetworkObjects)
            {
                ClearNetworkedPrefab(kv.Value);
            }
            AnchorPrefabSpawnerNetworkObjects.Clear();

            ClearMRUKNetworkRooms();
        }

        protected override void ClearPrefabs(MRUKRoom room)
        {
            List<MRUKAnchor> anchorsToRemove = new();
            foreach (var kv in AnchorPrefabSpawnerNetworkObjects)
            {
                if (kv.Key.Room != room)
                {
                    continue;
                }

                ClearNetworkedPrefab(kv.Value);
                anchorsToRemove.Add(kv.Key);
            }

            foreach (var anchor in anchorsToRemove)
            {
                AnchorPrefabSpawnerNetworkObjects.Remove(anchor);
            }

            ClearMRUKNetworkRoom(room);
        }

        protected void ClearNetworkedPrefab(NetworkObject no)
        {
            runner.Despawn(no);
        }

        protected void ClearHardwareRoomElementObject(GameObject hardwareRoomElementGameObject)
        {
            Destroy(hardwareRoomElementGameObject);
        }

        private void ClearMRUKNetworkRooms()
        {
            var rooms = new List<MRUKRoom>(MRUKRoomNetworkObjects.Keys);
            foreach (var room in rooms)
            {
                if (MRUKRoomNetworkObjects.ContainsKey(room) == false) 
                    continue;
                ClearMRUKNetworkRoom(room);
            }

            MRUKRoomNetworkObjects.Clear();
        }


        private void ClearMRUKNetworkRoom(MRUKRoom room)
        {
            if (MRUKRoomNetworkObjects.ContainsKey(room))
            {
                runner.Despawn(MRUKRoomNetworkObjects[room]);
                MRUKRoomNetworkObjects.Remove(room);
            }
        }

        #endregion

        #region Computation
        Vector3 GetDirectionAwayFromClosestWall(MRUKRoom room, MRUKAnchor anchor, out int cardinalAxisIndex, List<int> excludedAxes = null)
        {
            float closestWallDistance = Mathf.Infinity;
            // Due to the odd rotation of anchors, we need to use transform.up here instead of transform.forward
            // as forward actually points upwards.
            Vector3 awayFromWall = anchor.transform.up;
            cardinalAxisIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                if (excludedAxes != null && excludedAxes.Contains(i))
                {
                    continue;
                }

                // shoot rays along cardinal directions
                Vector3 cardinalAxis = Quaternion.Euler(0, 90f * i, 0) * -anchor.transform.up;

                foreach (var wallAnchor in room.WallAnchors)
                {
                    if (wallAnchor.Raycast(new Ray(anchor.transform.position, cardinalAxis), closestWallDistance, out var outHit))
                    {
                        closestWallDistance = outHit.distance;
                        // whichever wall is closest, point Z-forward away from it
                        cardinalAxisIndex = i;
                        awayFromWall = -cardinalAxis;
                    }
                }
            }

            return awayFromWall;
        }

        Bounds RotateVolumeBounds(Bounds bounds, int rotation)
        {
            var center = bounds.center;
            var size = bounds.size;
            return rotation switch
            {
                1 => new Bounds(new Vector3(-center.y, center.x, center.z), new Vector3(size.y, size.x, size.z)),
                2 => new Bounds(new Vector3(-center.x, -center.x, center.z), size),
                3 => new Bounds(new Vector3(center.y, -center.x, center.z), new Vector3(size.y, size.x, size.z)),
                _ => bounds
            };
        }

        void MatchAspectRatio(MRUKAnchor anchorInfo, bool calculateFacingDirection, Vector3 prefabSize,
            Vector3 volumeSize, ref int cardinalAxisIndex, ref Bounds volumeBounds, ref Vector3 localScale)
        {
            var prefabSizeRotated = new Vector3(prefabSize.z, prefabSize.y, prefabSize.x);
            var scaleRotated = new Vector3(volumeSize.x / prefabSizeRotated.x,
                volumeSize.z / prefabSizeRotated.y, volumeSize.y / prefabSizeRotated.z);

            var distortion = Mathf.Max(localScale.x, localScale.z) / Mathf.Min(localScale.x, localScale.z);
            var distortionRotated = Mathf.Max(scaleRotated.x, scaleRotated.z) /
                                    Mathf.Min(scaleRotated.x, scaleRotated.z);

            var rotateToMatchAspectRatio = distortion > distortionRotated;
            if (rotateToMatchAspectRatio)
            {
                cardinalAxisIndex = 1;
            }

            if (calculateFacingDirection)
            {
                GetDirectionAwayFromClosestWall(MRUK.Instance.GetCurrentRoom(), anchorInfo, out cardinalAxisIndex,
                    rotateToMatchAspectRatio ? new List<int> { 0, 2 } : new List<int> { 1, 3 });
            }

            if (cardinalAxisIndex == 0 || !anchorInfo.VolumeBounds.HasValue)
            {
                return; // no need to update the volume bounds
            }

            // Update the volume bounds
            volumeBounds = RotateVolumeBounds(anchorInfo.VolumeBounds.Value, cardinalAxisIndex);
            volumeSize = volumeBounds.size;
            localScale = new Vector3(volumeSize.x / prefabSize.x, volumeSize.z / prefabSize.y,
                volumeSize.y / prefabSize.z); // flipped z and y to correct orientation
        }
        private GameObject LabelToPrefab(MRUKAnchor.SceneLabels labels, MRUKAnchor anchor,
              out AnchorPrefabGroup prefabGroup)
        {
            foreach (var item in PrefabsToSpawn)
            {
                if ((item.Labels & labels) == 0 || ((item.Prefabs == null ||
                                                     item.Prefabs.Count == 0) &&
                                                    item.PrefabSelection != SelectionMode.Custom))
                {
                    continue;
                }

                GameObject prefabObjectToSpawn = null;
                if (item.PrefabSelection == SelectionMode.Custom)
                {
                    prefabObjectToSpawn = CustomPrefabSelection(anchor, item.Prefabs);
                }
                else
                {
                    prefabObjectToSpawn =
                        AnchorPrefabSpawnerUtilities.SelectPrefab(anchor, item.PrefabSelection, item.Prefabs,
                            _random);
                }

                prefabGroup = item;
                return prefabObjectToSpawn;
            }

            prefabGroup = new();
            return null;
        }

        protected override void SpawnPrefabs(bool clearPrefabs = true)
        {
            base.SpawnPrefabs(clearPrefabs);
            if (MRUK.Instance.Rooms.Count == 0)
            {
                // Spawn fallback furniture
                foreach (var fallbackInfo in fallbackFurnituresPositions)
                {
                    if (fallbackInfo.shouldSpawn == false)
                    {
                        continue;
                    }
                    foreach (var item in PrefabsToSpawn)
                    {
                        if ((item.Labels & fallbackInfo.labels) == 0 || ((item.Prefabs == null ||
                                                             item.Prefabs.Count == 0) &&
                                                            item.PrefabSelection != SelectionMode.Custom))
                        {
                            continue;
                        }

                        var prefabObjectToSpawn = item.Prefabs[0];

                        if (fallbackInfo.fakePositionTransformsRoot != null)
                        {
                            foreach (Transform child in fallbackInfo.fakePositionTransformsRoot)
                            {
                                if (fallbackInfo.fakePositionTransforms.Contains(child) == false)
                                {
                                    fallbackInfo.fakePositionTransforms.Add(child);
                                }
                            }
                        }

                        foreach (var fakePositionTransform in fallbackInfo.fakePositionTransforms)
                        {
                            if (fakePositionTransform == null) continue;
                            var roomNetworkObject = runner.Spawn(prefabObjectToSpawn, fakePositionTransform.position, fakePositionTransform.rotation, onBeforeSpawned: (r, o) => {
                                o.name = "Fallback MRUK NetWorkRoom";
                                o.transform.localScale = fakePositionTransform.lossyScale;
                            });
                        }
                    }
                }
                status = Status.SpawnedWithFallbackFurniturePositions;
            }
            else
            {
                status = Status.Spawned;

            }
        }

        private void OnApplicationPause(bool pause)
        {
            _isPaused = pause;
        }

        private void OnApplicationFocus(bool focus)
        {
            _isFocused = focus;
        }


        void TryToSpawnPrefabs()
        {
            // Ensure that there is no permission waiting
            bool canSpawnPrefabs = _mrukPermissionWaiter == null || _mrukPermissionWaiter.isSceneLoadWaiting == false;
            _hasBeenWaitingForPermissions = _hasBeenWaitingForPermissions || (canSpawnPrefabs == false);

            if (canSpawnPrefabs == false)
            {
                status = Status.WaitingForPermissions;
            }
            if (canSpawnPrefabs && _mrukPermissionWaiter != null && _mrukPermissionWaiter.isSceneLoaded == false && _mrukPermissionWaiter.hasSceneLoadedBeenDelayed)
            {
                canSpawnPrefabs = false;
                status = Status.WaitingForMRUKSceneLoadedCallback;
            }

#if !UNITY_EDITOR
            // In editor, we might be having several editor on the same computer, so the focus might be hard to have
            if (canSpawnPrefabs && (_isPaused || _isFocused == false))
            {
                status = Status.WaitingForAppFocus;
                canSpawnPrefabs = false;
            }
#endif

            // We let MRUK load the scene properly before spawning the prefabs
            if (canSpawnPrefabs)
            {
                if (_endOfMRUKLoadSceneTimeout == -1 && _hasBeenWaitingForPermissions)
                {
                    // We delay a bit has we had a Android permission screen a moment ago: we might have another one displayed soon (microphone permissions, ...) that will send the app out of focus, and could cause issue in room analysis
                    _endOfMRUKLoadSceneTimeout = Time.time + _mrukLoadSceneDelay;
                    status = Status.WaitingForAppOtherPotentialPermissionRequests;
                }
                if (_endOfMRUKLoadSceneTimeout >= Time.time)
                {
                    // still delayed a bit
                    canSpawnPrefabs = false;
                }
            }
            else
            {
                _endOfMRUKLoadSceneTimeout = -1;
            }

            if (canSpawnPrefabs)
            {
                _shouldSpawnPrefabs = false;
                SpawnPrefabs();
            }
            else
            {
                _shouldSpawnPrefabs = true;
            }
        }
        #endregion

        #region OVRManagerHMDMount handling
#if OCULUS_SDK_AVAILABLE
        [ContextMenu("OnOVRManagerHMDMounted")]
        private void OnOVRManagerHMDMounted()
        {
            if (_roomResetRequired && resetRoomScanOnHMDReturn)
            {
                Debug.Log("The headset was unmounted, and is now put back: reset the user room, as the MR positioning might be lost, hence invalidating the room scan");
                ReloadScene();
            }
        }

        [ContextMenu("OnOVRManagerHMDUnmounted")]
        private void OnOVRManagerHMDUnmounted()
        {
            Debug.Log("OnOVRManagerHMDUnmounted: we will reset the user room on user return, as the MR positioning might be lost, hence invalidating the room scan");
            _roomResetRequired = true;
        }
#endif
        #endregion

        #region INetworkRunnerCallbacks
        public virtual void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (runner.LocalPlayer == player)
            {
                TryToSpawnPrefabs();
            }
        }
        #endregion

        #region INetworkRunnerCallbacks (unused)


        public void OnConnectedToServer(NetworkRunner runner)
        {
        }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
#if !FUSION_2_1_OR_NEWER
        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }
#endif
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
#if FUSION_2_1_OR_NEWER
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
#endif
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        #endregion
    }
#else
    public class NetworkedAnchorPrefabSpawner : MonoBehaviour { }
#endif
}
