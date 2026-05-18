using System;
using UnityEngine;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// Stub implementation for Editor and unsupported platforms.
    /// Logs a warning — offerwall only works on iOS and Android.
    /// </summary>
    internal class OfferwallNativeStub : IOfferwallNative
    {
        public void Show(OfferwallSettings settings, Action<string> onDismissed)
        {
            Debug.LogWarning("[XsollaOfferwall] Offerwall is only supported on iOS and Android. " +
                             "This call is a no-op in the Editor and on other platforms.");
            onDismissed?.Invoke(null);
        }

        public void Dismiss()
        {
            Debug.LogWarning("[XsollaOfferwall] Dismiss is only supported on iOS and Android.");
        }
    }
}
