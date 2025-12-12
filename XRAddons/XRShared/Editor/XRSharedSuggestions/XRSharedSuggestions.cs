#if UNITY_EDITOR
using Fusion.XRShared.Tools;
using Photon.Tools;
using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using System.Threading.Tasks;
#if OPENXR_PACKAGE_AVAILABLE
using UnityEngine.XR.OpenXR;
using UnityEditor.XR.OpenXR.Features;
#if XRHANDS_AVAILABLE
using UnityEngine.XR.Hands.OpenXR;
#endif
#endif

namespace Fusion.Addons.Automatization.XRShared
{
    public struct OpenXRModulePackageSuggestion : ISuggestedApplicableChangeDescriptor
    {
        public string Description => "Add OpenXR package";
        public string Tooltip => "OpenXR package is required to add the OpenXR plug-in to the XR management plugins settings";

        // Optional: by default, a simple "Apply" option will appear
        public string[] AlternativeApplyDescriptions => new string[] { "Add OpenXR package" };

        public bool ShouldDisplay => OpenXRModulePackageSuggestion.IsRequired;

        public bool IsAlreadyApplied => IsRequired == false;

        public bool IsApplicable => true;

        public static bool IsRequired
        {
            get
            {
                try
                {
                    Assembly.Load("Unity.XR.OpenXR");
                    return false;
                }
                catch (Exception)
                {
                    return true;
                }
            }
        }

        public void Apply(string selectedApplyDescription)
        {
            XRAddonsDependencyManager.InstallDependencyIfNotPresent("com.unity.xr.openxr");
        }
    }

    public struct OpenXRModuleActivationSuggestion : ISuggestedApplicableChangeDescriptor
    {
        public string Description => "Add an XR plug-in to enable XR development";

        // Optional: by default, a simple "Apply" option will appear
        public string[] AlternativeApplyDescriptions => new string[] { "Add OpenXR" };

        public string Tooltip => IsApplicable ? "" : "OpenXR package not yet installed. Install it to apply this suggestion";

        public bool IsAlreadyApplied => IsRequired == false;

        public bool IsApplicable =>
            // Open XR package installation is required
            OpenXRModulePackageSuggestion.IsRequired == false;

        public static bool IsRequired
        {
            get
            {
                var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                XRManagerSettings xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(targetGroup)?.AssignedSettings;

                if (xrSettings == null)
                {
                    // We will create the settings during Apply, by opening the related settings
                    return true;
                }

                return xrSettings.activeLoaders.Count == 0;
            }
        }

        public async Task<XRManagerSettings> CreateSettingsForBuildTarget(BuildTargetGroup targetGroup)
        {
            XRManagerSettings xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(targetGroup)?.AssignedSettings;

            if (xrSettings == null)
            {
                SettingsService.OpenProjectSettings("Project/XR Plug-in Management");
                int watchDog = 200;
                while (watchDog > 0)
                {
                    Debug.Log("Waiting for XRSettings creation after  opening XR Plug-in Management settings ...");
                    await Task.Delay(10);
                    watchDog--;
                    xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(targetGroup)?.AssignedSettings;
                    if (xrSettings != null)
                    {
                        break;
                    }
                }
            }
            return xrSettings;
        }

        public async void Apply(string selectedApplyDescription)
        {
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            XRManagerSettings xrSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(targetGroup)?.AssignedSettings;
            bool shouldReopenSuggestionScreen = false;
            if (xrSettings == null)
            {
                xrSettings = await CreateSettingsForBuildTarget(targetGroup);
                shouldReopenSuggestionScreen = true;
            }


            if (xrSettings == null)
            {
                UnityEngine.Debug.LogError($"Unexpected: no XR settings");
                return;
            }

            var loaderId = "UnityEngine.XR.OpenXR.OpenXRLoader";
            var didAssign = XRPackageMetadataStore.AssignLoader(xrSettings, loaderId, targetGroup);

            if (didAssign == false)
            {
                UnityEngine.Debug.LogError("Unable to assign request XR loader. Please configure it manually");
            }
            else
            {
                if (shouldReopenSuggestionScreen)
                    SettingsService.OpenProjectSettings(PhotonSuggestedChangesSettings.SettingsProviderPath);
                await Task.Delay(100); // To let the option cache be refreshed
                UnityEditor.SettingsService.NotifySettingsProviderChanged();
            }
        }
    }
    public struct OpenXRHandTrackingSuggestion : ISuggestedApplicableChangeDescriptor
    {
        public string Description => "Enable OpenXR hand tracking";
        public string Tooltip => IsApplicable ? "This OpenXR feature is required to enable hand tracking" : "OpenXR package not yet installed or selected in XR Management view. Install it and select it first to apply this suggestion";

        // Optional: by default, a simple "Apply" option will appear
        public string[] AlternativeApplyDescriptions => new string[] { "Enable OpenXR hand tracking feature" };

        public bool IsAlreadyApplied => IsRequired == false;

        // Open XR package installation is required
        public bool IsApplicable =>
            // Open XR package installation and activation is required
            OpenXRModulePackageSuggestion.IsRequired == false
            // Open XR loader needs to be selected
            && OpenXRModuleActivationSuggestion.IsRequired == false;

        public static bool IsRequired
        {
            get
            {
#if OPENXR_PACKAGE_AVAILABLE && XRHANDS_AVAILABLE
                var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
                var hts = settings.GetFeatures<HandTracking>();
                foreach (var feature in hts)
                {
                    if (feature.enabled)
                    {
                        return false;
                    }
                }
                return true;
#else
                return false;
#endif

            }
        }

        public async void Apply(string selectedApplyDescription)
        {
#if OPENXR_PACKAGE_AVAILABLE && XRHANDS_AVAILABLE
            var targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;

            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var hts = settings.GetFeatures<HandTracking>();
            foreach (var feature in hts)
            {
                feature.enabled = true;
            }
            await Task.Delay(500); // To let the option cache be refreshed
            UnityEditor.SettingsService.NotifySettingsProviderChanged();
#endif
        }
    }

    [InitializeOnLoad]
    public class XRSharedSuggestions
    {
        static XRSharedSuggestions()
        {
            PhotonSuggestedChangesManager.SuggestedChangeDescriptors.Add(new OpenXRModulePackageSuggestion());
            PhotonSuggestedChangesManager.SuggestedChangeDescriptors.Add(new OpenXRModuleActivationSuggestion());
            PhotonSuggestedChangesManager.SuggestedChangeDescriptors.Add(new OpenXRHandTrackingSuggestion());
        }
    }
}
#endif
