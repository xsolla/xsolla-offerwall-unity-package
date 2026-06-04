#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

/// <summary>
/// Injects NSUserTrackingUsageDescription into the Xcode Info.plist after an iOS build.
/// Required for AppTrackingTransparency — without this key the ATT prompt will not appear
/// and the app will be rejected by App Store review.
///
/// Edit <see cref="UsageDescription"/> below to match your app's privacy policy language.
/// </summary>
public static class AttPostProcessor
{
    private const string UsageDescription =
        "This identifier will be used to deliver personalized offers and measure ad performance.";

    [PostProcessBuild(101)]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
    {
        if (buildTarget != BuildTarget.iOS)
            return;

        string plistPath = Path.Combine(buildPath, "Info.plist");

        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict root = plist.root;

        const string key = "NSUserTrackingUsageDescription";

        if (!root.values.ContainsKey(key))
        {
            root.SetString(key, UsageDescription);
            plist.WriteToFile(plistPath);
            Debug.Log($"[AttPostProcessor] Added {key} to Info.plist.");
        }
        else
        {
            Debug.Log($"[AttPostProcessor] {key} already present — skipping.");
        }
    }
}
#endif
