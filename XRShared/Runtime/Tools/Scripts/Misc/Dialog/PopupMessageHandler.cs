#if UNITY_EDITOR
using Fusion.XR.Shared.Automatization;
using UnityEditor;
#endif

using Fusion.XR.Shared.Core;
using System.Threading.Tasks;
using UnityEngine;

namespace Fusion.XRShared.Tools
{
    /// <summary>
    /// Display message popup in front of the hardware rig camera
    /// </summary>
    public class PopupMessageHandler : MonoBehaviour
    {
        public PopupMessage popup;

        [Header("Popup spawn (if not set above)")]
        public GameObject popupPrefab;

        [Header("Hardware rig positioning")]
        public bool positionPopupInFrontOfHardwareRig = true;

        public Vector3 positionOffsetRelativeToHeadset = new Vector3(0, 0, 0.7f);
        public Vector3 rotationOffsetRelativeToHeadset = new Vector3(0, 0, 0);

        public float teleportDistanceWhenVisible = 1f;
        public float smoothRepositionPopupDistanceAfterDisplay = 0.02f;
        public float smoothRepositionPopupMaxDurationForPermanentMessagesAfterDisplay = 2f;
        public float smoothingSpeed = 0.2f;

        public static PopupMessageHandler _instance;
        public static PopupMessageHandler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.FindAnyObjectByType<PopupMessageHandler>();
                }
                return _instance;
            }
        }

        protected bool IsDisplayed => popup != null && popup.gameObject.activeSelf;
        protected bool IsDisplayingPermanentMessage => IsDisplayed && popupEndDisplayTime == -1;

        protected IHardwareRig _hardwareRig;

        const string PopupPrefabName = "DefaultPopup";

        float popupStartDisplayTime = -1;
        float popupEndDisplayTime = -1;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        [ContextMenu("TestPermanentMessage")]
        void TestPermanentMessage()
        {
            ShowPermanentMessage("Test");
        }

        [ContextMenu("TestMessage")]
        void TestMessage()
        {
            ShowMessage("Test", 10);
        }

        private void Update()
        {
            if (IsDisplayed)
            {
                if (popupEndDisplayTime == -1)
                {
                    popup.SetProgress(0);
                }
                else if (popupEndDisplayTime < Time.time)
                {
                    HideMessage();
                }
                else
                {
                    float progress = (Time.time - popupStartDisplayTime) / (popupEndDisplayTime - popupStartDisplayTime);
                    popup.SetProgress(progress);
                }
            }

            SmoothPositioning();
        }

        private void OnValidate()
        {
            _hardwareRig = HardwareRigsRegistry.GetHardwareRig();
#if UNITY_EDITOR
            if (popupPrefab == null && popup == null)
            {
                if (AssetLookup.TryFindAsset(new Fusion.XR.Shared.Automatization.AssetLookup.AssetLookupCriteria(PopupPrefabName, extension: "prefab", requiredPathElements: new string[] { "XRShared" }), out GameObject prefab))
                {
                    this.popupPrefab = prefab;
                }
                else
                {
                    Debug.LogError("Unable to find " + PopupPrefabName);
                }
            }
#endif
        }

        public void ShowPermanentMessage(string text)
        {
            ShowMessage(text, displayDuration: 0);
        }

        public async void ShowMessage(string text, float displayDuration = 3)
        {
            if (popup == null)
            {
                var popupGameObject = GameObject.Instantiate(popupPrefab);
                popup = popupGameObject.GetComponentInChildren<PopupMessage>();
            }
            if (IsDisplayed == false && popup)
            {
                popup.gameObject.SetActive(true);
            }

            popup.text.text = text;

            if (positionPopupInFrontOfHardwareRig)
            {
                await WaitForHardwareRig();
                PositionPopup();
            }

            popupStartDisplayTime = Time.time;
            if (displayDuration > 0)
            {
                popupEndDisplayTime = Time.time + displayDuration;
            }
            else
            {
                popupEndDisplayTime = -1;
                popup.SetProgress(0);
            }
        }

        public void HideMessage()
        {
            if (IsDisplayed && popup)
            {
                popup.gameObject.SetActive(false);
            }
            popupStartDisplayTime = -1;
            popupEndDisplayTime = -1;
        }

        public async Task WaitForHardwareRig()
        {
            while (_hardwareRig == null)
            {
                _hardwareRig = HardwareRigsRegistry.GetHardwareRig();
                if (_hardwareRig == null)
                {
                    Debug.Log("Looking for hardware rig ...");
                    await AsyncTask.Delay(100);
                }
            }
        }

        public Pose PoseToAlignWithHardwareRig()
        {
            Pose pose = default;
            if (_hardwareRig != null && _hardwareRig.Headset != null && popup && popup.gameObject.activeSelf)
            {
                var headsetTransform = _hardwareRig.Headset.gameObject.transform;

                var windowPosition = headsetTransform.TransformPoint(positionOffsetRelativeToHeadset);
                Quaternion windowRotation = Quaternion.Euler(rotationOffsetRelativeToHeadset) * Quaternion.Euler(0, headsetTransform.eulerAngles.y, 0);
                pose.position = windowPosition;
                pose.rotation = windowRotation;
            }
            return pose;
        }

        public void PositionPopup()
        {
            if (_hardwareRig != null && popup && popup.gameObject.activeSelf)
            {
                var popupPose = PoseToAlignWithHardwareRig();
                popup.transform.position = popupPose.position;
                popup.transform.rotation = popupPose.rotation;
                popup.transform.localScale = _hardwareRig.transform.localScale;
            }
            else
            {
                Debug.LogError("No popup found");
            }
        }

        protected void SmoothPositioning()
        {
            if (IsDisplayed && positionPopupInFrontOfHardwareRig && (teleportDistanceWhenVisible > 0 || smoothRepositionPopupDistanceAfterDisplay > 0))
            {
                Pose desiredPopupPose = PoseToAlignWithHardwareRig();
                var distance = Vector3.Distance(desiredPopupPose.position, popup.transform.position);
                if (teleportDistanceWhenVisible > 0 && distance > teleportDistanceWhenVisible)
                {
                    PositionPopup();
                }
                if (smoothRepositionPopupDistanceAfterDisplay > 0 && distance > smoothRepositionPopupDistanceAfterDisplay)
                {
                    var smoothingEndTime = IsDisplayingPermanentMessage ? (popupStartDisplayTime + smoothRepositionPopupMaxDurationForPermanentMessagesAfterDisplay) : popupEndDisplayTime;
                    if (Time.time < smoothingEndTime)
                    {
                        var progress = (smoothingSpeed * Time.deltaTime) / distance;
                        var position = Vector3.Lerp(popup.transform.position, desiredPopupPose.position, progress);
                        var rotation = Quaternion.Slerp(popup.transform.rotation, desiredPopupPose.rotation, progress);
                        popup.transform.rotation = rotation;
                        popup.transform.position = position;
                    }
                }
            }
        }
    }
}
