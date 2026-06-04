using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// Stub implementation for Editor and unsupported platforms.
    /// Logs a warning — offerwall only works on iOS and Android.
    /// </summary>
    internal class OfferwallNativeStub : IOfferwallNative
    {
        public OfferwallSettings GetSettings() => new OfferwallSettings();

        public void SetSettings(OfferwallSettings settings)
        {
            // No-op in the Editor and on unsupported platforms.
        }

        public void Connect(Action<XOError> onComplete)
        {
            Debug.LogWarning("[XsollaOfferwall] Connect is only supported on iOS and Android. " +
                             "This call is a no-op in the Editor and on other platforms.");
            onComplete?.Invoke(null);
        }

        public void Show(string placementId, Dictionary<string, string> customParams, Action<string> onDismissed)
        {
            Debug.LogWarning("[XsollaOfferwall] Offerwall is only supported on iOS and Android. " +
                             "This call is a no-op in the Editor and on other platforms.");
            onDismissed?.Invoke(null);
        }

        public void SetUserId(string userId)
        {
            // No-op in the Editor and on unsupported platforms.
        }

        public string GetUserId() => null;

        public void SetPrivacyPolicy(OfferwallPrivacyPolicy privacyPolicy)
        {
            // No-op in the Editor and on unsupported platforms.
        }

        public OfferwallPrivacyPolicy GetPrivacyPolicy() => new OfferwallPrivacyPolicy();

        public void Dismiss()
        {
            Debug.LogWarning("[XsollaOfferwall] Dismiss is only supported on iOS and Android.");
        }

        public void SetAndroidDeviceIdEnabled(bool enabled)
        {
            // Android-only feature — no-op in the Editor and on unsupported platforms.
        }

        public bool IsAndroidDeviceIdEnabled()
        {
            // Android-only feature — always returns false in the Editor and on unsupported platforms.
            return false;
        }
    }
}
