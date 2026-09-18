using Fusion.XR.Shared.Core;
using Fusion.XRShared.Locomotion;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DisplayWorldLocomotionAnchor is in charge to display or hide the grid used to "move the world"
/// </summary>
public class DisplayWorldLocomotionAnchor : MonoBehaviour
{
    [SerializeField] SelfLocomotionGrabbable anchor;
    public Vector3 spawnPositionOffsetRelativeToHeadset = new Vector3(0, -0.1f, 0.3f);

    Vector3 initialScale;

    bool displayNeeded = false;

    public bool forceDisplay = false;
    bool buttonPressDisplay = false;
    public bool displayOnButtonPress = false;
    public List<string> displayButtons = new List<string> { "primaryButton", "secondaryButton" };

#if INPUTSYSTEM_AVAILABLE
    LocalInputTracker _toggleInputTracker;
#endif

    bool ShouldDisplay => forceDisplay || buttonPressDisplay;

    public bool IsAnchorDisplayed => anchor.gameObject.activeSelf;
    private void Awake()
    {
        initialScale = anchor.transform.localScale;
        anchor.onUngrab.AddListener(OnUngrab);
#if INPUTSYSTEM_AVAILABLE
        _toggleInputTracker = new LocalInputTracker(displayButtons);
#endif
    }

    private void OnDisable()
    {
        if (anchor)
        {
            anchor.gameObject.SetActive(false);
        }
        forceDisplay = false;
    }

    void OnUngrab()
    {
        HideAnchor();
        if (ShouldDisplay) displayNeeded = true;
    }

    public void ToggleForceDisplay()
    {
        forceDisplay = !forceDisplay;
    }

    private void Update()
    {
        buttonPressDisplay = false;
        if (displayOnButtonPress)
        {
            if (_toggleInputTracker != null && _toggleInputTracker.ReadAnyButtonPressed() is bool currentState)
            {
                buttonPressDisplay = currentState;
            }
        }
        if (displayNeeded)
        {
            displayNeeded = false;
            DisplayAnchor();
        }
        else if (ShouldDisplay)
        {
            if (IsAnchorDisplayed == false) DisplayAnchor();
        }
        else
        {
            if (IsAnchorDisplayed) HideAnchor();
        }
    }

    public void DisplayAnchor()
    {
        anchor.gameObject.SetActive(true);
        var rig = HardwareRigsRegistry.GetHardwareRig();
        var headsetTransform = rig.Headset.transform;
        var offset = new Vector3(
            spawnPositionOffsetRelativeToHeadset.x * rig.transform.localScale.x,
            spawnPositionOffsetRelativeToHeadset.y * rig.transform.localScale.y,
            spawnPositionOffsetRelativeToHeadset.z * rig.transform.localScale.z
            );
        var anchorPosition = headsetTransform.position + headsetTransform.TransformDirection(offset);
        Quaternion anchorRotation;
        bool flip = false;
        if (flip)
        {
            anchorRotation = Quaternion.Euler(0, headsetTransform.eulerAngles.y, 0);
        }
        else
        {
            anchorRotation = Quaternion.Euler(0, 180 + headsetTransform.eulerAngles.y, 0);
        }
        anchor.transform.position = anchorPosition;
        anchor.transform.rotation = anchorRotation;
        anchor.transform.localScale = new Vector3(
            initialScale.x * rig.transform.localScale.x,
            initialScale.y * rig.transform.localScale.y,
            initialScale.z * rig.transform.localScale.z
            );
    }

    void HideAnchor()
    {
        anchor.gameObject.SetActive(false);
    }
}
