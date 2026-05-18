using System;
using UnityEngine;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// Main entry point for the Xsolla Offerwall SDK.
    /// Routes to native iOS/Android implementations at runtime.
    /// </summary>
    public static class XsollaOfferwall
    {
        private static IOfferwallNative _native;

        private static IOfferwallNative Native
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
        /// Shows the offerwall with the specified settings.
        /// </summary>
        /// <param name="settings">Offerwall configuration.</param>
        /// <param name="onDismissed">Called when the offerwall is dismissed. Null error means success.</param>
        public static void Show(OfferwallSettings settings, Action<string> onDismissed = null)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrEmpty(settings.PlacementId))
                throw new ArgumentException("PlacementId is required", nameof(settings));
            if (string.IsNullOrEmpty(settings.UserId))
                throw new ArgumentException("UserId is required", nameof(settings));

            Native.Show(settings, onDismissed);
        }

        /// <summary>
        /// Dismisses the currently displayed offerwall, if any.
        /// </summary>
        public static void Dismiss()
        {
            Native.Dismiss();
        }
    }
}
