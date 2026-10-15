# Changelog

All notable changes to the Xsolla Offerwall SDK for Unity will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-10-15

### Added

- `XsollaOfferwall.Show(placementId, customParams, onOpened, onError, onClosed)` — new overload reporting distinct open/error/closed callbacks, matching the iOS and Android SDKs.
- `XsollaOfferwall.PrivacyPolicy.GppString` / `GppSid` — IAB Global Privacy Platform (GPP) consent string and section ID(s), matching the Android and iOS SDKs. Like the other IAB-backed fields, reads always reflect whatever is currently in shared IAB storage (e.g. written by a CMP/UMP consent flow) — no separate refresh step.
- [SAMPLE APP] Settings screen: GPP String / GPP Section ID(s) fields, and a "Show Consent Form" button that drives Google's User Messaging Platform (UMP) SDK and refreshes the settings UI from the SDK afterwards.
- [SAMPLE APP] UMP consent demo: Test Device Hashed ID(s) field, Debug Geography selector, and a "Clear IAB Values" button to reset consent state for testing.
- [SAMPLE APP] Prompts to install the Google Mobile Ads package when the sample is imported without it, so the consent demo above can compile and run. Declining is remembered; install later via Window > Xsolla Offerwall > Sample > Install Google Mobile Ads Dependency.
- [SAMPLE APP] Prompts to fill any empty AdMob App ID field with Google's public test ID, so the consent demo runs without an AdMob account. Configurable anytime via Window > Xsolla Offerwall > Sample > Configure Demo AdMob App ID.
- Settings window now displays the installed External Dependency Manager (EDM) version.
- EDM minimum version check for Swift Package Manager (requires 1.2.187+): on package load and when switching the iOS integration to SPM, a warning is shown and the integration falls back to CocoaPods if the requirement is not met.
- "Skip EDM Version Check" toggle in the Settings window iOS section, for projects that manage EDM separately.
- New package dependency: `com.unity.nuget.newtonsoft-json` (3.2.2+), used to safely edit `Packages/manifest.json` during EDM4U auto-install.

### Changed

- Targets Xsolla Offerwall Android SDK 0.4.0 and iOS SDK 0.4.0.

### Deprecated

- `OfferwallSettings.OpenExternalLinksInBrowser` and `XsollaOfferwall.Settings.OpenExternalLinksInBrowser` are deprecated and ignored. External links always open in the device browser: the setting is deprecated on Android, was never exposed on iOS, and disabling it caused broken link handling. The value is no longer sent to or read from the native SDKs — the getter always returns `true` and the setter is a no-op. Both members will be removed in v1.0.0; remove any usage.
- `XsollaOfferwall.Show(placementId, customParams, onDismissed)` — use the new `onOpened`/`onError`/`onClosed` overload instead, which distinguishes open failures from a normal close and matches the iOS and Android SDKs. All three arguments are now required, so `Show(placementId)` and `Show(placementId, customParams)` use the new overload. Will be removed in v1.0.0.

### Fixed

- `XsollaOfferwall.Show` no longer throws an exception when `placementId` is missing; the native SDK now reports the error via `onError` (or `onDismissed` on the deprecated overload).
- Fixed manifest.json corruption: the EDM4U auto-installer now parses and edits `Packages/manifest.json` as JSON instead of splicing text, so it can no longer produce invalid JSON (e.g. duplicate `scopedRegistries` entries, or a trailing comma when an existing registry/scopes array was empty).

## [0.3.0] - 2026-07-08

### Added

- Settings editor window (Window > Xsolla Offerwall > Settings). Creates `Assets/XsollaOfferwallSDK/Resources/XsollaOfferwallRuntimeSettings.asset`
  - Display the SDK version
  - Runtime default settings. Values are applied automatically before the first scene loads via `XsollaOfferwallRuntimeInit`, with lower priority than any explicit calls made later in game code.
    - Android Device ID toggle 
    - Screen orientation selector
    - Log level selector. 
  - App Set ID toggle; when enabled, adds `com.google.android.gms:play-services-appset` to `XsollaDependencies.xml` with a configurable version (default `16.1.0`); preference persists across editor sessions; enabled by default
  - iOS integration selector in the Settings window: Swift Package Manager (default) or CocoaPods
- `XsollaOfferwall.SetPublisherUserIds()` / `GetPublisherUserIds()` — sets the publisher user IDs
- [SAMPLE APP] Publisher User IDs list UI in Settings screen — view, add, and remove publisher user IDs
- [SAMPLE APP] Settings screen: expanded PrivacyPolicy config, moved UserID
- [SAMPLE APP] Log Level App UI in Settings, stored in `PlayerPrefs`
- [SAMPLE APP] Orientation selector (Portrait / Landscape / Unspecified) to Settings screen
- [SAMPLE APP] Example script to increase CocoaPods minimum deployment targets.

### Changed

- Xsolla Offerwall Android SDK 0.3.0
- Xsolla Offerwall iOS SDK 0.3.0

### Fixed

- Declared `com.unity.modules.androidjni` and `com.unity.modules.jsonserialize` as package dependencies so the SDK compiles in projects that have removed these built-in Unity modules.

### Removed

- `XsollaOfferwall.Dismiss()` — programmatic dismissal was not supported by the underlying native SDKs.

## [0.2.0] - 2026-06-04

### Added

- App Tracking Transparancy Prompt to Sample App
- Android Device ID toggle in SDK interface, with Sample App UI.

### Changed

- Xsolla-Offerwall-ios SDK v0.2.0
- Xsolla-Offerwall-android v0.2.0
- Android minSdk now 23, however requires 24+ for content
- iOS min deployment target remains at 12.0, however now requires 15.0 for content
- `XsollaOfferwall.Connect()` no longer accepts OfferwallSettings
- `OfferwallSettings` now set via `XsollaOfferwall.Settings` object
- Moved `userId` from `OfferwallSettings` to `XsollaOfferwall.setUserId()`
- Moved `placementId` from `OfferwallSettings` to `XsollaOfferwall.show()` parameter
- Moved `customParams` from `OfferwallSettings` to `XsollaOfferwall.show()` optional parameter
- Moved `PrivacyPolicy` from `OfferwallSettings` to XsollaOfferwall.PrivacyPolicy` object

## [0.1.0] - 2026-05-18

### Added

- Initial release of the Xsolla Offerwall SDK for Unity.
- Native bridge for Android via Maven dependency (`com.xsolla.android:offerwall`).
- Native bridge for iOS via Objective-C (`XsollaOfferwallBridge.mm`).
- `XsollaOfferwall` C# facade for opening the offerwall.
- `OfferwallSettings` For Offerwall configuration
- Editor installer (`XsollaOfferwallInstaller`) for automated dependency setup.
- iOS post-build processor (`XsollaIosBuildPostProcessor`) for Xcode project configuration.
- Example scene demonstrating SDK integration (`Samples~/Example`).
- Support for Unity 2021.3 LTS and above, Android SDK 24+ and iOS 12.0+.
