using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

/// <summary>
/// Keeps the IOS_SUPPORT_AVAILABLE scripting define symbol in sync with whether
/// com.unity.ads.ios-support is installed in this project.
///
/// When the package is present the symbol is added to the iOS build-target defines,
/// allowing sample code to conditionally compile ATT (App Tracking Transparency)
/// integration via #if UNITY_IOS && IOS_SUPPORT_AVAILABLE.
///
/// When the package is absent the symbol is removed so that ATT code is excluded
/// and no missing-assembly errors are produced.
/// </summary>
[InitializeOnLoad]
public static class IosPackageSymbolDefiner
{
    private const string PackageName = "com.unity.ads.ios-support";
    private const string Symbol     = "IOS_SUPPORT_AVAILABLE";

    private static ListRequest _request;

    static IosPackageSymbolDefiner()
    {
        // offlineMode: true — reads the local cache; no network call needed.
        _request = Client.List(offlineMode: true);
        EditorApplication.update += OnUpdate;
    }

    private static void OnUpdate()
    {
        if (_request == null || !_request.IsCompleted) return;
        EditorApplication.update -= OnUpdate;

        bool found = false;
        if (_request.Status == StatusCode.Success)
        {
            foreach (var pkg in _request.Result)
            {
                if (string.Equals(pkg.name, PackageName, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }
        }

        SetDefine(BuildTargetGroup.iOS, Symbol, found);
        _request = null;
    }

    private static void SetDefine(BuildTargetGroup group, string symbol, bool add)
    {
        var raw     = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        var defines = new System.Collections.Generic.List<string>(
            raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

        bool dirty = add ? !defines.Contains(symbol) : defines.Remove(symbol);
        if (!dirty) return;

        if (add) defines.Add(symbol);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", defines));
    }
}
