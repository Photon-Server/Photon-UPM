using Fusion.XR.Shared.Base;
using Fusion.XR.Shared.Core;
using System.Collections.Generic;
using UnityEngine;
#if ARFOUNDATION_AVAILABLE
using UnityEngine.XR.ARFoundation;
#endif

public class TogglePassthrough : MonoBehaviour
{
    public List<GameObject> passthroughOnlyGameObjects = new List<GameObject>();
    public List<GameObject> vrOnlyGameObjects = new List<GameObject>();

    public enum Status
    {
        Undefined,
        VRMode,
        PassthroughMode
    }

    public Status status = Status.Undefined;

    [Header("VR mode")]
    [Tooltip("The color that will be replaced by transparent. Avoid using black, or the fade out effect would show the real life view instead")]
    public bool shouldFetchVRConfigurationFromCameraDefaults = true;
    public CameraClearFlags mrClearFlags = CameraClearFlags.SolidColor;
    public Color mrBackgroundcolor = new Color(0, 0, 0, 0);
    public CameraClearFlags vrClearFlags = CameraClearFlags.Skybox;
    [Tooltip("The color that will be replaced by transparent")]
    public Color vrBackgroundColor = Color.magenta;
    [Tooltip("ARCameraManager is not required for AndroidXR passthrough. If you target AndroidXR only, uncheck this")]
    public bool ignoreARCameraManagerPresence = false;

    Camera _rigCamera = null;

    Camera RigCamera
    {
        get
        {
            FindRigCamera();
            return _rigCamera;
        }
    }

    private void Update()
    {
        if (_rigCamera == null)
            FindRigCamera();
    }

    [ContextMenu("ToggleMode")]
    public void ToggleMode()
    {
        FindRigCamera(); 
        if (status == Status.PassthroughMode)
        {
            DesactivatePassthrough();
        }
        else if (status == Status.VRMode)
        {
            ActivatePassthrough();
        }
    }

    [ContextMenu("ActivatePassthrough")]
    public void ActivatePassthrough()
    {
        FindRigCamera();
        if (status == Status.PassthroughMode)
            return;

        var rigCamera = RigCamera;
        if (rigCamera == null)
            return;

        rigCamera.clearFlags = mrClearFlags;
        rigCamera.backgroundColor = mrBackgroundcolor;
        status = Status.PassthroughMode;

        AdaptToStatus();
    }

    [ContextMenu("DesactivatePassthrough")]
    public void DesactivatePassthrough()
    {
        FindRigCamera();
        if (status == Status.VRMode)
            return;

        var rigCamera = RigCamera;
        if (rigCamera == null)
            return;

        rigCamera.clearFlags = vrClearFlags;
        rigCamera.backgroundColor = vrBackgroundColor;
        status = Status.VRMode;

        AdaptToStatus();
    }

    void AdaptToStatus()
    {
        foreach (var o in passthroughOnlyGameObjects)
        {
            bool shouldBeActive = status == Status.PassthroughMode;
            if (o.activeSelf != shouldBeActive)
                o.SetActive(shouldBeActive);
        }
        foreach (var o in vrOnlyGameObjects)
        {
            bool shouldBeActive = status == Status.VRMode;
            if (o.activeSelf != shouldBeActive)
                o.SetActive(shouldBeActive);
        }
    }

    void FindRigCamera()
    {
        if (_rigCamera != null)
            return;
        IHardwareRig rig = HardwareRigsRegistry.GetHardwareRig();
        if (rig == null)
            return;
        _rigCamera = rig.HardwareHeadset?.HeadsetCamera;
        if(_rigCamera != null)
        {
            bool passthroughMode = true;
#if ARFOUNDATION_AVAILABLE
            var arCameraManager = _rigCamera.GetComponentInChildren<ARCameraManager>();
            if (ignoreARCameraManagerPresence == false && arCameraManager == null)
            {
                Debug.LogError("TogglePassthrough requires a ARCameraManager component on the camera. If you are targeting AndroidXR only, uncheck ignoreARCameraManagerPresence");
                passthroughMode = false;
            }
#endif
            if (_rigCamera.backgroundColor.a != mrBackgroundcolor.a || _rigCamera.clearFlags != mrClearFlags)
            {
                passthroughMode = false;
            }
            status = passthroughMode ? Status.PassthroughMode : Status.VRMode;

            if (status == Status.VRMode && shouldFetchVRConfigurationFromCameraDefaults)
            {
                vrClearFlags = _rigCamera.clearFlags;
                vrBackgroundColor = _rigCamera.backgroundColor;
            }
            AdaptToStatus();
        }
    }
}
