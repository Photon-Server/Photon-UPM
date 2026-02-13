using Fusion.Addons.VoiceHelpers;
#if PHOTON_VOICE_AVAILABLE
using Photon.Voice.Unity.UtilityScripts;
using System;

#endif
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;

namespace Fusion.Addons.VoiceHelpersTools
{
    /// <summary>
    /// FusionVoiceSetup subclasses, that rely on a Permissionsrequester, instead of a MicrophonePermission, fro microphone permission requests on Android.
    /// 
    /// Should be used when other permissions access are required (camera access, scene detection, ...), to avoid any collision during right requests
    /// </summary>
    public class FusionVoiceSetupWithPermissionsRequester : FusionVoiceSetup
    {
        FusionVoiceClientPermissionWaiter voicePermissionWaiter;
        public bool callOnMicrophonePermissionChangeForPermissionGrantedOnly = true;
#if PHOTON_VOICE_AVAILABLE
        protected override void PermissionRequestSetup()
        {
            microphonePermission = recorder.GetComponent<MicrophonePermission>();
            if (microphonePermission != null)
            {
                Debug.LogError("FusionVoiceSetupWithPermissionsRequester is used, so a MicrophonePermission is not required (a VoicePermissionWaiter will be used instead)");
            }
            voicePermissionWaiter = recorder.GetComponent<FusionVoiceClientPermissionWaiter>();
            if (voicePermissionWaiter == null)
            {
                voicePermissionWaiter = recorder.gameObject.AddComponent<FusionVoiceClientPermissionWaiter>();
                if (callOnMicrophonePermissionChangeForPermissionGrantedOnly)
                {
                    voicePermissionWaiter.onPermissionGranted.AddListener(() => { OnMicrophonePermissionChange(true); });
                }
                else
                {
                    voicePermissionWaiter.onPermissionChanged.AddListener(OnMicrophonePermissionChange);
                }
            }
        }
#endif
    }
}
