#if MRUK_AVAILABLE
using Meta.XR.MRUtilityKit;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Fusion.Addons.AnchorsAddon
{
    /// <summary>
    /// AnchorTag handles the visual aspect of the anchors (texts, sliders, size)
    /// </summary>
    public class AnchorTag : MonoBehaviour
    {
        public string anchorId;
        public TMPro.TextMeshPro idText;
        public TMPro.TextMeshPro detailText;
        public GameObject anchorVisual;
        public GameObject anchorVisualForKnownTagDisplayMode;
        public bool slidersMustBeDisplayed = true;
        public Slider slider;
        public Slider secondarySlider;
        public GameObject panelCanvas;        
        public bool displayPanelCanvas = false;
        Color defaultDetailTextColor;
        public bool autoregisterToWorldTrackingComponents = false;
        public string mrukTrackableIdPrefix = "";
        // If set, the visual for this anchor will match the other one size
        public AnchorTag referenceAnchor;
        public Vector3 visualLocalPosition = Vector3.zero;
        public Vector3 visualLocalScale = Vector3.one;

        public List<GameObject> objectsToDisplayWhenDetected = new List<GameObject>();

        [Tooltip("IsDetected results. Only for debugging purposes")]
        [SerializeField] bool isDetected = false;

        [Header ("MRUK")]
        [SerializeField] bool adaptVisualForMURKTrackable = false;
        [SerializeField] Vector3 eulerRotationAdapatationForMURKtrackable = new Vector3(180, 0, 0);


#if MRUK_AVAILABLE
        [HideInInspector]
        public MRUKTrackable mrukTrackable;
#endif
        public bool IsDetected
        {
            get
            {
                isDetected = gameObject.activeSelf && enabled;
#if MRUK_AVAILABLE
                if (mrukTrackable && mrukTrackable.IsTracked == false)
                {
                    isDetected = false;
                }
#endif
                return isDetected;
            }
        }

        public bool ISMRUKTrackedAnchor
        {
            get
            {
#if MRUK_AVAILABLE
                if (mrukTrackable)
                {
                    return true;
                }
#endif
                return false;
            }
        }

        private void Awake()
        {
            if (panelCanvas)
            {
                panelCanvas.SetActive(displayPanelCanvas);
            }
            if (detailText != null)
            {
                defaultDetailTextColor = detailText.color;
            }

            if (anchorVisual)
            {
                ActivateKnownTagDisplayMode(false);
            }
                

#if MRUK_AVAILABLE
            mrukTrackable = GetComponentInParent<MRUKTrackable>();
            if(mrukTrackable != null)
            {
                autoregisterToWorldTrackingComponents = true;
            }
#endif
        }

        private void OnDestroy()
        {
            if (autoregisterToWorldTrackingComponents)
            {
                foreach (var worldAnchorTracking in FindObjectsByType<IRLAnchorTracking>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    worldAnchorTracking.UnregisterIRLAnchor(this);
                }
            }
        }

        private void Start()
        {
            if(idText == null)
            {
                idText = GetComponentInChildren<TMPro.TextMeshPro>();
            }
#if MRUK_AVAILABLE
            if (mrukTrackable != null)
            {
                var MRUKpayload = mrukTrackableIdPrefix + mrukTrackable.MarkerPayloadString;
                // Security, in case of v78 SDK x v83 OS incompatibility
                MRUKpayload = MRUKpayload.Trim((char)0);
                anchorId = MRUKpayload;
            }

            if (adaptVisualForMURKTrackable && anchorVisualForKnownTagDisplayMode && (mrukTrackable != null || referenceAnchor?.mrukTrackable != null))
            {
                anchorVisualForKnownTagDisplayMode.transform.localRotation *= Quaternion.Euler(eulerRotationAdapatationForMURKtrackable);
            }

#endif
            SetAnchorId(anchorId);
            if (autoregisterToWorldTrackingComponents)
            {
                foreach(var worldAnchorTracking in FindObjectsByType<IRLAnchorTracking>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    worldAnchorTracking.RegisterIRLAnchor(this, overrideDisableAtStart: false);
                }
            }
        }

        private void Update()
        {
            if (referenceAnchor != null)
            {
                visualLocalPosition = referenceAnchor.visualLocalPosition;
                visualLocalScale = referenceAnchor.visualLocalScale;
            }
#if MRUK_AVAILABLE
            else if (mrukTrackable)
            {
                if (IsDetected && mrukTrackable.PlaneRect is Rect rect)
                {
                    visualLocalPosition = new Vector3(rect.center.x * rect.width, rect.center.y * rect.height, 0);
                    visualLocalScale = new Vector3(rect.width, rect.height, 0.01f);
                }
            }

            // Security, in case of v78 SDK x v83 OS incompatibility
            if (float.IsNaN(visualLocalPosition.x) || float.IsNaN(visualLocalPosition.y) || float.IsNaN(visualLocalPosition.z))
            {
                if (anchorVisual)
                {
                    visualLocalPosition = anchorVisual.transform.localPosition;
                    visualLocalScale = anchorVisual.transform.localScale;
                }
            }
#endif

            if (anchorVisual)
            {
                if(anchorVisual.transform.localPosition != visualLocalPosition)
                    anchorVisual.transform.localPosition = visualLocalPosition;
                if(anchorVisual.transform.localScale != visualLocalScale)
                    anchorVisual.transform.localScale = visualLocalScale;
            }

            foreach (GameObject objectToDisplay in objectsToDisplayWhenDetected)
            {
                objectToDisplay.SetActive(IsDetected);
            }
        }

        public void SetAnchorId(string anchorId)
        {
            this.anchorId = anchorId;
            if (idText)
            {
                idText.text = "Tag ID : " + anchorId;
            }
        }

        public void SetDetailText(string text)
        {
            if(detailText != null) detailText.text = text;
        }

        public void SetAnchorProgress(float progress)
        {
            if (slider != null && slidersMustBeDisplayed)
            {
                if (slider.isActiveAndEnabled == false)
                {
                    slider.enabled = true;
                    slider.gameObject.SetActive(true);
                }
                slider.value = Mathf.Clamp01(progress);
            }
        }

        public void SetAnchorSecondaryProgress(float progress)
        {
            if (secondarySlider != null && slidersMustBeDisplayed)
            {
                if (secondarySlider.isActiveAndEnabled == false)
                {
                    secondarySlider.enabled = true;
                    secondarySlider.gameObject.SetActive(true);
                }
                secondarySlider.value = Mathf.Clamp01(progress);
            }
        }

        public void ResetDetailTextColor()
        {
            if(detailText != null)
            {
                detailText.color = defaultDetailTextColor;
            }
        }

        public void ActivateKnownTagDisplayMode(bool activate)
        {
            slidersMustBeDisplayed = activate == false;
            slider.gameObject?.SetActive(slidersMustBeDisplayed);
            panelCanvas?.SetActive(activate == false);
            anchorVisualForKnownTagDisplayMode?.SetActive(activate);
        }

    }
}
