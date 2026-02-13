
using UnityEngine;

namespace Fusion.Samples.IndustriesComponents
{
    [DefaultExecutionOrder(-10000)]
    public class DisableForDesktop : MonoBehaviour
    {
        [SerializeField] bool disableInEditor = false;
        [SerializeField] MonoBehaviour behaviourToDisable;

        private void OnEnable()
        {
            CheckMonoBehaviour();
        }

        private void Update()
        {
            CheckMonoBehaviour();
        }

        void CheckMonoBehaviour()
        {
#if !UNITY_ANDROID && !UNITY_EDITOR
            behaviourToDisable.enabled = false;
#endif

#if UNITY_EDITOR
            if (disableInEditor) behaviourToDisable.enabled = false;
#endif
        }
    }
}

