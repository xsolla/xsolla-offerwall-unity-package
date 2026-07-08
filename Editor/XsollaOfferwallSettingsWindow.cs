using UnityEditor;
using UnityEngine;

namespace Xsolla.Offerwall.Editor
{
    /// <summary>
    /// Editor window for Xsolla Offerwall SDK settings.
    /// Open via Window > Xsolla Offerwall > Settings.
    ///
    /// Build settings (iOS integration mode, App Set ID) are applied at build time
    /// by rewriting XsollaDependencies.xml.
    ///
    /// Runtime defaults (orientation, log level, Android Device ID) are baked into
    /// <c>XsollaOfferwallRuntimeSettings.asset</c> and applied automatically before
    /// the first scene loads via <see cref="XsollaOfferwallRuntimeInit"/>.
    /// </summary>
    public class XsollaOfferwallSettingsWindow : EditorWindow
    {
        [MenuItem("Window/Xsolla Offerwall/Settings")]
        public static void Open()
        {
            var window = GetWindow<XsollaOfferwallSettingsWindow>(utility: false, title: "Offerwall Settings");
            window.minSize = new Vector2(340, 380);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Xsolla Offerwall SDK", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Version", XsollaOfferwallVersion.UnityVersion);
            }

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Runtime Defaults", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            DrawRuntimeDefaultsSection();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("iOS", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            DrawIOSSection();

            EditorGUILayout.Space(12);
            EditorGUILayout.LabelField("Android", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            DrawAppSetIdSection();

            EditorGUILayout.Space(10);
        }

        private static void DrawRuntimeDefaultsSection()
        {
            // Android Device ID
            bool deviceIdEnabled    = XsollaOfferwallEditorSettings.RuntimeAndroidDeviceIdEnabled;
            bool newDeviceIdEnabled = EditorGUILayout.Toggle(
                new GUIContent(
                    "Android Device ID",
                    "Calls XsollaOfferwall.SetAndroidDeviceIdEnabled before the first scene loads. " +
                    "Android only — ignored on iOS."),
                deviceIdEnabled);

            if (newDeviceIdEnabled != deviceIdEnabled)
                XsollaOfferwallEditorSettings.RuntimeAndroidDeviceIdEnabled = newDeviceIdEnabled;

            // Orientation
            OfferwallOrientation orientation    = XsollaOfferwallEditorSettings.RuntimeOrientation;
            OfferwallOrientation newOrientation = (OfferwallOrientation)EditorGUILayout.EnumPopup(
                new GUIContent(
                    "Orientation",
                    "Sets XsollaOfferwall.Settings.Orientation before the first scene loads."),
                orientation);

            if (newOrientation != orientation)
                XsollaOfferwallEditorSettings.RuntimeOrientation = newOrientation;

            // Log Level
            OfferwallLogLevel logLevel    = XsollaOfferwallEditorSettings.RuntimeLogLevel;
            OfferwallLogLevel newLogLevel = (OfferwallLogLevel)EditorGUILayout.EnumPopup(
                new GUIContent(
                    "Log Level",
                    "Sets XsollaOfferwall.Settings.LogLevel before the first scene loads."),
                logLevel);

            if (newLogLevel != logLevel)
                XsollaOfferwallEditorSettings.RuntimeLogLevel = newLogLevel;
        }

        private static void DrawIOSSection()
        {
            IOSIntegrationMode current = XsollaOfferwallEditorSettings.IOSIntegration;
            IOSIntegrationMode next    = (IOSIntegrationMode)EditorGUILayout.EnumPopup(
                new GUIContent(
                    "Integration",
                    "iOS dependency manager used for XsollaOfferwallSDK. " +
                    "Changing this rewrites XsollaDependencies.xml immediately."),
                current);

            if (next != current)
                XsollaOfferwallEditorSettings.IOSIntegration = next;

            string version    = XsollaOfferwallEditorSettings.IOSVersion;
            string newVersion = EditorGUILayout.DelayedTextField(
                new GUIContent(
                    "SDK Version",
                    "XsollaOfferwallSDK version written to XsollaDependencies.xml. " +
                    "Press Enter or click away to apply."),
                version);

            if (newVersion != version)
                XsollaOfferwallEditorSettings.IOSVersion = newVersion;
        }

        private static void DrawAppSetIdSection()
        {
            bool enabled    = XsollaOfferwallEditorSettings.AppSetIdEnabled;
            bool newEnabled = EditorGUILayout.Toggle(
                new GUIContent(
                    "Enable App Set ID",
                    "Adds com.google.android.gms:play-services-appset to XsollaDependencies.xml"),
                enabled);

            if (newEnabled != enabled)
                XsollaOfferwallEditorSettings.AppSetIdEnabled = newEnabled;

            if (!newEnabled)
                return;

            EditorGUI.indentLevel++;

            string version    = XsollaOfferwallEditorSettings.AppSetIdVersion;
            string newVersion = EditorGUILayout.DelayedTextField(
                new GUIContent(
                    "play-services-appset",
                    "Maven artifact version added to XsollaDependencies.xml. " +
                    "Press Enter or click away to apply."),
                version);

            if (newVersion != version)
                XsollaOfferwallEditorSettings.AppSetIdVersion = newVersion;

            EditorGUI.indentLevel--;
        }
    }
}
