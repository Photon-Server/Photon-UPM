using Fusion.XR.Shared.Automatization;
using Fusion.XR.Shared.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(SimulatedHandsSetup.EXECUTION_ORDER)]
public class SimulatedHandsSetup : MonoBehaviour
{
    public const int EXECUTION_ORDER = -5000;
    [SerializeField]
    GameObject leftSimulatedHandPrefab;

    [SerializeField]
    GameObject rightSimulatedHandPrefab;

    [Tooltip("Invisible material: on Android, animated mesh needs to have a material for the animation to work. If the hand should be invisible, but we need proper finger positions, we need a transparent material to let the animation work")]
    [SerializeField]
    Material transparentMaterial;

    const string LEFT_SIMULATED_HAND_PREFAB = "LeftControllerSimulatedHand";
    const string RIGHT_SIMULATED_HAND_PREFAB = "RightControllerSimulatedHand";
    const string TRANSPARENT_MATERIAL = "TransparentMaterialForHardwareHands";

    public Vector3 leftHandPositionOffset = new Vector3(0, -0.02f, 0.04f);
    public Vector3 rightHandPositionOffset = new Vector3(0, -0.02f, 0.04f);

    IHardwareRig hardwareRig;
    List<IRigPart> controllerRigParts = new List<IRigPart>();

    private void Awake()
    {
        hardwareRig = GetComponent<IHardwareRig>();
    }

    // Update is called once per frame
    void Update()
    {
        SimpleHandsVerification();
    }

    void SimpleHandsVerification()
    {
        foreach (var rigPart in hardwareRig.RigParts)
        {
            if (rigPart is IHardwareController controller)
            {
                // Skip the check if already setup
                if (controllerRigParts.Contains(rigPart)) continue;

                controllerRigParts.Add(rigPart);

                var prefab = controller.Side == RigPartSide.Left ? leftSimulatedHandPrefab : rightSimulatedHandPrefab;
                var offset = controller.Side == RigPartSide.Left ? leftHandPositionOffset : rightHandPositionOffset;

                var simulatedHand = GameObject.Instantiate(prefab);
                simulatedHand.transform.parent = rigPart.transform;
                simulatedHand.transform.localRotation = Quaternion.identity;
                simulatedHand.transform.localPosition = offset;

                var rigPartVisualizer = controller.gameObject.GetComponent<RigPartVisualizer>();
                if (rigPartVisualizer != null)
                {
                    rigPartVisualizer.materialWhileShouldNotDisplay = transparentMaterial;
                    foreach(var r in simulatedHand.GetComponentsInChildren<Renderer>(true))
                    {
                        if (rigPartVisualizer.renderersToAdapt.Contains(r) == false)
                        {
                            rigPartVisualizer.renderersToAdapt.Add(r);
                        }

                    }
                }
            }
        }
    }

    private void OnValidate()
    {
#if UNITY_EDITOR

        if (leftSimulatedHandPrefab == null)
        {
            if (AssetLookup.TryFindAsset(LEFT_SIMULATED_HAND_PREFAB, out GameObject leftPrefab, requiredPathElement: "SimpleHands"))
            {
                leftSimulatedHandPrefab = leftPrefab;
            }
            if (AssetLookup.TryFindAsset(RIGHT_SIMULATED_HAND_PREFAB, out GameObject rightPrefab, requiredPathElement: "SimpleHands"))
            {
                rightSimulatedHandPrefab = rightPrefab;
            }
            if (AssetLookup.TryFindAsset(TRANSPARENT_MATERIAL, out Material material))
            {
                transparentMaterial = material;
            }
        }
#endif
    }
}
