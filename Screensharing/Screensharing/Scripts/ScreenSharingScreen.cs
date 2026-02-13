#if PHOTON_VOICE_AVAILABLE
using Photon.Voice;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if PHOTON_VOICE_VIDEO_ENABLE
    using VoiceVideoTextureShader3D = Photon.Voice.Unity.VideoTexture.Shader3D;
#endif

namespace Fusion.Addons.ScreenSharing
{
    /***
     * 
     * ScreenSharingScreen manages the screen sharing renderer visibility :
     * When a screensharing is in progress : 
     *          - the screen renderer is enabled and the material is set with the one provided by the ScreensharingEmitter
     *          - the material shader matrix is updated every frame (required for URP in VR)
     *          - the "notPlayingObject" game object is disabled
     *          
     * When the screensharing is stopped : 
     *          - the screen renderer is disabled and the material is restored with the initial one
     *          - the "notPlayingObject" game object is enabled according to the VisibilityBehaviour settings
     * 
    *  Note: used mostly for receiver screens, it can also be used for an emitter preview screen. ToggleScreenVisibility should be called by the emitter to active/desactive the screen's view
     ***/
    [DefaultExecutionOrder(ScreenSharingScreen.EXECUTION_ORDER)] 
    public class ScreenSharingScreen : MonoBehaviour
    {
        // We use a late execution order, to be sure that any move is adressed before passing the world matrix to the shader
        public const int EXECUTION_ORDER = 10_000;

        public Renderer screenRenderer;
        public UnityEvent<bool> onScreensharingScreenVisibility = new UnityEvent<bool>();
        Material initialMaterial;
        public bool isRendering = false;
        [Tooltip(" Set it to true if you target Oculus Quest in single pass (will apply the dedicated QuestVideoTextureExt3D shader)")]
        public bool usingShaderRequiringMatrix = true;
        [Tooltip("If usingShaderRequiringMatrix is true, on Android, a ScreenSharingScreenTextureProjection will be added if none is present. This allows to use mipmap, and prevents a shader issue, where only one texture can be visible with the same shader")]
        public bool automaticallyAddTextureProjection = true;
        [Tooltip(" Set it to true if you are not using video memory to store the video content, but just a regular texture: it will skip using a shader provided by the video SDK")]
        public bool useRegularMaterial = false;

        [Header("Debug")]
        public TMPro.TMP_Text debugStateText;
        public TMPro.TMP_Text debugEventText;
        [SerializeField] bool shouldIgnorePlatformForTextureProjectionRequirementCheck = false;


        public void LogEvent(string txt)
        {
            Debug.Log(txt);
            if (debugEventText != null) debugEventText.text = $"[{Time.time:0.0}]" + txt;
        }

        public void LogErrorEvent(string txt)
        {
            Debug.Log(txt);
            if (debugEventText != null) debugEventText.text = $"[{Time.time:0.0}]" + txt;
        }

        public void LogState(string txt)
        {
            if (debugStateText != null)
            {
                debugStateText.text = txt;
            }
        }

#if PHOTON_VOICE_VIDEO_ENABLE
        string customQuestScreenShaderName = "QuestVideoTextureExt3D";

        public interface IScreenSharingScreenListener {
            public void PlaybackEnabled(Material videoMaterial, IVideoPlayer videoPlayer, int playerId, object userData);
            public void PlaybackDisabled(IVideoPlayer videoPlayer);
        }
        public List<IScreenSharingScreenListener> listeners = new List<IScreenSharingScreenListener>();
        ScreenSharingScreenTextureProjection textureProjection = null;

        private IVideoPlayer currentVideoPlayer;
        [System.Flags]
        public enum VisibilityBehaviour
        {
            None = 0,
            HideScreenRendererWhenNotPlaying = 1,
            DisplayNotPlayingObjectWhenNotPlaying = 2
        }
        public VisibilityBehaviour visibilityBehaviour = VisibilityBehaviour.None;

        public GameObject notPlayingObject;


        private void Awake()
        {
            if (debugEventText != null) debugEventText.text = "";
            if (debugStateText != null) debugStateText.text = "";

            if (screenRenderer == null) screenRenderer = GetComponentInChildren<Renderer>();
            if (screenRenderer)
                initialMaterial = screenRenderer.material;
            if (notPlayingObject && (visibilityBehaviour & VisibilityBehaviour.DisplayNotPlayingObjectWhenNotPlaying) != VisibilityBehaviour.DisplayNotPlayingObjectWhenNotPlaying)
            {
                LogErrorEvent("A notPlayingObject is set, but DisplayNotPlayingObjectWhenNotPlaying option is not choosen: the object won't be used");
            }
            foreach(var listener in GetComponentsInChildren<IScreenSharingScreenListener>())
            {
                if (listeners.Contains(listener) == false)
                {
                    listeners.Add(listener);
                }
            }
            textureProjection = GetComponent<ScreenSharingScreenTextureProjection>();
            ToggleScreenVisibility(false);

            Application.onBeforeRender += OnBeforeRender;
        }

        private void OnDestroy()
        {
            Application.onBeforeRender -= OnBeforeRender;

        }

        private void Update()
        {
            SendPositionMatrixToShader();
        }
        
        void OnBeforeRender()
        {
            SendPositionMatrixToShader();
        }

        void SendPositionMatrixToShader() { 
            // Needed for the URP VR shader
            if (isRendering && usingShaderRequiringMatrix && useRegularMaterial == false)
            {
                screenRenderer.material.SetMatrix("_localToWorldMatrix", screenRenderer.transform.localToWorldMatrix);
            }
        }

        public static bool IsSinglePassShaderRequired()
        {
            return UnityEngine.XR.XRSettings.stereoRenderingMode != UnityEngine.XR.XRSettings.StereoRenderingMode.MultiPass;
        }

        public Material PrepareMaterial(Texture texture, Flip flip)
        {
            Material material = null;
            if (useRegularMaterial)
            {
                material = screenRenderer.material;
                material.mainTexture = texture;
                material.SetVector("_Flip", new Vector4(flip.IsHorizontal ? -1 : 1, flip.IsVertical ? -1 : 1, 0, 0));
            }
            else if (usingShaderRequiringMatrix && Application.platform == RuntimePlatform.Android)
            {
                var shader = Resources.Load<Shader>(customQuestScreenShaderName);
                if (shader == null)
                {
                    throw new System.Exception("Shader resource " + customQuestScreenShaderName + " fails to load");
                }
                material = new Material(shader);
                material.SetTexture("_MainTex", texture);
                material.SetVector("_Flip", new Vector4(flip.IsHorizontal ? -1 : 1, flip.IsVertical ? -1 : 1, 0, 0));
            }
            else
            {
                usingShaderRequiringMatrix = false;
                material = VoiceVideoTextureShader3D.MakeMaterial(texture, flip);
            }
            return material;
        }

        public Material SetupMaterial(Texture texture, Flip flip, Vector2Int resolution, int fps)
        {
            LogEvent($"Setting up material ({fps}fps)");
            var videoMaterial = PrepareMaterial(texture, flip);
            bool isTextureProjectionRequired = usingShaderRequiringMatrix && useRegularMaterial == false;
            if (shouldIgnorePlatformForTextureProjectionRequirementCheck)
            {
                isTextureProjectionRequired = useRegularMaterial == false;
            }
            if (isTextureProjectionRequired)
            {
                if (textureProjection == null && automaticallyAddTextureProjection)
                {
                    textureProjection = gameObject.AddComponent<ScreenSharingScreenTextureProjection>();
                }
            }
            if (textureProjection)
            {
                textureProjection.PrepareTextureForResolution(new Vector2((float)resolution.x, (float)resolution.y));
                textureProjection.lowerResFPS = fps;
            }
            ToggleScreenVisibility(true);
            screenRenderer.material = videoMaterial;
            return videoMaterial;
        }

        public void EnablePlayback(IVideoPlayer videoPlayer, int playerId, object userData, Vector2Int resolution, int fps)
        {
            if (currentVideoPlayer != null)
            {
                LogEvent($"Screen reused by another player {videoPlayer}. Note: make sure that the initial player is disposed by orchestration logic.");
            }
            else
            {
                LogEvent("Playback started on screen for videoPlayer " + videoPlayer);
            }                

            currentVideoPlayer = videoPlayer;
            var flip = videoPlayer.Flip;
            var screenTexture = videoPlayer.PlatformView as Texture;
            var videoMaterial = SetupMaterial(screenTexture, flip, resolution, fps);

            foreach (var listener in listeners)
            {
                listener.PlaybackEnabled(videoMaterial, videoPlayer, playerId, userData);
            }
        }

        public void DisablePlayback(IVideoPlayer videoPlayer)
        {
            if (videoPlayer != currentVideoPlayer)
            {
                LogEvent("Not stopping playback because videoPlayer hasbeen reused by another player");
                return;
            }
            else
            {
                LogEvent("Playback stopped for videoPlayer " + videoPlayer);
            }

            currentVideoPlayer = null;
            ToggleScreenVisibility(false);
            screenRenderer.material = initialMaterial;

            foreach (var listener in listeners)
            {
                listener.PlaybackDisabled(videoPlayer);
            }
        }

        public virtual void ToggleScreenVisibility(bool ShouldScreenBeDisplayed)
        {
            isRendering = ShouldScreenBeDisplayed;
            if ((visibilityBehaviour & VisibilityBehaviour.HideScreenRendererWhenNotPlaying) == VisibilityBehaviour.HideScreenRendererWhenNotPlaying)
            {
                if (debugEventText != null) debugEventText.enabled = ShouldScreenBeDisplayed;
                if (debugStateText != null) debugStateText.enabled = ShouldScreenBeDisplayed;
                if (screenRenderer != null)
                    screenRenderer.enabled = ShouldScreenBeDisplayed;
                else
                    Debug.LogError("Missing screen renderer");
            }
            if (notPlayingObject && (visibilityBehaviour & VisibilityBehaviour.DisplayNotPlayingObjectWhenNotPlaying) == VisibilityBehaviour.DisplayNotPlayingObjectWhenNotPlaying)
            {
                notPlayingObject.SetActive(!ShouldScreenBeDisplayed);
            }
            if (onScreensharingScreenVisibility != null) onScreensharingScreenVisibility.Invoke(ShouldScreenBeDisplayed);
        }

#endif
    }
}
