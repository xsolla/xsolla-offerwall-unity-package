using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        XsollaOfferwall.PrivacyPolicy.GppString       = _gppString;
        XsollaOfferwall.PrivacyPolicy.GppSid          = _gppSid;

        var customParams = _customParams.Count > 0
            ? new Dictionary<string, string>(_customParams)
            : null;

        Log($"Showing offerwall (params: {_customParams.Count}, " +
            $"gdpr: {NullableBoolStr(_subjectToGDPR)}, " +
            $"consentAge: {NullableBoolStr(_belowConsentAge)})");
        _statusLabel.text = "Opening...";

        XsollaOfferwall.Show(
            placementId,
            customParams: customParams,
            onOpened: OnOpened,
            onError: OnError,
            onClosed: OnClosed);
    }

    /// <summary>
    /// "Show Consent Form" button — requests an update of the user's UMP consent info and shows
    /// whichever form currently applies (initial consent form if required, otherwise the
    /// privacy options form). Combines the previous separate "Show Consent" / "Update" buttons.
    /// Always refreshes the privacy policy fields from native storage afterwards, since Google's
    /// UMP SDK writes GPP/TCF/CCPA values there regardless of whether a form was actually shown.
    /// </summary>
    private void ShowConsentAndPrivacyOptions()
    {
        if (_isPrimingDebugGeography) return;

        Log("Requesting UMP consent info update...");
        UmpConsentManager.RequestConsentAndShowPrivacyOptions(_debugGeography, _testDeviceHashedIds,
            error => OnConsentFlowComplete("Show Consent Form", error));
    }

    /// <summary>
    /// "Clear IAB Values" button — clears IAB TCF/CCPA/GPP consent strings, resets UMP's consent
    /// state, and clears the SDK's privacy policy override fields, then re-primes the currently
    /// selected debug geography (see <see cref="PrimeDebugGeography"/>) so the next "Show Consent
    /// Form" tap isn't an unprimed "first call" again — Clear IAB Values resets consent info
    /// without changing the geography, so nothing else would trigger that re-priming.
    /// </summary>
    private void ClearIabValues()
    {
        Log("Clearing IAB privacy values...");
        IabPrivacyClearing.ClearIabPrivacyValues();
        RefreshSettingsFromNative();
        Log("IAB privacy values cleared");
        PrimeDebugGeography();
    }

    /// <summary>
    /// Warms up the currently selected debug geography with a throwaway consent-info update (see
    /// <see cref="UmpConsentManager.PrimeDebugGeography"/> for why this exists). "Show Consent
    /// Form" is disabled ("Preparing…") until this completes, so its own request can't race the
    /// priming request. Called on screen build, whenever the debug geography changes, and after
    /// Clear IAB Values.
    /// </summary>
    private void PrimeDebugGeography()
    {
        _isPrimingDebugGeography = true;
        SetShowConsentButtonState(interactable: false, label: "Preparing…");

        UmpConsentManager.PrimeDebugGeography(_debugGeography, _testDeviceHashedIds, () =>
        {
            _isPrimingDebugGeography = false;
            SetShowConsentButtonState(interactable: true, label: "Show Consent Form");
        });
    }

    private void SetShowConsentButtonState(bool interactable, string label)
    {
        if (_showConsentButton)      _showConsentButton.interactable = interactable;
        if (_showConsentButtonLabel) _showConsentButtonLabel.text     = label;
    }

    /// <summary>Splits a comma-separated test device ID list into trimmed, non-empty entries.</summary>
    private static List<string> ParseTestDeviceIds(string csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? new List<string>()
            : csv.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

    private void OnConsentFlowComplete(string action, string error)
    {
        if (error != null)
            Log($"{action} ERROR: {error}");
        else
            Log($"{action} completed");

        // The consent flow (if shown) writes GPP/TCF/CCPA values into shared IAB storage.
        // PrivacyPolicy reads always reflect current storage, so just refresh the settings UI.
        RefreshSettingsFromNative();
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

    private void OnOpened()
    {
        Log("Offerwall opened");
        _statusLabel.text = "Opened";
    }

    private void OnError(XOError error)
    {
        Log($"ERROR: {error.Message}");
        _statusLabel.text = "Error";
    }

    private void OnClosed()
    {
        Log("Offerwall dismissed successfully");
        _statusLabel.text = "Dismissed";
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
        _gppString       = XsollaOfferwall.PrivacyPolicy.GppString  ?? "";
        _gppSid          = XsollaOfferwall.PrivacyPolicy.GppSid     ?? "";

        _refreshGdpr?.Invoke(_subjectToGDPR);
        _refreshBelowAge?.Invoke(_belowConsentAge);
        if (_userConsentInput) _userConsentInput.text = userConsent;
        if (_usPrivacyInput)   _usPrivacyInput.text   = usPrivacy;
        if (_gppStringInput)   _gppStringInput.text   = _gppString;
        if (_gppSidInput)      _gppSidInput.text      = _gppSid;
        RefreshPrivacyOptionsStatusLabel();

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

    private void RefreshPrivacyOptionsStatusLabel()
    {
        if (!_privacyOptionsStatusLabel) return;
        _privacyOptionsStatusLabel.text = UmpConsentManager.IsPrivacyOptionsRequired
            ? "Privacy options entry point required — \"Show Consent Form\" will reopen it."
            : "No privacy options entry point required yet.";
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
        if (_gppStringInput)   _gppString  = _gppStringInput.text;
        if (_gppSidInput)      _gppSid     = _gppSidInput.text;
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
