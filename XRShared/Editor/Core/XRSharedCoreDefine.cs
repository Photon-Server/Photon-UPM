#define INCLUDE_LEGACY_DEFINE
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

[InitializeOnLoad]
public class XRSharedCoreDefine
{
    const string DEFINE = "XRSHARED_CORE_ADDON_AVAILABLE";
    const string LEGACY_DEFINE = "XRSHARED_CORE_ADDON_AVAILABLE";

    static XRSharedCoreDefine()
    {
        var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));

        if (defines.Contains(DEFINE) == false)
        {
            defines = $"{defines};{DEFINE}";
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), defines);
        }
#if INCLUDE_LEGACY_DEFINE
        if (defines.Contains(LEGACY_DEFINE) == false)
        {
            defines = $"{defines};{LEGACY_DEFINE}";
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group), defines);
        }
#endif
    }
}
#endif