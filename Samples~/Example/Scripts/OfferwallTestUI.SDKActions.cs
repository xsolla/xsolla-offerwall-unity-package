using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xsolla.Offerwall;
#if UNITY_IOS && IOS_SUPPORT_AVAILABLE
using Unity.Advertisement.IosSupport;
#endif

public partial class OfferwallTestUI
{
    private void ConnectOfferwall()
    {
        SyncInputFields();
        XsollaOfferwall.Settings.OpenExternalLinksInBrowser = true;

        Log("Connecting...");
        if (_connectStatusLabel) _connectStatusLabel.text = "Connecting...";
        _statusLabel.text = "Connecting...";

        XsollaOfferwall.Connect(error =>
        {
            if (error != null)
            {
                Log($"Connect ERROR: {error.Message}");
                if (_connectStatusLabel) _connectStatusLabel.text = $"Error: {error.Message}";
                _statusLabel.text = "Connect failed";
            }
            else
            {
                Log("Connected successfully");
                if (_connectStatusLabel) _connectStatusLabel.text = "Connected";
                _statusLabel.text = "Connected";
                RefreshSettingsFromNative();
            }
        });
    }

    private void ShowOfferwall()
    {
        SyncInputFields();

        // Apply privacy policy — always push all values including null so "Not Set" clears native state.
        // _subjectToGDPR / _belowConsentAge are kept in sync with native via RefreshSettingsFromNative()
        // after connect, so these reflect the user's intent rather than stale defaults.
        XsollaOfferwall.PrivacyPolicy.SubjectToGDPR   = _subjectToGDPR;
        XsollaOfferwall.PrivacyPolicy.BelowConsentAge = _belowConsentAge;
        XsollaOfferwall.PrivacyPolicy.UserConsent     = userConsent;
        XsollaOfferwall.PrivacyPolicy.UsPrivacy       = usPrivacy;

        var customParams = _customParams.Count > 0
            ? new Dictionary<string, string>(_customParams)
            : null;

        Log($"Showing offerwall (params: {_customParams.Count}, " +
            $"gdpr: {NullableBoolStr(_subjectToGDPR)}, " +
            $"consentAge: {NullableBoolStr(_belowConsentAge)})");
        _statusLabel.text = "Opening...";

        XsollaOfferwall.Show(placementId, customParams: customParams, onDismissed: OnDismissed);
    }

    private void EnableAndroidDeviceId()
    {
        XsollaOfferwall.SetAndroidDeviceIdEnabled(true);
        RefreshAndroidDeviceIdLabel();
        Log($"Android Device ID enabled ({XsollaOfferwall.IsAndroidDeviceIdEnabled()})");
    }

    private void DisableAndroidDeviceId()
    {
        XsollaOfferwall.SetAndroidDeviceIdEnabled(false);
        RefreshAndroidDeviceIdLabel();
        Log($"Android Device ID disabled ({XsollaOfferwall.IsAndroidDeviceIdEnabled()})");
    }

    private void RefreshAndroidDeviceIdLabel()
    {
        if (_androidDeviceIdLabel) _androidDeviceIdLabel.text = GetAndroidDeviceIdStateText();
    }

    private static string GetAndroidDeviceIdStateText()
    {
        bool e = XsollaOfferwall.IsAndroidDeviceIdEnabled();
        return $"Android Device ID: {(e ? "Enabled" : "Disabled")} (Android only)";
    }

    private void OnDismissed(string error)
    {
        if (error != null)
        {
            Log($"ERROR: {error}");
            _statusLabel.text = "Error";
        }
        else
        {
            Log("Offerwall dismissed successfully");
            _statusLabel.text = "Dismissed";
        }
    }

    /// <summary>
    /// Reads all SDK state from the native layer and updates the settings UI to match.
    /// Called after connect succeeds so the UI reflects persisted values rather than defaults.
    /// </summary>
    private void RefreshSettingsFromNative()
    {
        // Privacy policy
        _subjectToGDPR   = XsollaOfferwall.PrivacyPolicy.SubjectToGDPR;
        _belowConsentAge = XsollaOfferwall.PrivacyPolicy.BelowConsentAge;
        userConsent      = XsollaOfferwall.PrivacyPolicy.UserConsent ?? "";
        usPrivacy        = XsollaOfferwall.PrivacyPolicy.UsPrivacy   ?? "";

        _refreshGdpr?.Invoke(_subjectToGDPR);
        _refreshBelowAge?.Invoke(_belowConsentAge);
        if (_userConsentInput) _userConsentInput.text = userConsent;
        if (_usPrivacyInput)   _usPrivacyInput.text   = usPrivacy;

        // User ID
        var nativeUserId = XsollaOfferwall.GetUserId() ?? "";
        if (nativeUserId != userId)
        {
            userId = nativeUserId;
            if (_userIdInput) _userIdInput.text = userId;
        }

        // Log level
        _refreshLogLevel?.Invoke(XsollaOfferwall.Settings.LogLevel);

        // Orientation
        _refreshOrientation?.Invoke(XsollaOfferwall.Settings.Orientation);

        // Version label
        UpdateVersionLabel();

        Log($"Settings refreshed from native — gdpr: {NullableBoolStr(_subjectToGDPR)}, " +
            $"consentAge: {NullableBoolStr(_belowConsentAge)}, userId: {userId}, " +
            $"logLevel: {XsollaOfferwall.Settings.LogLevel?.ToString() ?? "Default"}, " +
            $"orientation: {XsollaOfferwall.Settings.Orientation}");
    }

    private void UpdateVersionLabel()
    {
        if (_versionLabel) _versionLabel.text = XsollaOfferwall.GetVersion();
    }

    private void SyncInputFields()
    {
        if (_placementIdInput) placementId = _placementIdInput.text;
        if (_userIdInput)
        {
            userId = _userIdInput.text;
            XsollaOfferwall.SetUserId(userId);
        }
        if (_userConsentInput) userConsent = _userConsentInput.text;
        if (_usPrivacyInput)   usPrivacy   = _usPrivacyInput.text;
    }

    private void Log(string message)
    {
        string ts = System.DateTime.Now.ToString("HH:mm:ss");
        _logMessages += $"[{ts}] {message}\n";
        if (_logText)       _logText.text = _logMessages;
        if (_logScrollRect) Canvas.ForceUpdateCanvases();
        Debug.Log($"[OfferwallTest] {message}");
    }

    private static string NullableBoolStr(bool? v) =>
        v.HasValue ? v.Value.ToString().ToLower() : "not set";

#if UNITY_IOS && IOS_SUPPORT_AVAILABLE
    private void RequestAttAuthorization()
    {
        Log("Requesting ATT authorization...");
        if (_attStatusLabel) _attStatusLabel.text = "Status: Requesting...";
        StartCoroutine(RequestAttCoroutine());
    }

    private IEnumerator RequestAttCoroutine()
    {
        ATTrackingStatusBinding.RequestAuthorizationTracking();
        while (ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
               ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            yield return null;

        var status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
        Log($"ATT status: {status}");
        if (_attStatusLabel) _attStatusLabel.text = $"Status: {status}";
    }
#endif
}
