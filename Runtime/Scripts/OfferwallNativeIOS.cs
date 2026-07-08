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
        private static Action<XOError> _pendingConnectCallback;

        [DllImport("__Internal")]
        private static extern void _XsollaOfferwall_Connect(ConnectCallbackDelegate callback);

        [DllImport("__Internal")]
        private static extern void _XsollaOfferwall_Show(
            string placementId,
            string customParamsJson,
            DismissCallbackDelegate callback
        );

        [DllImport("__Internal")]
        private static extern void _XsollaOfferwall_SetPrivacyPolicy(
            int subjectToGDPR,
            string userConsent,
            int belowConsentAge,
            string usPrivacy
        );

        [DllImport("__Internal")]
        private static extern void _XsollaOfferwall_GetSettings(out int orientation, out int logLevel);

        [DllImport("__Internal")]
        private static extern void _XsollaOfferwall_SetSettings(int orientation, int logLevel);

        [DllImport("__Internal")]
        private static extern IntPtr _XsollaOfferwall_GetUserId();

        [DllImport("__Internal")]
        private static extern void _XsollaOfferwall_GetPrivacyPolicy(
            out int subjectToGDPR,
            out IntPtr userConsent,
            out int belowConsentAge,
            out IntPtr usPrivacy
        );

        [DllImport("__Internal")]
        private static extern void _XsollaOfferwall_SetUserId(string userId);

        [DllImport("__Internal")]
        private static extern void _XsollaOfferwall_SetPublisherUserIds(string idsJson);

        [DllImport("__Internal")]
        private static extern IntPtr _XsollaOfferwall_GetPublisherUserIds();

        [DllImport("__Internal")]
        private static extern IntPtr _XsollaOfferwall_GetVersion();

        private delegate void ConnectCallbackDelegate(string error);
        private delegate void DismissCallbackDelegate(string error);

        [MonoPInvokeCallback(typeof(ConnectCallbackDelegate))]
        private static void OnConnected(string error)
        {
            var callback = _pendingConnectCallback;
            _pendingConnectCallback = null;
            XOError xoError = error != null ? new XOError(error) : null;
            MainThreadDispatcher.Enqueue(() => callback?.Invoke(xoError));
        }

        [MonoPInvokeCallback(typeof(DismissCallbackDelegate))]
        private static void OnDismissed(string error)
        {
            var callback = _pendingCallback;
            _pendingCallback = null;
            MainThreadDispatcher.Enqueue(() => callback?.Invoke(error));
        }

        public OfferwallSettings GetSettings()
        {
            _XsollaOfferwall_GetSettings(out int orientation, out int logLevel);

            return new OfferwallSettings
            {
                Orientation = orientation switch
                {
                    1 => OfferwallOrientation.Landscape,
                    2 => OfferwallOrientation.Unspecified,
                    _ => OfferwallOrientation.Portrait,
                },
                LogLevel = logLevel switch
                {
                    0 => OfferwallLogLevel.Verbose,
                    2 => OfferwallLogLevel.Debug,
                    3 => OfferwallLogLevel.Info,
                    5 => OfferwallLogLevel.Warning,
                    6 => OfferwallLogLevel.Error,
                    _ => null,
                },
                // openExternalLinksInBrowser not readable from iOS — return cached value
                OpenExternalLinksInBrowser = true,
            };
        }

        public void SetSettings(OfferwallSettings settings)
        {
            int orientation = settings.Orientation switch
            {
                OfferwallOrientation.Landscape   => 1,
                OfferwallOrientation.Unspecified => 2,
                _                                => 0,
            };

            // Map to XMOLogLevel raw values: verbose=0, debug=2, info=3, warning=5, error=6
            int logLevel = settings.LogLevel.HasValue ? settings.LogLevel.Value switch
            {
                OfferwallLogLevel.Verbose => 0,
                OfferwallLogLevel.Debug   => 2,
                OfferwallLogLevel.Info    => 3,
                OfferwallLogLevel.Warning => 5,
                OfferwallLogLevel.Error   => 6,
                _                         => -1,
            } : -1;

            _XsollaOfferwall_SetSettings(orientation, logLevel);
        }

        public void Connect(Action<XOError> onComplete)
        {
            _pendingConnectCallback = onComplete;
            _XsollaOfferwall_Connect(OnConnected);
        }

        public void Show(string placementId, Dictionary<string, string> customParams, Action<string> onDismissed)
        {
            _pendingCallback = onDismissed;

            string customParamsJson = null;
            if (customParams != null && customParams.Count > 0)
                customParamsJson = DictToJson(customParams);

            _XsollaOfferwall_Show(placementId, customParamsJson, OnDismissed);
        }

        public void SetUserId(string userId)
        {
            _XsollaOfferwall_SetUserId(userId);
        }

        public string GetUserId()
        {
            var ptr = _XsollaOfferwall_GetUserId();
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : null;
        }

        // Wraps a bare string array so it round-trips through JsonUtility, which cannot
        // (de)serialize a top-level JSON array on its own.
        [Serializable]
        private class StringListWrapper
        {
            public string[] ids;
        }

        public void SetPublisherUserIds(List<string> publisherUserIds)
        {
            var wrapper = new StringListWrapper { ids = publisherUserIds?.ToArray() ?? Array.Empty<string>() };
            _XsollaOfferwall_SetPublisherUserIds(JsonUtility.ToJson(wrapper));
        }

        public List<string> GetPublisherUserIds()
        {
            var ptr = _XsollaOfferwall_GetPublisherUserIds();
            if (ptr == IntPtr.Zero) return new List<string>();

            var json = Marshal.PtrToStringUTF8(ptr);
            if (string.IsNullOrEmpty(json)) return new List<string>();

            var wrapper = JsonUtility.FromJson<StringListWrapper>(json);
            return wrapper?.ids != null ? new List<string>(wrapper.ids) : new List<string>();
        }

        public OfferwallPrivacyPolicy GetPrivacyPolicy()
        {
            _XsollaOfferwall_GetPrivacyPolicy(
                out int gdprInt, out IntPtr userConsentPtr,
                out int belowAgeInt, out IntPtr usPrivacyPtr);

            return new OfferwallPrivacyPolicy
            {
                SubjectToGDPR   = gdprInt     >= 0 ? gdprInt     == 1 : (bool?)null,
                BelowConsentAge = belowAgeInt >= 0 ? belowAgeInt == 1 : (bool?)null,
                UserConsent     = userConsentPtr != IntPtr.Zero ? Marshal.PtrToStringUTF8(userConsentPtr) : null,
                UsPrivacy       = usPrivacyPtr   != IntPtr.Zero ? Marshal.PtrToStringUTF8(usPrivacyPtr)   : null,
            };
        }

        public void SetPrivacyPolicy(OfferwallPrivacyPolicy pp)
        {
            _XsollaOfferwall_SetPrivacyPolicy(
                pp.SubjectToGDPR.HasValue  ? (pp.SubjectToGDPR.Value  ? 1 : 0) : -1,
                pp.UserConsent,
                pp.BelowConsentAge.HasValue ? (pp.BelowConsentAge.Value ? 1 : 0) : -1,
                pp.UsPrivacy
            );
        }

        public string GetNativeVersion()
        {
            var ptr = _XsollaOfferwall_GetVersion();
            return ptr != IntPtr.Zero ? Marshal.PtrToStringUTF8(ptr) : "unknown";
        }

        public void SetAndroidDeviceIdEnabled(bool enabled)
        {
            // Android-only feature — no-op on iOS.
        }

        public bool IsAndroidDeviceIdEnabled()
        {
            // Android-only feature — always returns false on iOS.
            return false;
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

        private static string EscapeJson(string s)
        {
            return s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
        }
    }
}
#endif
