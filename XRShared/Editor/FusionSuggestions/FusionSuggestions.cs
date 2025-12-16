#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using Photon.Tools;

namespace Fusion.Addons.Automatization.XRShared
{
    
    public struct InstallFusionSuggestion : ISuggestedApplicableChangeDescriptor
    {
        public string Description => "Install Fusion";
        public string Tooltip => "XRAddons requires Fusion";

        // Optional: by default, a simple "Apply" option will appear
        public string[] AlternativeApplyDescriptions => new string[] { "Download and install Fusion" };

        public bool IsAlreadyApplied => false;

        public void Apply(string selectedApplyDescription){
            Application.OpenURL("https://doc.photonengine.com/fusion/current/getting-started/sdk-download");
        }

    }

    [InitializeOnLoad]
    public class FusionSuggestions
    {
        static FusionSuggestions()
        {
            Debug.LogError("XR add-ons require Fusion SDK. Please go to https://doc.photonengine.com/fusion/current/getting-started/sdk-download to download and install it");
            PhotonSuggestedChangesManager.SuggestedChangeDescriptors.Add(new InstallFusionSuggestion());
        }
    }
}
#endif
