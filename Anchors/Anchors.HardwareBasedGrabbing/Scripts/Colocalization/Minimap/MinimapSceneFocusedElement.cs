using Fusion.XR.Shared.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Fusion.Addons.AnchorsAddon.Colocalization.Minimap
{
    [DefaultExecutionOrder(EXECUTION_ORDER)]
    public class MinimapSceneFocusedElement : MonoBehaviour
    {
        public const int EXECUTION_ORDER = NetworkIRLRoomAssociatedPart.EXECUTION_ORDER + 10;
        public SceneFocusedElement sceneFocusedElement;
        List<GameObject> _minimapRenderers = new List<GameObject>();
        IRLRoomsMinimap _minimap;

        public void ConfigurePositionProxy(SceneFocusedElement sceneFocusedElement, IRLRoomsMinimap minimap)
        {
            this.sceneFocusedElement = sceneFocusedElement;
            _minimap = minimap;

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


        #region Renderers handling

        void SetupRenderers()
        {
            GameObject prefab = sceneFocusedElement.minimapPrefab;
            Transform followedTransform = sceneFocusedElement.transform;
            string rendererBaseName = $"{sceneFocusedElement.name}";
            var scale = followedTransform.localScale;

            CreateFollowRenderer(baseName: rendererBaseName, prefab, followedTransform, scale); ;
        }

        void CreateFollowRenderer(string baseName, GameObject prefab, Transform followedTransform, Vector3 scale)
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
            rendererProxy.adaptScale = true;

            var collider = minimapRenderer.GetComponent<Collider>();
            if (collider)
            {
                collider.enabled = false;
            }
        }
        #endregion
    }

}
