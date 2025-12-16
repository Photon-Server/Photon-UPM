#if UNITY_EDITOR
// Comment before release
//#define FUSION_ADDONS_DEVELOPMENT_MODE

#if XRSHARED_PACKAGE_AVAILABLE
#define DISPLAY_XRADDONS_AUTOINSTALLER
#else
#if FUSION_ADDONS_DEVELOPMENT_MODE
// Required for the Photon team while developing the installer. remove it to hide the menu entry
#define DISPLAY_XRADDONS_AUTOINSTALLER
// Prevent erroneous installation while developping the tool 
#define DISABLE_XRADDONS_INSTALLATION
#endif
#endif


using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

#if DISPLAY_XRADDONS_AUTOINSTALLER
namespace Fusion.Addons.Automatization
{
    static class XRAddonsSettings
    {
        const string XRAddonsSettingsPath = "Project/Photon/Install XRAddons";
        [MenuItem("Tools/Fusion/XRAddons/Configure add-ons", priority = 5_000)]
        static void OpenInstallXRAddonsSettings()
        {
            SettingsService.OpenProjectSettings(XRAddonsSettingsPath);
        }

        [SettingsProvider]
        public static SettingsProvider CreateInstallXRAddonsProvider()
        {
            return new SettingsProvider(XRAddonsSettingsPath, SettingsScope.Project)
            {
                label = "Install XRAddons",
                activateHandler = (_, root) =>
                {
                    VisualElement container = new VisualElement();
                    container.style.flexDirection = FlexDirection.Column;

                    XRAddonsDependencyManager.CleanupRequests();

                    bool installAllowed = true;
                    if (XRAddonsDependencyManager.RequestHandlers.Count > 0)
                    {
                        // Should disable UI
                        container.Add(new Label("Install in progress, please wait ..."));
                        installAllowed = false;
                    }
#if DISABLE_XRADDONS_INSTALLATION
                    installAllowed = false;
#endif

                    foreach (var addon in XRAddonsDependencyManager.AddonsDependencies)
                    {
                        var installAddonButton = new Button(() => {
                        });
                        installAddonButton.text = $"Install {addon.addonName}";
                        installAddonButton.enabledSelf = installAllowed;
                        container.Add(installAddonButton);
                        // TODO Cache presence result to avoid doing it too often
                        ConfigureAddButton(installAddonButton, addon);
                    }

                    container.Add(new Label("-------"));
                    var sampleGroupButton = new Button(() => {
                        DependenciesDescription.GenerateDependenciesGroup("Meta drawing with MXPen", new List<string> { "MetaCoreIntegration", "MXInkIntegration" });
                    });
                    sampleGroupButton.text = $"Group install: Meta drawing with MXPen";
                    sampleGroupButton.enabledSelf = installAllowed;
                    container.Add(sampleGroupButton); 
                    //AddBackground(sampleGroupButton, "https://dev-doc.photonengine.com/docs/img/fusion/v2/samples/industries/industries-addons/core/XRSharedCore-GeneralArchitectureRig.jpg", 250);

                    root.Add(container);
                },
                keywords = new System.Collections.Generic.HashSet<string>(new[] { "Fusion", "XR" })
            };
        }

        static async void AddBackground(VisualElement element, string url, float height)
        {
            string cachePath = Application.temporaryCachePath + "/" + Path.GetFileName(url);
            bool usingCache = false;
            if (File.Exists(cachePath))
            {
                usingCache = true;
                url = "file://" + cachePath;
                Debug.LogError("Using cache " + url);

            }
            else
            {
                Debug.LogError("No cache at " + cachePath);
            }
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
            {

                bool detached = false;
                element.RegisterCallback<DetachFromPanelEvent>((e) =>
                {
                    detached = true;
                });
                await uwr.SendWebRequest();

                while (uwr.isDone == false)
                {
                    Debug.LogError("Wait for image to download");
                    await Task.Delay(1000);
                }

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.Log(uwr.error);
                }
                else
                {
                    if (detached)
                    {
                        Debug.LogError("Detached early: not aloccating image");
                        return;
                    }
                    Debug.LogError("!!! Need to unalloc when not needed: Texture released required");
                    // Get downloaded asset bundle
                    var texture = DownloadHandlerTexture.GetContent(uwr);
                    element.style.backgroundImage = texture;
                    element.style.height = height;
                    //element.style.backgroundSize = new StyleBackgroundSize()
                    element.RegisterCallback<DetachFromPanelEvent>((e) =>
                    {
                        GameObject.DestroyImmediate(texture);
                        Debug.LogError("Texture released");
                    });
                    if (usingCache == false)
                    {
                        var fileContent = uwr.downloadHandler.data;
                        Debug.LogError("Saved to " + cachePath);
                        System.IO.File.WriteAllBytes(cachePath, fileContent);
                    }
                }
            }
        }

        static async void ConfigureAddButton(Button installAddonButton, XRAddonsDependencyManager.XRAddonsDependency addon)
        {
            installAddonButton.text = $"Verifying {addon.addonName} presence ...";
            var packageInfo = await XRAddonsDependencyManager.IsDependencyPresent(addon.addonName);

            if (packageInfo != null)
            {
                installAddonButton.text = $"{addon.addonName} installed: open in package manager to update or remove";
                installAddonButton.clickable = new Clickable(() => {
                    UnityEditor.PackageManager.UI.Window.Open(packageInfo.name);
                });
            }
            else
            {
                installAddonButton.text = "Install " + addon.addonName;
                installAddonButton.clickable = new Clickable(() => {
                    // Notify of change to reload the UI (to disable install buttons while downloading)
                    InstallDependencyIfNotPresent(addon);
                });
            }
        }

        static async void InstallDependencyIfNotPresent(XRAddonsDependencyManager.XRAddonsDependency addon)
        {
            DoInstallDependencyIfNotPresent(addon);
            // Wait a bit for the install to be launched before refreshing the settings screen
            await Task.Delay(500);
            UnityEditor.SettingsService.NotifySettingsProviderChanged();
        }

        static async void DoInstallDependencyIfNotPresent(XRAddonsDependencyManager.XRAddonsDependency addon)
        {
            await XRAddonsDependencyManager.InstallDependencyIfNotPresentAsync(addon.addonName, updateIfPresent: true);

            XRAddonsDependencyManager.CleanupRequests();
            bool installInprogress = false;
            int watchdog = 50;
            while (XRAddonsDependencyManager.RequestHandlers.Count > 0 && watchdog > 0)
            {
                installInprogress = true;
                var log = "Install still in progress .. \n";
                foreach (var request in XRAddonsDependencyManager.RequestHandlers)
                {
                    log += $" - {request.Request.Status} {request.Request.Error?.errorCode} {request.Request.Error?.message}  \n";

                }
                Debug.LogError(log);
                await Task.Delay(2000);
                XRAddonsDependencyManager.CleanupRequests();
                watchdog--;
            }
            UnityEditor.SettingsService.NotifySettingsProviderChanged();
            if (installInprogress)
                Debug.LogError("Install finished !");
        }
    }


}
#endif
#endif
