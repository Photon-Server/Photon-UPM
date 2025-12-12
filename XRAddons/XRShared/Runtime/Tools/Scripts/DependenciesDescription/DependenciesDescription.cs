using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Allow to specify add-ons to auto-insall (for a sample for instance)
/// Only working when used with XRShared installed as an UPM package
/// </summary>
[CreateAssetMenu(fileName = "DependenciesDescription", menuName = "Fusion Addons/DependenciesDescription")]
public class DependenciesDescription : ScriptableObject
{
    public enum InstallMode
    {
        DoNotAutoInstall,
        KeepInstalled,
        InstallAllOnce
    }

    public bool autoInstallDependencies = true;
    public InstallMode installMode = InstallMode.InstallAllOnce;

    public List<string> requiredDependencies;

    private void OnEnable()
    {
        if (autoInstallDependencies && installMode != InstallMode.DoNotAutoInstall)
        {
            InstallDependencies();
        }
    }

    [ContextMenu("InstallDepedencies")]
#if UNITY_EDITOR
    async void InstallDependencies()
    {
        // We only install dependencies packages if XRShared is installed as a package
        if (requiredDependencies != null && requiredDependencies.Count > 0 && await Fusion.Addons.Automatization.XRAddonsDependencyManager.IsDependencyPresent("XRShared") != null)
        {
            int installedPackages = 0;
            foreach (var dependency in requiredDependencies)
            {
                bool present = await Fusion.Addons.Automatization.XRAddonsDependencyManager.IsDependencyPresent(dependency) != null;
                if (present == false)
                {
                    Debug.LogError($"-> Installing {dependency}");
                    Fusion.Addons.Automatization.XRAddonsDependencyManager.InstallDependencyIfNotPresent(dependency);
                }
                else
                {
                    installedPackages++;
                }
            }
            if (installedPackages == requiredDependencies.Count && installMode == InstallMode.InstallAllOnce && autoInstallDependencies)
            {
                autoInstallDependencies = false;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
#else
    void InstallDependencies() {}
#endif

    /// <summary>
    /// Generate (and trigger install) of a group of dependencies, as a DependenciesDescription file in Assets/XR/XRAddons
    /// Usage: GenerateDependenciesGroup("GroupFileName", new List<string> { "AddonName" });
    /// </summary>
    /// <param name="name"></param>
    /// <param name="requiredDependencies"></param>
    /// <param name="logErrorIfExists"></param>
    public static void GenerateDependenciesGroup(string name, List<string> requiredDependencies, bool logErrorIfExists = false)
    {
#if UNITY_EDITOR
        if (AssetDatabase.IsValidFolder("Assets/XR") == false)
        {
            AssetDatabase.CreateFolder("Assets", "XR");
        }
        if (AssetDatabase.IsValidFolder("Assets/XR/XRAddons") == false)
        {
            AssetDatabase.CreateFolder("Assets/XR", "XRAddons");
        }
        if (AssetDatabase.IsValidFolder("Assets/XR/XRAddons"))
        {
            var assetPath = $"Assets/XR/XRAddons/{name}.asset";
            if (AssetDatabase.AssetPathExists(assetPath) == false)
            {
                DependenciesDescription dependenciesDescription = ScriptableObject.CreateInstance<DependenciesDescription>();
                dependenciesDescription.requiredDependencies = requiredDependencies;
                AssetDatabase.CreateAsset(dependenciesDescription, assetPath);
                AssetDatabase.SaveAssets();
                dependenciesDescription.InstallDependencies();
            }
            else
            {
                if (logErrorIfExists) Debug.LogError($"Unable to create dependencies group: {assetPath} already exists");
            }
        }
        else
        {
            Debug.LogError("Unable to create dependencies group");
        }
#endif
    }
}
