# Changelog

All notable changes to the Xsolla Offerwall SDK for Unity will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
