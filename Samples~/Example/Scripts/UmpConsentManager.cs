#if GMA_AVAILABLE
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
using Xsolla.Offerwall;
#endif

/// <summary>
/// Geography options for simulating a specific consent region while testing UMP, independent
/// of the device's real location. Mirrors Google's own <c>DebugGeography</c> enum (Android:
/// <c>ConsentDebugSettings.DebugGeography</c>, iOS: <c>DebugGeography</c>) so it can be exposed
/// in the Settings screen dropdown without the UI code needing the GoogleMobileAds package.
/// </summary>
public enum UmpDebugGeography
{
    Disabled,
    Eea,
    RegulatedUsState,
    Other,
}

/// <summary>
/// Thin wrapper around Google's User Messaging Platform (UMP) SDK for the sample app's
/// Settings screen. Mirrors the reference implementations shipped in the native test apps
/// (Android: UmpConsentManager.kt, iOS: UmpConsentManager.swift): a single combined
/// "Show Consent Form" action, a user-editable debug geography + test device ID list (rather
/// than hardcoded constants), and a full consent reset.
///
/// This lives in the sample app only — the Xsolla Offerwall SDK package itself has no
/// dependency on Google Mobile Ads / UMP, matching the native SDKs. Compiled only when the
/// com.google.ads.mobile package is present (see GMA_AVAILABLE in the samples .asmdef); the
/// buttons that call into this class are hidden otherwise.
/// </summary>
public static class UmpConsentManager
{
#if GMA_AVAILABLE
    /// <summary>
    /// Whether the app must show a privacy options entry point (e.g. an "Update Consent"
    /// button) so the user can change their choice after the initial consent flow.
    /// Only meaningful after <see cref="RequestConsentAndShowPrivacyOptions"/> has completed
    /// at least once.
    /// </summary>
    public static bool IsPrivacyOptionsRequired =>
        ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

    /// <summary>
    /// Requests an update of the user's consent information and shows whichever form is
    /// currently applicable: the initial consent form if one is required, otherwise the
    /// privacy options form if the user has already made a choice and can revise it. Combines
    /// what used to be separate "Show Consent" and "Update" buttons.
    ///
    /// Deciding which single form to show BEFORE making any form-related call — rather than
    /// always calling <c>LoadAndShowConsentFormIfRequired</c> and conditionally falling back to
    /// <c>ShowPrivacyOptionsForm</c> — is deliberate, not just a style choice. The Unity
    /// GoogleMobileAds/UMP bridge tracks a single native "current form" + dismiss-listener slot;
    /// making a second form-related call in the same flow as a first one (even when the first
    /// was a same-tick no-op, which it always is outside the GDPR/EEA initial-consent case)
    /// collides with that slot, and the second call's dismiss listener never fires — logged by
    /// the SDK as "The consent form dismiss callback is null." (see
    /// googleads-mobile-unity#3709, "Consent Form: Multiple Shown at Same time"). Calling at
    /// most one of these two APIs per invocation avoids the collision entirely.
    /// </summary>
    public static void RequestConsentAndShowPrivacyOptions(
        UmpDebugGeography debugGeography, List<string> testDeviceHashedIds, System.Action<string> onComplete)
    {
        var requestParameters = new ConsentRequestParameters
        {
            ConsentDebugSettings = new ConsentDebugSettings
            {
                DebugGeography      = ToNativeDebugGeography(debugGeography),
                TestDeviceHashedIds = testDeviceHashedIds ?? new List<string>(),
            },
        };

        ConsentInformation.Update(requestParameters, updateError =>
        {
            if (updateError != null)
            {
                // On Android these callbacks fire on the UMP SDK's JNI thread, not Unity's
                // main thread — onComplete usually touches UI, so it must be marshaled back.
                MainThreadDispatcher.Enqueue(() => onComplete?.Invoke(updateError.Message));
                return;
            }

            if (ConsentInformation.ConsentStatus == ConsentStatus.Required)
            {
                ConsentForm.LoadAndShowConsentFormIfRequired(formError =>
                    MainThreadDispatcher.Enqueue(() => onComplete?.Invoke(formError?.Message)));
            }
            else if (IsPrivacyOptionsRequired)
            {
                ConsentForm.ShowPrivacyOptionsForm(privacyError =>
                    MainThreadDispatcher.Enqueue(() => onComplete?.Invoke(privacyError?.Message)));
            }
            else
            {
                MainThreadDispatcher.Enqueue(() => onComplete?.Invoke(null));
            }
        });
    }

    /// <summary>
    /// Clears all consent decisions UMP has stored so the next <see cref="RequestConsentAndShowPrivacyOptions"/>
    /// call behaves like a first launch. Used by "Clear IAB Values", and by <see cref="PrimeDebugGeography"/>
    /// before it re-establishes a geography.
    /// </summary>
    public static void ResetConsentInformation() => ConsentInformation.Reset();

    /// <summary>
    /// Resets consent information and performs one full consent-info update for <paramref name="debugGeography"/>
    /// in the background, discarding the result (invokes <paramref name="onComplete"/> once that
    /// finishes, on success or failure). A no-op that calls <paramref name="onComplete"/> immediately
    /// when <paramref name="debugGeography"/> is <see cref="UmpDebugGeography.Disabled"/>.
    ///
    /// Why this exists: observed behavior (not something Google documents) is that, on a real
    /// device, a newly picked debug geography isn't reliably reflected by the very next
    /// <see cref="ConsentInformation.Update"/> call after it's set — even with an immediately
    /// preceding <see cref="ConsentInformation.Reset"/> — only by the *second* call made for that
    /// geography. This matches the exact same workaround in the native Android/iOS test apps
    /// (<c>UmpConsentManager.primeDebugGeography</c> / <c>primeDebugGeography(debugGeography:...)</c>),
    /// which report the identical symptom (works on the second attempt, not the first).
    ///
    /// Calling this once, as soon as <paramref name="debugGeography"/> is (re-)established, and
    /// disabling "Show Consent Form" until <paramref name="onComplete"/> fires, spends that
    /// unreliable "first call" up front without racing it against the button's own request. The
    /// button's request is then effectively the *second* call for that geography, and reliably
    /// reflects it.
    ///
    /// Deliberately does NOT also call <c>ConsentForm.Load</c>/<c>LoadAndShowConsentFormIfRequired</c>/
    /// <c>ShowPrivacyOptionsForm</c> here, matching the native apps: the Unity GoogleMobileAds/UMP
    /// bridge tracks a single native "current form" + dismiss-listener slot (see the note on
    /// <see cref="RequestConsentAndShowPrivacyOptions"/>), and an earlier version of this method
    /// that also loaded a form left that slot in a state that broke the real request's dismiss
    /// callback moments later. Only <c>Update</c> is needed to warm up the geography.
    /// </summary>
    public static void PrimeDebugGeography(
        UmpDebugGeography debugGeography, List<string> testDeviceHashedIds, System.Action onComplete)
    {
        if (debugGeography == UmpDebugGeography.Disabled)
        {
            onComplete?.Invoke();
            return;
        }

        ConsentInformation.Reset();

        var requestParameters = new ConsentRequestParameters
        {
            ConsentDebugSettings = new ConsentDebugSettings
            {
                DebugGeography      = ToNativeDebugGeography(debugGeography),
                TestDeviceHashedIds = testDeviceHashedIds ?? new List<string>(),
            },
        };

        ConsentInformation.Update(requestParameters, _ =>
        {
            MainThreadDispatcher.Enqueue(() => onComplete?.Invoke());
        });
    }

    private static DebugGeography ToNativeDebugGeography(UmpDebugGeography geography) => geography switch
    {
        UmpDebugGeography.Eea              => DebugGeography.EEA,
        UmpDebugGeography.RegulatedUsState => DebugGeography.RegulatedUSState,
        UmpDebugGeography.Other            => DebugGeography.Other,
        _                                   => DebugGeography.Disabled,
    };
#else
    public static bool IsPrivacyOptionsRequired => false;

    public static void RequestConsentAndShowPrivacyOptions(
        UmpDebugGeography debugGeography, System.Collections.Generic.List<string> testDeviceHashedIds,
        System.Action<string> onComplete) =>
        onComplete?.Invoke("Google Mobile Ads / UMP package not installed.");

    public static void ResetConsentInformation() { }

    public static void PrimeDebugGeography(
        UmpDebugGeography debugGeography, System.Collections.Generic.List<string> testDeviceHashedIds,
        System.Action onComplete) =>
        onComplete?.Invoke();
#endif
}
