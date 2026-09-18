#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Fusion.Addons.Automatization
{
    [InitializeOnLoad]
    public class XRAddonsDependencyManager
    {
        static XRAddonsDependencyManager()
        {
            CheckDependencies();
        }

        [System.Serializable]
        public struct XRAddonsDependency
        {
            public string addonName;
            public PackageDependencyManager.PackageInstallInfo packageInstallInfo;
            public List<string> requiredDependencies;

            public string ConsolidatedDownloadUrl
            {
                get
                {
                    if (string.IsNullOrEmpty(packageInstallInfo.downloadUrl) && string.IsNullOrEmpty(addonName) == false)
                    {
                        return "https://github.com/Photon-Server/Photon-UPM.git?path=/" + addonName  + "#fusion/v2/fusion-xr";
                    }

                    return packageInstallInfo.downloadUrl;
                }
            }

            public string ConsolidatedPackageName
            {
                get
                {
                    if (string.IsNullOrEmpty(packageInstallInfo.packageName) && string.IsNullOrEmpty(addonName) == false)
                    {
                        return "com.photonengine.prototyping.addon." + addonName.ToLower();
                    }

                    return packageInstallInfo.packageName;
                }
            }
        }

        public static List<XRAddonsDependency> AddonsDependencies = new List<XRAddonsDependency> {
            new XRAddonsDependency { addonName = "XRShared", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "DataSyncHelpers", requiredDependencies = new List<string> { }, },
            new XRAddonsDependency { addonName = "LineDrawing", requiredDependencies = new List<string> { "DataSyncHelpers" }, },
            new XRAddonsDependency { addonName = "MetaCoreIntegration", requiredDependencies = new List<string> { }, },
            new XRAddonsDependency { addonName = "MXInkIntegration", requiredDependencies = new List<string> { "LineDrawing" }, },
            // Incoming packages (dependency analysis required)
            new XRAddonsDependency { addonName = "Anchors", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "AudioRoom", requiredDependencies = new List<string> { "DynamicAudioGroup" }, },
            new XRAddonsDependency { addonName = "BlockingContact", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "ChatBubble", requiredDependencies = new List<string> { "AudioRoom"  }, },
            new XRAddonsDependency { addonName = "ConnectionManager", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "DesktopFocus", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "Drawing", requiredDependencies = new List<string> { "InteractiveMenu", "BlockingContact"  }, },
            new XRAddonsDependency { addonName = "DynamicAudioGroup", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "ExtendedRigSelection", requiredDependencies = new List<string> { "ConnectionManager" }, },
            new XRAddonsDependency { addonName = "Feedback", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "InteractiveMenu", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "LocomotionValidation", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "Magnets", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "Physics", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "PositionDebugging", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "Reconnection", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "Screensharing", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "SocialDistancing", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "Spaces", requiredDependencies = new List<string> { "ConnectionManager" }, },
            new XRAddonsDependency { addonName = "StickyNotes", requiredDependencies = new List<string> { "TextureDrawing" }, },
            new XRAddonsDependency { addonName = "StructureCohesion", requiredDependencies = new List<string> { "Magnets" }, },
            //new XRAddonsDependency { addonName = "SubscriberRegistry", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "TextureDrawing", requiredDependencies = new List<string> { "BlockingContact", "DataSyncHelpers" }, },
            new XRAddonsDependency { addonName = "UISynchronization", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "VirtualKeyboard", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "VisionOsHelpers", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "VoiceHelpers", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "WatchMenu", requiredDependencies = new List<string> {  }, },
            new XRAddonsDependency { addonName = "XRITIntegration", requiredDependencies = new List<string> {  }, },
        };

        public static List<PackageDependencyManager.IRequestHandler> RequestHandlers => PackageDependencyManager.RequestHandlers;

        public static void CheckDependencies()
        {
            foreach (var addon in AddonsDependencies)
            {
                if (string.IsNullOrEmpty(addon.ConsolidatedPackageName) == false)
                {
                    //Debug.Log($"Checking {addon.ConsolidatedPackageName} ...");
                    new PackageDependencyManager.PackagePresenceCheck(addon.ConsolidatedPackageName, (packageInfo) => {
                        if (packageInfo != null && addon.requiredDependencies != null)
                        {
                            //Debug.Log($"Addon {addon.ConsolidatedPackageName} package is installed. Checking its dependencies");
                            foreach (var dependency in addon.requiredDependencies)
                            {
                                InstallDependencyIfNotPresent(dependency);
                            }
                        }
                    });
                }
            }
        }

        public static async Task<UnityEditor.PackageManager.PackageInfo> IsDependencyPresent(string dependency)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo = null;
            string requiredPackageName = dependency;
            if (FindAddonInfo(dependency) is XRAddonsDependency requiredAddon)
            {
                // Another XRAddon
                requiredPackageName = requiredAddon.ConsolidatedPackageName;
            }
            bool lookingForPackage = true;
            new PackageDependencyManager.PackagePresenceCheck(requiredPackageName, (requiredAddonPackageInfo) => {
                packageInfo = requiredAddonPackageInfo;
                lookingForPackage = false;
            });
            int watchDog = 200;
            while (lookingForPackage && watchDog > 0)
            {
                await Task.Delay(5);
                watchDog--;
            }
            return packageInfo;
        }

        public static async void InstallDependencyIfNotPresent(string dependency, bool updateIfPresent = false)
        {
            await InstallDependencyIfNotPresentAsync(dependency, updateIfPresent);
        }

        public static async Task InstallDependencyIfNotPresentAsync(string dependency, bool updateIfPresent = false)
        {
            if (FindAddonInfo(dependency) is XRAddonsDependency requiredAddon)
            {
                // Another XRAddon
                await PackageDependencyManager.InstallPackageIfNotPresent(requiredAddon.ConsolidatedPackageName, requiredAddon.ConsolidatedDownloadUrl, updateIfPresent);
            }
            else
            {
                // Probably a regular package by name
                await PackageDependencyManager.InstallPackageIfNotPresent(dependency, dependency, updateIfPresent);
            }
        }

        public static XRAddonsDependency? FindAddonInfo(string addonName)
        {
            foreach (var addon in AddonsDependencies)
            {
                if (addon.addonName == addonName)
                {
                    return addon;
                }
            }
            return null;
        }
    }
}
#endif