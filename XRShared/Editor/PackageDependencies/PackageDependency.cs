#if UNITY_EDITOR
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Fusion.Addons.Automatization
{
    public static class PackageDependencyManager
    {
        [System.Serializable]
        public struct PackageInstallInfo
        {
            public string packageName;
            public string downloadUrl;
        }

        public static List<IRequestHandler> RequestHandlers = new List<IRequestHandler>();

        public static async Task InstallPackageIfNotPresent(string requiredPackageName, bool updateIfPresent = false)
        {
            await InstallPackageIfNotPresent(requiredPackageName, requiredPackageName, updateIfPresent);
        }

        public static async Task InstallPackageIfNotPresent(string requiredPackageName, string requiredPackageInstallIdentifier, bool updateIfPresent = false)
        {
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

            bool installWaitLogged = false;

            int watchDog = 2400;
            while (watchDog > 0 && (presenceCheckRunning || installRequestRunning))
            {
                if (installRequestRunning && installWaitLogged == false)
                {
                    installWaitLogged = true;
                    Debug.Log($"Installing {requiredPackageName}. Please wait for the install to finish ...");
                }
                //if (installRequestRunning == false) Debug.Log($"Waiting for presence check to finish ... ");
                await Task.Delay(50);
                watchDog--;
            }
        }

        public static void RemovePackageIfPresent(string requiredPackageName)
        {
            new PackagePresenceCheck(requiredPackageName, (requiredAddonPackageInfo) =>
            {
                if (requiredAddonPackageInfo != null)
                {
                    Debug.LogWarning($"{requiredPackageName} is present : will re removed");
                    Debug.Log($"Removing {requiredPackageName} (a Unity reboot might be required) ...");
                    Client.Remove(requiredPackageName);
                }
            });
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