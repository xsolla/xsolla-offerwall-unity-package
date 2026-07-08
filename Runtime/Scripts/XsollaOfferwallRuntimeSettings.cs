using UnityEngine;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// Baked runtime defaults for the Xsolla Offerwall SDK.
    ///
    /// This asset is generated and updated automatically by the
    /// <c>Window &gt; Xsolla Offerwall &gt; Settings</c> editor window. Do not edit by
    /// hand — changes will be overwritten on the next domain reload.
    ///
    /// At runtime these values are applied via
    /// <see cref="XsollaOfferwallRuntimeInit"/> before the first scene loads,
    /// giving them lower priority than any explicit calls made later (e.g. from
    /// game code or a settings UI).
    /// </summary>
    public class XsollaOfferwallRuntimeSettings : ScriptableObject
    {
        /// <summary>
        /// Whether the Android hardware Device ID is included in offerwall requests.
        /// Android only — a no-op on iOS.
        /// </summary>
        public bool androidDeviceIdEnabled = false;

        /// <summary>Screen orientation used when the offerwall is displayed.</summary>
        public OfferwallOrientation orientation = OfferwallOrientation.Portrait;

        /// <summary>SDK log verbosity.</summary>
        public OfferwallLogLevel logLevel = OfferwallLogLevel.Info;
    }
}
