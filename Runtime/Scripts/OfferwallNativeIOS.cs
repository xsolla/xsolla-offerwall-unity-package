#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// iOS implementation. Calls XMOfferwallManager via Objective-C bridge.
    /// </summary>
    internal class OfferwallNativeIOS : IOfferwallNative
    {
        private const string Tag = "XsollaOfferwall.iOS";

        private static Action<string> _pendingCallback;

        [DllImport("__Internal")]
        private static extern void _XsollaOfferwall_Show(
            string placementId,
            string userId,
            string customParamsJson,
            string privacyPolicyJson,
            DismissCallbackDelegate callback
        );

        [DllImport("__Internal")]
        private static extern void _XsollaOfferwall_Dismiss();

        private delegate void DismissCallbackDelegate(string error);

        [MonoPInvokeCallback(typeof(DismissCallbackDelegate))]
        private static void OnDismissed(string error)
        {
            var callback = _pendingCallback;
            _pendingCallback = null;
            MainThreadDispatcher.Enqueue(() => callback?.Invoke(error));
        }

        public void Show(OfferwallSettings settings, Action<string> onDismissed)
        {
            _pendingCallback = onDismissed;

            string customParamsJson = null;
            if (settings.CustomParameters != null && settings.CustomParameters.Count > 0)
            {
                customParamsJson = DictToJson(settings.CustomParameters);
            }

            string privacyJson = null;
            if (settings.PrivacyPolicy != null)
            {
                privacyJson = PrivacyPolicyToJson(settings.PrivacyPolicy);
            }

            _XsollaOfferwall_Show(
                settings.PlacementId,
                settings.UserId,
                customParamsJson,
                privacyJson,
                OnDismissed
            );
        }

        public void Dismiss()
        {
            _XsollaOfferwall_Dismiss();
        }

        private static string DictToJson(Dictionary<string, string> dict)
        {
            // Simple JSON serialization without external dependency
            var parts = new List<string>();
            foreach (var kvp in dict)
            {
                parts.Add($"\"{EscapeJson(kvp.Key)}\":\"{EscapeJson(kvp.Value)}\"");
            }
            return "{" + string.Join(",", parts) + "}";
        }

        private static string PrivacyPolicyToJson(OfferwallPrivacyPolicy pp)
        {
            var parts = new List<string>();
            if (pp.SubjectToGDPR.HasValue)
                parts.Add($"\"subjectToGDPR\":{(pp.SubjectToGDPR.Value ? "true" : "false")}");
            if (pp.UserConsent != null)
                parts.Add($"\"userConsent\":\"{EscapeJson(pp.UserConsent)}\"");
            if (pp.BelowConsentAge.HasValue)
                parts.Add($"\"belowConsentAge\":{(pp.BelowConsentAge.Value ? "true" : "false")}");
            if (pp.UsPrivacy != null)
                parts.Add($"\"usPrivacy\":\"{EscapeJson(pp.UsPrivacy)}\"");
            return "{" + string.Join(",", parts) + "}";
        }

        private static string EscapeJson(string s)
        {
            return s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
        }
    }
}
#endif
