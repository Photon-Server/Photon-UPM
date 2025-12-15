using UnityEngine;
using UnityEngine.InputSystem.UI;
#if XRIT_ENABLED
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif
public class ConfigureDeviceRaycasterForXRIT : MonoBehaviour
{
#if XRIT_ENABLED
    private void Awake()
    {
        XRUIInputModule xruiinputModule = FindAnyObjectByType<XRUIInputModule>();
            if(xruiinputModule != null)
            {
                TrackedDeviceRaycaster trackedDeviceRaycaster = GetComponent<TrackedDeviceRaycaster>();
                if (trackedDeviceRaycaster)
                {
                    Destroy(trackedDeviceRaycaster);
                }
                gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
     }
#endif
}
