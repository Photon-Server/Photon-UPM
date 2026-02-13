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

    public abstract class AndroidTextureVideoRecorderBase : IVideoRecorderPusher
    {
        int encoderFPS = 30;
        public Action<IVideoRecorder> OnReady { get; set; }

        protected AndroidTextureVideoEncoder encoder;
        protected bool onReadyPending;
        RenderTexture encodedRenderTexture;
        RenderTexture previewRenderTexture;

        [Tooltip("If true, the recorder will make sure that the preview texture (PlatformView) is in the proper orientation (Flip.None). " +
            "However, it requires an additionnal Graphics.Blit call. " +
            "Otherwise, the image is flipped, and Flip returns flip.Vertical")]
        public bool automaticPreviewFlip = true;

        #region IVideoRecorderPusher
        public IVideoSink VideoSink { private get; set; }
        #endregion

        #region IVideoRecorder

        public IEncoder Encoder
        {
            get { return encoder; }
        }

        public string Error => Encoder == null ? "" : Encoder.Error;

        public object PlatformView => previewRenderTexture;

        public int Width { get; private set; } = 0;

        public int Height { get; private set; } = 0;

        public Rotation Rotation => Rotation.Rotate0;

        public Flip Flip => automaticPreviewFlip ? Flip.None : Flip.Vertical;

        protected bool isCollecting = false;

        public void Dispose()
        {
            Debug.Log("[AndroidTextureVideoRecorderBase] Dispose");
            isCollecting = false;
            encoder.Dispose();
            encoder = null;
            DisposeRenderTextures();
        }
        #endregion

        void DisposeRenderTextures()
        {
            if (encodedRenderTexture != null)
            {
                encodedRenderTexture.Release();
            }
            if (previewRenderTexture != null)
            {
                previewRenderTexture.Release();
            }

            GameObject.Destroy(encodedRenderTexture);
            GameObject.Destroy(previewRenderTexture);
        }

        void PrepareRenderTextures()
        {
            if (encodedRenderTexture == null || encodedRenderTexture.width != Width || encodedRenderTexture.height != Height)
            {
                DisposeRenderTextures();
                encodedRenderTexture = new RenderTexture(Width, Height, 0);
                if (automaticPreviewFlip)
                {
                    previewRenderTexture = new RenderTexture(Width, Height, 0);
                }
                else
                {
                    previewRenderTexture = encodedRenderTexture;
                }
            }
        }

        protected virtual void UpdateRenderTextures(Texture src)
        {
            PrepareRenderTextures();

            Graphics.Blit(src, encodedRenderTexture, new Vector2(1, -1), new Vector2(0, 1)); // flip vertically
            if (automaticPreviewFlip)
            {
                Graphics.Blit(src, previewRenderTexture);
            }
        }

        public virtual void Init(IVoiceLogger logger, VoiceInfo info)
        {
            Width = info.Width;
            Height = info.Height;
            encoderFPS = info.FPS;
            Debug.Log($"[{this.GetType().Name}.Init] Encoder FPS: {info.FPS} / Width: {Width} / Height: {Height}");

            encoder = new AndroidTextureVideoEncoder(logger, info);
            if (encoder.Error == null)
            {
                onReadyPending = true;
            }

            StartCollectingTextureUpdates();
        }

        protected abstract bool IsCollectingPossible();

        protected abstract Texture CollectTexture();

        async void StartCollectingTextureUpdates()
        {
            Debug.Log("[AndroidTextureVideoRecorderBase] Collecting started");
            try
            {
                isCollecting = true;
                int delayMS = (int)(1000 / encoderFPS);
                while (isCollecting)
                {
                    if (IsCollectingPossible())
                    {
                        var texture = CollectTexture();
                        if (texture != null)
                        {

                            UpdateRenderTextures(texture);
                            EncodeTexture(encodedRenderTexture);
                        }
                    }
                    await Task.Delay(delayMS);
                }
                Debug.Log("[AndroidTextureVideoRecorderBase] Collecting stopped");

            }
            catch (System.Exception e)
            {
                if (isCollecting)
                {
                    Debug.LogException(e);
                    Debug.LogError("Restart AndroidTextureVideoRecorderBase collecting after an exception");
                    StartCollectingTextureUpdates();
                }
            }
        }

        void EncodeTexture(RenderTexture src)
        {
            if (encoder != null)
            {
                if (OnReady != null && onReadyPending)
                {
                    OnReady(this);
                    onReadyPending = false;
                }
                var texturePointer = src.GetNativeTexturePtr();
                int textureId = texturePointer.ToInt32();
                encoder.encodeTexture(textureId);
            }
        }
    }
#endif
}

