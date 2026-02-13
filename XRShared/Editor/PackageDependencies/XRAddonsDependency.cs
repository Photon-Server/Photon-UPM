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
        public struct PackageInstallInfo
        {
            public string packageName;
            public string downloadUrl;
        }

        [System.Serializable]
        public struct XRAddonsDependency
        {
            public string addonName;
            public PackageInstallInfo packageInstallInfo;
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

        public static List<IRequestHandler> RequestHandlers = new List<IRequestHandler>();

        public static void CheckDependencies()
        {
            foreach (var addon in AddonsDependencies)
            {
                if (string.IsNullOrEmpty(addon.ConsolidatedPackageName) == false)
                {
                    //Debug.Log($"Checking {addon.ConsolidatedPackageName} ...");
                    new PackagePresenceCheck(addon.ConsolidatedPackageName, (packageInfo) => {
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
            new PackagePresenceCheck(requiredPackageName, (requiredAddonPackageInfo) => {
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
                await InstallPackageIfNotPresent(requiredAddon.ConsolidatedPackageName, requiredAddon.ConsolidatedDownloadUrl, updateIfPresent);
            }
            else
            {
                // Probably a regular package by name
                await InstallPackageIfNotPresent(dependency, dependency, updateIfPresent);
            }
        }

        public static async Task InstallPackageIfNotPresent(string requiredPackageName, string requiredPackageInstallIdentifier, bool updateIfPresent = false)
        {
            // TODO Add a package presence check by url (for git-url dependencies)

            bool presenceCheckRunning = true;
            bool installRequestRunning = false;
            PackageInstallRequest installRequest = null;
            new PackagePresenceCheck(requiredPackageName, (requiredAddonPackageInfo) =>
            {
                if (requiredAddonPackageInfo == null || updateIfPresent)
                {
                    if (requiredAddonPackageInfo == null)
                    {
                        Debug.LogWarning($"Missing {requiredPackageName}: installing it ({requiredPackageInstallIdentifier})");
                    }
                    else
                    {
                        Debug.LogWarning($"Updating {requiredPackageName} ({requiredPackageInstallIdentifier})");
                    }

                    installRequestRunning = true;
                    installRequest = new PackageInstallRequest(requiredPackageInstallIdentifier, (package) => {
                        if (package != null)
                        {
                            if (requiredAddonPackageInfo == null)
                            {
                                Debug.Log("Installed " + requiredPackageInstallIdentifier);
                            }
                            else
                            {
                                Debug.Log("Updated " + requiredPackageInstallIdentifier);
                            }
                        }
                        else
                        {
                            Debug.LogError("Failed to install " + requiredPackageInstallIdentifier);

                        }
                        installRequestRunning = false;
                    });
                    if (installRequest.Request.IsCompleted == false)
                    {
                        RequestHandlers.Add(installRequest);
                    }
                    presenceCheckRunning = false;
                }
                else
                {
                    presenceCheckRunning = false;
                }
            });

            int watchDog = 2400;
            while (watchDog > 0 && (presenceCheckRunning || installRequestRunning))
            {
                if (installRequestRunning) Debug.Log($"Waiting for install finish ...");
                //if (installRequestRunning == false) Debug.Log($"Waiting for presence check to finish ... ");
                await Task.Delay(50);
                watchDog--;
            }
        }

        public static void CleanupRequests()
        {
            int i = RequestHandlers.Count - 1;
            while (i >= 0)
            {
                if (RequestHandlers[i].Request.IsCompleted || RequestHandlers[i].Request.Status != StatusCode.InProgress)
                {
                    RequestHandlers.RemoveAt(i);
                }
                i--;
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

        public interface IRequestHandler
        {
            UnityEditor.PackageManager.Requests.Request Request { get; }
        }

        public static class PackageListCache
        {
            static PackageCollection CachedCollection = null;
            static double LastRequestTime = -1;
            static UnityEditor.PackageManager.StatusCode LastStatusCode;
            static List<IRequester> Requesters = new List<IRequester>();
            static bool IsRequesting = false;
            public static UnityEditor.PackageManager.Requests.ListRequest CurrentRequest;

            public interface IRequester
            {
                void OnListRequestComplete(StatusCode lastStatusCode, PackageCollection cachedCollection, double lastRequestTime);
            }

            public static bool IsLastRequestValid => LastRequestTime != -1 && (EditorApplication.timeSinceStartup - LastRequestTime) < 10;

            public static void Request(IRequester requester)
            {
                if (IsLastRequestValid)
                {
                    // Debug.LogError("Last request valid");
                    requester.OnListRequestComplete(LastStatusCode, CachedCollection, LastRequestTime);
                }
                else
                {
                    Requesters.Add(requester);
                    if (IsRequesting == false)
                    {
                        // Debug.LogError("Start request");
                        IsRequesting = true;
                        CurrentRequest = Client.List(offlineMode: true, includeIndirectDependencies: true);
                        EditorApplication.update += Progress;
                    }
                    else
                    {
                        // Debug.LogError("Waiting for current request result");

                    }
                }
            }

            static void Progress()
            {
                if (CurrentRequest.IsCompleted)
                {
                    IsRequesting = false;
                    LastRequestTime = EditorApplication.timeSinceStartup;
                    CachedCollection = CurrentRequest.Result;
                    LastStatusCode = CurrentRequest.Status;
                    CurrentRequest = null;
                    foreach (var requester in Requesters)
                    {
                        requester.OnListRequestComplete(LastStatusCode, CachedCollection, LastRequestTime);
                    }
                    EditorApplication.update -= Progress;
                }
            }

        }

        // Alternative (simplified, with cache, version) of Fusion.XRShared.Tools.PackagePresenceCheck, to allow direct usage of the file as a standalone script
        public class PackagePresenceCheck : IRequestHandler, PackageListCache.IRequester
        {
            public Request Request => PackageListCache.CurrentRequest;

            string[] packageNames = null;
            public delegate void ResultDelegate(UnityEditor.PackageManager.PackageInfo packageInfo);
            ResultDelegate resultCallback;

            Dictionary<string, UnityEditor.PackageManager.PackageInfo> results = new Dictionary<string, UnityEditor.PackageManager.PackageInfo>();

            public PackagePresenceCheck(string packageName, ResultDelegate resultCallback)
            {
                this.packageNames = new string[] { packageName };
                this.resultCallback = resultCallback;
                PackageListCache.Request(this);
            }

            public void OnListRequestComplete(StatusCode lastStatusCode, PackageCollection cachedCollection, double lastRequestTime)
            {
                bool resultCallbackReturned = false;
                results = new Dictionary<string, UnityEditor.PackageManager.PackageInfo>();
                if (lastStatusCode == StatusCode.Success)
                {
                    foreach (var info in cachedCollection)
                    {
                        foreach (var checkedPackageName in packageNames)
                        {
                            if (info.name == checkedPackageName)
                            {
                                results[checkedPackageName] = info;
                                if (resultCallback != null)
                                {
                                    resultCallbackReturned = true;
                                    resultCallback(info);
                                }
                                break;
                            }
                        }
                    }
                }
                if (resultCallback != null && resultCallbackReturned == false)
                {
                    resultCallback(null);
                }
            }
        }

        public class PackageInstallRequest : IRequestHandler
        {
            public Request Request => request;
            string packageName = null;
            UnityEditor.PackageManager.Requests.AddRequest request;
            public delegate void ResultDelegate(UnityEditor.PackageManager.PackageInfo packageInfo);
            ResultDelegate resultCallback;

            int progressId;
            float estimatedTime = 40;
            float startTime = 0;
            public PackageInstallRequest(string packageName, ResultDelegate resultCallback, bool useOfflineMode = true, float estimatedTime = 40)
            {
                startTime = Time.time;
                this.estimatedTime = estimatedTime;
                progressId = UnityEditor.Progress.Start("Running one task");
                this.packageName = packageName;
                this.resultCallback = resultCallback;
                request = Client.Add(packageName);
                EditorApplication.update += Progress;
            }

            void Progress()
            {
                UnityEditor.Progress.Report(progressId, (Time.time - startTime) / estimatedTime, $"Installing {packageName} ...");
                if (request.IsCompleted)
                {
                    if (request.Status == StatusCode.Success)
                    {
                        var package = request.Result;
                        if (resultCallback != null)
                        {
                            resultCallback(package);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[PackageInstallRequest] Install {packageName} => {request.Status}: ({request.Error?.errorCode}) {request.Error?.message}");
                        if (resultCallback != null)
                        {
                            resultCallback(null);
                        }
                    }

                    EditorApplication.update -= Progress;
                    UnityEditor.Progress.Remove(progressId);
                }
            }
        }
    }
}
#endif