using System;
using System.Collections.Generic;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// Interface for platform-specific offerwall implementations.
    /// </summary>
    internal interface IOfferwallNative
    {
        void SetSettings(OfferwallSettings settings);
        OfferwallSettings GetSettings();
        void Connect(Action<XOError> onComplete);

        /// <summary>
        /// Shows the offerwall for the given placement.
        /// </summary>
        /// <param name="onOpened">Called once the offerwall has been presented on screen.</param>
        /// <param name="onError">
        /// Called if the offerwall could not be shown. When this fires, neither
        /// <paramref name="onOpened"/> nor <paramref name="onClosed"/> will fire for this call.
        /// </param>
        /// <param name="onClosed">Called when the offerwall has been dismissed, after having been successfully opened.</param>
        void Show(
            string placementId,
            Dictionary<string, string> customParams,
            Action onOpened,
            Action<XOError> onError,
            Action onClosed);

        void SetUserId(string userId);
        string GetUserId();

        /// <summary>
        /// Sets the publisher user IDs sent as the X-Publisher-User-IDs header with every offerwall request.
        /// Pass an empty list to clear.
        /// </summary>
        void SetPublisherUserIds(List<string> publisherUserIds);

        /// <summary>
        /// Returns the publisher user IDs currently set on the SDK. Always non-null; empty if none are set.
        /// </summary>
        List<string> GetPublisherUserIds();

        void SetPrivacyPolicy(OfferwallPrivacyPolicy privacyPolicy);
        OfferwallPrivacyPolicy GetPrivacyPolicy();

        /// <summary>
        /// Returns the version string reported by the native SDK.
        /// Stub and Editor implementations return the Unity SDK version.
        /// </summary>
        string GetNativeVersion();

        /// <summary>
        /// Sets whether the Android Device ID is included in offerwall requests.
        /// Android only — implementations on other platforms should be a no-op.
        /// </summary>
        void SetAndroidDeviceIdEnabled(bool enabled);

        /// <summary>
        /// Returns whether the Android Device ID is currently enabled for offerwall requests.
        /// Android only — implementations on other platforms should return false.
        /// </summary>
        bool IsAndroidDeviceIdEnabled();
    }
}
