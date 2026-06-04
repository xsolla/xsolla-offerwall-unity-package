#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <XsollaOfferwallSDK/XsollaOfferwallSDK-Swift.h>

// Unity callback function pointer types
typedef void (*OfferwallConnectCallback)(const char* error);
typedef void (*OfferwallDismissCallback)(const char* error);

// Store callbacks so they persist across async calls
static OfferwallConnectCallback g_connectCallback = NULL;
static OfferwallDismissCallback g_dismissCallback = NULL;

extern "C" {

    // orientation: 0=portrait, 1=landscape, 2=unspecified/all
    // logLevel: XMOLogLevel raw value, -1 = not set
    void _XsollaOfferwall_GetSettings(int* outOrientation, int* outLogLevel) {
        XMOfferwallSettings* s = [XMOfferwallManager shared].settings;

        UIInterfaceOrientationMask o = s.supportedOrientations;
        if (o == UIInterfaceOrientationMaskLandscape)     *outOrientation = 1;
        else if (o == UIInterfaceOrientationMaskAll)      *outOrientation = 2;
        else                                              *outOrientation = 0;

        *outLogLevel = (int)s.logLevel;
    }

    // orientation: 0=portrait, 1=landscape, 2=unspecified/all
    // logLevel: XMOLogLevel raw value (verbose=0 .. error=6), -1 = platform default
    // openExternalLinksInBrowser: not directly exposed on iOS, ignored
    void _XsollaOfferwall_SetSettings(int orientation, int logLevel) {
        dispatch_async(dispatch_get_main_queue(), ^{
            XMOfferwallSettings* s = [XMOfferwallManager shared].settings;

            switch (orientation) {
                case 1:  s.supportedOrientations = UIInterfaceOrientationMaskLandscape; break;
                case 2:  s.supportedOrientations = UIInterfaceOrientationMaskAll;       break;
                default: s.supportedOrientations = UIInterfaceOrientationMaskPortrait;  break;
            }

            if (logLevel >= 0) {
                s.logLevel = (XMOLogLevel)logLevel;
            }
        });
    }

    void _XsollaOfferwall_Connect(OfferwallConnectCallback callback) {
        g_connectCallback = callback;

        dispatch_async(dispatch_get_main_queue(), ^{
            [[XMOfferwallManager shared] connectWithCompletion:^(NSError* error) {
                if (g_connectCallback) {
                    if (error) {
                        const char* errMsg = [error.localizedDescription UTF8String];
                        g_connectCallback(errMsg);
                    } else {
                        g_connectCallback(NULL);
                    }
                    g_connectCallback = NULL;
                }
            }];
        });
    }

    void _XsollaOfferwall_Show(
        const char* placementId,
        const char* customParamsJson,
        OfferwallDismissCallback callback
    ) {
        NSString* placementIdStr = [NSString stringWithUTF8String:placementId];

        g_dismissCallback = callback;

        dispatch_async(dispatch_get_main_queue(), ^{
            // Parse custom parameters if provided
            NSMutableDictionary<NSString*, NSString*>* customParams = nil;
            if (customParamsJson) {
                NSString* jsonStr = [NSString stringWithUTF8String:customParamsJson];
                NSData* data = [jsonStr dataUsingEncoding:NSUTF8StringEncoding];
                if (data) {
                    NSDictionary* params = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
                    if (params && [params isKindOfClass:[NSDictionary class]]) {
                        customParams = [NSMutableDictionary new];
                        [params enumerateKeysAndObjectsUsingBlock:^(id key, id obj, BOOL* stop) {
                            if ([key isKindOfClass:[NSString class]] && [obj isKindOfClass:[NSString class]]) {
                                customParams[key] = obj;
                            }
                        }];
                        if (customParams.count == 0) customParams = nil;
                    }
                }
            }

            void (^completion)(NSError*) = ^(NSError* error) {
                if (g_dismissCallback) {
                    g_dismissCallback(error ? [error.localizedDescription UTF8String] : NULL);
                    g_dismissCallback = NULL;
                }
            };

            if (customParams) {
                [[XMOfferwallManager shared] showWithPlacementId:placementIdStr
                                             customQueryParameters:customParams
                                                       completion:completion];
            } else {
                [[XMOfferwallManager shared] showWithPlacementId:placementIdStr
                                                       completion:completion];
            }
        });
    }

    // subjectToGDPR / belowConsentAge: -1 = null, 0 = false, 1 = true
    void _XsollaOfferwall_SetPrivacyPolicy(
        int subjectToGDPR,
        const char* userConsent,
        int belowConsentAge,
        const char* usPrivacy
    ) {
        dispatch_async(dispatch_get_main_queue(), ^{
            XMOPrivacyPolicy* pp = [XMOfferwallManager shared].privacyPolicy;
            pp.subjectToGDPR   = subjectToGDPR   >= 0 ? @(subjectToGDPR   == 1) : nil;
            pp.belowConsentAge = belowConsentAge >= 0 ? @(belowConsentAge == 1) : nil;
            pp.userConsent     = userConsent  ? [NSString stringWithUTF8String:userConsent]  : nil;
            pp.usPrivacy       = usPrivacy    ? [NSString stringWithUTF8String:usPrivacy]    : nil;
        });
    }

    const char* _XsollaOfferwall_GetUserId() {
        NSString* userId = [XMOfferwallManager shared].userId;
        return userId ? strdup([userId UTF8String]) : NULL;
    }

    // Writes privacy policy fields into caller-supplied out-params.
    // Nullable booleans use the -1 / 0 / 1 convention (see SetPrivacyPolicy).
    void _XsollaOfferwall_GetPrivacyPolicy(
        int* outSubjectToGDPR,
        char** outUserConsent,
        int* outBelowConsentAge,
        char** outUsPrivacy
    ) {
        XMOPrivacyPolicy* pp = [XMOfferwallManager shared].privacyPolicy;
        *outSubjectToGDPR   = pp.subjectToGDPR   ? (pp.subjectToGDPR.boolValue   ? 1 : 0) : -1;
        *outBelowConsentAge = pp.belowConsentAge  ? (pp.belowConsentAge.boolValue ? 1 : 0) : -1;
        *outUserConsent = pp.userConsent ? strdup([pp.userConsent UTF8String]) : NULL;
        *outUsPrivacy   = pp.usPrivacy   ? strdup([pp.usPrivacy   UTF8String]) : NULL;
    }

    void _XsollaOfferwall_SetUserId(const char* userId) {
        NSString* userIdStr = userId ? [NSString stringWithUTF8String:userId] : nil;
        dispatch_async(dispatch_get_main_queue(), ^{
            [XMOfferwallManager shared].userId = userIdStr;
        });
    }

    void _XsollaOfferwall_Dismiss() {
        dispatch_async(dispatch_get_main_queue(), ^{
            [[XMOfferwallManager shared] dismissAnimated:YES];
        });
    }
}
