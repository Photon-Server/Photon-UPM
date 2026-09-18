using Fusion.XR.Shared.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Fusion.Addons.WatchMenu
{
    /// <summary>
    /// This class listens to the Unity Button component’s `OnButtonClick` event and handles visual, audio and haptic feedback.
    /// Create your own subclass and override `OnButtonClick()` to define custom behavior when the user presses the button.
    /// </summary>

    public class RadialMenuButtonAction : MonoBehaviour
    {

        [SerializeField] protected Button button;
        public Image image;
        public TMPro.TMP_Text text;
        public Color activeColor = Color.white;
        public Color inactiveColor = Color.blue;

        [Header("Audio Feedback")]
        public bool playSoundWhenTouched = true;
        [SerializeField] IFeedbackHandler feedback;
        [SerializeField] string audioType;

        // shouldBeDisplayed is used to set if the button should be displayed on not
        public bool shouldBeDisplayed = true;

        // isActive is used to set button status (on/off)
        public bool isActive = false;

        [Header("Named action")]
        [Tooltip("If a NamedActionManager is present, the action associated with this name will be triggered")]
        public string associatedNamedAction = "";

        protected virtual void Awake()
        {

            if (button == null)
            {
                button = GetComponent<Button>();
            }
            if (text == null)
            {
                text = GetComponent<TMPro.TMP_Text>();
            }

            if (button == null)
            {
                Debug.LogError("button not found");
            }

            if (feedback == null)
            {
                feedback = GetComponentInParent<IFeedbackHandler>();
            }

            if (feedback == null)
            {
                Debug.LogError("feedback not found");
            }

            if (image == null)
            {
                image = GetComponentInChildren<Image>();
            }
           
        }

        private void OnEnable()
        {
            if(button != null)
                button.onClick.AddListener(OnButtonClick);

            UpdateButtonColor();
        }

        private void OnDisable()
        {
            if (button != null)
                button.onClick.RemoveListener(OnButtonClick);
        }

        protected virtual void OnButtonClick()
        {
            Debug.Log("Press bouton " + this.gameObject.name);
            isActive = !isActive;
            UpdateButtonColor();
            PlayAudioFeedback();
            if(string.IsNullOrEmpty(associatedNamedAction) == false && NamedActionManager.SharedInstance != null)
            {
                NamedActionManager.Invoke(associatedNamedAction);
            }
        }

        protected void UpdateButtonColor()
        {
            image.color = isActive ? activeColor : inactiveColor;
        }

        protected void PlayAudioFeedback()
        {
            if (playSoundWhenTouched && feedback != null && feedback.IsAudioFeedbackIsPlaying() == false)
            {
                feedback.PlayAudioFeedback(audioType);
            }
        }

        private void Update()
        {
            if (button && button.enabled != shouldBeDisplayed)
            {
                button.enabled = shouldBeDisplayed;
                image.gameObject.SetActive(shouldBeDisplayed);
            }
        }

    }
}
