using Fusion.XR.Shared.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.AnchorsAddon.Colocalization.Minimap
{
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    public class MinimapRoomAssociatedPart : MonoBehaviour
    {
        public const int EXECUTION_ORDER = NetworkIRLRoomAssociatedPart.EXECUTION_ORDER + 10;
        public NetworkIRLRoomAssociatedPart roomPart;
        public LocalPoseProxy localPoseProxy;
        public bool displayOnlyMainMemberAssociatedParts = true;

        List<GameObject> _minimapRenderers = new List<GameObject>();
        IRLRoomsMinimap _minimap;
        bool _lastShouldDisplay = true;
        bool _lastDisplayOnlyMainMemberAssociatedParts = true;
        bool isMainMemberPart = false;

        bool IsLocalRoomPart => roomPart.RoomId == IRLRoomManager.SharedInstance.localNetworkIRLRoomMember?.RoomId;

        private void Awake()
        {
            _lastDisplayOnlyMainMemberAssociatedParts = displayOnlyMainMemberAssociatedParts;
        }
        public void ConfigurePositionProxy(NetworkIRLRoomAssociatedPart roomPart, IRLRoomsMinimap minimap)
        {
            this.roomPart = roomPart;
            _minimap = minimap;

            localPoseProxy = gameObject.AddComponent<LocalPoseProxy>();
            localPoseProxy.source = roomPart.transform;
            localPoseProxy.target = transform;
            localPoseProxy.targetReferential = minimap.minimapReferential;

            localPoseProxy.AdaptPosition();

            roomPart.onPreviewing.AddListener(OnPreviewing);
            roomPart.onStopPreviewing.AddListener(OnStopPreviewing);
            SetupRenderers();
        }

        private void OnDestroy()
        {
            foreach (var o in _minimapRenderers)
            {
                Destroy(o);
            }
            _minimapRenderers.Clear();
        }

        private void Update()
        {
            if(roomPart == null || roomPart.Object == null || roomPart.Object.IsValid == false)
            {
                return;
            }

            isMainMemberPart = roomPart.Object && roomPart.Object.IsValid && roomPart.ReferenceRoomMember == IRLRoomManager.SharedInstance.RoomMainMember(roomPart.RoomId);
            var shouldDisplay = displayOnlyMainMemberAssociatedParts == false || isMainMemberPart;
            if (_lastShouldDisplay != shouldDisplay || _lastDisplayOnlyMainMemberAssociatedParts != displayOnlyMainMemberAssociatedParts)
            {
                foreach(var rendererObject in _minimapRenderers)
                {
                    foreach(var renderer in rendererObject.GetComponentsInChildren<Renderer>())
                    {
                        renderer.enabled = shouldDisplay;
                    }
                }
                _lastShouldDisplay = shouldDisplay;
                _lastDisplayOnlyMainMemberAssociatedParts = displayOnlyMainMemberAssociatedParts;
            }
        }

        void OnPreviewing()
        {
            localPoseProxy.disableLateUpdateAdaptation = true;
        }

        void OnStopPreviewing()
        {
            localPoseProxy.disableLateUpdateAdaptation = false;
        }

        #region Renderers handling

        void SetupRenderers()
        {
            GameObject prefab = null;
            Transform followedTransform = roomPart.transform;
            Material overrideMaterial = IsLocalRoomPart ? _minimap.materialConfiguration.localUserMaterial : _minimap.materialConfiguration.remoteUserMaterial;
            string rendererBaseName = $"{roomPart.partType}-{name}";
            var scale = followedTransform.localScale;

            if (roomPart.partType == NetworkIRLRoomAssociatedPart.PartType.User)
            {
                prefab = _minimap.minimapUserPrefab;
                if (roomPart.HasStateAuthority && _minimap.minimapLocalUserPrefab != null)
                {
                    prefab = _minimap.minimapLocalUserPrefab;
                }
                followedTransform = roomPart.transform;
                overrideMaterial = null;
                if (roomPart.TryGetComponent<NetworkRig>(out var rig))
                {
                    followedTransform = rig.Headset.transform;
                }
            }
            if (roomPart.partType == NetworkIRLRoomAssociatedPart.PartType.Wall)
            {
                prefab = _minimap.minimapWallPrefab;
            }
            if (roomPart.partType == NetworkIRLRoomAssociatedPart.PartType.Table)
            {
                prefab = _minimap.minimapTablePrefab;
            }
            CreateFollowRenderer(baseName: rendererBaseName, prefab, followedTransform, scale, overrideMaterial); ;
        }

        void CreateFollowRenderer(string baseName, GameObject prefab, Transform followedTransform, Vector3 scale, Material overrideMaterial)
        {
            if (prefab == null) return;
            var minimapRenderer = GameObject.Instantiate(prefab);
            _minimapRenderers.Add(minimapRenderer);
            minimapRenderer.transform.parent = _minimap.minimapReferential;
            minimapRenderer.transform.localScale = scale;
            minimapRenderer.name = $"Renderer-{baseName}";
            var rendererProxy = minimapRenderer.AddComponent<LocalPoseProxy>();
            rendererProxy.source = followedTransform;
            rendererProxy.target = minimapRenderer.transform;
            rendererProxy.targetReferential = _minimap.minimapReferential;
            if (overrideMaterial != null)
            {
                foreach (var r in minimapRenderer.GetComponentsInChildren<Renderer>())
                {
                    r.material = overrideMaterial;
                }
            }
            var collider = minimapRenderer.GetComponent<Collider>();
            if (collider)
            {
                collider.enabled = false;
            }
            _lastShouldDisplay = true;
            _lastDisplayOnlyMainMemberAssociatedParts = displayOnlyMainMemberAssociatedParts;
        }
        #endregion
    }

}
