using Fusion.XR.Shared.Base;
using Fusion.XR.Shared.Core;
using UnityEngine;
#if XRIT_ENABLED
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Fusion.XR.Shared.Core.Tools;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
#endif

#if XRHANDS_ENABLED
using UnityEngine.XR.Hands;
#endif

namespace Fusion.XR.Shared.XRHands
{
    public class XRITHardwareRig : HardwareRig
    {
        [Header("Automatic rigParts detection and configuration")]
        [SerializeField] bool autodetectandConfigureRigParts = true;


#if XRIT_ENABLED
        [SerializeField] RigPartVisualizer.Mode controllerVisualizationMode = RigPartVisualizer.Mode.DisplayWhileOffline;
        [SerializeField] RigPartVisualizer.Mode handVisualizationMode = RigPartVisualizer.Mode.DisplayWhileOffline;
#endif

        [Header("Simulated hand for controllers")]
        [Tooltip("If true, instead of a controller, an animated hand will be added and used when a controller is detected (not relevant when simultaneous controller/hand is active)")]
        public bool simulateHandForControllers = false;
        public GameObject leftHandPrefab;
        public GameObject rightHandPrefab;
        [Tooltip("When online, the default config of the rigPartVisualizer of the controller is to hide the hardware rig renderers. " +
            "But, to allow animations to run properly on android (to have in the hardware rig usable finger positions), instead of hidding the renderer we can use an invisible material")]
        public Material materialWhenOnline;

#if XRIT_ENABLED
        [SerializeField] bool neverDetectHands = false;
        bool leftControllerXRInteractionGroupFound = false;
        bool rightControllerXRInteractionGroupFound = false;
        bool cameraRigFound = false;

#if XRHANDS_ENABLED
        bool leftHandSkeletonDriverFound = false;
        bool rightHandSkeletonDriverFound = false;
#endif

#endif





        protected virtual void Awake()
        {
            if (autodetectandConfigureRigParts) AutomaticRigPartsDetection();
        }


        private void Update()
        {
            if (autodetectandConfigureRigParts) AutomaticRigPartsDetection();
        }

        protected void AutomaticRigPartsDetection()
        {
#if XRIT_ENABLED
            if (leftControllerXRInteractionGroupFound == false || rightControllerXRInteractionGroupFound == false)
            {
                foreach (var interactionGroup in GetComponentsInChildren<XRInteractionGroup>())
                {
                    if (interactionGroup.GetComponent<IHardwareRigPart>() != null)
                    {
                        // Already set up, nothing to do
                        continue;
                    }
                    bool isLeftHand = false;
                    if (interactionGroup.name.Contains("Left", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        isLeftHand = true;
                    }
                    if (interactionGroup.name.Contains("Controller", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (isLeftHand == true && leftControllerXRInteractionGroupFound) continue;
                        if (isLeftHand == false && rightControllerXRInteractionGroupFound) continue;

                        // Controller
                        var part = interactionGroup.gameObject.AddComponent<HardwareController>();
                        // XRIT already manages gameobject active status
                        part.disabledGameObjectWhenNotTracked = false;
                        part.Side = isLeftHand ? RigPartSide.Left : RigPartSide.Right;

                        var handPrefab = isLeftHand ? leftHandPrefab : rightHandPrefab;
                        if (simulateHandForControllers && handPrefab != null)
                        {
                            var leftHand = GameObject.Instantiate(handPrefab);
                            leftHand.transform.parent = part.transform;
                            leftHand.transform.localPosition = Vector3.zero;
                            leftHand.transform.localRotation = Quaternion.identity;

                            var indexMarker = part.GetComponentInChildren<IndexTipMarker>();
                            if (indexMarker)
                            {
                                // Move poke interaction poke point ot index of the model
                                var pokeInteractor = part.GetComponentInChildren<XRPokeInteractor>(true);
                                if (pokeInteractor)
                                {
                                    pokeInteractor.attachTransform = indexMarker.transform;
                                }
                            }
                        }

                        // Controller command
                        part.gameObject.AddComponent<HardwareControllerCommand>();

                        // visualizer
                        var visualizer = AddVisualizer(part.gameObject, controllerVisualizationMode);
                        if (simulateHandForControllers)
                        {
                            visualizer.materialWhileShouldNotDisplay = materialWhenOnline;
                        }

                        if (isLeftHand)
                        {
                            leftControllerXRInteractionGroupFound = true;
                        }
                        else
                        {
                            rightControllerXRInteractionGroupFound = true;
                        }

                    }
                }
            }

#if XRHANDS_ENABLED
            if (neverDetectHands == false && (leftHandSkeletonDriverFound == false || rightHandSkeletonDriverFound == false))
            {
                foreach (var interactionGroup in GetComponentsInChildren<XRHandSkeletonDriver>())
                {
                    if (interactionGroup.GetComponent<IHardwareRigPart>() != null)
                    {
                        // Already set up, nothing to do
                        continue;
                    }
                    bool isLeftHand = false;
                    if (interactionGroup.name.Contains("Left", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        isLeftHand = true;
                    }
                    if (interactionGroup.name.Contains("Hand", System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (isLeftHand == true && leftHandSkeletonDriverFound) continue;
                        if (isLeftHand == false && rightHandSkeletonDriverFound) continue;

                        // Hand
                        var part = interactionGroup.gameObject.AddComponent<XRHandsHardwareHand>();
                        // XRIT already manages gameobject active status
                        part.disabledGameObjectWhenNotTracked = false;
                        var visualizer = AddVisualizer(part.gameObject, handVisualizationMode);
                        part.Side = isLeftHand ? RigPartSide.Left : RigPartSide.Right;

                        if (isLeftHand)
                        {
                            leftHandSkeletonDriverFound = true;
                        }
                        else
                        {
                            rightHandSkeletonDriverFound = true;
                        }
                    }
                }
            }
#endif
            if (cameraRigFound == false)
            {
                foreach (var camera in GetComponentsInChildren<Camera>())
                {
                    if (camera.GetComponent<IHardwareRigPart>() != null)
                    {
                        // Already set up, nothing to do
                        continue;
                    }

                    if (camera.isActiveAndEnabled == false) continue;

                    var part = camera.gameObject.AddComponent<HardwareHeadset>();
                    // XRIT already manages gameobject active status
                    part.disabledGameObjectWhenNotTracked = false;
                    cameraRigFound = true;
                }
            }

#endif
        }

        protected virtual RigPartVisualizer AddVisualizer(GameObject gameObject, RigPartVisualizer.Mode mode)
        {
            var visualizer = gameObject.AddComponent<RigPartVisualizer>();
            visualizer.mode = mode;

#if XRIT_ENABLED
            foreach (var curve in gameObject.GetComponentsInChildren<CurveVisualController>(true))
            {
                foreach (var renderer in curve.GetComponentsInChildren<Renderer>(true))
                {
                    visualizer.renderersToIgnore.Add(renderer);
                }
            }
#endif
            return visualizer;
        }
    }
}

