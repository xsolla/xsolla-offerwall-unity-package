using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

namespace Xsolla.Offerwall.Editor
{
    // Public: referenced across assembly boundaries by host-project CI helpers
    // (e.g. Assets/Editor/BuildHelper.cs), which live outside this asmdef.
    public enum IOSIntegrationMode { SwiftPackageManager, CocoaPods }

    /// <summary>
    /// Persists Xsolla Offerwall editor settings via <see cref="EditorPrefs"/> and keeps
    /// <c>XsollaDependencies.xml</c> in sync with those settings on every domain reload
    /// and whenever a setting changes.
    /// </summary>
    [InitializeOnLoad]
    public static class XsollaOfferwallEditorSettings
    {
        // ── EditorPrefs keys ──────────────────────────────────────────────────────
        private const string AppSetIdEnabledKey  = "Xsolla.Offerwall.AppSetIdEnabled";
        private const string AppSetIdVersionKey  = "Xsolla.Offerwall.AppSetIdVersion";
        private const string IOSIntegrationKey   = "Xsolla.Offerwall.IOSIntegration";
        private const string IOSVersionKey       = "Xsolla.Offerwall.IOSVersion";

        // Runtime defaults
        private const string RuntimeAndroidDeviceIdKey = "Xsolla.Offerwall.Runtime.AndroidDeviceIdEnabled";
        private const string RuntimeOrientationKey     = "Xsolla.Offerwall.Runtime.Orientation";
        private const string RuntimeLogLevelKey        = "Xsolla.Offerwall.Runtime.LogLevel";

        // Generated outside the SDK package folder so it is not included in package exports.
        // Resources.Load resolves any Resources/ folder under Assets/, so the location is flexible.
        private const string RuntimeSettingsAssetPath =
            "Assets/XsollaOfferwallSDK/Resources/XsollaOfferwallRuntimeSettings.asset";

        // ── Android constants ─────────────────────────────────────────────────────
        internal const string AppSetIdDefaultVersion = "16.1.0";
        private  const string AppSetIdSpec           = "com.google.android.gms:play-services-appset";

        // ── iOS constants ─────────────────────────────────────────────────────────
        // Native iOS SDK version — independent of the Unity package version.
        // Bump this when a new iOS SDK release is picked up, without necessarily
        // bumping the Unity plugin version at the same time.
        internal const string IOSDefaultVersion = "0.3.0";

        internal const string SpmPackageUrl  = "https://github.com/xsolla/xsolla-offerwall-ios-swift-package.git";
        internal const string SpmProductName = "XsollaOfferwallSDK";

        internal const string CocoaPodsSourceUrl = "https://github.com/xsolla/xsolla-offerwall-ios-cocoapods.git";
        internal const string CocoaPodName       = "XsollaOfferwallSDK";
        internal const string CocoaPodMinTarget  = "12.0";

        // ── InitializeOnLoad ──────────────────────────────────────────────────────

        static XsollaOfferwallEditorSettings()
        {
            // Defer until Unity has finished its current import/resolve pass.
            EditorApplication.delayCall += SyncDependenciesXml;
            EditorApplication.delayCall += SyncRuntimeSettings;
        }

        // ── Properties ────────────────────────────────────────────────────────────

        internal static bool AppSetIdEnabled
        {
            get => EditorPrefs.GetBool(AppSetIdEnabledKey, true);
            set { EditorPrefs.SetBool(AppSetIdEnabledKey, value); SyncDependenciesXml(); }
        }

        internal static string AppSetIdVersion
        {
            get => EditorPrefs.GetString(AppSetIdVersionKey, AppSetIdDefaultVersion);
            set
            {
                EditorPrefs.SetString(AppSetIdVersionKey, value);
                if (AppSetIdEnabled) SyncDependenciesXml();
            }
        }

        public static IOSIntegrationMode IOSIntegration
        {
            get => (IOSIntegrationMode)EditorPrefs.GetInt(IOSIntegrationKey, (int)IOSIntegrationMode.SwiftPackageManager);
            set { EditorPrefs.SetInt(IOSIntegrationKey, (int)value); SyncDependenciesXml(); }
        }

        /// <summary>
        /// iOS native SDK version written to XsollaDependencies.xml for both SPM and CocoaPods.
        /// Defaults to <see cref="IOSDefaultVersion"/>, which is versioned independently of
        /// the Unity package — bump <see cref="IOSDefaultVersion"/> when picking up a new
        /// iOS SDK release, without necessarily bumping the Unity plugin version.
        /// </summary>
        internal static string IOSVersion
        {
            get => EditorPrefs.GetString(IOSVersionKey, IOSDefaultVersion);
            set { EditorPrefs.SetString(IOSVersionKey, value); SyncDependenciesXml(); }
        }

        // ── Runtime defaults ──────────────────────────────────────────────────────

        /// <summary>
        /// Whether the Android Device ID is included in offerwall requests.
        /// Applied via <see cref="XsollaOfferwall.SetAndroidDeviceIdEnabled"/> before
        /// the first scene loads. Android only — ignored on iOS.
        /// </summary>
        internal static bool RuntimeAndroidDeviceIdEnabled
        {
            get => EditorPrefs.GetBool(RuntimeAndroidDeviceIdKey, false);
            set { EditorPrefs.SetBool(RuntimeAndroidDeviceIdKey, value); SyncRuntimeSettings(); }
        }

        /// <summary>
        /// Default screen orientation for the offerwall.
        /// Applied via <see cref="XsollaOfferwall.Settings.Orientation"/> before the
        /// first scene loads.
        /// </summary>
        internal static OfferwallOrientation RuntimeOrientation
        {
            get => (OfferwallOrientation)EditorPrefs.GetInt(RuntimeOrientationKey, (int)OfferwallOrientation.Portrait);
            set { EditorPrefs.SetInt(RuntimeOrientationKey, (int)value); SyncRuntimeSettings(); }
        }

        /// <summary>
        /// Default SDK log verbosity.
        /// Applied via <see cref="XsollaOfferwall.Settings.LogLevel"/> before the
        /// first scene loads.
        /// </summary>
        internal static OfferwallLogLevel RuntimeLogLevel
        {
            get => (OfferwallLogLevel)EditorPrefs.GetInt(RuntimeLogLevelKey, (int)OfferwallLogLevel.Info);
            set { EditorPrefs.SetInt(RuntimeLogLevelKey, (int)value); SyncRuntimeSettings(); }
        }

        // ── XML sync ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Rewrites <c>XsollaDependencies.xml</c> to reflect the current settings.
        /// Skips the write when the file is already correct.
        /// </summary>
        internal static void SyncDependenciesXml()
        {
            if (!FindXmlPaths(out string fullPath, out string assetPath))
            {
                Debug.LogWarning("[XsollaOfferwall] XsollaDependencies.xml not found — cannot sync dependencies.");
                return;
            }

            try
            {
                var doc = XDocument.Load(fullPath);

                bool changed  = SyncAndroidAppSetId(doc);
                     changed |= SyncIOSDependency(doc);

                if (!changed) return;

                SaveXml(doc, fullPath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[XsollaOfferwall] Failed to sync XsollaDependencies.xml: {ex.Message}");
            }
        }

        // ── Android: App Set ID ───────────────────────────────────────────────────

        private static bool SyncAndroidAppSetId(XDocument doc)
        {
            XElement androidPackages = doc.Root?.Element("androidPackages");
            if (androidPackages == null) return false;

            string desiredSpec = $"{AppSetIdSpec}:{AppSetIdVersion}";

            List<XElement> existing = androidPackages
                .Elements("androidPackage")
                .Where(e => ((string)e.Attribute("spec") ?? string.Empty).StartsWith(AppSetIdSpec + ":"))
                .ToList();

            bool alreadyCorrect = AppSetIdEnabled
                ? existing.Count == 1 && (string)existing[0].Attribute("spec") == desiredSpec
                : existing.Count == 0;

            if (alreadyCorrect) return false;

            foreach (XElement el in existing) el.Remove();
            if (AppSetIdEnabled)
                androidPackages.Add(new XElement("androidPackage", new XAttribute("spec", desiredSpec)));

            return true;
        }

        // ── iOS: SPM / CocoaPods ──────────────────────────────────────────────────

        private static bool SyncIOSDependency(XDocument doc)
        {
            XElement root = doc.Root;
            if (root == null) return false;

            XElement existingSpm  = root.Element("remoteSwiftPackage");
            XElement existingPods = root.Element("iosPods");

            if (IOSIntegration == IOSIntegrationMode.SwiftPackageManager)
            {
                bool alreadyCorrect = existingSpm  != null
                    && (string)existingSpm.Attribute("url")     == SpmPackageUrl
                    && (string)existingSpm.Attribute("version") == IOSVersion
                    && existingPods == null;

                if (alreadyCorrect) return false;

                existingSpm?.Remove();
                existingPods?.Remove();
                root.Add(BuildSpmElement());
            }
            else
            {
                XElement existingPod = existingPods?.Element("iosPod");
                bool alreadyCorrect  = existingPods != null
                    && existingPod   != null
                    && (string)existingPod.Attribute("version") == IOSVersion
                    && existingSpm == null;

                if (alreadyCorrect) return false;

                existingSpm?.Remove();
                existingPods?.Remove();
                root.Add(BuildCocoaPodsElement());
            }

            return true;
        }

        private static XElement BuildSpmElement() =>
            new XElement("remoteSwiftPackage",
                new XAttribute("url",     SpmPackageUrl),
                new XAttribute("version", IOSVersion),
                new XElement("swiftPackage", new XAttribute("name", SpmProductName)));

        private static XElement BuildCocoaPodsElement() =>
            new XElement("iosPods",
                new XElement("sources",
                    new XElement("source", CocoaPodsSourceUrl)),
                new XElement("iosPod",
                    new XAttribute("name",         CocoaPodName),
                    new XAttribute("version",      IOSVersion),
                    new XAttribute("minTargetSdk", CocoaPodMinTarget)));

        // ── Runtime settings ScriptableObject ────────────────────────────────────

        /// <summary>
        /// Creates or updates the <see cref="XsollaOfferwallRuntimeSettings"/> asset
        /// that <see cref="XsollaOfferwallRuntimeInit"/> loads at runtime.
        /// Skips the write when the asset is already up to date.
        /// </summary>
        internal static void SyncRuntimeSettings()
        {
            try
            {
                string dir = Path.GetDirectoryName(RuntimeSettingsAssetPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var asset = AssetDatabase.LoadAssetAtPath<XsollaOfferwallRuntimeSettings>(RuntimeSettingsAssetPath);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<XsollaOfferwallRuntimeSettings>();
                    AssetDatabase.CreateAsset(asset, RuntimeSettingsAssetPath);
                }

                bool changed = asset.androidDeviceIdEnabled != RuntimeAndroidDeviceIdEnabled
                            || asset.orientation            != RuntimeOrientation
                            || asset.logLevel               != RuntimeLogLevel;

                if (!changed) return;

                asset.androidDeviceIdEnabled = RuntimeAndroidDeviceIdEnabled;
                asset.orientation            = RuntimeOrientation;
                asset.logLevel               = RuntimeLogLevel;

                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[XsollaOfferwall] Failed to sync runtime settings: {ex.Message}");
            }
        }

        // ── XML I/O ───────────────────────────────────────────────────────────────

        private static void SaveXml(XDocument doc, string fullPath)
        {
            var settings = new XmlWriterSettings
            {
                Indent             = true,
                IndentChars        = "  ",
                NewLineChars       = "\n",
                OmitXmlDeclaration = true,
            };
            using (XmlWriter writer = XmlWriter.Create(fullPath, settings))
                doc.Save(writer);
        }

        /// <summary>
        /// Locates <c>XsollaDependencies.xml</c> via the AssetDatabase so the path
        /// resolves correctly regardless of where the SDK is installed.
        /// </summary>
        private static bool FindXmlPaths(out string fullPath, out string assetPath)
        {
            foreach (string guid in AssetDatabase.FindAssets("XsollaDependencies"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("XsollaDependencies.xml", StringComparison.OrdinalIgnoreCase))
                {
                    assetPath = path;
                    fullPath  = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
                    return true;
                }
            }
            fullPath  = null;
            assetPath = null;
            return false;
        }
    }
}
