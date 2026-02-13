
#if PHOTON_VOICE_AVAILABLE
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
#endif
using UnityEngine;
using Fusion.XRShared.Tools;
using System.Reflection;

namespace Fusion.Addons.VoiceHelpers
{
    /// <summary>
    /// Wait for Microphone access permissions to be available before letting FusionVoiceClient connect
    /// Note: recordWhenJoined should be disabled on the recorder
    /// 
    /// Should be used alongside a PermissionsRequester, to align permissions on the desired timing (to avoid conflicting requests)
    /// </summary>
#if PHOTON_VOICE_AVAILABLE
    [RequireComponent(typeof(FusionVoiceClient))]
#endif
    [DefaultExecutionOrder(FusionVoiceClientPermissionWaiter.LOWPRIORITY_EXECUTION_ORDER)]
    public class FusionVoiceClientPermissionWaiter : PermissionWaiter
    {
        public const int LOWPRIORITY_EXECUTION_ORDER = PermissionWaiter.EXECUTION_ORDER + 50;
#if PHOTON_VOICE_AVAILABLE
        FusionVoiceClient fusionVoiceClient;
        bool shouldRestartRecording = false;
        bool isPaused = false;
        bool isFocused = true;

        #region PermissionWaiter override 
        public override string PermissionName => "android.permission.RECORD_AUDIO";

        protected override void Awake()
        {
            fusionVoiceClient = GetComponent<FusionVoiceClient>();
            base.Awake();
        }

        protected override void OnPermissionRequired()
        {
            base.OnPermissionRequired();

            // Disable recording, to restart it only when microphone access is granted
            PreventRecordersToStartRecording();
        }

        /// <summary>
        /// Should be called manually (for instance in a PermissionsRequester callback) if checkPermisisonGrantAutomatically is not checked
        /// </summary>
        public override void OnPermissionGranted()
        {
            base.OnPermissionGranted();

            // Reenable recording
            TryRestartRecording();
        }
        #endregion

        void TryRestartRecording()
        {
            if (isPaused == false && isFocused == true)
            {
                RestartRecording();
            }
            else
            {
                // We avoid restart the voice recording while the application is paused, as it may cause issues
                Debug.Log($"[PermissionsRequester-{GetType().Name}] We avoid restarting the voice recording while the application is paused, as it may cause issues");
                shouldRestartRecording = true;
            }
        }

        protected override void Update()
        {
            base.Update();
            if (shouldRestartRecording)
            {
                TryRestartRecording();
            }
        }

        private void OnApplicationPause(bool pause)
        {
            isPaused = pause;
            Debug.Log($"[PermissionsRequester-{GetType().Name}] OnApplicationPause isPaused:{isPaused} / isFocused:{isFocused} / shouldRestartRecording:{shouldRestartRecording}");
        }

        private void OnApplicationFocus(bool focus)
        {
            isFocused = focus;
            Debug.Log($"[PermissionsRequester-{GetType().Name}] OnApplicationFocus isPaused:{isPaused} / isFocused:{isFocused} / shouldRestartRecording:{shouldRestartRecording}");
        }

        void RestartRecording()
        {
            Debug.Log($"[PermissionsRequester-{GetType().Name}] RestartRecording");
            if (fusionVoiceClient.PrimaryRecorder) RestartRecordingForRecorder(fusionVoiceClient.PrimaryRecorder);
            var recorder = fusionVoiceClient.GetComponentInChildren<Recorder>();
            if (recorder != null)
            {
                RestartRecordingForRecorder(recorder);
            }
            shouldRestartRecording = false;
        }

        void PreventRecordersToStartRecording()
        {
            if (fusionVoiceClient.PrimaryRecorder) PreventRecorderToStartRecording(fusionVoiceClient.PrimaryRecorder);
            var recorder = fusionVoiceClient.GetComponentInChildren<Recorder>();
            if (recorder != null)
            {
                PreventRecorderToStartRecording(recorder);
            }
        }

        void RestartRecordingForRecorder(Recorder recorder)
        {
            ChangeRecorderRecording(recorder, isRecording: true);
        }

        void PreventRecorderToStartRecording(Recorder recorder)
        {
            ChangeRecorderRecording(recorder, isRecording: false);
        }

        void ChangeRecorderRecording(Recorder recorder, bool isRecording)
        {
            if (recorder == null) return;
            recorder.RecordingEnabled = isRecording;
            
            // Adapt recordWhenJoined
            var fieldToNeutralize = "recordWhenJoined";
            try
            {

                var field = recorder.GetType().GetField(fieldToNeutralize, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    bool value = (bool)field.GetValue(recorder);
                    if (value != isRecording)
                    {
                        Debug.Log($"[PermissionsRequester-{GetType().Name}] Changer Recorder.{fieldToNeutralize} to {isRecording}");
                        field.SetValue(recorder, isRecording);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PermissionsRequester] Error while trying to change Recorder.{fieldToNeutralize} to {isRecording}. Should adapt {GetType().Name} to the new Recorder interface");
                Debug.LogException(e);
            }
        }
#endif
    }
}
