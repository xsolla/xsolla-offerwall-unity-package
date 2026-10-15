using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Xsolla.Offerwall.Editor
{
    /// <summary>
    /// Helpers for reading and comparing the installed External Dependency Manager version.
    ///
    /// EDM can be present in two locations:
    /// <list type="bullet">
    ///   <item><description>
    ///     <b>UPM package</b> — listed in <c>Packages/packages-lock.json</c>.
    ///   </description></item>
    ///   <item><description>
    ///     <b>Assets folder</b> — imported as a <c>.unitypackage</c>.
    ///     EDM labels every asset it ships with <c>gvh_version-X.Y.Z</c>, which is
    ///     stored in the corresponding <c>.meta</c> file and readable without loading
    ///     any Unity objects.
    ///   </description></item>
    /// </list>
    /// <see cref="GetInstalledVersion"/> checks both locations and returns the first
    /// version it finds, preferring the UPM copy.
    /// </summary>
    internal static class EdmVersionHelper
    {
        internal const string PackageId         = "com.google.external-dependency-manager";
        internal const string MinimumSpmVersion = "1.2.187";

        // Known root folder names when EDM is imported as a .unitypackage.
        private static readonly string[] EdmAssetFolderNames =
        {
            "ExternalDependencyManager",  // EDM4U 1.2.x+
            "PlayServicesResolver",        // legacy name (pre-1.2)
        };

        private const string SkipVersionCheckKey = "Xsolla.Offerwall.SkipEdmVersionCheck";

        /// <summary>
        /// When <c>true</c>, all EDM version checks are suppressed.
        /// Persisted in <see cref="EditorPrefs"/> so it survives domain reloads.
        /// </summary>
        internal static bool SkipVersionCheck
        {
            get => EditorPrefs.GetBool(SkipVersionCheckKey, false);
            set => EditorPrefs.SetBool(SkipVersionCheckKey, value);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the installed EDM version string, or <c>null</c> if EDM is not found.
        /// Checks the UPM lock file first, then falls back to the Assets folder.
        /// </summary>
        internal static string GetInstalledVersion()
            => GetVersionFromLockFile() ?? GetVersionFromAssets();

        /// <summary>
        /// Returns <c>true</c> when the version check is disabled, EDM is not installed,
        /// or the installed version is at least <see cref="MinimumSpmVersion"/>.
        /// </summary>
        internal static bool MeetsMinimumSpmRequirement()
        {
            if (SkipVersionCheck)
                return true;

            string installed = GetInstalledVersion();
            return installed == null || IsVersionAtLeast(installed, MinimumSpmVersion);
        }

        // ── Version sources ───────────────────────────────────────────────────────

        /// <summary>
        /// Reads the EDM version from <c>Packages/packages-lock.json</c> (UPM install).
        /// </summary>
        private static string GetVersionFromLockFile()
        {
            string lockPath = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "Packages", "packages-lock.json"));

            if (!File.Exists(lockPath))
                return null;

            return ParseVersionFromLockFile(File.ReadAllText(lockPath), PackageId);
        }

        /// <summary>
        /// Reads the EDM version from an Assets-based install by scanning <c>.meta</c>
        /// files in the known EDM folders for a <c>gvh_version-X.Y.Z</c> label.
        /// EDM labels every asset it ships with this label; the value is stored in the
        /// plain-text <c>.meta</c> file so no Unity asset loading is required.
        /// </summary>
        private static string GetVersionFromAssets()
        {
            const string labelPrefix = "gvh_version-";

            foreach (string folderName in EdmAssetFolderNames)
            {
                // Check both the root and the Editor sub-folder (most DLLs live there).
                string[] searchDirs =
                {
                    Path.Combine(Application.dataPath, folderName, "Editor"),
                    Path.Combine(Application.dataPath, folderName),
                };

                foreach (string dir in searchDirs)
                {
                    if (!Directory.Exists(dir))
                        continue;

                    foreach (string metaFile in Directory.GetFiles(dir, "*.meta", SearchOption.TopDirectoryOnly))
                    {
                        string version = ExtractGvhVersionLabel(File.ReadAllText(metaFile), labelPrefix);
                        if (version != null)
                            return version;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Scans the content of a <c>.meta</c> file for a line containing
        /// <paramref name="labelPrefix"/> and returns the version string that follows it,
        /// or <c>null</c> if the label is absent.
        /// </summary>
        internal static string ExtractGvhVersionLabel(string metaContent, string labelPrefix)
        {
            int idx = metaContent.IndexOf(labelPrefix, StringComparison.Ordinal);
            if (idx < 0)
                return null;

            int start = idx + labelPrefix.Length;
            int end   = metaContent.IndexOfAny(new[] { '\n', '\r' }, start);
            if (end < 0)
                end = metaContent.Length;

            string version = metaContent.Substring(start, end - start).Trim();
            return string.IsNullOrEmpty(version) ? null : version;
        }

        // ── Lock-file parsing ─────────────────────────────────────────────────────

        /// <summary>
        /// Extracts the <c>"version"</c> value for <paramref name="packageId"/> from
        /// Unity's <c>packages-lock.json</c>.  Each package entry follows the layout:
        /// <code>
        ///   "com.example.package": {
        ///     "version": "1.2.3",
        ///     …
        ///   }
        /// </code>
        /// </summary>
        internal static string ParseVersionFromLockFile(string json, string packageId)
        {
            int pkgIdx = json.IndexOf($"\"{packageId}\"", StringComparison.Ordinal);
            if (pkgIdx < 0)
                return null;

            // Search within the next 512 chars of that entry (more than enough for one block).
            int searchLen     = Math.Min(512, json.Length - pkgIdx);
            int versionKeyIdx = json.IndexOf("\"version\"", pkgIdx, searchLen, StringComparison.Ordinal);
            if (versionKeyIdx < 0)
                return null;

            int colonIdx = json.IndexOf(':', versionKeyIdx);
            if (colonIdx < 0)
                return null;

            int quoteOpen = json.IndexOf('"', colonIdx + 1);
            if (quoteOpen < 0)
                return null;

            int quoteClose = json.IndexOf('"', quoteOpen + 1);
            if (quoteClose <= quoteOpen)
                return null;

            return json.Substring(quoteOpen + 1, quoteClose - quoteOpen - 1);
        }

        // ── Version comparison ────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> when <paramref name="version"/> is greater than or equal
        /// to <paramref name="minimum"/>, using numeric component comparison.
        /// Unparseable components are treated as 0.
        /// </summary>
        internal static bool IsVersionAtLeast(string version, string minimum)
        {
            int[] v = SplitComponents(version);
            int[] m = SplitComponents(minimum);
            int   n = Math.Max(v.Length, m.Length);

            for (int i = 0; i < n; i++)
            {
                int vi = i < v.Length ? v[i] : 0;
                int mi = i < m.Length ? m[i] : 0;
                if (vi != mi)
                    return vi > mi;
            }

            return true; // equal
        }

        private static int[] SplitComponents(string version)
        {
            string[] parts  = (version ?? string.Empty).Split('.');
            var      result = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                int.TryParse(parts[i], out result[i]);
            return result;
        }
    }
}
