#if PHOTON_VOICE_AVAILABLE
using Photon.Voice;
using IVoiceLogger = Photon.Voice.ILogger;
#endif
using System;
using System.Threading.Tasks;
using UnityEngine;


namespace Fusion.Addons.ScreenSharing
{
    public interface IEmitterController
    {
        public Task WaitForWebcamAvailability();
#if PHOTON_VOICE_AVAILABLE
        public DeviceInfo? WebcamDeviceInfo();
#endif
        public bool IsPlatformController();
        public bool ShouldForceEmissionResolution(out Vector2Int resolution);
        public void OnStopEmitting();
        public void OnStartEmitting();
    }

    public interface ICustomRecorderEmitterController : IEmitterController
    {
        public bool ShouldPreviewUseVideoMaterial => true;
#if PHOTON_VOICE_VIDEO_ENABLE
        public IVideoRecorder GetVideoRecorder(IVoiceLogger logger, VoiceInfo info, DeviceInfo camDevice, Action<IVideoRecorder> onReady);
#endif
    }
}
