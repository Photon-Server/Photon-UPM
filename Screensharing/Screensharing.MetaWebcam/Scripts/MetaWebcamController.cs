using Meta.XR;
using Photon.Voice;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using IVoiceLogger = Photon.Voice.ILogger;

namespace Fusion.Addons.ScreenSharing
{
#if PHOTON_VOICE_VIDEO_AVAILABLE && PHOTON_VOICE_VIDEO_ENABLE
    public class MetaWebcamController : MonoBehaviour, ICustomRecorderEmitterController
    {
        public static MetaWebcamController DefaultMetaWebcamController;


        [SerializeField]
        bool useInEditor = true;

        public Vector2Int requestedResolution = MetaWebcamController.DefaultResolution;

        static Vector2Int[] ValidResolutions = new Vector2Int[] {
            new Vector2Int(0, 0),
            new Vector2Int(320, 240),
            new Vector2Int(640, 480),
            new Vector2Int(800, 600),
            new Vector2Int(1280, 960),
            new Vector2Int(1280, 1280),
        };
        static Vector2Int DefaultResolution = new Vector2Int(1280, 960);
        [SerializeField] bool checkIfResolutionIsValid = false;

        [SerializeField]
        PassthroughCameraAccess passthroughCameraAccess;

        [Tooltip("If true, a MetaWebcamVideoRecorder will be created and use PassthroughCameraAccess.GetTexture() to access the image. Otherwise, the Photon Video SDK will handle the recorder creation (and use low level Android camera API)")]
        public bool streamWithPassthroughCameraAccessTexture = true;
        [Tooltip("If true, requestedResolution is ignored, and passthroughCameraAccess.Requestedresolution is used instead")]
        public bool usePassthroughCameraAccessResolution = false;
        [Tooltip("If true, the MetaWebcamVideoRecorder will make sure that the preview texture (its PlatformView) is in the proper orientation (and its FLip will return Flip.None). " +
            "However, it requires an additionnal Graphics.Blit call. " +
            "Otherwise, the image is flipped, and Flip returns flip.Vertical")]
        public bool automaticFlipPassthroughCameraAccessPreview = true;

        protected virtual void Awake()
        {
            if (DefaultMetaWebcamController == null)
            {
                DefaultMetaWebcamController = this;
            }
            if (passthroughCameraAccess == null)
            {
                passthroughCameraAccess = FindAnyObjectByType<PassthroughCameraAccess>(FindObjectsInactive.Include);
            }
            if (streamWithPassthroughCameraAccessTexture && passthroughCameraAccess == null)
            {
                Debug.LogError("[MetaWebcamController] Missing PassthroughCameraAccess");
            }
        }

        private void OnDestroy()
        {
            if(DefaultMetaWebcamController == this)
            {
                DefaultMetaWebcamController = null;
            }
        }

        public bool IsPlatformController() {
#if UNITY_ANDROID && !UNITY_EDITOR
            return true;
#else
            return useInEditor;
#endif
        }

        public async Task WaitForWebcamAvailability()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (streamWithPassthroughCameraAccessTexture)
            {
                while (MetaHeadsetCameraPermissionRequester.SharedInstance == null || MetaHeadsetCameraPermissionRequester.SharedInstance.isRequesting)
                {
                    if (MetaHeadsetCameraPermissionRequester.SharedInstance == null)
                    {
                        Debug.Log("[MetaWebcamController] No MetaHeadsetCameraPermissionRequester: creating one...");
                        var requester = new GameObject("MetaHeadsetCameraPermissionRequester");
                        requester.AddComponent<MetaHeadsetCameraPermissionRequester>();
                    }

                    if (MetaHeadsetCameraPermissionRequester.SharedInstance.isRequesting)
                    {
                        Debug.Log("[MetaWebcamController] MetaHeadsetCameraPermissionRequester: waiting for request answer...");
                        await Task.Delay(1000);
                    }
                }
            }
            else
            { 
                while (MetaWebcamPermissionRequester.SharedInstance == null || MetaWebcamPermissionRequester.SharedInstance.isRequesting)
                {
                    if (MetaWebcamPermissionRequester.SharedInstance == null)
                    {
                        Debug.Log("[MetaWebcamController] No MetaWebcamPermissionRequester: creating one...");
                        var requester = new GameObject("MetaWebcamPermissionRequester");
                        requester.AddComponent<MetaWebcamPermissionRequester>();
                    }

                    if (MetaWebcamPermissionRequester.SharedInstance.isRequesting)
                    {
                        Debug.Log("[MetaWebcamController] MetaWebcamPermissionRequester: waiting for request answer...");
                        await Task.Delay(1000);
                    }
                }
            }
#else
            await Task.FromResult(false);
#endif
        }

        public void OnStopEmitting()
        {
            if (streamWithPassthroughCameraAccessTexture == false)
            {
                if (passthroughCameraAccess != null)
                {
                    StartCoroutine(WebcamReset());
                }
            }   
        }

        public void OnStartEmitting()
        {
            if(streamWithPassthroughCameraAccessTexture == false)
            {
                if (passthroughCameraAccess != null)
                {
                    passthroughCameraAccess.enabled = false;
                }
            }
        }

        IEnumerator WebcamReset()
        {
            if (passthroughCameraAccess == null)
            {
                yield break;
            }
            passthroughCameraAccess.enabled = false;

            Debug.Log("Shuting down camera ...");
            yield return new WaitForSeconds(1);
            Debug.Log("Reactivating camera");

            passthroughCameraAccess.enabled = true;
        }

        public bool ShouldForceEmissionResolution(out Vector2Int resolution)
        {
            resolution = requestedResolution;

            if (streamWithPassthroughCameraAccessTexture)
            {
                if (usePassthroughCameraAccessResolution && passthroughCameraAccess)
                {
                    resolution = passthroughCameraAccess.RequestedResolution;
                }
                // TODO If usePassthroughCameraAccessResolution is false, we could potentially make sure that the requested resolution has the same ratio that the resolution collected through the passthroughCameraAccess.
            }
            else
            {
                // The VideoSDK will access Android Webcam API  requesting the resolution we return: it has to be a supported resolution
                if (checkIfResolutionIsValid)
                {
                    bool isResolutionValid = false;
                    foreach (var validResolution in ValidResolutions)
                    {
                        if (validResolution == resolution)
                        {
                            isResolutionValid = true;
                        }
                    }
                    if (isResolutionValid == false)
                    {
                        resolution = DefaultResolution;
                    }
                }
            }

            Debug.Log($"Configured Video Resolution {resolution.x}x{resolution.y}");
            return true;
        }

        public DeviceInfo? WebcamDeviceInfo()
        {
            return null;
        }

        public IVideoRecorder GetVideoRecorder(IVoiceLogger logger, VoiceInfo info, DeviceInfo camDevice, Action<IVideoRecorder> onReady)
        {
            IVideoRecorder recorder = null;
            if (streamWithPassthroughCameraAccessTexture)
            {
                var metaWebcamVideoRecorder = new MetaWebcamVideoRecorder();
                metaWebcamVideoRecorder.automaticPreviewFlip = automaticFlipPassthroughCameraAccessPreview;
                metaWebcamVideoRecorder.Init(logger, info);
                metaWebcamVideoRecorder.OnReady += onReady;
                recorder = metaWebcamVideoRecorder;
            }
            return recorder;
        }

        public bool ShouldPreviewUseVideoMaterial
        {
            get
            {
                // When using a MetaWebcamVideoRecorder, no need to have a video material for the preview (the platform view is a regular render texture)
                return streamWithPassthroughCameraAccessTexture == false;
            }
        }
    }
#else
    public class MetaWebcamController : MonoBehaviour { }
#endif
}
