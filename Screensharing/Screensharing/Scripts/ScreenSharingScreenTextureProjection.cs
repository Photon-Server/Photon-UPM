#if XRSHARED_CORE_ADDON_AVAILABLE
using Fusion.XR.Shared;
using Fusion.XR.Shared.Core;
#endif
using System.Collections.Generic;
using UnityEngine;


namespace Fusion.Addons.ScreenSharing
{
    /**
     * 
     * Record a ScreenSharingscreen content (with a temporary camera) and project it on a regular texture on a same size renderer
     * 
     * Handles multiple screen projection in the scene at the same time (making sure that only one records at the same time)
     * 
     * The video playback texture does not support mip mapping.
     * This script captures the video renderer to store it in a mip mappable texture
     * 
     * It can also bypass issues when 2 video are visisble at the same time (the video shader for Android does not support to display two Android external memory video at the same time)
     * 
     * Finally, it can also solve visual effect (image delay) when the rendering surface is moving (being grabbed)
     */
    [DefaultExecutionOrder(ScreenSharingScreen.EXECUTION_ORDER)]
    public class ScreenSharingScreenTextureProjection : MonoBehaviour
    {
        public ScreenSharingScreen screen;
        public Camera screenRenderTextureCamera;
        public float lowerResFPS = 1f;
#if XRSHARED_CORE_ADDON_AVAILABLE
        public RendererVisible projectionTargetRendererVisible;
#endif
        public Renderer projectionTargetRenderer;
        public float bias = 0.12f;
        float nextCapture = 0;
        bool isRendering = false;
        RenderTexture cameraTexture;
        [Header("Texture settings (replaced by screenRenderTextureCamera.targetTexture if any)")]
        [Tooltip("Leave it to 0,0 if you want to use the values in the screenRenderTextureCamera.targetTexture. Values are required if you don't have set a camera or its target texture to allow automatic camera creation and target render texture configuration")]
        public Vector2 lowResResolution = new Vector2(0, 0);
        public int anisoLevel = 0;
        public UnityEngine.Experimental.Rendering.GraphicsFormat renderTextureDepthStencilFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.D16_UNorm;
        [Header("Low res renderer settings (used only if no lowResRenderer is provided)")]
        public Material lowResMaterial = null;
        [Header("Layer override")]
        public string lowResRendererLayerName = "Default";
        public string screenLayerName = "InvisibleForLocalPlayer";

        #region Multiscreen handling
        //If several screen texture projection cohexist, we want to have only one recorded at a given time
        static List<ScreenSharingScreenTextureProjection> ScreenProjections = new List<ScreenSharingScreenTextureProjection>();
        static bool NextScreenProjectionUpdatedThisFrame = false;
        static ScreenSharingScreenTextureProjection NextRenderingProjection;
        static ScreenSharingScreenTextureProjection LastRenderingProjection;
        static ScreenSharingScreenTextureProjection RenderingProjectionThisFrame;
        #endregion

        #region Projection disabling handling
        [Tooltip("The video shader for Android has an issue when 2 video are visible at the same time (it does not work):" +
            " if the screen projection is used only to bypass this issue (not to add mimapping to the texture, ...), the projection is not needed when there is only one screen rendering. " +
            "In this case, you can set this param to true")]
        [SerializeField] bool disableScreenProjectionForSingleScreenProjection = true;
        [Tooltip("When moving a screen using the Android external video shader, moving the surface might cause unpleasant visual effect: in this case, projecting is safer")]
        [SerializeField] bool forceProjectionWhenGrabbing = true;


        bool projectionModeEnabled = false;
        bool projectionCameraSetup = false;
        int originalScreenRendererLayer;
        bool registerInScreenProjectionsList = false;
        IGrabbable grabbable;
        #endregion

        private void Awake()
        {
            if (screen == null)
            {
                screen = GetComponentInChildren<ScreenSharingScreen>();
            }
            if (screen == null)
            {
                Debug.LogError("Missing screen");
            }
            grabbable = GetComponentInParent<IGrabbable>();
        }

        private void RegisterScreenProjection()
        {
            if (registerInScreenProjectionsList) return;
            registerInScreenProjectionsList = true;
            ScreenProjections.Add(this);
        }

        private void UnregisterScreenProjection()
        {
            if (registerInScreenProjectionsList == false) return;
            registerInScreenProjectionsList = false;
            if (ScreenProjections.Contains(this))
            {
                ScreenProjections.Remove(this);
            }
        }

        private void OnDestroy()
        {
            if (cameraTexture) Destroy(cameraTexture);
            UnregisterScreenProjection();
        }

        public Vector3 rendererPosition;
        public Quaternion rendererRotation;

        #region Camera setup
        void EnableProjectionMode()
        {
            if (projectionModeEnabled) return;
            projectionModeEnabled = true;
            ConfigureCamera();
            projectionTargetRenderer.enabled = screen.isRendering;
            if (screen && screen.screenRenderer)
            {
                screen.screenRenderer.enabled = screen.isRendering;
                if (string.IsNullOrEmpty(screenLayerName) == false)
                {
                    screen.screenRenderer.gameObject.layer = LayerMask.NameToLayer(screenLayerName);
                }
            }
        }

        bool screenIsRenderingOnProjectionModeDesactivation = false;
        void DisableProjectionMode()
        {
            if (projectionModeEnabled == false && screenIsRenderingOnProjectionModeDesactivation == screen.isRendering) return;
            projectionModeEnabled = false;
            if (projectionTargetRenderer)
            {
                projectionTargetRenderer.enabled = false;
            }
            screenIsRenderingOnProjectionModeDesactivation = false;
            if (screen && screen.screenRenderer)
            {
                screenIsRenderingOnProjectionModeDesactivation = screen.isRendering;
                screen.screenRenderer.enabled = screen.isRendering;
                if (string.IsNullOrEmpty(screenLayerName) == false)
                {
                    screen.screenRenderer.gameObject.layer = originalScreenRendererLayer;
                }
            }
        }

        void ConfigureCamera()
        {
            if (projectionCameraSetup) return;
            projectionCameraSetup = true;
#if XRSHARED_CORE_ADDON_AVAILABLE
            if (projectionTargetRendererVisible == null) projectionTargetRendererVisible = GetComponentInChildren<RendererVisible>();
            if (projectionTargetRendererVisible != null && projectionTargetRenderer == null) projectionTargetRenderer = projectionTargetRendererVisible.GetComponent<Renderer>();
#endif

            if (projectionTargetRenderer == null)
            {
                if (screen && screen.screenRenderer)
                {
                    // Backup screen renderer original layer
                    originalScreenRendererLayer = screen.screenRenderer.gameObject.layer;

                    // Create default low res renderer
                    var lowResRendererGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    lowResRendererGO.name = "LowResRenderer";
                    projectionTargetRenderer = lowResRendererGO.GetComponent<Renderer>();
                    lowResRendererGO.transform.parent = screen.screenRenderer.transform.parent;
                    lowResRendererGO.transform.localPosition = screen.screenRenderer.transform.localPosition;
                    lowResRendererGO.transform.localRotation = screen.screenRenderer.transform.localRotation;
                    lowResRendererGO.transform.localScale = screen.screenRenderer.transform.localScale;
#if XRSHARED_CORE_ADDON_AVAILABLE
                    projectionTargetRendererVisible = lowResRendererGO.AddComponent<RendererVisible>();
#endif
                    projectionTargetRenderer.material = lowResMaterial;
                    if (lowResMaterial == null)
                    {
                        var shader = Shader.Find("Universal Render Pipeline/Unlit");
                        if (shader == null)
                        {
                            Debug.LogError("Default shader not found (not using URP?). Please set lowResMaterial, or preapre a lowResRenderer directly");
                        } else
                        {
                            lowResMaterial = new Material(shader);
                            lowResMaterial.color = Color.white;
                        }
                    }
                    projectionTargetRenderer.material = lowResMaterial;

                    if (string.IsNullOrEmpty(lowResRendererLayerName) == false)
                    {
                        projectionTargetRenderer.gameObject.layer = LayerMask.NameToLayer(lowResRendererLayerName);
                    }
                }
                else
                {
                    Debug.LogError("Missing lowResRenderer");
                }
            }

            if (screen && screen.screenRenderer && string.IsNullOrEmpty(screenLayerName) == false)
            {
                screen.screenRenderer.gameObject.layer = LayerMask.NameToLayer(screenLayerName);
            }

            if (screenRenderTextureCamera == null)
            {
                screenRenderTextureCamera = GetComponentInChildren<Camera>();
            }
            if (screenRenderTextureCamera == null)
            {
                CreateDefaultCamera();
            }

            DetermineTargetResolution();           

            // Create the camera render texure, ensuring it supports mimap
            PrepareTextureForResolution(lowResResolution);


            if (screenRenderTextureCamera)
            {
               // Make sure the camera does not run automatically
                screenRenderTextureCamera.enabled = false;

                // Ensure the camera only records the screen layer
                screenRenderTextureCamera.cullingMask = 1 << screen.screenRenderer.gameObject.layer;
            }

            projectionTargetRenderer.enabled = false;

            DetermineNextCapture();

#if XRSHARED_CORE_ADDON_AVAILABLE
            if (projectionTargetRendererVisible && projectionTargetRendererVisible.isVisible == false)
                Debug.Log("ScreenSharingScreenLODHandler Initial biais: " + cameraTexture.mipMapBias);
#endif

        }

        public void PrepareTextureForResolution(Vector2 resolution)
        {
            lowResResolution = resolution;
            if (cameraTexture == null || cameraTexture.width != (int)lowResResolution.x || cameraTexture.height != (int)lowResResolution.y)
            {
                if (cameraTexture)
                {
                    Destroy(cameraTexture);
                }
                RenderTextureFormat rtFormat = RenderTextureFormat.Default;
                if (screenRenderTextureCamera && screenRenderTextureCamera.targetTexture != null)
                {
                    anisoLevel = screenRenderTextureCamera.targetTexture.anisoLevel;
                    rtFormat = screenRenderTextureCamera.targetTexture.format;
                }
                cameraTexture = new RenderTexture((int)lowResResolution.x, (int)lowResResolution.y, 0, rtFormat);
                cameraTexture.useMipMap = true;
                cameraTexture.anisoLevel = anisoLevel;
                cameraTexture.depthStencilFormat = renderTextureDepthStencilFormat;
            }

            if (screenRenderTextureCamera)
            {
                // Use the render texture as the camera output
                screenRenderTextureCamera.targetTexture = cameraTexture;
            }

            if (projectionTargetRenderer)
            {
                // Edit low res renderer material with the ouput texture form the camera
                projectionTargetRenderer.material.mainTexture = cameraTexture;
            }
        }

        void CreateDefaultCamera()
        {
            var cameraGo = new GameObject("LODCamera");
            cameraGo.transform.parent = transform;
            cameraGo.transform.localPosition = new Vector3(0, 0, -0.01f);
            cameraGo.transform.localRotation = Quaternion.identity;
            if (screen && screen.screenRenderer)
            {
                cameraGo.transform.parent = screen.screenRenderer.transform;
                cameraGo.transform.localPosition = new Vector3(0, 0, -0.01f);
                cameraGo.transform.localRotation = Quaternion.identity;
            }
            screenRenderTextureCamera = cameraGo.AddComponent<Camera>();
            screenRenderTextureCamera.nearClipPlane = 0f;
            screenRenderTextureCamera.farClipPlane = 0.02f;
            screenRenderTextureCamera.clearFlags = CameraClearFlags.Color;
            screenRenderTextureCamera.backgroundColor = Color.black;
            AdaptLODCameraViewport();

            rendererPosition = screen.screenRenderer.transform.localPosition;
            rendererRotation = screen.screenRenderer.transform.localRotation;
        }

        void DetermineTargetResolution()
        {
            if (screenRenderTextureCamera && screenRenderTextureCamera.targetTexture == null)
            {
                if (lowResResolution.x == 0 || lowResResolution.y == 0)
                {
                    lowResResolution.x = 1024;
                    lowResResolution.y = 768;
                    if (screen && screen.screenRenderer)
                    {
                        // Fallback values
                        var scale = screen.screenRenderer.transform.lossyScale;
                        if (scale.x > 0)
                        {
                            lowResResolution.y = lowResResolution.x * scale.y / scale.x;
                        }
                    }
                    Debug.LogWarning($"Missing screenRenderTextureCamera. Set it, or add it as a child with a targetTexture, or set lowResResolution values. Using fallback values for lowResResolution: {lowResResolution.x}x{lowResResolution.y}");
                }
            }

            if (screenRenderTextureCamera.targetTexture != null)
            {
                if (lowResResolution.x == 0) lowResResolution.x = screenRenderTextureCamera.targetTexture.width;
                if (lowResResolution.y == 0) lowResResolution.y = screenRenderTextureCamera.targetTexture.height;
            }
        }

        Vector3 scaleUsedForViewPortAdaptation; 

        [ContextMenu("AdaptLODCameraViewport")]
        void AdaptLODCameraViewport()
        {
            screenRenderTextureCamera.orthographic = true;
            float xOffset = 0;
            float yOffset = 0;
            if (screen && screen.screenRenderer)
            {
                bool isScreenRendererEnabled = screen.screenRenderer.enabled;
                screen.screenRenderer.enabled = true;
                var scale = screen.screenRenderer.transform.lossyScale;
                screen.screenRenderer.enabled = isScreenRendererEnabled;
                scaleUsedForViewPortAdaptation = scale;
                if (scale.y > scale.x)
                {
                    screenRenderTextureCamera.orthographicSize = scale.x / 2;
                }
                else
                {
                    screenRenderTextureCamera.orthographicSize = scale.y / 2;
                }
                screenRenderTextureCamera.transform.localPosition = new Vector3(0, 0, -0.01f / scale.z);
            }
            else
            {
                Debug.LogError("Missing screen");
            }
            screenRenderTextureCamera.rect = new Rect(xOffset, yOffset, 1, 1);
        }
        #endregion 

        void DetermineNextCapture()
        {
            nextCapture = Time.time + 1f / lowerResFPS;
        }

        void Capture()
        {
            if (screenRenderTextureCamera)
            {
                screenRenderTextureCamera.targetTexture.mipMapBias = bias;
                screenRenderTextureCamera.Render();
            }
            RenderingProjectionThisFrame = this;
        }

        public bool ShouldRender
        {
            get
            {
                if (screen == null || screen.isRendering == false)
                {
                    return false;
                }
                if (Time.time <= nextCapture)
                {
                    return false;
                }
#if XRSHARED_CORE_ADDON_AVAILABLE
                if (projectionTargetRendererVisible && projectionTargetRendererVisible.isVisible == false)
                {
                    return false;
                }
#endif
                return true;
            }
        }

        float lastCaptureTime;
        private void Update()
        {
            ScreenProjectionHandling();
        }

        bool IsScreenProjectionRequired
        {
            get
            {
                // The video shader for Android has an issue when 2 video are visible at the same time (it does not work): if the screen projection is used only to bypass this issue, the projection is not needed when there is only one screen rendering
                bool screenProjectionRequired = disableScreenProjectionForSingleScreenProjection == false || ScreenProjections.Count > 1;
                if(forceProjectionWhenGrabbing && grabbable != null && grabbable.IsGrabbed)
                {
                    // When moving a screen using the Android external video shader, moving the surface might cause unpleasant visual effect (the world matrix sent to the shader capturing a positioning slightly "late"): in this case, projecting is safer
                    screenProjectionRequired = true;
                }
                return screenProjectionRequired;
            }
        }

        void ScreenProjectionHandling() {
            if (screen && screen.isRendering)
            {
                RegisterScreenProjection();
            } 
            else
            {
                UnregisterScreenProjection();
            }

            if (IsScreenProjectionRequired)
            {
                EnableProjectionMode();
            }
            else
            {
                DisableProjectionMode();
                return;
            }

            if (screen && screen.screenRenderer && screen.screenRenderer.transform.lossyScale != scaleUsedForViewPortAdaptation)
            {
                screen.LogEvent("[TextureProjection] AdaptViewPort for screen scale change");
                AdaptLODCameraViewport();
            }

            PrepareNextScreenProjectionLogic();

            if (screen.isRendering != isRendering)
            {
                // Rendering just started/stoped on the screen, we enable the projection screen accordingly
                projectionTargetRenderer.enabled = screen.isRendering;
                isRendering = screen.isRendering;
                if(isRendering == false)
                {
                    screen.LogEvent($"[TextureProjection] (lcp:{(Time.time - lastCaptureTime):000.00}) Stopped rendering");
                    screen.LogState("");
                }
                else
                {
                    screen.LogEvent($"[TextureProjection] (lcp:{(Time.time - lastCaptureTime):000.00}) Started rendering");
                }
            }

            if (ShouldRenderThisFrame == false)
            {
                if (ShouldUseMultipleProjectionProtection())
                {
                    // Not our turn to render. We hide our screen, to avoid recording it by mistake with another screen porjection
                    screen.screenRenderer.enabled = false;
                    screen.LogState($"[TextureProjection] (lcp:{(Time.time - lastCaptureTime):000.00}) Not our turn to render");
                }
                return;
            }
            else
            {
                if (ShouldUseMultipleProjectionProtection())
                {
                    screen.screenRenderer.enabled = true;
                }
                screen.LogState($"[TextureProjection] (lcp:{(Time.time - lastCaptureTime):000.00}) Rendering...");
                lastCaptureTime = Time.time;
                Capture();
                DetermineNextCapture();
            }
        }

        private void LateUpdate()
        {
            UpdateNextScreenProjection();
        }

        #region Multiple projection handling
        protected virtual bool ShouldUseMultipleProjectionProtection()
        {
            // Note: not needed in XR multi-pass with the regular video SDK shader: only in single-pass with the specific Android stereo shader
            // TODO: automatically detect this setup
            return true;
        }

        bool ShouldRenderThisFrame { 
            get
            {
                bool allowedToRenderThisFrame = true;
                if (ShouldUseMultipleProjectionProtection())
                {
                    ScreenSharingScreenTextureProjection projectionThatShouldRenderThisFrame = NextRenderingProjection;
                    if (LastRenderingProjection != null && LastRenderingProjection != this)
                    {
                        // We make sure that we have one frame where no one is rendering between 2 distinct rendering to avoid potential issue with 2 material with an OES shader loaded at the same time (only 1 would render)
                        return false;
                    }
                    allowedToRenderThisFrame = projectionThatShouldRenderThisFrame == this;
                }

                return ShouldRender && allowedToRenderThisFrame;
            }
        }

        static void PrepareNextScreenProjectionLogic()
        {
            if (NextScreenProjectionUpdatedThisFrame)
            {
                NextScreenProjectionUpdatedThisFrame = false;
            }
        }

        static void UpdateNextScreenProjection()
        {
            if (NextScreenProjectionUpdatedThisFrame == false)
            {
                NextScreenProjectionUpdatedThisFrame = true;

                // Security check in case the next rendering projection has stopped neededing to render, to avoid locking the other projections form rendering
                if (NextRenderingProjection && NextRenderingProjection.ShouldRender == false)
                {
                    NextRenderingProjection = null;
                }

                // Determine next rendering projection
                if (RenderingProjectionThisFrame != null)
                {
                    var projectionIndex = ScreenProjections.IndexOf(RenderingProjectionThisFrame);
                    if (projectionIndex >= 0)
                    {
                        // Looking for the next projection screen needing to render
                        ScreenSharingScreenTextureProjection candidateScreen = null;
                        int nextProjectionIndex = projectionIndex;
                        while (candidateScreen != RenderingProjectionThisFrame)
                        {
                            nextProjectionIndex = (nextProjectionIndex + 1) % ScreenProjections.Count;
                            candidateScreen = ScreenProjections[nextProjectionIndex];
                            if (candidateScreen.ShouldRender)
                            {
                                NextRenderingProjection = candidateScreen;
                                break;
                            }
                        }
                        if (candidateScreen == RenderingProjectionThisFrame && candidateScreen.ShouldRender == false)
                        {
                            // No cnadidates needing to render (we exited the while due to having tested all the projections, with no success)
                            NextRenderingProjection = null;
                        }
                    }
                }
                else if (NextRenderingProjection == null)
                {
                    foreach(var proj in ScreenProjections)
                    {
                        if (proj.ShouldRender)
                        {
                            NextRenderingProjection = proj;
                            break;
                        }
                    }
                }

                // Store the last rendering projection
                LastRenderingProjection = RenderingProjectionThisFrame;
                RenderingProjectionThisFrame = null;
            }
        }
        #endregion
    }
}
