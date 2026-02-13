using Meta.XR;
using Photon.Voice;
using Photon.Voice.Unity;
using System;
using System.Threading.Tasks;
using Unity.Collections;
using UnityEngine;
using IVoiceLogger = Photon.Voice.ILogger;

namespace Fusion.Addons.ScreenSharing
{
#if PHOTON_VOICE_VIDEO_ENABLE

    public class MetaWebcamVideoRecorder : AndroidTextureVideoRecorderBase
    {
        PassthroughCameraAccess passthroughCameraAccess;

        public override void Init(IVoiceLogger logger, VoiceInfo info)
        {
            passthroughCameraAccess = GameObject.FindAnyObjectByType<PassthroughCameraAccess>(FindObjectsInactive.Include);
            if (passthroughCameraAccess == null)
            {
                Debug.LogError("[MetaWebcamVideoRecorderPusher] Missing passthroughCameraAccess: required to acccess camera passthrough");
            }
            base.Init(logger, info);
        }

        protected override bool IsCollectingPossible()
        {
            if (passthroughCameraAccess)
            {
                if (passthroughCameraAccess.enabled == false)
                {
                    passthroughCameraAccess.enabled = true;
                    return false;
                }
                return true;
            }
            return false;
        }

        protected override Texture CollectTexture()
        {
            return passthroughCameraAccess.GetTexture();
        }
    }
#endif
}
