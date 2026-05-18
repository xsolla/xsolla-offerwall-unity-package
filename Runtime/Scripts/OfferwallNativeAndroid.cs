#if UNITY_ANDROID
using System;
using UnityEngine;

namespace Xsolla.Offerwall
{
    /// <summary>
    /// Android implementation. Calls OfferwallManager via AndroidJavaObject.
    /// Uses the XsollaOfferwallSDK AAR bundled in Plugins/Android.
    /// </summary>
    internal class OfferwallNativeAndroid : IOfferwallNative
    {
        private const string Tag = "XsollaOfferwall.Android";
        private const string SettingsClass = "com.xsolla.android.mobile.offerwall.OfferwallClientSettings";
        private const string SettingsBuilderClass = "com.xsolla.android.mobile.offerwall.OfferwallClientSettings$Builder";
        private const string ClientClass = "com.xsolla.android.mobile.offerwall.OfferwallClient";
        private const string ClientBuilderClass = "com.xsolla.android.mobile.offerwall.OfferwallClient$Builder";
        private const string PrivacyPolicyClass = "com.xsolla.android.mobile.offerwall.OfferwallClientSettings$PrivacyPolicy";

        private AndroidJavaObject _client;

        public void Show(OfferwallSettings settings, Action<string> onDismissed)
        {
            try
            {
                var activity = GetCurrentActivity();

                // Build OfferwallClientSettings
                using var settingsBuilder = new AndroidJavaObject(SettingsBuilderClass);
                settingsBuilder.Call<AndroidJavaObject>("withPlacementId", settings.PlacementId);
                settingsBuilder.Call<AndroidJavaObject>("withUserId", settings.UserId);

                // Privacy policy
                if (settings.PrivacyPolicy != null)
                {
                    var pp = settings.PrivacyPolicy;
                    using var privacyArgs = BuildPrivacyPolicyArgs(pp);
                    if (privacyArgs != null)
                    {
                        settingsBuilder.Call<AndroidJavaObject>("withPrivacyPolicy", privacyArgs);
                    }
                }

                using var nativeSettings = settingsBuilder.Call<AndroidJavaObject>("build");

                // Build OfferwallClient
                using var clientBuilder = new AndroidJavaObject(ClientBuilderClass);
                clientBuilder.Call<AndroidJavaObject>("withSettings", nativeSettings);
                _client = clientBuilder.Call<AndroidJavaObject>("build");

                // Connect
                _client.Call("connect", new ConnectCallback(activity, _client, settings, onDismissed));
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] Show failed: {e}");
                onDismissed?.Invoke(e.Message);
            }
        }

        public void Dismiss()
        {
            try
            {
                _client?.Call("shutdown");
                _client = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"[{Tag}] Dismiss failed: {e}");
            }
        }

        private static AndroidJavaObject GetCurrentActivity()
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        }

        private static AndroidJavaObject BuildPrivacyPolicyArgs(OfferwallPrivacyPolicy pp)
        {
            // PrivacyPolicy(subjectToGDPR: Boolean?, userConsent: String?, belowConsentAge: Boolean?, usPrivacy: String?)
            // Use Java boxed Boolean for nullable booleans
            AndroidJavaObject gdpr = pp.SubjectToGDPR.HasValue
                ? new AndroidJavaObject("java.lang.Boolean", pp.SubjectToGDPR.Value)
                : null;
            AndroidJavaObject belowAge = pp.BelowConsentAge.HasValue
                ? new AndroidJavaObject("java.lang.Boolean", pp.BelowConsentAge.Value)
                : null;

            return new AndroidJavaObject(PrivacyPolicyClass, gdpr, pp.UserConsent, belowAge, pp.UsPrivacy);
        }

        /// <summary>
        /// Callback proxy for OfferwallClient.connect().
        /// On success, opens the offerwall. On error, reports back to C#.
        /// </summary>
        private class ConnectCallback : AndroidJavaProxy
        {
            private readonly AndroidJavaObject _activity;
            private readonly AndroidJavaObject _client;
            private readonly OfferwallSettings _settings;
            private readonly Action<string> _onDismissed;

            public ConnectCallback(AndroidJavaObject activity, AndroidJavaObject client,
                OfferwallSettings settings, Action<string> onDismissed)
                : base("com.xsolla.android.mobile.offerwall.IOfferwallClientConnectCallback")
            {
                _activity = activity;
                _client = client;
                _settings = settings;
                _onDismissed = onDismissed;
            }

            // Called from Java: void onCallback(Either<Error, Void> result)
            void onCallback(AndroidJavaObject result)
            {
                bool isRight = result.Call<bool>("isRight");

                if (!isRight)
                {
                    var error = result.Call<AndroidJavaObject>("getLeft");
                    var message = error?.Call<string>("toString") ?? "Connect failed";
                    Debug.LogError($"[{Tag}] Connect failed: {message}");
                    MainThreadDispatcher.Enqueue(() => _onDismissed?.Invoke(message));
                    return;
                }

                // Connected — now open
                try
                {
                    var openCallback = new OpenCallback(_onDismissed);

                    // Build open params with custom params if provided
                    AndroidJavaObject openParams = null;
                    if (_settings.CustomParameters != null && _settings.CustomParameters.Count > 0)
                    {
                        using var paramsBuilder = new AndroidJavaObject(
                            "com.xsolla.android.mobile.offerwall.OfferwallOpenParams$Builder");
                        using var customParamsBuilder = new AndroidJavaObject(
                            "com.xsolla.android.mobile.offerwall.OfferwallOpenParams$CustomParams$Builder");

                        foreach (var kvp in _settings.CustomParameters)
                        {
                            customParamsBuilder.Call<AndroidJavaObject>("addParam", kvp.Key, kvp.Value);
                        }

                        using var customParams = customParamsBuilder.Call<AndroidJavaObject>("build");
                        paramsBuilder.Call<AndroidJavaObject>("withCustomParams", customParams);
                        openParams = paramsBuilder.Call<AndroidJavaObject>("build");
                    }

                    _client.Call<AndroidJavaObject>("open", _activity, openParams, openCallback);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[{Tag}] Open failed: {e}");
                    MainThreadDispatcher.Enqueue(() => _onDismissed?.Invoke(e.Message));
                }
            }
        }

        /// <summary>
        /// Callback proxy for OfferwallClient.open().
        /// </summary>
        private class OpenCallback : AndroidJavaProxy
        {
            private readonly Action<string> _onDismissed;

            public OpenCallback(Action<string> onDismissed)
                : base("com.xsolla.android.mobile.offerwall.IOfferwallOpenCallback")
            {
                _onDismissed = onDismissed;
            }

            void onCallback(AndroidJavaObject result)
            {
                bool isRight = result.Call<bool>("isRight");
                if (isRight)
                {
                    MainThreadDispatcher.Enqueue(() => _onDismissed?.Invoke(null));
                }
                else
                {
                    var error = result.Call<AndroidJavaObject>("getLeft");
                    var message = error?.Call<string>("toString") ?? "Offerwall error";
                    MainThreadDispatcher.Enqueue(() => _onDismissed?.Invoke(message));
                }
            }
        }
    }
}
#endif
