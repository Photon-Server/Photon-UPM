using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Fusion.XR.Shared.Core
{
    // Interface to allow components to customize the logic of any parent or child RigPartVisualizer, to either ignore a renderer or add addition constraints before showing it 
    public interface IRigPartVisualizerCustomizer
    {
        public bool ShouldIgnoreRenderer(Renderer r);
        public bool ShouldCustomizeRendererShouldDisplay(Renderer r, out bool shouldDisplay);
    }

    // Interface to track to automatically add game object to objects to adapt
    public interface IRigPartVisualizerGameObjectToAdapt : IUnityBehaviour { }

    /// <summary>
    /// Will display/hide all renderers in renderersToAdapt, accordingly to display mode (when offline, when offline, never, always)
    /// Can replace the material, instead of hidden the renderer, when ShouldNotDisplayMaterial is set.
    /// 
    /// If adaptRenderersDuringUpdate is set to false, another component can customize the adaptation, by calling AdaptRenderers(shouldDisplay) manually.
    ///  This component can use ShouldDisplay() as a starting value, and then customize it
    ///  
    /// The renderer enable adaption is done in Update. To override it in another component, use LateUpdate
    /// </summary>

    [DefaultExecutionOrder(100_000)]
    public class RigPartVisualizer : MonoBehaviour
    {
        [Header("Renderer adaptation configuraiton")]
        [Tooltip("If true, automatically fills renderersToAdapt (unless renderersToAdapt is not empty)")]
        public bool autofillRenderersToAdapt = true;
        [Tooltip("Automatically filled with all children renderers if empty and autofillRendererToAdapt is true")]
        public List<Renderer> renderersToAdapt = new List<Renderer>();
        [Tooltip("Renderer that should not be included in renderersToAdapt (mostly useful when it is automatically filled)")]
        public List<Renderer> renderersToIgnore = new List<Renderer>();
        [Tooltip("If not null, instead of being hidden, a renderer that should not be displayed will receive this material. " +
            "Useful for transparent material, used when a animator should still run to animate bones in order to have a position" +
            " - on Android, a disabled renderer would not animate the skeleton")]
        public Material materialWhileShouldNotDisplay;
        protected Dictionary<Renderer, Material> overridenRendererInitialMaterial = new Dictionary<Renderer, Material>();
        protected Dictionary<Renderer, bool> renderersInitialEnabled = new Dictionary<Renderer, bool>();

        [Header("Canvas adaptation configuraiton")]
        [Tooltip("If true, automatically fills canvasesToAdapt (unless canvasesToAdapt is not empty)")]
        public bool autofillCanvasesToAdapt = true;
        [Tooltip("Automatically filled with all children canvases if empty and autofillCanvasesToAdapt is true")]
        public List<Canvas> canvasesToAdapt = new List<Canvas>();
        [Tooltip("Canvas that should not be included in canvasesToAdapt (mostly useful when it is automatically filled)")]
        public List<Canvas> canvasesToIgnore = new List<Canvas>();

        [Header("GameObject adaptation configuraiton")]
        [Tooltip("Game Object that should be included in renderersToAdapt")]
        public List<GameObject> gameObjectsToAdapt = new List<GameObject>();

        [Header("Options")]

        public IRigPart rigPart;
        [Tooltip("Set it to false to stop automatic adaptation. relavant if another component calls AdaptRenderers(shouldDisplay) manually (it can use ShouldDisplay() as a starting value, and then customize it)")]
        public bool adaptRenderersDuringUpdate = true;
        public List<IRigPartVisualizerCustomizer> customizers = new List<IRigPartVisualizerCustomizer>();

        [Header("Status")]
        // Takes into account potential direct calls to Adapt when adaptRenderersDuringUpdate is disabled (for NetworkrigPart with adaptRenderersToTrackingStatus enabled typically)
        public bool lastAppliedShouldDisplay = false;


        [System.Flags]
        public enum Mode
        {
            NeverDisplay = 0,
            DisplayWhileOnline = 1,
            DisplayWhileOffline = 2,
            DisplayAlways = 1 | 2
        }

        public Mode mode = Mode.DisplayAlways;


        protected virtual void Awake()
        {
            AddObjectContentToAdapt(gameObject, shouldFillRenderers: autofillRenderersToAdapt, shouldFillCanvases: autofillCanvasesToAdapt);
        }

        public void AddObjectContentToAdapt(GameObject o, bool shouldFillRenderers = true, bool shouldFillCanvases = true, bool shouldAdaptGameObject = false, bool includeDisabledComponents = false)
        {
            if (o == null) return;
            if (shouldFillRenderers)
            {
                foreach (var r in o.GetComponentsInChildren<Renderer>(includeDisabledComponents))
                {
                    if (renderersToAdapt.Contains(r) == false) renderersToAdapt.Add(r);
                }
            }
            if (shouldFillCanvases)
            {
                foreach (var c in o.GetComponentsInChildren<Canvas>(includeDisabledComponents))
                {
                    if (canvasesToAdapt.Contains(c) == false) canvasesToAdapt.Add(c);
                }
            }

            foreach (var customizer in o.GetComponentsInChildren<IRigPartVisualizerCustomizer>(includeDisabledComponents))
            {
                if (customizers.Contains(customizer) == false) customizers.Add(customizer);
            }

            foreach (var customizer in o.GetComponentsInParent<IRigPartVisualizerCustomizer>(includeDisabledComponents))
            {
                if (customizers.Contains(customizer) == false) customizers.Add(customizer);
            }

            foreach (var c in GetComponentsInChildren<IRigPartVisualizerGameObjectToAdapt>(includeDisabledComponents))
            {
                if (gameObjectsToAdapt.Contains(c.gameObject) == false)
                {
                    gameObjectsToAdapt.Add(c.gameObject);
                }
            }

            if (shouldAdaptGameObject)
            {
                if (gameObjectsToAdapt.Contains(o) == false)
                {
                    gameObjectsToAdapt.Add(o);
                }
            }
        }

        public void RemoveObjectContentToAdapt(GameObject o, bool shouldFillRenderers = true, bool shouldFillCanvases = true, bool shouldAdaptGameObject = false, bool includeDisabledComponents = false)
        {
            if (o == null) return;
            if (shouldFillRenderers)
            {
                foreach (var r in o.GetComponentsInChildren<Renderer>(includeDisabledComponents))
                {
                    if (renderersToAdapt.Contains(r)) renderersToAdapt.Remove(r);
                }
            }
            if (shouldFillCanvases)
            {
                foreach (var c in o.GetComponentsInChildren<Canvas>(includeDisabledComponents))
                {
                    if (canvasesToAdapt.Contains(c)) canvasesToAdapt.Remove(c);
                }
            }

            foreach (var customizer in o.GetComponentsInChildren<IRigPartVisualizerCustomizer>(includeDisabledComponents))
            {
                if (customizers.Contains(customizer)) customizers.Remove(customizer);
            }

            foreach (var customizer in o.GetComponentsInParent<IRigPartVisualizerCustomizer>(includeDisabledComponents))
            {
                if (customizers.Contains(customizer)) customizers.Remove(customizer);
            }

            foreach (var c in GetComponentsInChildren<IRigPartVisualizerGameObjectToAdapt>(includeDisabledComponents))
            {
                if (gameObjectsToAdapt.Contains(c.gameObject))
                {
                    gameObjectsToAdapt.Remove(c.gameObject);
                }
            }

            if (shouldAdaptGameObject)
            {
                if (gameObjectsToAdapt.Contains(o))
                {
                    gameObjectsToAdapt.Remove(o);
                }
            }
        }

        private void Update()
        {
            if (adaptRenderersDuringUpdate)
            {
                bool shouldDisplay = ShouldDisplay();
                Adapt(shouldDisplay);
            }
        }

        public void ReApplyAdapt()
        {
            Adapt(lastAppliedShouldDisplay);
        }

        public void Adapt(bool shouldDisplay)
        {
            lastAppliedShouldDisplay = shouldDisplay;
            AdaptRenderers(shouldDisplay);
            AdaptGameObjects(shouldDisplay);
            AdaptCanvases(shouldDisplay);
        }

        void AdaptRenderers(bool shouldDisplay)
        {
            foreach (var r in renderersToAdapt)
            {
                if (renderersToIgnore.Contains(r)) continue;
                if (renderersInitialEnabled.ContainsKey(r) == false) renderersInitialEnabled[r] = r.enabled;
                bool shouldBeIgnoredDuetoCustomizer = false;
                foreach (var customizer in customizers)
                {
                    if (customizer.ShouldIgnoreRenderer(r))
                    {
                        shouldBeIgnoredDuetoCustomizer = true;
                        break;
                    }
                    if (customizer.ShouldCustomizeRendererShouldDisplay(r, out bool customizerShouldDisplay))
                    {
                        shouldDisplay = shouldDisplay && customizerShouldDisplay;
                    }
                }
                if (shouldBeIgnoredDuetoCustomizer) continue;

                if (materialWhileShouldNotDisplay != null)
                {
                    // Restore renderer initial state if the material was not set before hand the renderer had the time to be disabled
                    if (r.enabled != renderersInitialEnabled[r])
                    {
                        r.enabled = renderersInitialEnabled[r];
                    }

                    // Adat renderer material to shouldDisplay
                    if (shouldDisplay == false && overridenRendererInitialMaterial.ContainsKey(r) == false)
                    {
                        overridenRendererInitialMaterial[r] = r.material;
                        r.material = materialWhileShouldNotDisplay;
                    }
                    if (shouldDisplay && overridenRendererInitialMaterial.ContainsKey(r))
                    {
                        r.material = overridenRendererInitialMaterial[r];
                        overridenRendererInitialMaterial.Remove(r);
                    }
                }
                else
                {
                    // Adapt renderer enabled to shouldDisplay
                    if (r != null && r.enabled != shouldDisplay)
                    {
                        r.enabled = shouldDisplay;
                    }
                }
            }
        }

        void AdaptGameObjects(bool shouldDisplay)
        {
            foreach (var gameObjectToAdapt in gameObjectsToAdapt)
            {
                if (gameObjectToAdapt != null && gameObjectToAdapt.activeInHierarchy != shouldDisplay)
                {
                    gameObjectToAdapt.SetActive(shouldDisplay);
                }
            }
        }

        void AdaptCanvases(bool shouldDisplay)
        {
            foreach (var c in canvasesToAdapt)
            {
                if (canvasesToIgnore.Contains(c)) continue;
                
                // Adapt canvas enabled to shouldDisplay
                if (c != null && c.enabled != shouldDisplay)
                {
                    c.enabled = shouldDisplay;
                }
            }
        }

        public bool ShouldDisplay()
        {
            if (rigPart == null)
            {
                rigPart = GetComponentInParent<IRigPart>();
            }

            bool isOnline = rigPart != null && rigPart.IsOnline();

            bool shouldDisplay = ShouldDisplay(isOnline);
            if(rigPart is IHardwareRigPart hardwareRigPart)
            {
                shouldDisplay = shouldDisplay && hardwareRigPart.TrackingStatus == RigPartTrackingstatus.Tracked;
            }
            return shouldDisplay;
        }

        public bool ShouldDisplay(bool isOnline)
        {
            bool shouldDisplay;
            if (isOnline)
            {
                shouldDisplay = (mode & Mode.DisplayWhileOnline) == Mode.DisplayWhileOnline;
            }
            else
            {
                shouldDisplay = (mode & Mode.DisplayWhileOffline) == Mode.DisplayWhileOffline;
            }
            return shouldDisplay;
        }
    }
}
