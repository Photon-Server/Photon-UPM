using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Fusion.XR.Shared.Tools
{
    public class Visibility : MonoBehaviour, IVisibility
    {
        public bool isVisible = true;

        public List<Renderer> renderers = new List<Renderer>();
        public List<Image> images = new List<Image>();
        public List<TextMeshProUGUI> tmpTexts = new List<TextMeshProUGUI>();

        private void Awake()
        {
            if (renderers == null || renderers.Count == 0)
            {
                renderers = new List<Renderer>(GetComponentsInChildren<Renderer>());
            }
            if (images == null || images.Count == 0)
            {
                images = new List<Image>(GetComponentsInChildren<Image>());
            }
            if (tmpTexts == null || tmpTexts.Count == 0)
            {
                tmpTexts = new List<TextMeshProUGUI>(GetComponentsInChildren<TextMeshProUGUI>());
            }
        }

        private void Start()
        {
            OnVisibilityChange();
        }

        private void OnVisibilityChange()
        {
            foreach (var renderer in renderers)
            {
                if (renderer != null && renderer.enabled != isVisible)
                {
                    renderer.enabled = isVisible;
                }
            }

            foreach (var image in images)
            {
                if (image != null && image.enabled != isVisible)
                {
                    image.enabled = isVisible;
                }
            }

            foreach (var text in tmpTexts)
            {
                if (text != null && text.enabled != isVisible)
                {
                    text.enabled = isVisible;
                }
            }
        }

        public void ChangeVisibility(bool visible)
        {
            isVisible = visible;
            OnVisibilityChange();
        }
    }
}
