# Changelog

All notable changes to the Xsolla Offerwall SDK for Unity will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
