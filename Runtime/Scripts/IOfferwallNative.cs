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
        void Show(string placementId, Dictionary<string, string> customParams, Action<string> onDismissed);
        void SetUserId(string userId);
        string GetUserId();
        void SetPrivacyPolicy(OfferwallPrivacyPolicy privacyPolicy);
        OfferwallPrivacyPolicy GetPrivacyPolicy();
        void Dismiss();

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
