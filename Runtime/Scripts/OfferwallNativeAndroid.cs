#if UNITY_ANDROID
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// Android implementation. Calls XsollaOfferwallSdk via AndroidJavaObject.
    /// Uses the XsollaOfferwallSDK AAR bundled in Plugins/Android.
    /// </summary>
    internal class OfferwallNativeAndroid : IOfferwallNative
    {
        private const string Tag = "XsollaOfferwall.Android";
        private const string SdkClass = "com.xsolla.offerwallsdk.XsollaOfferwallSdk";
        private const string XOPrivacyPolicyClass = "com.xsolla.offerwallsdk.XOPrivacyPolicy";

        public void SetSettings(OfferwallSettings settings)
        {
            try
            {
                // settings is a private val on the SDK — mutate the existing instance in place
                using var sdkClass = new AndroidJavaClass(SdkClass);
                using var sdk = sdkClass.GetStatic<AndroidJavaObject>("INSTANCE");
                using var xoSettings = sdk.Call<AndroidJavaObject>("getSettings");

                xoSettings.Call("setOpenExternalLinksInBrowser", settings.OpenExternalLinksInBrowser);

                // Orientation
                using var orientationClass = new AndroidJavaClass("com.xsolla.offerwallsdk.XOSettings$Orientation");
                var orientationName = settings.Orientation switch
                {
                    OfferwallOrientation.Landscape   => "Landscape",
                    OfferwallOrientation.Unspecified => "Unspecified",
                    _                                => "Portrait",
                };
                using var orientation = orientationClass.GetStatic<AndroidJavaObject>(orientationName);
                xoSettings.Call("setOrientation", orientation);

                // LogLevel — always call setLogLevel so the SDK Logger reflects the intent:
                // a non-null value sets the explicit level; null resets Logger to its internal
                // default (ERROR), undoing any level set earlier in the same session.
                using var logLevelClass = new AndroidJavaClass("com.xsolla.offerwallsdk.util.LogLevel");
                AndroidJavaObject logLevel = null;
                if (settings.LogLevel.HasValue)
                {
                    var logLevelName = settings.LogLevel.Value switch
                    {
                        OfferwallLogLevel.Verbose => "VERBOSE",
                        OfferwallLogLevel.Debug   => "DEBUG",
                        OfferwallLogLevel.Info    => "INFO",
                        OfferwallLogLevel.Warning => "WARN",
                        OfferwallLogLevel.Error   => "ERROR",
                        _                         => "INFO",
                    };
                    logLevel = logLevelClass.GetStatic<AndroidJavaObject>(logLevelName);
                }
                xoSettings.Call("setLogLevel", logLevel);
                logLevel?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] SetSettings failed: {e}");
            }
        }

        public OfferwallSettings GetSettings()
        {
            try
            {
                using var sdkClass = new AndroidJavaClass(SdkClass);
                using var sdk = sdkClass.GetStatic<AndroidJavaObject>("INSTANCE");
                using var xoSettings = sdk.Call<AndroidJavaObject>("getSettings");

                var result = new OfferwallSettings
                {
                    OpenExternalLinksInBrowser = xoSettings.Call<bool>("getOpenExternalLinksInBrowser"),
                };

                using var orientation = xoSettings.Call<AndroidJavaObject>("getOrientation");
                result.Orientation = orientation.Call<string>("name") switch
                {
                    "Landscape"   => OfferwallOrientation.Landscape,
                    "Unspecified" => OfferwallOrientation.Unspecified,
                    _             => OfferwallOrientation.Portrait,
                };

                using var logLevel = xoSettings.Call<AndroidJavaObject>("getLogLevel");
                result.LogLevel = logLevel == null ? null : logLevel.Call<string>("name") switch
                {
                    "VERBOSE" => OfferwallLogLevel.Verbose,
                    "DEBUG"   => OfferwallLogLevel.Debug,
                    "INFO"    => OfferwallLogLevel.Info,
                    "WARN"    => OfferwallLogLevel.Warning,
                    "ERROR"   => OfferwallLogLevel.Error,
                    _         => null,
                };

                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] GetSettings failed: {e}");
                return new OfferwallSettings();
            }
        }

        public void Connect(Action<XOError> onComplete)
        {
            try
            {
                using var activity = GetCurrentActivity();
                using var context = activity.Call<AndroidJavaObject>("getApplicationContext");
                using var sdkClass = new AndroidJavaClass(SdkClass);
                using var sdk = sdkClass.GetStatic<AndroidJavaObject>("INSTANCE");
                sdk.Call("connect", context, new ConnectCallback(onComplete));
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] Connect failed: {e}");
                onComplete?.Invoke(new XOError(e.Message));
            }
        }

        public void Show(string placementId, Dictionary<string, string> customParams, Action<string> onDismissed)
        {
            try
            {
                var activity = GetCurrentActivity();

                // Build a java.util.HashMap from customParams if provided
                AndroidJavaObject nativeMap = null;
                if (customParams != null && customParams.Count > 0)
                {
                    nativeMap = new AndroidJavaObject("java.util.HashMap");
                    foreach (var kvp in customParams)
                        nativeMap.Call<AndroidJavaObject>("put", kvp.Key, kvp.Value);
                }

                // Obtain the XsollaOfferwallSdk singleton (Kotlin object → INSTANCE field)
                using var sdkClass = new AndroidJavaClass(SdkClass);
                using var sdk = sdkClass.GetStatic<AndroidJavaObject>("INSTANCE");

                sdk.Call("show", activity, placementId, nativeMap, new OpenCallback(onDismissed));
                nativeMap?.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] Show failed: {e}");
                onDismissed?.Invoke(e.Message);
            }
        }

        public void SetPrivacyPolicy(OfferwallPrivacyPolicy pp)
        {
            try
            {
                ApplyPrivacyPolicy(pp);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] SetPrivacyPolicy failed: {e}");
            }
        }

        public void SetUserId(string userId)
        {
            try
            {
                using var sdkClass = new AndroidJavaClass(SdkClass);
                using var sdk = sdkClass.GetStatic<AndroidJavaObject>("INSTANCE");
                sdk.Call("setUserId", userId);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] SetUserId failed: {e}");
            }
        }

        public string GetUserId()
        {
            try
            {
                using var sdkClass = new AndroidJavaClass(SdkClass);
                using var sdk = sdkClass.GetStatic<AndroidJavaObject>("INSTANCE");
                return sdk.Call<string>("getUserId");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] GetUserId failed: {e}");
                return null;
            }
        }

        public void SetPublisherUserIds(List<string> publisherUserIds)
        {
            try
            {
                using var sdkClass = new AndroidJavaClass(SdkClass);
                using var sdk = sdkClass.GetStatic<AndroidJavaObject>("INSTANCE");
                using var list = new AndroidJavaObject("java.util.ArrayList");
                if (publisherUserIds != null)
                {
                    // Kotlin's setPublisherUserIds(List<String>) calls isNotBlank() on each
                    // entry; a smuggled-in null (JNI bypasses Kotlin's null-safety) would NPE.
                    foreach (var id in publisherUserIds)
                        if (id != null)
                            list.Call<bool>("add", id);
                }
                sdk.Call("setPublisherUserIds", list);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] SetPublisherUserIds failed: {e}");
            }
        }

        public List<string> GetPublisherUserIds()
        {
            try
            {
                using var sdkClass = new AndroidJavaClass(SdkClass);
                using var sdk = sdkClass.GetStatic<AndroidJavaObject>("INSTANCE");
                using var list = sdk.Call<AndroidJavaObject>("getPublisherUserIds");
                int count = list.Call<int>("size");
                var result = new List<string>(count);
                for (int i = 0; i < count; i++)
                    result.Add(list.Call<string>("get", i));
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] GetPublisherUserIds failed: {e}");
                return new List<string>();
            }
        }

        public OfferwallPrivacyPolicy GetPrivacyPolicy()
        {
            try
            {
                using var ppClass = new AndroidJavaClass(XOPrivacyPolicyClass);
                using var pp = ppClass.GetStatic<AndroidJavaObject>("INSTANCE");

                using var gdpr = pp.Call<AndroidJavaObject>("getSubjectToGDPR");
                using var belowAge = pp.Call<AndroidJavaObject>("getBelowConsentAge");

                return new OfferwallPrivacyPolicy
                {
                    SubjectToGDPR   = gdpr     != null ? gdpr.Call<bool>("booleanValue")     : (bool?)null,
                    BelowConsentAge = belowAge != null ? belowAge.Call<bool>("booleanValue") : (bool?)null,
                    UserConsent     = pp.Call<string>("getUserConsent"),
                    UsPrivacy       = pp.Call<string>("getUsPrivacy"),
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] GetPrivacyPolicy failed: {e}");
                return new OfferwallPrivacyPolicy();
            }
        }

        public string GetNativeVersion()
        {
            try
            {
                using var sdkClass = new AndroidJavaClass(SdkClass);
                using var sdk = sdkClass.GetStatic<AndroidJavaObject>("INSTANCE");
                return sdk.Call<string>("getVersion");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] GetNativeVersion failed: {e}");
                return "unknown";
            }
        }

        public void SetAndroidDeviceIdEnabled(bool enabled)
        {
            try
            {
                using var activity = GetCurrentActivity();
                using var context = activity.Call<AndroidJavaObject>("getApplicationContext");
                using var sdkClass = new AndroidJavaClass(SdkClass);
                using var sdk = sdkClass.GetStatic<AndroidJavaObject>("INSTANCE");
                sdk.Call("setAndroidDeviceIdEnabled", context, enabled);
                Debug.Log($"[{Tag}] SetAndroidDeviceIdEnabled({enabled})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] SetAndroidDeviceIdEnabled failed: {e}");
            }
        }

        public bool IsAndroidDeviceIdEnabled()
        {
            try
            {
                using var activity = GetCurrentActivity();
                using var context = activity.Call<AndroidJavaObject>("getApplicationContext");
                using var sdkClass = new AndroidJavaClass(SdkClass);
                using var sdk = sdkClass.GetStatic<AndroidJavaObject>("INSTANCE");
                return sdk.Call<bool>("isAndroidDeviceIdEnabled", context);
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] IsAndroidDeviceIdEnabled failed: {e}");
                return false;
            }
        }

        private static AndroidJavaObject GetCurrentActivity()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        /// <summary>
        /// Applies privacy policy values to the XOPrivacyPolicy singleton.
        /// XOPrivacyPolicy is a Kotlin object; its var properties are set via Java setters.
        /// Nullable Boolean fields require a boxed java.lang.Boolean or null.
        /// </summary>
        private static void ApplyPrivacyPolicy(OfferwallPrivacyPolicy pp)
        {
            using var ppClass = new AndroidJavaClass(XOPrivacyPolicyClass);
            using var ppInstance = ppClass.GetStatic<AndroidJavaObject>("INSTANCE");

            // Nullable Boolean → boxed java.lang.Boolean or null
            AndroidJavaObject gdpr = pp.SubjectToGDPR.HasValue
                ? new AndroidJavaObject("java.lang.Boolean", pp.SubjectToGDPR.Value)
                : null;
            AndroidJavaObject belowAge = pp.BelowConsentAge.HasValue
                ? new AndroidJavaObject("java.lang.Boolean", pp.BelowConsentAge.Value)
                : null;

            ppInstance.Call("setSubjectToGDPR", gdpr);
            ppInstance.Call("setUserConsent", pp.UserConsent);
            ppInstance.Call("setBelowConsentAge", belowAge);
            ppInstance.Call("setUsPrivacy", pp.UsPrivacy);

            gdpr?.Dispose();
            belowAge?.Dispose();
        }

        /// <summary>
        /// Callback proxy for XsollaOfferwallSdk.connect().
        /// Reports success or failure back to C# via <see cref="XOError"/>.
        /// </summary>
        private class ConnectCallback : AndroidJavaProxy
        {
            private readonly Action<XOError> _onComplete;

            public ConnectCallback(Action<XOError> onComplete)
                : base("com.xsolla.offerwallsdk.IOfferwallClientConnectCallback")
            {
                _onComplete = onComplete;
            }

            // Called from Java: void onSuccess()
            void onSuccess()
            {
                MainThreadDispatcher.Enqueue(() => _onComplete?.Invoke(null));
            }

            // Called from Java: void onFailure(XOError error)
            void onFailure(AndroidJavaObject error)
            {
                var message = error?.Call<string>("getMessage") ?? "Connect failed";
                Debug.LogError($"[{Tag}] Connect failed: {message}");
                MainThreadDispatcher.Enqueue(() => _onComplete?.Invoke(new XOError(message)));
            }
        }

        /// <summary>
        /// Callback proxy for XsollaOfferwallSdk.show().
        /// </summary>
        private class OpenCallback : AndroidJavaProxy
        {
            private readonly Action<string> _onDismissed;

            public OpenCallback(Action<string> onDismissed)
                : base("com.xsolla.offerwallsdk.IOfferwallOpenCallback")
            {
                _onDismissed = onDismissed;
            }

            // Called from Java: void onOpened()
            void onOpened()
            {
                Debug.Log($"[{Tag}] Offerwall opened.");
            }

            // Called from Java: void onError(XOError error)
            void onError(AndroidJavaObject error)
            {
                var message = error?.Call<string>("getMessage") ?? "Offerwall error";
                Debug.LogError($"[{Tag}] Offerwall error: {message}");
                MainThreadDispatcher.Enqueue(() => _onDismissed?.Invoke(message));
            }

            // Called from Java: void onClosed()
            void onClosed()
            {
                MainThreadDispatcher.Enqueue(() => _onDismissed?.Invoke(null));
            }
        }
    }
}
#endif
