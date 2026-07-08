using System;
using System.Collections.Generic;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// Main entry point for the Xsolla Offerwall SDK.
    /// Routes to native iOS/Android implementations at runtime.
    /// </summary>
    public static class XsollaOfferwall
    {
        private static IOfferwallNative _native;

        internal static IOfferwallNative Native
        {
            get
            {
                if (_native == null)
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                    _native = new OfferwallNativeAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
                    _native = new OfferwallNativeIOS();
#else
                    _native = new OfferwallNativeStub();
#endif
                }
                return _native;
            }
        }

        /// <summary>
        /// SDK-wide display and presentation settings.
        /// Set individual properties to push each value to the native SDK immediately:
        /// <code>XsollaOfferwall.Settings.Orientation = OfferwallOrientation.Landscape;</code>
        /// Maps to <c>XOSettings</c> on Android and <c>OfferwallSettings</c> on iOS.
        /// </summary>
        public static class Settings
        {
            private static readonly OfferwallSettings _settings = new OfferwallSettings();

            /// <summary>
            /// Whether external links open in the device browser. Defaults to true.
            /// Android only — ignored on iOS.
            /// </summary>
            public static bool OpenExternalLinksInBrowser
            {
                get => Native.GetSettings().OpenExternalLinksInBrowser;
                set { _settings.OpenExternalLinksInBrowser = value; Native.SetSettings(_settings); }
            }

            /// <summary>Screen orientation for the offerwall. Defaults to Portrait.</summary>
            public static OfferwallOrientation Orientation
            {
                get => Native.GetSettings().Orientation;
                set { _settings.Orientation = value; Native.SetSettings(_settings); }
            }

            /// <summary>
            /// Log verbosity. Null uses the platform default (verbose in debug, warning in release).
            /// </summary>
            public static OfferwallLogLevel? LogLevel
            {
                get => Native.GetSettings().LogLevel;
                set { _settings.LogLevel = value; Native.SetSettings(_settings); }
            }

            /// <summary>
            /// Pushes the cached C#-side settings back to the native SDK.
            /// Called after a successful connect so any explicit settings (e.g. LogLevel)
            /// survive SDK initialisation, which may replace the native settings object.
            /// </summary>
            internal static void Reapply() => Native.SetSettings(_settings);
        }

        /// <summary>
        /// Privacy and consent settings (GDPR, COPPA, CCPA).
        /// Set individual properties to push each value to the native SDK immediately:
        /// <code>XsollaOfferwall.PrivacyPolicy.SubjectToGDPR = true;</code>
        /// Maps to <c>XOPrivacyPolicy</c> on Android and <c>OfferwallManager.shared.privacyPolicy</c> on iOS.
        /// </summary>
        public static class PrivacyPolicy
        {
            private static readonly OfferwallPrivacyPolicy _policy = new OfferwallPrivacyPolicy();

            /// <summary>Whether GDPR applies. Null means the server determines.</summary>
            public static bool? SubjectToGDPR
            {
                get => Native.GetPrivacyPolicy().SubjectToGDPR;
                set { _policy.SubjectToGDPR = value; Native.SetPrivacyPolicy(_policy); }
            }

            /// <summary>User consent value: "0" (no consent), "1" (consent given), or IAB TCF string.</summary>
            public static string UserConsent
            {
                get => Native.GetPrivacyPolicy().UserConsent;
                set { _policy.UserConsent = value; Native.SetPrivacyPolicy(_policy); }
            }

            /// <summary>Whether the user is under 13 (COPPA) or below local consent age (GDPR).</summary>
            public static bool? BelowConsentAge
            {
                get => Native.GetPrivacyPolicy().BelowConsentAge;
                set { _policy.BelowConsentAge = value; Native.SetPrivacyPolicy(_policy); }
            }

            /// <summary>IAB US Privacy String (e.g. "1YNN").</summary>
            public static string UsPrivacy
            {
                get => Native.GetPrivacyPolicy().UsPrivacy;
                set { _policy.UsPrivacy = value; Native.SetPrivacyPolicy(_policy); }
            }
        }

        /// <summary>
        /// Connects the SDK, making it ready to show offerwalls.
        /// Call <see cref="SetUserId"/> before connecting.
        /// Must complete successfully before calling <see cref="Show"/>.
        /// </summary>
        /// <param name="onComplete">Called when the connection attempt finishes. Null error means success.</param>
        public static void Connect(Action<XOError> onComplete = null)
        {
            Native.Connect(error =>
            {
                // Re-push cached settings after connect. The SDK may initialise a fresh
                // settings object internally during connect, discarding any pre-connect
                // mutations (e.g. an explicit LogLevel). Reapply ensures the C#-side
                // values survive and are reflected when the UI reads them back.
                if (error == null)
                    Settings.Reapply();

                onComplete?.Invoke(error);
            });
        }

        /// <summary>
        /// Shows the offerwall for the given placement.
        /// </summary>
        /// <param name="placementId">The placement ID from Publisher Account.</param>
        /// <param name="customParams">Optional additional query parameters appended to the offerwall URL.</param>
        /// <param name="onDismissed">Called when the offerwall is dismissed. Null error means success.</param>
        public static void Show(
            string placementId,
            Dictionary<string, string> customParams = null,
            Action<string> onDismissed = null)
        {
            if (string.IsNullOrEmpty(placementId))
                throw new ArgumentException("placementId is required", nameof(placementId));

            Native.Show(placementId, customParams, onDismissed);
        }

        /// <summary>
        /// Sets the player's user ID on the SDK. Can be called before or after <see cref="Connect"/>.
        /// </summary>
        public static void SetUserId(string userId)
        {
            Native.SetUserId(userId);
        }

        /// <summary>
        /// Returns the player's user ID currently set on the SDK.
        /// </summary>
        public static string GetUserId()
        {
            return Native.GetUserId();
        }

        /// <summary>
        /// Sets the publisher user IDs sent as the X-Publisher-User-IDs header with every offerwall request.
        /// Replaces any previously set publisher user IDs. Pass an empty list (or null) to clear.
        /// Can be called before or after <see cref="Connect"/>. Persisted natively between sessions.
        /// </summary>
        public static void SetPublisherUserIds(List<string> publisherUserIds)
        {
            Native.SetPublisherUserIds(publisherUserIds ?? new List<string>());
        }

        /// <summary>
        /// Convenience overload for setting a single publisher user ID.
        /// Passing null or an empty string clears the list.
        /// </summary>
        public static void SetPublisherUserIds(string publisherUserId)
        {
            SetPublisherUserIds(string.IsNullOrEmpty(publisherUserId)
                ? new List<string>()
                : new List<string> { publisherUserId });
        }

        /// <summary>
        /// Returns the publisher user IDs currently set on the SDK. Always non-null; empty if none are set.
        /// </summary>
        public static List<string> GetPublisherUserIds()
        {
            return Native.GetPublisherUserIds() ?? new List<string>();
        }

        /// <summary>
        /// Returns the SDK version string.
        /// If the Unity package version matches the native SDK version, returns the version directly.
        /// If they differ, returns a combined string e.g. "Unity-0.3.0-alpha1,Android-0.2.0".
        /// In the Editor the Unity version is always returned.
        /// </summary>
        public static string GetVersion()
        {
            var nativeVersion = Native.GetNativeVersion();
            if (nativeVersion == XsollaOfferwallVersion.UnityVersion)
                return nativeVersion;

#if UNITY_ANDROID && !UNITY_EDITOR
            return $"Unity-{XsollaOfferwallVersion.UnityVersion},Android-{nativeVersion}";
#elif UNITY_IOS && !UNITY_EDITOR
            return $"Unity-{XsollaOfferwallVersion.UnityVersion},iOS-{nativeVersion}";
#else
            return XsollaOfferwallVersion.UnityVersion;
#endif
        }

        /// <summary>
        /// Sets whether the Android Device ID is included in offerwall requests.
        /// The setting is persisted across app launches via SharedPreferences.
        /// This is an Android-only feature; calling it on iOS or in the Editor is a safe no-op.
        /// </summary>
        public static void SetAndroidDeviceIdEnabled(bool enabled)
        {
            Native.SetAndroidDeviceIdEnabled(enabled);
        }

        /// <summary>
        /// Returns whether the Android Device ID is currently enabled for offerwall requests.
        /// Defaults to true on Android when the setting has never been explicitly changed.
        /// Always returns false on iOS and in the Editor.
        /// </summary>
        public static bool IsAndroidDeviceIdEnabled()
        {
            return Native.IsAndroidDeviceIdEnabled();
        }
    }
}
