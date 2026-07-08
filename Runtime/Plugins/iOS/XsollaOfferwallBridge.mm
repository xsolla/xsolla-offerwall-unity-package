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
        // XMOfferwallManager is @MainActor; read on the main queue to avoid data races
        // and to guarantee we see the latest value from any pending SetSettings dispatch.
        void (^read)(void) = ^{
            XMOfferwallSettings* s = [XMOfferwallManager shared].settings;

            UIInterfaceOrientationMask o = s.supportedOrientations;
            if (o == UIInterfaceOrientationMaskLandscape)     *outOrientation = 1;
            else if (o == UIInterfaceOrientationMaskAll)      *outOrientation = 2;
            else                                              *outOrientation = 0;

            *outLogLevel = (int)s.logLevel;
        };
        if ([NSThread isMainThread]) read();
        else dispatch_sync(dispatch_get_main_queue(), read);
    }

    // orientation: 0=portrait, 1=landscape, 2=unspecified/all
    // logLevel: XMOLogLevel raw value (verbose=0 .. error=6), -1 = platform default
    // openExternalLinksInBrowser: not directly exposed on iOS, ignored
    void _XsollaOfferwall_SetSettings(int orientation, int logLevel) {
        // Apply synchronously on whatever thread calls us (matching the ObjC test app pattern
        // where logLevelChanged: sets settings directly on the main thread). Using dispatch_async
        // would defer the change to the next run-loop cycle; if connect() runs before that
        // cycle the SDK initialises its logger at the old level.
        // XMOfferwallSettings is an NSObject class (reference type) — direct property mutation
        // is sufficient; no need to call the settings setter back on the manager.
        void (^apply)(void) = ^{
            switch (orientation) {
                case 1:  [XMOfferwallManager shared].settings.supportedOrientations = UIInterfaceOrientationMaskLandscape; break;
                case 2:  [XMOfferwallManager shared].settings.supportedOrientations = UIInterfaceOrientationMaskAll;       break;
                default: [XMOfferwallManager shared].settings.supportedOrientations = UIInterfaceOrientationMaskPortrait;  break;
            }
            if (logLevel >= 0) {
                [XMOfferwallManager shared].settings.logLevel = (XMOLogLevel)logLevel;
            }
        };
        if ([NSThread isMainThread]) apply();
        else dispatch_sync(dispatch_get_main_queue(), apply);
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
        // Capture string values before dispatch (caller stack may be gone by async time)
        NSString* userConsentStr = userConsent ? [NSString stringWithUTF8String:userConsent] : nil;
        NSString* usPrivacyStr   = usPrivacy   ? [NSString stringWithUTF8String:usPrivacy]   : nil;
        void (^apply)(void) = ^{
            XMOPrivacyPolicy* pp = [XMOfferwallManager shared].privacyPolicy;
            pp.subjectToGDPR   = subjectToGDPR   >= 0 ? @(subjectToGDPR   == 1) : nil;
            pp.belowConsentAge = belowConsentAge >= 0 ? @(belowConsentAge == 1) : nil;
            pp.userConsent     = userConsentStr;
            pp.usPrivacy       = usPrivacyStr;
        };
        if ([NSThread isMainThread]) apply();
        else dispatch_sync(dispatch_get_main_queue(), apply);
    }

    const char* _XsollaOfferwall_GetUserId() {
        __block const char* result = NULL;
        void (^read)(void) = ^{ NSString* uid = [XMOfferwallManager shared].userId; result = uid ? strdup([uid UTF8String]) : NULL; };
        if ([NSThread isMainThread]) read();
        else dispatch_sync(dispatch_get_main_queue(), read);
        return result;
    }

    // Writes privacy policy fields into caller-supplied out-params.
    // Nullable booleans use the -1 / 0 / 1 convention (see SetPrivacyPolicy).
    void _XsollaOfferwall_GetPrivacyPolicy(
        int* outSubjectToGDPR,
        char** outUserConsent,
        int* outBelowConsentAge,
        char** outUsPrivacy
    ) {
        void (^read)(void) = ^{
            XMOPrivacyPolicy* pp = [XMOfferwallManager shared].privacyPolicy;
            *outSubjectToGDPR   = pp.subjectToGDPR   ? (pp.subjectToGDPR.boolValue   ? 1 : 0) : -1;
            *outBelowConsentAge = pp.belowConsentAge  ? (pp.belowConsentAge.boolValue ? 1 : 0) : -1;
            *outUserConsent = pp.userConsent ? strdup([pp.userConsent UTF8String]) : NULL;
            *outUsPrivacy   = pp.usPrivacy   ? strdup([pp.usPrivacy   UTF8String]) : NULL;
        };
        if ([NSThread isMainThread]) read();
        else dispatch_sync(dispatch_get_main_queue(), read);
    }

    void _XsollaOfferwall_SetUserId(const char* userId) {
        NSString* userIdStr = userId ? [NSString stringWithUTF8String:userId] : nil;
        void (^apply)(void) = ^{ [XMOfferwallManager shared].userId = userIdStr; };
        if ([NSThread isMainThread]) apply();
        else dispatch_sync(dispatch_get_main_queue(), apply);
    }

    // idsJson is a JSON object of the form {"ids":["id1","id2"]}, matching the wrapper
    // Unity's JsonUtility requires to (de)serialize a bare string array.
    void _XsollaOfferwall_SetPublisherUserIds(const char* idsJson) {
        NSMutableArray<NSString*>* ids = [NSMutableArray new];
        if (idsJson) {
            NSString* jsonStr = [NSString stringWithUTF8String:idsJson];
            NSData* data = [jsonStr dataUsingEncoding:NSUTF8StringEncoding];
            if (data) {
                id parsed = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
                if ([parsed isKindOfClass:[NSDictionary class]]) {
                    NSArray* arr = ((NSDictionary*)parsed)[@"ids"];
                    if ([arr isKindOfClass:[NSArray class]]) {
                        for (id item in arr) {
                            if ([item isKindOfClass:[NSString class]]) [ids addObject:item];
                        }
                    }
                }
            }
        }
        void (^apply)(void) = ^{ [XMOfferwallManager shared].publisherUserIds = ids; };
        if ([NSThread isMainThread]) apply();
        else dispatch_sync(dispatch_get_main_queue(), apply);
    }

    // Returns a JSON object of the form {"ids":["id1","id2"]} (see _XsollaOfferwall_SetPublisherUserIds).
    const char* _XsollaOfferwall_GetPublisherUserIds() {
        __block NSArray<NSString*>* ids = nil;
        void (^read)(void) = ^{ ids = [XMOfferwallManager shared].publisherUserIds; };
        if ([NSThread isMainThread]) read();
        else dispatch_sync(dispatch_get_main_queue(), read);

        NSDictionary* wrapper = @{ @"ids": ids ?: @[] };
        NSData* data = [NSJSONSerialization dataWithJSONObject:wrapper options:0 error:nil];
        NSString* json = data ? [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] : @"{\"ids\":[]}";
        return strdup([json UTF8String]);
    }

    const char* _XsollaOfferwall_GetVersion() {
        NSBundle* sdkBundle = [NSBundle bundleForClass:[XMOfferwallManager class]];
        NSString* version = [sdkBundle objectForInfoDictionaryKey:@"CFBundleShortVersionString"];
        return version ? strdup([version UTF8String]) : strdup("unknown");
    }
}
