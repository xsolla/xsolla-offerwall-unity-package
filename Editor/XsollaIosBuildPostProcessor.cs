#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Xsolla.Offerwall.Editor
{
    /// <summary>
    /// Post-process build script that ensures XsollaOfferwallSDK
    /// Swift Package product is linked in the Unity-iPhone target's
    /// "Link Binary with Libraries" phase of the generated Xcode project.
    ///
    /// EDM4U (External Dependency Manager for Unity) adds the remote Swift Package
    /// references from XsollaDependencies.xml, but does not always wire the products
    /// into the link phase of the main app target. This script guarantees that linkage.
    /// </summary>
    public static class XsollaIosBuildPostProcessor
    {
        // Swift Package repository URL — must match XsollaDependencies.xml exactly.
        private const string OfferwallPackageUrl =
            "https://github.com/xsolla/xsolla-offerwall-ios-swift-package.git";

        // Package version — must match XsollaDependencies.xml exactly.
        private const string OfferwallPackageVersion = "0.2.0";

        // The Swift Package product names to link.
        private static readonly string[] OfferwallProducts = { "XsollaOfferwallSDK" };

        // Run after EDM4U's own post-process step (callbackOrder > 0 is sufficient).
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.iOS)
                return;

            string pbxProjectPath = PBXProject.GetPBXProjectPath(buildPath);

            var project = new PBXProject();
            project.ReadFromFile(pbxProjectPath);

            string mainTargetGuid = project.GetUnityMainTargetGuid();

            if (string.IsNullOrEmpty(mainTargetGuid))
            {
                Debug.LogError("[XsollaIosBuildPostProcessor] Could not find Unity-iPhone target GUID.");
                return;
            }

            // Link XsollaOfferwallSDK ---------------------------------------------------
            LinkSwiftPackageProducts(
                project,
                mainTargetGuid,
                OfferwallPackageUrl,
                OfferwallPackageVersion,
                OfferwallProducts
            );

            project.WriteToFile(pbxProjectPath);

            Debug.Log("[XsollaIosBuildPostProcessor] Swift Package products linked to Unity-iPhone target.");
        }

        /// <summary>
        /// Ensures a remote Swift Package reference exists in the project and that
        /// each of its named products is listed under "Link Binary with Libraries"
        /// for the given target.
        /// </summary>
        private static void LinkSwiftPackageProducts(
            PBXProject project,
            string targetGuid,
            string packageUrl,
            string packageVersion,
            string[] productNames)
        {
            // AddRemotePackageReferenceAtVersion is idempotent when the URL already
            // exists in the project (EDM4U may have added it already). Either way it
            // returns the package reference GUID we need for linking.
            string packageRefGuid = project.AddRemotePackageReferenceAtVersion(
                packageUrl,
                packageVersion
            );

            foreach (string product in productNames)
            {
                // AddRemotePackageFrameworkToProject adds the product to:
                //   - XCSwiftPackageProductDependency list on the target
                //   - the "Link Binary with Libraries" build phase
                // It is safe to call multiple times; duplicate entries are not created.
                project.AddRemotePackageFrameworkToProject(
                    targetGuid,
                    product,
                    packageRefGuid,
                    weak: false
                );

                Debug.Log($"[XsollaIosBuildPostProcessor] Linked {product} (from {packageUrl}) to target {targetGuid}.");
            }
        }
    }
}
#endif
