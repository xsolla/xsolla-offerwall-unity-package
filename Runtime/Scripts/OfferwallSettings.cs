using System;
using System.Collections.Generic;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// Configuration for displaying the offerwall.
    /// </summary>
    [Serializable]
    public class OfferwallSettings
    {
        /// <summary>
        /// The placement ID from Publisher Account. Required.
        /// </summary>
        public string PlacementId;

        /// <summary>
        /// The player's gamer ID. Required.
        /// </summary>
        public string UserId;

        /// <summary>
        /// Additional query parameters appended to the offerwall URL.
        /// </summary>
        public Dictionary<string, string> CustomParameters;

        /// <summary>
        /// Privacy and consent settings for ad compliance.
        /// </summary>
        public OfferwallPrivacyPolicy PrivacyPolicy;

        public OfferwallSettings(string placementId, string userId)
        {
            PlacementId = placementId;
            UserId = userId;
        }
    }

    /// <summary>
    /// Privacy and consent settings for ad compliance (GDPR, COPPA, CCPA).
    /// All fields are nullable. When null, the value is not sent.
    /// </summary>
    [Serializable]
    public class OfferwallPrivacyPolicy
    {
        /// <summary>
        /// Whether GDPR applies. Null means the server determines.
        /// </summary>
        public bool? SubjectToGDPR;

        /// <summary>
        /// User consent value: "0" (no consent), "1" (consent given), or IAB TCF string.
        /// </summary>
        public string UserConsent;

        /// <summary>
        /// Whether the user is under 13 (COPPA) or below local consent age (GDPR).
        /// </summary>
        public bool? BelowConsentAge;

        /// <summary>
        /// IAB US Privacy String (e.g. "1YNN").
        /// </summary>
        public string UsPrivacy;
    }
}
