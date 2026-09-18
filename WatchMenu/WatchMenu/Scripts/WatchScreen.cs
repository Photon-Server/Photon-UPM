using TMPro;
using UnityEngine;

namespace Fusion.Addons.WatchMenu
{
    public interface IWatchScreenListener
    {
        void RegisterWatchScreen(WatchScreen s);
        void UnregisterWatchScreen(WatchScreen s);
    }
    /// <summary>
    /// WatchScreen provides the function to update the text on the watch
    /// </summary>

    public class WatchScreen : MonoBehaviour
    {
        [SerializeField] TMP_Text watchText = null;

        private void Awake()
        {
            if (watchText == null)
            {
                watchText = GetComponentInChildren<TMP_Text>();
            }
        }

        private void Start()
        {
            foreach(var l in GetComponentsInParent<IWatchScreenListener>(true))
            {
                l.RegisterWatchScreen(this);
            }
        }

        private void OnDestroy()
        {
            foreach (var l in GetComponentsInParent<IWatchScreenListener>(true))
            {
                l.UnregisterWatchScreen(this);
            }
        }

        public void UpdateWatchText(string text)
        {
            if (watchText)
            {
                watchText.text = text;
            }
        }
    }
}
