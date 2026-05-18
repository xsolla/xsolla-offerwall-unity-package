using System;
using System.IO;
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
    /// gap by writing the required registry scope and dependency entry itself, then
    /// calling Client.Resolve() so Unity picks up EDM4U without any manual steps.
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
        private const string EdmPackageId = "com.google.external-dependency-manager";
        private const string EdmVersion   = "1.2.187";
        private const string OpenUpmUrl   = "https://package.openupm.com";
        private const string OpenUpmName  = "package.openupm.com";

        // Known root folder names used by EDM4U when imported as a .unitypackage.
        private static readonly string[] EdmAssetFolders =
        {
            "ExternalDependencyManager",  // EDM4U 1.2.x+
            "PlayServicesResolver",        // legacy name (pre-1.2)
        };

        // Prevents repeated checks within the same editor session.
        private const string SessionKey = "Xsolla.Offerwall.EdmChecked";

        static XsollaOfferwallInstaller()
        {
            if (SessionState.GetBool(SessionKey, false))
                return;

            SessionState.SetBool(SessionKey, true);

            // Defer until Unity has finished its current resolve/import pass.
            EditorApplication.delayCall += EnsureEdmInstalled;
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
            string modified  = original;
            bool   changed   = false;

            // 1. Ensure the dependency entry exists.
            if (!HasDependency(modified, EdmPackageId))
            {
                modified = InsertDependency(modified, EdmPackageId, EdmVersion);
                changed  = true;
                Debug.Log($"[XsollaOfferwall] Added {EdmPackageId}@{EdmVersion} to manifest dependencies.");
            }

            // 2. Ensure the OpenUPM registry has the EDM scope so UPM can resolve it.
            if (!HasOpenUpmScope(modified, EdmPackageId))
            {
                modified = AddOpenUpmScope(modified, EdmPackageId);
                changed  = true;
                Debug.Log($"[XsollaOfferwall] Added '{EdmPackageId}' scope to the OpenUPM scoped registry.");
            }

            if (!changed)
                return;

            File.WriteAllText(manifestPath, modified);
            Debug.Log("[XsollaOfferwall] manifest.json updated — resolving packages to install EDM4U…");
            Client.Resolve();
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

        // ── Inspection helpers ─────────────────────────────────────────────────────

        private static bool HasDependency(string json, string packageId)
            => json.Contains($"\"{packageId}\"");

        /// <summary>
        /// Returns true only when the EDM package id appears inside an OpenUPM
        /// registry block (i.e. it is listed as a scope, not just in dependencies).
        /// </summary>
        private static bool HasOpenUpmScope(string json, string packageId)
        {
            int urlIdx = json.IndexOf(OpenUpmUrl, StringComparison.Ordinal);
            if (urlIdx < 0) return false;

            // Scope forward to the closing brace of this registry object.
            int braceClose = json.IndexOf('}', urlIdx);
            if (braceClose < 0) return false;

            return json.IndexOf($"\"{packageId}\"", urlIdx, braceClose - urlIdx, StringComparison.Ordinal) >= 0;
        }

        // ── Mutation helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Inserts a dependency entry immediately after the opening brace of the
        /// "dependencies" object.
        /// </summary>
        private static string InsertDependency(string json, string packageId, string version)
        {
            const string marker = "\"dependencies\": {";
            int idx = json.IndexOf(marker, StringComparison.Ordinal);
            if (idx < 0)
            {
                Debug.LogError("[XsollaOfferwall] Could not locate \"dependencies\" block in manifest.json.");
                return json;
            }

            int insertAt = idx + marker.Length;
            return json.Insert(insertAt, $"\n    \"{packageId}\": \"{version}\",");
        }

        /// <summary>
        /// Adds the EDM package scope to the OpenUPM scoped registry's scopes array.
        /// If no OpenUPM registry exists, a new scopedRegistries entry is created
        /// before the "dependencies" block.
        /// </summary>
        private static string AddOpenUpmScope(string json, string packageId)
        {
            // Case A: OpenUPM registry already present — inject scope into its array.
            int urlIdx = json.IndexOf(OpenUpmUrl, StringComparison.Ordinal);
            if (urlIdx >= 0)
            {
                int scopesIdx = json.IndexOf("\"scopes\"", urlIdx, StringComparison.Ordinal);
                if (scopesIdx >= 0)
                {
                    int arrayOpen = json.IndexOf('[', scopesIdx);
                    if (arrayOpen >= 0)
                        return json.Insert(arrayOpen + 1, $"\n        \"{packageId}\",");
                }
            }

            // Case B: No OpenUPM registry — prepend a new scoped registry block.
            string newRegistry =
                "\"scopedRegistries\": [\n" +
                "    {\n" +
                $"      \"name\": \"{OpenUpmName}\",\n" +
                $"      \"url\": \"{OpenUpmUrl}\",\n" +
                "      \"scopes\": [\n" +
                $"        \"{packageId}\"\n" +
                "      ]\n" +
                "    }\n" +
                "  ],\n  ";

            const string depsMarker = "\"dependencies\"";
            int depsIdx = json.IndexOf(depsMarker, StringComparison.Ordinal);
            if (depsIdx >= 0)
                return json.Insert(depsIdx, newRegistry);

            Debug.LogError("[XsollaOfferwall] Could not find an insertion point for scopedRegistries in manifest.json.");
            return json;
        }
    }
}
