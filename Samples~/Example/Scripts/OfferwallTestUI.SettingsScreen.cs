using UnityEngine;
using UnityEngine.UI;
using Xsolla.Offerwall;

public partial class OfferwallTestUI
{
    private void BuildSettingsScreen(Transform parent)
    {
        _settingsScreenGo = new GameObject("SettingsScreen", typeof(RectTransform));
        _settingsScreenGo.transform.SetParent(parent, false);
        Stretch(_settingsScreenGo);

        var scrollGo = CreateScrollView(_settingsScreenGo.transform, "SettingsScroll");
        Stretch(scrollGo);
        var c = scrollGo.transform.Find("Viewport/Content");

        CreateLabel(c, "Settings", 42, FontStyle.Bold, Color.black, TextAnchor.MiddleCenter);
        CreateSpacer(c, 20);

        // User ID
        var userIdSection = CreateSection(c, "User ID");
        CreateFieldLabel(userIdSection.transform, "User ID");
        _userIdInput = CreateInputField(userIdSection.transform, userId);
        XsollaOfferwall.SetUserId(userId);
        _userIdInput.onValueChanged.AddListener(v =>
        {
            userId = v;
            XsollaOfferwall.SetUserId(v);
        });
        CreateSpacer(c, 20);

        // Publisher User IDs
        var publisherUserIdsSection = CreateSection(c, "Publisher User IDs");
        BuildPublisherUserIdsUI(publisherUserIdsSection.transform);
        CreateSpacer(c, 20);

        // Privacy Policy
        var privSection = CreateSection(c, "Privacy Policy");

        CreateFieldLabel(privSection.transform, "Subject to GDPR");
        _refreshGdpr = CreateNullableBoolSelector(privSection.transform, _subjectToGDPR, v => _subjectToGDPR = v);
        CreateSpacer(privSection.transform, 20);

        CreateFieldLabel(privSection.transform, "User Consent");
        _userConsentInput = CreateInputField(privSection.transform, userConsent);
        _userConsentInput.onValueChanged.AddListener(v => userConsent = v);
        CreateSpacer(privSection.transform, 20);

        CreateFieldLabel(privSection.transform, "Below Consent Age");
        _refreshBelowAge = CreateNullableBoolSelector(privSection.transform, _belowConsentAge, v => _belowConsentAge = v);
        CreateSpacer(privSection.transform, 20);

        CreateFieldLabel(privSection.transform, "US Privacy");
        _usPrivacyInput = CreateInputField(privSection.transform, usPrivacy);
        _usPrivacyInput.onValueChanged.AddListener(v => usPrivacy = v);

        CreateSpacer(c, 20);

        // Orientation
        var orientationSection = CreateSection(c, "Orientation");
        _refreshOrientation = CreateOrientationSelector(orientationSection.transform,
            XsollaOfferwall.Settings.Orientation,
            v => XsollaOfferwall.Settings.Orientation = v);

        CreateSpacer(c, 20);

        // Log Level — persisted via PlayerPrefs because the Android SDK does not store logLevel
        // between sessions: XOSettings.logLevel resets to null (Logger defaults to ERROR) on every
        // app launch. Without this, the UI always shows "Default" after a restart even when the
        // user explicitly set a different level in a previous session.
        var logLevelSection = CreateSection(c, "Log Level");
        var savedLogLevel = LogLevelPref.Load();
        if (savedLogLevel.HasValue)
            XsollaOfferwall.Settings.LogLevel = savedLogLevel.Value;

        _refreshLogLevel = CreateLogLevelSelector(logLevelSection.transform,
            XsollaOfferwall.Settings.LogLevel,
            v =>
            {
                XsollaOfferwall.Settings.LogLevel = v;
                LogLevelPref.Save(v);
            });

        CreateSpacer(c, 20);

        // Device Identifiers — Android only
        if (Application.platform == RuntimePlatform.Android)
        {
            var devSection = CreateSection(c, "Device Identifiers");
            _androidDeviceIdLabel = CreateLabel(devSection.transform, GetAndroidDeviceIdStateText(),
                26, FontStyle.Normal, ColorGray, TextAnchor.MiddleLeft);
            CreateSpacer(devSection.transform, 10);
            CreateHorizontalButtonPair(devSection.transform,
                "Enable",  ColorBlue, EnableAndroidDeviceId,
                "Disable", ColorRed,  DisableAndroidDeviceId);
        }

        CreateSpacer(c, 40);
    }

    // ── Publisher User IDs UI ─────────────────────────────────────────────────

    /// <summary>
    /// Builds the publisher user IDs list UI: a dynamic list of current IDs with remove
    /// buttons, plus an input row to add new ones. Seeded from <see cref="XsollaOfferwall.GetPublisherUserIds"/>
    /// so previously persisted IDs are visible on load.
    /// </summary>
    private void BuildPublisherUserIdsUI(Transform parent)
    {
        _publisherUserIds.Clear();
        _publisherUserIds.AddRange(XsollaOfferwall.GetPublisherUserIds());

        // Dynamic list of current IDs
        var listGo = new GameObject("PublisherUserIdsList",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listGo.transform.SetParent(parent, false);
        var vlg = listGo.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 8;
        listGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _publisherUserIdsListParent = listGo.transform;

        CreateSpacer(parent, 12);

        // Add row: [Publisher User ID] [+ Add]
        var addRow = new GameObject("AddRow",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        addRow.transform.SetParent(parent, false);
        var addHlg = addRow.GetComponent<HorizontalLayoutGroup>();
        addHlg.childControlWidth      = true;
        addHlg.childControlHeight     = true;
        addHlg.childForceExpandWidth  = false;
        addHlg.childForceExpandHeight = true;
        addHlg.spacing = 8;
        var addRowLe = addRow.GetComponent<LayoutElement>();
        addRowLe.minHeight       = 64;
        addRowLe.preferredHeight = 64;

        _addPublisherUserIdInput = CreateRowInputField(addRow.transform, "", "Publisher User ID");

        var addBtnGo = new GameObject("AddBtn",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        addBtnGo.transform.SetParent(addRow.transform, false);
        addBtnGo.GetComponent<Image>().color = ColorGreen;
        var btnLe = addBtnGo.GetComponent<LayoutElement>();
        btnLe.minWidth       = 130;
        btnLe.preferredWidth = 130;
        addBtnGo.GetComponent<Button>().onClick.AddListener(AddPublisherUserId);

        var addLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        addLabelGo.transform.SetParent(addBtnGo.transform, false);
        var addLabel = addLabelGo.GetComponent<Text>();
        addLabel.text          = "+ Add";
        addLabel.font          = GetFont();
        addLabel.fontSize      = 24;
        addLabel.fontStyle     = FontStyle.Bold;
        addLabel.color         = Color.white;
        addLabel.alignment     = TextAnchor.MiddleCenter;
        addLabel.raycastTarget = false;
        StretchToParent(addLabelGo.GetComponent<RectTransform>());

        RefreshPublisherUserIdsList();
    }

    private void AddPublisherUserId()
    {
        string id = _addPublisherUserIdInput != null ? _addPublisherUserIdInput.text.Trim() : "";
        if (string.IsNullOrEmpty(id) || _publisherUserIds.Contains(id)) return;

        _publisherUserIds.Add(id);
        if (_addPublisherUserIdInput != null) _addPublisherUserIdInput.text = "";

        XsollaOfferwall.SetPublisherUserIds(_publisherUserIds);
        RefreshPublisherUserIdsList();
    }

    private void RemovePublisherUserId(string id)
    {
        _publisherUserIds.Remove(id);
        XsollaOfferwall.SetPublisherUserIds(_publisherUserIds);
        RefreshPublisherUserIdsList();
    }

    private void RefreshPublisherUserIdsList()
    {
        if (_publisherUserIdsListParent == null) return;

        // Deactivate then destroy all current children
        for (int i = _publisherUserIdsListParent.childCount - 1; i >= 0; i--)
        {
            var child = _publisherUserIdsListParent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        if (_publisherUserIds.Count == 0)
        {
            var emptyGo = new GameObject("Empty",
                typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            emptyGo.transform.SetParent(_publisherUserIdsListParent, false);
            var t = emptyGo.GetComponent<Text>();
            t.text      = "No publisher user IDs set";
            t.font      = GetFont();
            t.fontSize  = 24;
            t.fontStyle = FontStyle.Italic;
            t.color     = ColorGray;
            t.alignment = TextAnchor.MiddleLeft;
            var le = emptyGo.GetComponent<LayoutElement>();
            le.minHeight       = 44;
            le.preferredHeight = 44;
            return;
        }

        foreach (var id in _publisherUserIds)
        {
            string capturedId = id;

            // Row: [id ···············] [×]
            var rowGo = new GameObject("PublisherUserId_" + capturedId,
                typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowGo.transform.SetParent(_publisherUserIdsListParent, false);
            rowGo.GetComponent<Image>().color = new Color(0.92f, 0.95f, 1f);

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 8;
            hlg.padding = new RectOffset(16, 8, 0, 0);

            var rowLe = rowGo.GetComponent<LayoutElement>();
            rowLe.minHeight       = 56;
            rowLe.preferredHeight = 56;

            // id label
            var idGo = new GameObject("Id",
                typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            idGo.transform.SetParent(rowGo.transform, false);
            var idText = idGo.GetComponent<Text>();
            idText.text      = capturedId;
            idText.font      = GetFont();
            idText.fontSize  = 24;
            idText.color     = Color.black;
            idText.alignment = TextAnchor.MiddleLeft;
            idText.horizontalOverflow = HorizontalWrapMode.Wrap;
            idGo.GetComponent<LayoutElement>().flexibleWidth = 1;

            // × remove button
            var rmGo = new GameObject("Remove",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            rmGo.transform.SetParent(rowGo.transform, false);
            rmGo.GetComponent<Image>().color = ColorRed;
            var rmLe = rmGo.GetComponent<LayoutElement>();
            rmLe.minWidth       = 64;
            rmLe.preferredWidth = 64;
            rmGo.GetComponent<Button>().onClick.AddListener(() => RemovePublisherUserId(capturedId));

            var rmTextGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            rmTextGo.transform.SetParent(rmGo.transform, false);
            var rmText = rmTextGo.GetComponent<Text>();
            rmText.text          = "×";
            rmText.font          = GetFont();
            rmText.fontSize      = 34;
            rmText.fontStyle     = FontStyle.Bold;
            rmText.color         = Color.white;
            rmText.alignment     = TextAnchor.MiddleCenter;
            rmText.raycastTarget = false;
            StretchToParent(rmTextGo.GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// Six-segment control for log level laid out as two rows of three:
    /// [Default | Verbose | Debug] / [Info | Warning | Error].
    /// Returns an action to programmatically update the selection without firing onChange.
    /// </summary>
    private System.Action<OfferwallLogLevel?> CreateLogLevelSelector(
        Transform parent, OfferwallLogLevel? initial, System.Action<OfferwallLogLevel?> onChange)
    {
        string[]           labels = { "Default", "Verbose", "Debug", "Info", "Warning", "Error" };
        OfferwallLogLevel?[] values = { null,
            OfferwallLogLevel.Verbose, OfferwallLogLevel.Debug,
            OfferwallLogLevel.Info,    OfferwallLogLevel.Warning, OfferwallLogLevel.Error };

        var segImages = new Image[6];
        var segTexts  = new Text[6];

        // Build two rows of three segments each
        for (int row = 0; row < 2; row++)
        {
            var rowGo = new GameObject($"LogLevelRow{row}",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowGo.transform.SetParent(parent, false);
            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childControlWidth      = true;
            hlg.childControlHeight     = true;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            hlg.spacing = 4;
            var rowLe = rowGo.GetComponent<LayoutElement>();
            rowLe.minHeight       = 66;
            rowLe.preferredHeight = 66;
            for (int col = 0; col < 3; col++)
            {
                int idx = row * 3 + col;
                OfferwallLogLevel? segValue = values[idx];
                bool isActive = initial == segValue;

                var segGo = new GameObject("Seg_" + labels[idx],
                    typeof(RectTransform), typeof(Image), typeof(Button));
                segGo.transform.SetParent(rowGo.transform, false);

                segImages[idx]       = segGo.GetComponent<Image>();
                segImages[idx].color = isActive ? ColorSegActive : ColorSegInactive;

                OfferwallLogLevel? capturedVal = segValue;
                segGo.GetComponent<Button>().onClick.AddListener(() =>
                {
                    onChange(capturedVal);
                    for (int j = 0; j < 6; j++)
                    {
                        bool active = values[j] == capturedVal;
                        segImages[j].color = active ? ColorSegActive   : ColorSegInactive;
                        segTexts[j].color  = active ? Color.white      : ColorGray;
                    }
                });

                var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(segGo.transform, false);
                var t = textGo.GetComponent<Text>();
                t.text          = labels[idx];
                t.font          = GetFont();
                t.fontSize      = 22;
                t.fontStyle     = FontStyle.Bold;
                t.color         = isActive ? Color.white : ColorGray;
                t.alignment     = TextAnchor.MiddleCenter;
                t.raycastTarget = false;
                segTexts[idx]   = t;
                StretchToParent(textGo.GetComponent<RectTransform>());
            }

            if (row == 0) CreateSpacer(parent, 4);
        }

        return (newValue) =>
        {
            for (int j = 0; j < 6; j++)
            {
                bool active = values[j] == newValue;
                segImages[j].color = active ? ColorSegActive   : ColorSegInactive;
                segTexts[j].color  = active ? Color.white      : ColorGray;
            }
        };
    }

    /// <summary>
    /// Three-segment control: True | False | Not Set.
    /// Active segment is highlighted blue; selection updates the supplied <paramref name="onChange"/> callback.
    /// Returns an action that can be called to programmatically update the highlighted segment.
    /// </summary>
    private System.Action<bool?> CreateNullableBoolSelector(
        Transform parent, bool? initial, System.Action<bool?> onChange)
    {
        var rowGo = new GameObject("SegmentedControl",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGo.transform.SetParent(parent, false);

        var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true; // makes segments fill the row's height
        hlg.spacing = 4;

        var rowLe = rowGo.GetComponent<LayoutElement>();
        rowLe.minHeight       = 70;
        rowLe.preferredHeight = 70;

        string[] segLabels = { "True", "False", "Not Set" };
        bool?[]  segValues = { true,   false,   null      };

        var segImages = new Image[3];
        var segTexts  = new Text[3];

        for (int i = 0; i < 3; i++)
        {
            bool? segValue = segValues[i];
            bool  isActive = initial == segValue;

            var segGo = new GameObject("Seg_" + segLabels[i],
                typeof(RectTransform), typeof(Image), typeof(Button));
            segGo.transform.SetParent(rowGo.transform, false);

            segImages[i]       = segGo.GetComponent<Image>();
            segImages[i].color = isActive ? ColorSegActive : ColorSegInactive;

            // Capture loop variable for the listener closure
            int   capturedIdx = i;
            bool? capturedVal = segValue;
            segGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                onChange(capturedVal);
                for (int j = 0; j < 3; j++)
                {
                    bool active = segValues[j] == capturedVal;
                    segImages[j].color = active ? ColorSegActive   : ColorSegInactive;
                    segTexts[j].color  = active ? Color.white      : ColorGray;
                }
            });

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(segGo.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text          = segLabels[i];
            t.font          = GetFont();
            t.fontSize      = 26;
            t.fontStyle     = FontStyle.Bold;
            t.color         = isActive ? Color.white : ColorGray;
            t.alignment     = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            segTexts[i]     = t;
            StretchToParent(textGo.GetComponent<RectTransform>());
        }

        // Return a delegate that programmatically updates the highlighted segment without firing onChange.
        return (newValue) =>
        {
            for (int j = 0; j < 3; j++)
            {
                bool active = segValues[j] == newValue;
                segImages[j].color = active ? ColorSegActive   : ColorSegInactive;
                segTexts[j].color  = active ? Color.white      : ColorGray;
            }
        };
    }

    /// <summary>
    /// Three-segment control for orientation: Portrait | Landscape | Unspecified.
    /// Returns an action to programmatically update the selection without firing onChange.
    /// </summary>
    private System.Action<OfferwallOrientation> CreateOrientationSelector(
        Transform parent, OfferwallOrientation initial, System.Action<OfferwallOrientation> onChange)
    {
        string[]               labels = { "Portrait", "Landscape", "Unspecified" };
        OfferwallOrientation[] values = {
            OfferwallOrientation.Portrait,
            OfferwallOrientation.Landscape,
            OfferwallOrientation.Unspecified,
        };

        var rowGo = new GameObject("SegmentedControl",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGo.transform.SetParent(parent, false);

        var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 4;

        var rowLe = rowGo.GetComponent<LayoutElement>();
        rowLe.minHeight       = 70;
        rowLe.preferredHeight = 70;

        var segImages = new Image[3];
        var segTexts  = new Text[3];

        for (int i = 0; i < 3; i++)
        {
            OfferwallOrientation segValue = values[i];
            bool isActive = initial == segValue;

            var segGo = new GameObject("Seg_" + labels[i],
                typeof(RectTransform), typeof(Image), typeof(Button));
            segGo.transform.SetParent(rowGo.transform, false);

            segImages[i]       = segGo.GetComponent<Image>();
            segImages[i].color = isActive ? ColorSegActive : ColorSegInactive;

            OfferwallOrientation capturedVal = segValue;
            segGo.GetComponent<Button>().onClick.AddListener(() =>
            {
                onChange(capturedVal);
                for (int j = 0; j < 3; j++)
                {
                    bool active = values[j] == capturedVal;
                    segImages[j].color = active ? ColorSegActive   : ColorSegInactive;
                    segTexts[j].color  = active ? Color.white      : ColorGray;
                }
            });

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(segGo.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text          = labels[i];
            t.font          = GetFont();
            t.fontSize      = 26;
            t.fontStyle     = FontStyle.Bold;
            t.color         = isActive ? Color.white : ColorGray;
            t.alignment     = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            segTexts[i]     = t;
            StretchToParent(textGo.GetComponent<RectTransform>());
        }

        return (newValue) =>
        {
            for (int j = 0; j < 3; j++)
            {
                bool active = values[j] == newValue;
                segImages[j].color = active ? ColorSegActive   : ColorSegInactive;
                segTexts[j].color  = active ? Color.white      : ColorGray;
            }
        };
    }

    /// <summary>
    /// Persists the chosen log level across app launches via PlayerPrefs.
    /// The key is prefixed with the package name to avoid collisions.
    /// </summary>
    private static class LogLevelPref
    {
        private const string Key = "xsolla_offerwall_log_level";

        public static OfferwallLogLevel? Load()
        {
            var saved = PlayerPrefs.GetString(Key, string.Empty);
            return saved switch
            {
                nameof(OfferwallLogLevel.Verbose) => OfferwallLogLevel.Verbose,
                nameof(OfferwallLogLevel.Debug)   => OfferwallLogLevel.Debug,
                nameof(OfferwallLogLevel.Info)    => OfferwallLogLevel.Info,
                nameof(OfferwallLogLevel.Warning) => OfferwallLogLevel.Warning,
                nameof(OfferwallLogLevel.Error)   => OfferwallLogLevel.Error,
                _                                 => (OfferwallLogLevel?)null,
            };
        }

        public static void Save(OfferwallLogLevel? level)
        {
            if (level.HasValue)
                PlayerPrefs.SetString(Key, level.Value.ToString());
            else
                PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
