using UnityEngine;
using Xsolla.Offerwall;

/// <summary>
/// Clears IAB (Interactive Advertising Bureau) consent strings written by a CMP/UMP consent
/// flow, resets UMP's own consent state, and clears the Xsolla Offerwall SDK's privacy policy
/// override fields — so the app can be tested from a clean consent state without reinstalling.
/// Mirrors IabPrivacyClearing.swift (iOS) / IabPrivacyClearing.kt (Android) in the native test
/// apps. Maps to the "Clear IAB Values" button.
///
/// Lives in the sample app only, not the SDK — the SDK has no opinion on how a developer's CMP
/// stores its consent strings.
/// </summary>
public static class IabPrivacyClearing
{
    // Standard IAB TCF/CCPA/GPP keys written by UMP (and most other CMPs) to platform-shared
    // storage: NSUserDefaults on iOS, the app's default SharedPreferences on Android.
    private static readonly string[] IabKeys =
    {
        "IABTCF_gdprApplies",
        "IABTCF_TCString",
        "IABUSPrivacy_String",
        "IABGPP_HDR_GppString",
        "IABGPP_GppSID",
    };

    public static void ClearIabPrivacyValues()
    {
        ClearNativeIabKeys();
        UmpConsentManager.ResetConsentInformation();

        // These setters push straight to the native SDK's privacy policy override, matching
        // the native test apps' Clear IAB Values behaviour. BelowConsentAge is deliberately
        // left untouched — it has no IAB-backed key, so there is nothing to "clear".
        XsollaOfferwall.PrivacyPolicy.SubjectToGDPR = null;
        XsollaOfferwall.PrivacyPolicy.UserConsent   = null;
        XsollaOfferwall.PrivacyPolicy.UsPrivacy     = null;
        XsollaOfferwall.PrivacyPolicy.GppString     = null;
        XsollaOfferwall.PrivacyPolicy.GppSid        = null;
    }

    private static void ClearNativeIabKeys()
    {
#if UNITY_IOS && !UNITY_EDITOR
        // Unity's iOS PlayerPrefs implementation reads/writes NSUserDefaults.standardUserDefaults
        // directly — the same store IAB-compliant CMPs write to — so PlayerPrefs.DeleteKey is a
        // real removal from shared storage here, not just from Unity's own prefs.
        foreach (var key in IabKeys) PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
#elif UNITY_ANDROID && !UNITY_EDITOR
        ClearAndroidDefaultSharedPreferences();
#else
        Debug.LogWarning("[IabPrivacyClearing] Native IAB key clearing is only implemented for " +
            "iOS/Android devices; running in the Editor or on another platform only resets UMP " +
            "consent state and the SDK's own privacy policy fields.");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void ClearAndroidDefaultSharedPreferences()
    {
        // IAB-compliant CMPs (including UMP) write to the app's default SharedPreferences file,
        // i.e. android.preference.PreferenceManager.getDefaultSharedPreferences(context) — the
        // deprecated-but-still-functional android.preference API is used here rather than
        // androidx.preference so no extra Gradle dependency is required just to clear test data.
        using var playerClass   = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        using var activity      = playerClass.GetStatic<AndroidJavaObject>("currentActivity");
        using var preferenceMgr = new AndroidJavaClass("android.preference.PreferenceManager");
        using var prefs         = preferenceMgr.CallStatic<AndroidJavaObject>("getDefaultSharedPreferences", activity);
        using var editor        = prefs.Call<AndroidJavaObject>("edit");

        foreach (var key in IabKeys)
        {
            using var _ = editor.Call<AndroidJavaObject>("remove", key);
        }
        editor.Call("apply");
    }
#endif
}
