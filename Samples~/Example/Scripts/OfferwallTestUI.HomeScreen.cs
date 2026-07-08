using UnityEngine;
using UnityEngine.UI;

public partial class OfferwallTestUI
{
    private void BuildMainScreen(Transform parent)
    {
        _mainScreenGo = new GameObject("MainScreen", typeof(RectTransform));
        _mainScreenGo.transform.SetParent(parent, false);
        Stretch(_mainScreenGo);

        var scrollGo = CreateScrollView(_mainScreenGo.transform, "MainScroll");
        Stretch(scrollGo);
        var c = scrollGo.transform.Find("Viewport/Content");

        CreateLabel(c, "Offerwall Test App", 42, FontStyle.Bold, Color.black, TextAnchor.MiddleCenter);
        CreateSpacer(c, 20);

        // SDK Status
        var statusSection = CreateSection(c, "SDK Status");
        _statusLabel = CreateLabel(statusSection.transform, "Ready",
            28, FontStyle.Normal, ColorGray, TextAnchor.MiddleLeft);
        CreateSpacer(c, 20);

        // Connection — directly below status
        var connectSection = CreateSection(c, "Connection");
        _connectStatusLabel = CreateLabel(connectSection.transform, "Not connected",
            26, FontStyle.Normal, ColorGray, TextAnchor.MiddleLeft);
        CreateSpacer(connectSection.transform, 10);
        CreateActionButton(connectSection.transform, "Connect",
            "Initialise SDK before showing offerwall", ColorGreen, ConnectOfferwall);
        CreateSpacer(c, 20);

        // Offerwall — Placement ID and Show Offerwall grouped in one section
        var offerwallSection = CreateSection(c, "Offerwall");
        CreateFieldLabel(offerwallSection.transform, "Placement ID");
        _placementIdInput = CreateInputField(offerwallSection.transform, placementId);
        CreateSpacer(offerwallSection.transform, 12);
        CreateActionButton(offerwallSection.transform, "Show Offerwall",
            "Applies privacy policy and custom params if set", ColorBlue, ShowOfferwall);
        CreateSpacer(c, 20);

        // Custom Parameters — below Show Offerwall
        var paramsSection = CreateSection(c, "Custom Parameters");
        BuildCustomParamsUI(paramsSection.transform);
        CreateSpacer(c, 20);

#if UNITY_IOS && IOS_SUPPORT_AVAILABLE
        {
            var attSection = CreateSection(c, "App Tracking Transparency");
            _attStatusLabel = CreateLabel(attSection.transform, "Status: Not Requested",
                26, FontStyle.Normal, ColorGray, TextAnchor.MiddleLeft);
            CreateSpacer(attSection.transform, 10);
            CreateActionButton(attSection.transform, "Request ATT Authorization",
                "Shows the iOS tracking prompt", ColorPurple, RequestAttAuthorization);
            CreateSpacer(c, 20);
        }
#endif

        // Log
        var logSection = CreateSection(c, "Log");
        var logArea    = CreateLogArea(logSection.transform);
        _logText       = logArea.GetComponentInChildren<Text>();
        _logScrollRect = logArea.GetComponent<ScrollRect>();
        CreateSpacer(logSection.transform, 12);
        CreateSimpleButton(logSection.transform, "Clear Log", ColorGray,
            () => { _logMessages = ""; if (_logText) _logText.text = ""; });

        CreateSpacer(c, 40);
    }

    // ── Custom Params UI ──────────────────────────────────────────────────────

    private void BuildCustomParamsUI(Transform parent)
    {
        // Dynamic list of current params
        var listGo = new GameObject("ParamsList",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        listGo.transform.SetParent(parent, false);
        var vlg = listGo.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 8;
        listGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        _customParamsListParent = listGo.transform;

        CreateSpacer(parent, 12);

        // Add row: [Key] [Value] [+ Add]
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

        _addKeyInput   = CreateRowInputField(addRow.transform, "", "Key");
        _addValueInput = CreateRowInputField(addRow.transform, "", "Value");

        var addBtnGo = new GameObject("AddBtn",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        addBtnGo.transform.SetParent(addRow.transform, false);
        addBtnGo.GetComponent<Image>().color = ColorGreen;
        var btnLe = addBtnGo.GetComponent<LayoutElement>();
        btnLe.minWidth       = 130;
        btnLe.preferredWidth = 130;
        addBtnGo.GetComponent<Button>().onClick.AddListener(AddCustomParam);

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

        RefreshCustomParamsList();
    }

    private void AddCustomParam()
    {
        string key = _addKeyInput   != null ? _addKeyInput.text.Trim()   : "";
        string val = _addValueInput != null ? _addValueInput.text.Trim() : "";
        if (string.IsNullOrEmpty(key)) return;

        _customParams[key] = val;
        if (_addKeyInput   != null) _addKeyInput.text   = "";
        if (_addValueInput != null) _addValueInput.text = "";

        RefreshCustomParamsList();
    }

    private void RemoveCustomParam(string key)
    {
        _customParams.Remove(key);
        RefreshCustomParamsList();
    }

    private void RefreshCustomParamsList()
    {
        if (_customParamsListParent == null) return;

        // Deactivate then destroy all current children
        for (int i = _customParamsListParent.childCount - 1; i >= 0; i--)
        {
            var child = _customParamsListParent.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }

        if (_customParams.Count == 0)
        {
            var emptyGo = new GameObject("Empty",
                typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            emptyGo.transform.SetParent(_customParamsListParent, false);
            var t = emptyGo.GetComponent<Text>();
            t.text      = "No custom parameters set";
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

        foreach (var kvp in _customParams)
        {
            string capturedKey = kvp.Key;

            // Row: [key = value ···············] [×]
            var rowGo = new GameObject("Param_" + capturedKey,
                typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            rowGo.transform.SetParent(_customParamsListParent, false);
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

            // "key = value" label
            var kvGo = new GameObject("KV",
                typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            kvGo.transform.SetParent(rowGo.transform, false);
            var kvText = kvGo.GetComponent<Text>();
            kvText.text      = $"{capturedKey} = {kvp.Value}";
            kvText.font      = GetFont();
            kvText.fontSize  = 24;
            kvText.color     = Color.black;
            kvText.alignment = TextAnchor.MiddleLeft;
            kvText.horizontalOverflow = HorizontalWrapMode.Wrap;
            kvGo.GetComponent<LayoutElement>().flexibleWidth = 1;

            // × remove button
            var rmGo = new GameObject("Remove",
                typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            rmGo.transform.SetParent(rowGo.transform, false);
            rmGo.GetComponent<Image>().color = ColorRed;
            var rmLe = rmGo.GetComponent<LayoutElement>();
            rmLe.minWidth       = 64;
            rmLe.preferredWidth = 64;
            rmGo.GetComponent<Button>().onClick.AddListener(() => RemoveCustomParam(capturedKey));

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
}
