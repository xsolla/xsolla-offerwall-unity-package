#if UNITY_IOS
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Xsolla.Offerwall.Editor
{
    /// <summary>
    /// Post-process build script for iOS integration.
    ///
    /// Swift Package Manager mode:
    ///   EDM4U adds the remote Swift Package reference from XsollaDependencies.xml but
    ///   does not always wire the products into "Link Binary with Libraries" on the app
    ///   targets. This script guarantees that linkage on both Unity-iPhone and
    ///   UnityFramework.
    ///
    /// CocoaPods mode:
    ///   EDM4U reads the &lt;iosPods&gt; block from XsollaDependencies.xml, generates the
    ///   Podfile, and runs pod install. This script then links the downloaded XCFramework
    ///   to both Unity-iPhone and UnityFramework targets, and adds it to the
    ///   "Embed Frameworks" copy phase on Unity-iPhone with CodeSignOnCopy set.
    ///   Unity's PBXProject API does not expose a method for the CodeSignOnCopy
    ///   attribute, so it is patched into the serialised project file directly after
    ///   WriteToFile.
    ///
    /// The package URL/pod name and version are read from
    /// <see cref="XsollaOfferwallEditorSettings"/>, the single source of truth shared
    /// with XsollaDependencies.xml.
    /// </summary>
    public static class XsollaIosBuildPostProcessor
    {
        // Run after EDM4U's own post-process step.
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.iOS)
                return;

            string pbxProjectPath = PBXProject.GetPBXProjectPath(buildPath);
            var project = new PBXProject();
            project.ReadFromFile(pbxProjectPath);

            string mainTargetGuid      = project.GetUnityMainTargetGuid();
            string frameworkTargetGuid = project.GetUnityFrameworkTargetGuid();

            if (string.IsNullOrEmpty(mainTargetGuid))
            {
                Debug.LogError("[XsollaIosBuildPostProcessor] Could not find Unity-iPhone target GUID.");
                return;
            }

            bool isCocoaPods = XsollaOfferwallEditorSettings.IOSIntegration == IOSIntegrationMode.CocoaPods;

            if (isCocoaPods)
                HandleCocoaPods(project, buildPath, mainTargetGuid, frameworkTargetGuid);
            else
                HandleSPM(project, pbxProjectPath, mainTargetGuid, frameworkTargetGuid);

            project.WriteToFile(pbxProjectPath);

            // CodeSignOnCopy cannot be set via PBXProject API — patch the project file directly.
            if (isCocoaPods)
                InjectCodeSignOnCopy(pbxProjectPath, XsollaOfferwallEditorSettings.CocoaPodName);
        }

        // ── Swift Package Manager ─────────────────────────────────────────────────

        private static void HandleSPM(PBXProject project, string pbxProjectPath, string mainTargetGuid, string frameworkTargetGuid)
        {
            string productName = XsollaOfferwallEditorSettings.SpmProductName;
            string packageUrl  = XsollaOfferwallEditorSettings.SpmPackageUrl;

            // PBXProject.AddRemotePackageReferenceAtVersion always creates a brand
            // new XCRemoteSwiftPackageReference — it does not check whether one
            // already exists for this URL. EDM4U independently adds its own
            // reference from XsollaDependencies.xml, and Append builds carry
            // forward whatever a previous run of this script already added, so
            // calling it unconditionally produces duplicate package references
            // (and duplicate "Link Binary with Libraries" entries on
            // UnityFramework). Reuse an existing reference when one is found.
            string packageRefGuid = FindExistingPackageReferenceGuid(pbxProjectPath, packageUrl)
                ?? project.AddRemotePackageReferenceAtVersion(packageUrl, XsollaOfferwallEditorSettings.IOSVersion);

            foreach (string targetGuid in new[] { mainTargetGuid, frameworkTargetGuid })
            {
                if (string.IsNullOrEmpty(targetGuid)) continue;

                // AddRemotePackageFrameworkToProject adds the product to both
                // XCSwiftPackageProductDependency and "Link Binary with Libraries".
                // Safe to call multiple times — duplicates are not created.
                project.AddRemotePackageFrameworkToProject(targetGuid, productName, packageRefGuid, weak: false);
                Debug.Log($"[XsollaIosBuildPostProcessor] Linked SPM product '{productName}' to target {targetGuid}.");
            }
        }

        /// <summary>
        /// Finds the GUID of an existing <c>XCRemoteSwiftPackageReference</c> for
        /// the given repository URL by scanning the serialised project file.
        /// PBXProject has no public lookup for existing package references, so
        /// this is the only way to detect one already added by EDM4U or by a
        /// previous run of this script (see <see cref="HandleSPM"/>).
        /// </summary>
        private static string FindExistingPackageReferenceGuid(string pbxProjectPath, string url)
        {
            string content = File.ReadAllText(pbxProjectPath);

            // Matches: GUID /* XCRemoteSwiftPackageReference "..." */ = { isa = XCRemoteSwiftPackageReference; repositoryURL = "<url>"; ...
            Match match = Regex.Match(
                content,
                @"(\w+) /\* XCRemoteSwiftPackageReference[^*]*\*/ = \{\s*isa = XCRemoteSwiftPackageReference;\s*repositoryURL = " +
                $@"""{Regex.Escape(url)}"";");

            return match.Success ? match.Groups[1].Value : null;
        }

        // ── CocoaPods ─────────────────────────────────────────────────────────────

        private static void HandleCocoaPods(
            PBXProject project,
            string buildPath,
            string mainTargetGuid,
            string frameworkTargetGuid)
        {
            string podName   = XsollaOfferwallEditorSettings.CocoaPodName;
            string xcfwkPath = Path.Combine("Pods", podName, $"{podName}.xcframework");
            string xcfwkFull = Path.Combine(buildPath, xcfwkPath);

            if (!Directory.Exists(xcfwkFull))
            {
                Debug.LogError(
                    $"[XsollaIosBuildPostProcessor] XCFramework not found at '{xcfwkFull}'. " +
                    "Ensure EDM4U's CocoaPods integration ran successfully.");
                return;
            }

            // Reuse an existing file reference if present (e.g. Append builds).
            string fileGuid = project.FindFileGuidByProjectPath(xcfwkPath);
            if (string.IsNullOrEmpty(fileGuid))
                fileGuid = project.AddFile(xcfwkPath, xcfwkPath, PBXSourceTree.Source);

            // Link both targets.
            // Note: AddFileToBuild does not deduplicate. This post-processor is
            // designed for Replace builds; Append builds may produce duplicate link
            // entries that Xcode will warn about.
            foreach (string targetGuid in new[] { mainTargetGuid, frameworkTargetGuid })
            {
                if (string.IsNullOrEmpty(targetGuid)) continue;
                project.AddFileToBuild(targetGuid, fileGuid);
                Debug.Log($"[XsollaIosBuildPostProcessor] Linked '{podName}' XCFramework to target {targetGuid}.");
            }

            // Add to the Embed Frameworks copy phase on the app target only.
            // Embedding inside a framework target (UnityFramework) is not valid.
            // CodeSignOnCopy is applied afterwards via InjectCodeSignOnCopy.
            string embedPhase = project.GetCopyFilesBuildPhaseByTarget(mainTargetGuid, "Embed Frameworks", "", "10");
            if (string.IsNullOrEmpty(embedPhase))
                embedPhase = project.AddCopyFilesBuildPhase(mainTargetGuid, "Embed Frameworks", "", "10");

            project.AddFileToBuildSection(mainTargetGuid, embedPhase, fileGuid);
            Debug.Log($"[XsollaIosBuildPostProcessor] Added '{podName}' XCFramework to Embed Frameworks phase on Unity-iPhone.");
        }

        /// <summary>
        /// Patches the serialised Xcode project file to set <c>CodeSignOnCopy</c> on
        /// the XCFramework's embed build-file entry. This attribute is not accessible
        /// through Unity's PBXProject API.
        ///
        /// The pattern matches entries that do not yet carry a <c>settings</c> block,
        /// so the patch is idempotent across Append builds.
        /// </summary>
        private static void InjectCodeSignOnCopy(string pbxProjectPath, string frameworkName)
        {
            string content = File.ReadAllText(pbxProjectPath);
            string xcfwk   = $"{frameworkName}.xcframework";

            // Matches: GUID /* Name.xcframework in Embed Frameworks */ = {isa = PBXBuildFile; fileRef = GUID /* Name.xcframework */; };
            // Does NOT match entries that already contain a settings block.
            string pattern =
                $@"(\w+ /\* {Regex.Escape(xcfwk)} in Embed Frameworks \*/ = \{{" +
                $@"isa = PBXBuildFile; fileRef = \w+ /\* {Regex.Escape(xcfwk)} \*/; )" +
                $@"\}};";

            string replacement = "$1settings = {ATTRIBUTES = (CodeSignOnCopy, ); }; };";

            string modified = Regex.Replace(content, pattern, replacement);

            if (modified == content)
                return; // already set, or entry not found

            File.WriteAllText(pbxProjectPath, modified);
            Debug.Log($"[XsollaIosBuildPostProcessor] Set CodeSignOnCopy on '{xcfwk}' in Embed Frameworks phase.");
        }
    }
}
#endif
