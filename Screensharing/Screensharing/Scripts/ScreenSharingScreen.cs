#if PHOTON_VOICE_AVAILABLE
using Photon.Voice;
#endif
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
        [Tooltip("Optional: assign a RawImage for UI-based preview instead of a 3D MeshRenderer")]
        public RawImage screenRawImage;
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
#if PHOTON_VOICE_VIDEO_ENABLE
        [SerializeField] bool shouldIgnorePlatformForTextureProjectionRequirementCheck = false;
#endif

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

        /// <summary>
        /// When using RawImage mode, stores a reference to the source texture provider
        /// so the RawImage can be updated each frame (the Texture2D object may be
        /// recreated on resolution change).
        /// </summary>
        private System.Func<Texture> rawImageTextureSource;

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


        private bool UseRawImage => screenRawImage != null;

        private void Awake()
        {
            if (debugEventText != null) debugEventText.text = "";
            if (debugStateText != null) debugStateText.text = "";

            if (screenRawImage == null)
            {
                if (screenRenderer == null) screenRenderer = GetComponentInChildren<Renderer>();
            }
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
            UpdateRawImageTexture();
        }
        
        void OnBeforeRender()
        {
            SendPositionMatrixToShader();
        }

        void SendPositionMatrixToShader() {
            // Needed for the URP VR shader (not applicable in RawImage mode)
            if (isRendering && usingShaderRequiringMatrix && useRegularMaterial == false && UseRawImage == false && screenRenderer != null)
            {
                screenRenderer.material.SetMatrix("_localToWorldMatrix", screenRenderer.transform.localToWorldMatrix);
            }
        }

        /// <summary>
        /// When using RawImage mode, continuously update the texture reference.
        /// This handles the case where the Texture2D object is recreated
        /// (e.g., on resolution change in KotlinBufferToTextureConverter).
        /// </summary>
        void UpdateRawImageTexture()
        {
            if (UseRawImage == false || isRendering == false || rawImageTextureSource == null)
			{
				 return;
			}

            var currentTexture = rawImageTextureSource();
            if (currentTexture != null && screenRawImage.texture != currentTexture)
            {
                screenRawImage.texture = currentTexture;
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

        /// <summary>
        /// Sets up a RawImage-based preview with a live texture source.
        /// The textureSource function is called every frame to get the current
        /// texture, handling cases where the Texture2D is recreated.
        /// </summary>
        public void SetupRawImage(System.Func<Texture> textureSource, Flip flip = default)
        {
            if (screenRawImage == null)
            {
                LogErrorEvent("SetupRawImage called but screenRawImage is not assigned");
                return;
            }
            LogEvent($"Setting up RawImage preview (flip: H={flip.IsHorizontal} V={flip.IsVertical})");
            rawImageTextureSource = textureSource;
            var texture = textureSource();
            if (texture != null)
            {
                screenRawImage.texture = texture;
            }

            // Apply flip via uvRect: default is (0,0,1,1)
            // Vertical flip: y=1, height=-1; Horizontal flip: x=1, width=-1
            float x = flip.IsHorizontal ? 1f : 0f;
            float w = flip.IsHorizontal ? -1f : 1f;
            float y = flip.IsVertical ? 1f : 0f;
            float h = flip.IsVertical ? -1f : 1f;
            screenRawImage.uvRect = new Rect(x, y, w, h);

            ToggleScreenVisibility(true);
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

            Material videoMaterial = null;
            if (UseRawImage)
            {
                // On Android, the decoder returns Flip.None but the texture is natively
                // inverted. The Photon shader (_Flip) compensates implicitly, but for
                // RawImage/uvRect we need to invert the vertical flip manually.
                var rawFlip = flip;
                if (Application.platform == RuntimePlatform.Android)
                    rawFlip = flip * Flip.Vertical;

                SetupRawImage(() => videoPlayer.PlatformView as Texture, rawFlip);
            }
            else
            {
                videoMaterial = SetupMaterial(screenTexture, flip, resolution, fps);
            }

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
            if (UseRawImage)
            {
                screenRawImage.texture = null;
                rawImageTextureSource = null;
            }
            else if (screenRenderer != null)
            {
                screenRenderer.material = initialMaterial;
            }

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
                if (UseRawImage)
                {
                    screenRawImage.gameObject.SetActive(ShouldScreenBeDisplayed);
                }
                else if (screenRenderer != null)
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
