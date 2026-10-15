using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Xsolla.Offerwall.Editor
{
    /// <summary>
    /// Automatically ensures the External Dependency Manager for Unity (EDM4U) is
    /// present in the host project when this package is first imported.
    ///
    /// Unity's UPM cannot resolve packages from scoped registries that aren't already
    /// configured in the host project's manifest.json.  This installer bridges that
    /// gap by parsing manifest.json with Newtonsoft.Json (Json.NET), adding the
    /// required registry scope and dependency entry to the parsed object model, and
    /// writing the result back out, then calling Client.Resolve() so Unity picks up
    /// EDM4U without any manual steps.
    ///
    /// Flow:
    ///   1. [InitializeOnLoad] fires on every domain reload (including after install).
    ///   2. SessionState prevents redundant writes within one editor session.
    ///   3. EditorApplication.delayCall defers the file write until Unity is idle,
    ///      avoiding interference with an in-progress package resolve.
    ///   4. If EDM4U is found inside Assets/ a conflict warning is logged and the
    ///      UPM install is skipped — both copies cannot coexist safely.
    ///   5. If manifest.json already contains EDM4U nothing is written.
    /// </summary>
    [InitializeOnLoad]
    public static class XsollaOfferwallInstaller
    {
        private const string EdmPackageId = EdmVersionHelper.PackageId;
        private const string EdmVersion   = EdmVersionHelper.MinimumSpmVersion;
        private const string OpenUpmUrl   = "https://package.openupm.com";
        private const string OpenUpmName  = "package.openupm.com";

        // Known root folder names used by EDM4U when imported as a .unitypackage.
        private static readonly string[] EdmAssetFolders =
        {
            "ExternalDependencyManager",  // EDM4U 1.2.x+
            "PlayServicesResolver",        // legacy name (pre-1.2)
        };

        // Prevents repeated checks within the same editor session.
        private const string SessionKeyInstall      = "Xsolla.Offerwall.EdmChecked";
        private const string SessionKeyVersionCheck = "Xsolla.Offerwall.EdmVersionChecked";

        static XsollaOfferwallInstaller()
        {
            if (!SessionState.GetBool(SessionKeyInstall, false))
            {
                SessionState.SetBool(SessionKeyInstall, true);
                // Defer until Unity has finished its current resolve/import pass.
                EditorApplication.delayCall += EnsureEdmInstalled;
            }

            if (!SessionState.GetBool(SessionKeyVersionCheck, false))
            {
                SessionState.SetBool(SessionKeyVersionCheck, true);
                EditorApplication.delayCall += CheckEdmVersion;
            }
        }

        /// <summary>
        /// Checks whether the installed EDM version meets the minimum required for Swift
        /// Package Manager support.  If it does not, notifies the developer that
        /// CocoaPods will be used for iOS dependencies instead, and switches the iOS
        /// integration setting accordingly.
        /// </summary>
        private static void CheckEdmVersion()
        {
            if (EdmVersionHelper.MeetsMinimumSpmRequirement())
                return;

            string installed = EdmVersionHelper.GetInstalledVersion();

            // Switch iOS integration to CocoaPods so the build processor reflects reality.
            if (XsollaOfferwallEditorSettings.IOSIntegration == IOSIntegrationMode.SwiftPackageManager)
                XsollaOfferwallEditorSettings.IOSIntegration = IOSIntegrationMode.CocoaPods;

            EditorUtility.DisplayDialog(
                "Xsolla Offerwall — EDM Version Warning",
                $"The installed External Dependency Manager version ({installed}) is below " +
                $"{EdmVersionHelper.MinimumSpmVersion}, which is required for Swift Package Manager support.\n\n" +
                "This package will use CocoaPods for iOS dependencies instead.\n\n" +
                "To use Swift Package Manager, update EDM via Window → Package Manager.",
                "OK");
        }

        private static void EnsureEdmInstalled()
        {
            // ── Conflict check ─────────────────────────────────────────────────────
            // If EDM4U was previously imported as a .unitypackage it will live inside
            // Assets/.  Having it in both Assets/ and Packages/ causes duplicate type
            // errors and resolver conflicts, so we block the UPM install and ask the
            // developer to remove the Assets copy first.
            string conflictFolder = FindEdmInAssets();
            if (conflictFolder != null)
            {
                Debug.LogError(
                    $"[XsollaOfferwall] EDM4U conflict detected: '{conflictFolder}' exists inside your Assets folder. " +
                    "Having EDM4U in both Assets/ and the UPM package cache causes duplicate type errors and resolver conflicts. " +
                    "Please delete the Assets/ copy (and its .meta file) then re-import this package.");
                return;
            }

            // ── Manifest update ────────────────────────────────────────────────────
            string manifestPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Packages", "manifest.json"));

            if (!File.Exists(manifestPath))
            {
                Debug.LogWarning("[XsollaOfferwall] Could not find Packages/manifest.json — skipping EDM4U auto-install.");
                return;
            }

            string original = File.ReadAllText(manifestPath);
            JObject manifest;
            try
            {
                manifest = JObject.Parse(original);
            }
            catch (JsonException e)
            {
                Debug.LogError($"[XsollaOfferwall] Could not parse Packages/manifest.json ({e.Message}) — skipping EDM4U auto-install.");
                return;
            }

            bool changed = false;

            // 1. Ensure the dependency entry exists.
            JObject dependencies = GetOrCreateObject(manifest, "dependencies");
            if (dependencies[EdmPackageId] == null)
            {
                dependencies[EdmPackageId] = EdmVersion;
                changed = true;
                Debug.Log($"[XsollaOfferwall] Added {EdmPackageId}@{EdmVersion} to manifest dependencies.");
            }

            // 2. Ensure the OpenUPM registry has the EDM scope so UPM can resolve it.
            if (EnsureOpenUpmScope(manifest, EdmPackageId))
            {
                changed = true;
                Debug.Log($"[XsollaOfferwall] Added '{EdmPackageId}' scope to the OpenUPM scoped registry.");
            }

            if (!changed)
                return;

            File.WriteAllText(manifestPath, SerializeManifest(manifest));
            Debug.Log("[XsollaOfferwall] manifest.json updated — resolving packages to install EDM4U…");
            Client.Resolve();
        }

        /// <summary>
        /// Serializes <paramref name="manifest"/> with 2-space indentation and LF line
        /// endings, regardless of host OS. JToken.ToString(Formatting.Indented) writes
        /// line breaks via Environment.NewLine, which would rewrite every line in the
        /// file to CRLF on Windows even though we only changed a couple of entries —
        /// writing through JsonTextWriter with an explicit NewLine avoids that.
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

        // ── Conflict detection ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns the relative Assets-folder path of the first EDM4U directory found
        /// inside the project's Assets folder, or null if none is present.
        /// </summary>
        private static string FindEdmInAssets()
        {
            foreach (string folder in EdmAssetFolders)
            {
                string fullPath = Path.Combine(Application.dataPath, folder);
                if (Directory.Exists(fullPath))
                    return $"Assets/{folder}";
            }
            return null;
        }

        // ── Manifest mutation helpers ──────────────────────────────────────────────

        /// <summary>
        /// Ensures the OpenUPM scoped registry exists in <paramref name="manifest"/> and
        /// has <paramref name="packageId"/> listed as a scope, creating the registry
        /// entry (and/or the "scopedRegistries" array) if needed. Returns true if the
        /// manifest was modified.
        /// </summary>
        private static bool EnsureOpenUpmScope(JObject manifest, string packageId)
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
                    ["scopes"] = new JArray(packageId),
                };
                registries.Add(openUpm);
                return true;
            }

            JArray scopes = GetOrCreateArray(openUpm, "scopes");
            if (scopes.Any(scope => (string)scope == packageId))
                return false;

            scopes.Add(packageId);
            return true;
        }

        /// <summary>Gets the object value at <paramref name="key"/>, creating an empty one if absent or of the wrong type.</summary>
        private static JObject GetOrCreateObject(JObject parent, string key)
        {
            if (parent[key] is JObject existing)
                return existing;

            var created = new JObject();
            parent[key] = created;
            return created;
        }

        /// <summary>Gets the array value at <paramref name="key"/>, creating an empty one if absent or of the wrong type.</summary>
        private static JArray GetOrCreateArray(JObject parent, string key)
        {
            if (parent[key] is JArray existing)
                return existing;

            var created = new JArray();
            parent[key] = created;
            return created;
        }
    }
}
