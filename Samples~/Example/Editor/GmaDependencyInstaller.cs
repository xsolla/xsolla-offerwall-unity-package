using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

/// <summary>
/// Offers to add the Google Mobile Ads Unity package — and a demo AdMob App ID — to the host
/// project, so the sample's consent-prompt demo (<c>UmpConsentManager</c>) has Google's User
/// Messaging Platform (UMP) available to compile and run.
///
/// This lives in the sample only — the Xsolla Offerwall SDK package itself has no dependency
/// on Google Mobile Ads / UMP, and this installer never touches a project that hasn't imported
/// this sample (the script itself only exists on disk once the sample has been imported).
///
/// Flow:
///   1. On the first domain reload after the sample is imported (or any later Editor session
///      where the package is still missing), a dialog offers to install the package.
///   2. Choosing "Install" adds the dependency and the required OpenUPM registry scope to
///      Packages/manifest.json — parsed and edited with Newtonsoft.Json rather than splicing
///      text, so it can't produce invalid JSON — then triggers a resolve.
///   3. Choosing "Not Now" is remembered for this project via EditorUserSettings (project-local,
///      unlike EditorPrefs) so the dialog does not reappear on every Editor restart. The
///      dependency can still be installed later from
///      Window > Xsolla Offerwall > Sample > Install Google Mobile Ads Dependency.
///   4. Once the package is present, a second dialog checks the Android/iOS App ID fields in
///      <see cref="SettingsAssetPath"/> (creating the asset via the plugin's own code path if it
///      doesn't exist yet) and offers to fill in whichever ones are empty with Google's public
///      test App IDs — the same ones used in Google's own "HelloWorld" sample for this package —
///      purely so the demo runs without requiring a real AdMob account. Any field that already
///      holds a value is left untouched.
/// </summary>
[InitializeOnLoad]
public static class GmaDependencyInstaller
{
    private const string PackageId      = "com.google.ads.mobile";
    private const string PackageVersion = "11.3.0";
    private const string OpenUpmUrl     = "https://package.openupm.com";
    private const string OpenUpmName    = "package.openupm.com";
    private const string OpenUpmScope   = "com.google";

    private const string MenuPath = "Window/Xsolla Offerwall/Sample/Install Google Mobile Ads Dependency";

    // Project-local (stored in UserSettings/, not source control) — distinct from EditorPrefs,
    // which is shared across every project on the machine and would leak a decline here into
    // unrelated projects.
    private const string DeclinedConfigKey = "Xsolla.Offerwall.Sample.GmaPromptDeclined";

    // Session-only guard so the auto-prompt check runs once per Editor launch, not on every
    // domain reload (e.g. after each script compile).
    private const string SessionKeyChecked = "Xsolla.Offerwall.Sample.GmaChecked";

    // ── Demo AdMob App ID ──────────────────────────────────────────────────────────
    // Google's own public test App IDs (see the "HelloWorld" sample shipped inside the
    // com.google.ads.mobile package) — not tied to any AdMob account, safe to ship as a default.
    private const string DemoAndroidAppId = "ca-app-pub-3940256099942544~3347511713";
    private const string DemoIosAppId     = "ca-app-pub-3940256099942544~1458002511";

    private const string SettingsAssetPath      = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";
    private const string SettingsResourceName   = "GoogleMobileAdsSettings"; // Resources.Load path (no extension, relative to any Resources/ folder)
    private const string SettingsTypeQualifiedName = "GoogleMobileAds.Editor.GoogleMobileAdsSettings, GoogleMobileAds.Editor";

    private const string AppIdMenuPath        = "Window/Xsolla Offerwall/Sample/Configure Demo AdMob App ID";
    private const string DeclinedAppIdConfigKey = "Xsolla.Offerwall.Sample.GmaAppIdPromptDeclined";

    static GmaDependencyInstaller()
    {
        if (!SessionState.GetBool(SessionKeyChecked, false))
        {
            SessionState.SetBool(SessionKeyChecked, true);
            // Defer until Unity is idle, avoiding interference with an in-progress package resolve.
            EditorApplication.delayCall += MaybePromptOnLoad;
        }

        // Not session-gated like the check above: this needs to re-check on every domain reload
        // until the package finishes installing (which itself triggers one or more reloads within
        // the same Editor session), otherwise a user who accepts the package install would have to
        // restart the Editor before ever seeing this second prompt.
        EditorApplication.delayCall += MaybePromptAppIdOnLoad;
    }

    private static void MaybePromptOnLoad()
    {
        if (IsPackageDeclared())
            return;

        if (EditorUserSettings.GetConfigValue(DeclinedConfigKey) == "1")
            return;

        PromptInstall();
    }

    [MenuItem(MenuPath)]
    private static void InstallFromMenu()
    {
        if (IsPackageDeclared())
        {
            EditorUtility.DisplayDialog(
                "Xsolla Offerwall Sample",
                $"{PackageId} is already declared in Packages/manifest.json.",
                "OK");
            return;
        }

        PromptInstall();
    }

    private static void PromptInstall()
    {
        bool install = EditorUtility.DisplayDialog(
            "Xsolla Offerwall Sample — Consent Demo",
            "The sample's consent-prompt demo uses Google's User Messaging Platform (UMP), " +
            $"which ships inside the Google Mobile Ads Unity package ({PackageId}).\n\n" +
            "This package isn't installed, so the demo is currently compiled out. Add it now?",
            "Install",
            "Not Now");

        if (install)
            InstallDependency();
        else
            EditorUserSettings.SetConfigValue(DeclinedConfigKey, "1");
    }

    private static void InstallDependency()
    {
        string manifestPath = GetManifestPath();
        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning("[GmaDependencyInstaller] Could not find Packages/manifest.json — " +
                              $"install {PackageId} manually via Package Manager instead.");
            return;
        }

        JObject manifest = ParseManifest(manifestPath);
        if (manifest == null)
            return;

        bool changed = false;

        JObject dependencies = GetOrCreateObject(manifest, "dependencies");
        if (dependencies[PackageId] == null)
        {
            dependencies[PackageId] = PackageVersion;
            changed = true;
        }

        if (EnsureOpenUpmScope(manifest))
            changed = true;

        if (!changed)
            return;

        File.WriteAllText(manifestPath, SerializeManifest(manifest));

        // Clear any earlier decline now that the dependency is actually present.
        EditorUserSettings.SetConfigValue(DeclinedConfigKey, "0");

        Debug.Log($"[GmaDependencyInstaller] Added {PackageId}@{PackageVersion} to manifest.json — resolving packages…");
        Client.Resolve();
    }

    private static bool IsPackageDeclared()
    {
        string manifestPath = GetManifestPath();
        if (!File.Exists(manifestPath))
            return false;

        JObject manifest = ParseManifest(manifestPath);
        return manifest?["dependencies"]?[PackageId] != null;
    }

    /// <summary>
    /// Ensures the OpenUPM scoped registry exists in <paramref name="manifest"/> and has
    /// <see cref="OpenUpmScope"/> listed, creating the registry entry (and/or the
    /// "scopedRegistries" array) if needed. Returns true if the manifest was modified.
    /// </summary>
    private static bool EnsureOpenUpmScope(JObject manifest)
    {
        JArray registries = GetOrCreateArray(manifest, "scopedRegistries");

        JObject openUpm = registries.OfType<JObject>()
            .FirstOrDefault(registry => (string)registry["url"] == OpenUpmUrl);

        if (openUpm == null)
        {
            openUpm = new JObject
            {
                ["name"] = OpenUpmName,
                ["url"] = OpenUpmUrl,
                ["scopes"] = new JArray(OpenUpmScope),
            };
            registries.Add(openUpm);
            return true;
        }

        JArray scopes = GetOrCreateArray(openUpm, "scopes");
        if (scopes.Any(scope => (string)scope == OpenUpmScope))
            return false;

        scopes.Add(OpenUpmScope);
        return true;
    }

    private static string GetManifestPath()
        => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));

    private static JObject ParseManifest(string path)
    {
        try
        {
            return JObject.Parse(File.ReadAllText(path));
        }
        catch (JsonException e)
        {
            Debug.LogError($"[GmaDependencyInstaller] Could not parse Packages/manifest.json ({e.Message}).");
            return null;
        }
    }

    /// <summary>
    /// Serializes <paramref name="manifest"/> with 2-space indentation and LF line endings,
    /// regardless of host OS — matching the SDK's own EDM4U installer so a mixed-OS team
    /// doesn't see the whole file rewritten to CRLF for a one-line change.
    /// </summary>
    private static string SerializeManifest(JObject manifest)
    {
        var stringWriter = new StringWriter { NewLine = "\n" };
        using (var jsonWriter = new JsonTextWriter(stringWriter)
        {
            Formatting = Formatting.Indented,
            IndentChar = ' ',
            Indentation = 2,
        })
        {
            manifest.WriteTo(jsonWriter);
        }
        return stringWriter.ToString() + "\n";
    }

    private static JObject GetOrCreateObject(JObject parent, string key)
    {
        if (parent[key] is JObject existing)
            return existing;

        var created = new JObject();
        parent[key] = created;
        return created;
    }

    private static JArray GetOrCreateArray(JObject parent, string key)
    {
        if (parent[key] is JArray existing)
            return existing;

        var created = new JArray();
        parent[key] = created;
        return created;
    }

    // ── Demo AdMob App ID ──────────────────────────────────────────────────────────

    private static void MaybePromptAppIdOnLoad()
    {
        if (!IsPackageDeclared())
            return; // nothing to configure until the package itself is installed

        if (EditorUserSettings.GetConfigValue(DeclinedAppIdConfigKey) == "1")
            return;

        Type settingsType = Type.GetType(SettingsTypeQualifiedName);
        if (settingsType == null)
            return; // GoogleMobileAds.Editor hasn't finished compiling yet — retried next reload

        if (!TryGetEmptyAppIdFields(settingsType, out bool androidEmpty, out bool iosEmpty))
            return; // both fields already have a value — nothing to do, and nothing to touch

        PromptConfigureAppId(settingsType, androidEmpty, iosEmpty);
    }

    [MenuItem(AppIdMenuPath)]
    private static void ConfigureAppIdFromMenu()
    {
        Type settingsType = Type.GetType(SettingsTypeQualifiedName);
        if (settingsType == null)
        {
            EditorUtility.DisplayDialog(
                "Xsolla Offerwall Sample",
                $"{PackageId} isn't installed yet. Install it first (Window > Xsolla Offerwall > " +
                "Sample > Install Google Mobile Ads Dependency), then try this again.",
                "OK");
            return;
        }

        if (!TryGetEmptyAppIdFields(settingsType, out bool androidEmpty, out bool iosEmpty))
        {
            EditorUtility.DisplayDialog(
                "Xsolla Offerwall Sample",
                $"Both App ID fields in {SettingsAssetPath} are already set — nothing to fill in. " +
                "Edit them directly via Assets > Google Mobile Ads > Settings, or clear a field to " +
                "be offered the demo App ID for it again.",
                "OK");
            return;
        }

        PromptConfigureAppId(settingsType, androidEmpty, iosEmpty);
    }

    private static void PromptConfigureAppId(Type settingsType, bool fillAndroid, bool fillIos)
    {
        string fieldDescription = fillAndroid && fillIos
            ? "AdMob App ID (Android and iOS)"
            : fillAndroid
                ? "Android AdMob App ID"
                : "iOS AdMob App ID";

        bool configure = EditorUtility.DisplayDialog(
            "Xsolla Offerwall Sample — AdMob App ID",
            $"Running the consent demo also needs a {fieldDescription} in {SettingsAssetPath}.\n\n" +
            "Use Google's public test App ID(s) so the demo runs without an AdMob account? " +
            "Any field that already has a value is left untouched, and you can swap in your own " +
            "App ID later via Assets > Google Mobile Ads > Settings.",
            "Use Demo ID(s)",
            "Not Now");

        if (configure)
            ConfigureDemoAppId(settingsType, fillAndroid, fillIos);
        else
            EditorUserSettings.SetConfigValue(DeclinedAppIdConfigKey, "1");
    }

    /// <summary>
    /// Checks the current values of the Google Mobile Ads settings asset's App ID fields without
    /// creating or modifying anything — via the public, non-generic <see cref="Resources.Load"/>
    /// overload, which returns null when the asset doesn't exist yet (rather than creating it, the
    /// way the package's own internal <c>LoadInstance()</c> would).
    /// Returns false (with both out params false) when the asset doesn't need any changes, i.e.
    /// both fields already hold a value.
    /// </summary>
    private static bool TryGetEmptyAppIdFields(Type settingsType, out bool androidEmpty, out bool iosEmpty)
    {
        var existing = Resources.Load(SettingsResourceName, settingsType) as ScriptableObject;
        if (existing == null)
        {
            androidEmpty = true;
            iosEmpty = true;
            return true;
        }

        androidEmpty = string.IsNullOrEmpty(GetPublicProperty(existing, "GoogleMobileAdsAndroidAppId"));
        iosEmpty     = string.IsNullOrEmpty(GetPublicProperty(existing, "GoogleMobileAdsIOSAppId"));
        return androidEmpty || iosEmpty;
    }

    /// <summary>
    /// Populates whichever of the Google Mobile Ads settings asset's App ID fields are empty with
    /// Google's public test App IDs, via reflection against the package's internal
    /// <c>GoogleMobileAdsSettings</c> type — its own <c>LoadInstance()</c> method is used to
    /// create/load the asset (the same code path the plugin itself uses), rather than hand-rolling
    /// the asset's YAML, so this keeps working even if the plugin ever changes that type's internal
    /// layout. Fields that already hold a value are left untouched.
    /// </summary>
    private static void ConfigureDemoAppId(Type settingsType, bool fillAndroid, bool fillIos)
    {
        MethodInfo loadInstance = settingsType.GetMethod(
            "LoadInstance", BindingFlags.NonPublic | BindingFlags.Static);
        if (loadInstance == null)
        {
            Debug.LogWarning("[GmaDependencyInstaller] Could not find GoogleMobileAdsSettings.LoadInstance() — " +
                              "set the AdMob App ID manually via Assets > Google Mobile Ads > Settings.");
            return;
        }

        var settings = loadInstance.Invoke(null, null) as ScriptableObject;
        if (settings == null)
        {
            Debug.LogWarning("[GmaDependencyInstaller] GoogleMobileAdsSettings.LoadInstance() returned null — " +
                              "set the AdMob App ID manually via Assets > Google Mobile Ads > Settings.");
            return;
        }

        if (fillAndroid)
            SetPublicProperty(settings, "GoogleMobileAdsAndroidAppId", DemoAndroidAppId);
        if (fillIos)
            SetPublicProperty(settings, "GoogleMobileAdsIOSAppId", DemoIosAppId);

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        EditorUserSettings.SetConfigValue(DeclinedAppIdConfigKey, "0");

        Debug.Log($"[GmaDependencyInstaller] Set {SettingsAssetPath} to use Google's public test " +
                   "AdMob App ID(s) for the consent demo. Replace with your own via Assets > Google Mobile Ads > Settings.");
    }

    private static string GetPublicProperty(object target, string propertyName)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(target) as string;
    }

    private static void SetPublicProperty(object target, string propertyName, string value)
    {
        PropertyInfo property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
        {
            Debug.LogWarning($"[GmaDependencyInstaller] GoogleMobileAdsSettings has no '{propertyName}' property — " +
                              "set the AdMob App ID manually via Assets > Google Mobile Ads > Settings.");
            return;
        }

        property.SetValue(target, value);
    }
}
