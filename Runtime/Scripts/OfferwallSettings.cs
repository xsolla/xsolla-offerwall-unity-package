using System;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// SDK-wide display and presentation settings. Assign to <see cref="XsollaOfferwall.Settings"/>
    /// before calling Connect. Maps to <c>XOSettings</c> on Android and <c>OfferwallSettings</c> on iOS.
    /// </summary>
    [Serializable]
    public class OfferwallSettings
    {
        /// <summary>
        /// Whether external links open in the device browser.
        /// </summary>
        /// <remarks>
        /// Deprecated and ignored. External links always open in the device browser:
        /// The value is no longer sent to the native SDKs and this field will be removed in v1.0.0.
        /// </remarks>
        [Obsolete("OpenExternalLinksInBrowser is deprecated and ignored. External links always open in the device browser. This field will be removed in v1.0.0.")]
        public bool OpenExternalLinksInBrowser = true;

        /// <summary>
        /// Screen orientation for the offerwall. Defaults to Portrait.
        /// </summary>
        public OfferwallOrientation Orientation = OfferwallOrientation.Portrait;

        /// <summary>
        /// Log verbosity. Null uses the platform default (verbose in debug, warning in release).
        /// </summary>
        public OfferwallLogLevel? LogLevel = null;
    }

    public enum OfferwallOrientation
    {
        Portrait,
        Landscape,
        /// <summary>Follows the device's current orientation.</summary>
        Unspecified,
    }

    public enum OfferwallLogLevel
    {
        Verbose,
        Debug,
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// Privacy and consent settings for ad compliance (GDPR, COPPA, CCPA).
    /// Pass to <see cref="XsollaOfferwall.SetPrivacyPolicy"/>.
    /// All fields are nullable — when null, the value is not sent.
    /// </summary>
    [Serializable]
    public class OfferwallPrivacyPolicy
    {
        /// <summary>Whether GDPR applies. Null means the server determines.</summary>
        public bool? SubjectToGDPR;

        /// <summary>User consent value: "0" (no consent), "1" (consent given), or IAB TCF string.</summary>
        public string UserConsent;

        /// <summary>Whether the user is under 13 (COPPA) or below local consent age (GDPR).</summary>
        public bool? BelowConsentAge;

        /// <summary>IAB US Privacy String (e.g. "1YNN").</summary>
        public string UsPrivacy;

        /// <summary>IAB Global Privacy Platform (GPP) consent string.</summary>
        public string GppString;

        /// <summary>IAB GPP Section ID(s) that apply (e.g. "7_8").</summary>
        public string GppSid;
    }
}
