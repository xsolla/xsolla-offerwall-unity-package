# Xsolla Offerwall SDK for Unity

Monetize your Unity players by displaying an offerwall where they earn virtual rewards by completing advertiser tasks. Bridges to native iOS and Android Offerwall SDKs.

For more info see https://xsolla.com/xsolla-ads 
## System Requirements

| Requirement | Minimum               |
|-------------|-----------------------|
| Unity | 2021.3 LTS            |
| Android | SDK 23+ (Android 6.0) |
| iOS | 12.0+                 |

## Installation

### UPM via Git URL

1. Open your Unity project.
2. Go to **Window > Package Manager**.
3. Click **+** and select **Add package from git URL**.
4. Enter: `https://github.com/xsolla/xsolla-offerwall-unity-package.git`
5. Click **Add**.

Or add directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.xsolla.offerwall": "https://github.com/xsolla/xsolla-offerwall-unity-package.git"
  }
}
```