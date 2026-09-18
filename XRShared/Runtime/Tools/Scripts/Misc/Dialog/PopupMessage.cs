using UnityEngine;
using UnityEngine.UI;

namespace Fusion.XRShared.Tools
{
    public class PopupMessage : MonoBehaviour
    {
        public TMPro.TMP_Text text;
        public Slider slider;

        private void Awake()
        {
            if (slider == null)
            {
                slider = GetComponentInChildren<Slider>();
            }
            SetProgress(0);
        }

        public void SetProgress(float progress)
        {
            if (slider != null)
            {
                if (progress > 0)
                {
                    if (slider.gameObject.activeSelf == false)
                    {
                        slider.gameObject.SetActive(true);
                    }
                    slider.value = progress;
                }
                if (progress <= 0 && slider.gameObject.activeSelf)
                {
                    slider.gameObject.SetActive(false);
                }
            }
        }
    }
}
