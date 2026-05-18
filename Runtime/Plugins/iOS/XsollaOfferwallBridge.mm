#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <XsollaOfferwallSDK/XsollaOfferwallSDK-Swift.h>

// Unity callback function pointer type
typedef void (*OfferwallDismissCallback)(const char* error);

// Store the callback so it persists across the async call
static OfferwallDismissCallback g_dismissCallback = NULL;

extern "C" {

    void _XsollaOfferwall_Show(
        const char* placementId,
        const char* userId,
        const char* customParamsJson,
        const char* privacyPolicyJson,
        OfferwallDismissCallback callback
    ) {
        NSString* placementIdStr = [NSString stringWithUTF8String:placementId];
        NSString* userIdStr = userId ? [NSString stringWithUTF8String:userId] : nil;

        g_dismissCallback = callback;

        dispatch_async(dispatch_get_main_queue(), ^{
            XMOfferwallSettings* settings = [[XMOfferwallSettings alloc] initWithPlacementId:placementIdStr];
            settings.userId = userIdStr;

            // Custom parameters
            if (customParamsJson) {
                NSString* jsonStr = [NSString stringWithUTF8String:customParamsJson];
                NSData* data = [jsonStr dataUsingEncoding:NSUTF8StringEncoding];
                if (data) {
                    NSDictionary* params = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
                    if (params && [params isKindOfClass:[NSDictionary class]]) {
                        NSMutableDictionary<NSString*, NSString*>* stringParams = [NSMutableDictionary new];
                        [params enumerateKeysAndObjectsUsingBlock:^(id key, id obj, BOOL* stop) {
                            if ([key isKindOfClass:[NSString class]] && [obj isKindOfClass:[NSString class]]) {
                                stringParams[key] = obj;
                            }
                        }];
                        if (stringParams.count > 0) {
                            settings.customQueryParameters = stringParams;
                        }
                    }
                }
            }

            // Privacy policy
            if (privacyPolicyJson) {
                NSString* jsonStr = [NSString stringWithUTF8String:privacyPolicyJson];
                NSData* data = [jsonStr dataUsingEncoding:NSUTF8StringEncoding];
                if (data) {
                    NSDictionary* ppDict = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
                    if (ppDict) {
                        XMOfferwallPrivacyPolicy* pp = [[XMOfferwallPrivacyPolicy alloc] init];

                        if (ppDict[@"subjectToGDPR"] && ppDict[@"subjectToGDPR"] != [NSNull null]) {
                            pp.subjectToGDPR = ppDict[@"subjectToGDPR"];
                        }
                        if (ppDict[@"userConsent"] && ppDict[@"userConsent"] != [NSNull null]) {
                            pp.userConsent = ppDict[@"userConsent"];
                        }
                        if (ppDict[@"belowConsentAge"] && ppDict[@"belowConsentAge"] != [NSNull null]) {
                            pp.belowConsentAge = ppDict[@"belowConsentAge"];
                        }
                        if (ppDict[@"usPrivacy"] && ppDict[@"usPrivacy"] != [NSNull null]) {
                            pp.usPrivacy = ppDict[@"usPrivacy"];
                        }

                        settings.privacyPolicy = pp;
                    }
                }
            }

            [[XMOfferwallManager shared] showWithSettings:settings completion:^(NSError* error) {
                if (g_dismissCallback) {
                    if (error) {
                        const char* errMsg = [error.localizedDescription UTF8String];
                        g_dismissCallback(errMsg);
                    } else {
                        g_dismissCallback(NULL);
                    }
                    g_dismissCallback = NULL;
                }
            }];
        });
    }

    void _XsollaOfferwall_Dismiss() {
        dispatch_async(dispatch_get_main_queue(), ^{
            [[XMOfferwallManager shared] dismissAnimated:YES];
        });
    }
}
