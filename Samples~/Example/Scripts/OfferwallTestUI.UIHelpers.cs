using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Static UI builder helpers shared across all OfferwallTestUI partial class files.
/// </summary>
public partial class OfferwallTestUI
{
    // ── Font ──────────────────────────────────────────────────────────────────

    private static Font _font;

    private static Font GetFont()
    {
        if (_font != null) return _font;
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (_font == null) _font = Font.CreateDynamicFontFromOSFont("sans-serif", 14);
        return _font;
    }

    // ── Layout helpers ────────────────────────────────────────────────────────

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().minHeight = height;
    }

    // ── Scroll view ───────────────────────────────────────────────────────────

    private static GameObject CreateScrollView(Transform parent, string name)
    {
        var scrollGo = new GameObject(name,
            typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(parent, false);
        scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0);

        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.horizontal        = false;
        sr.movementType      = ScrollRect.MovementType.Elastic;
        sr.scrollSensitivity = 40;

        var vpGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        vpGo.transform.SetParent(scrollGo.transform, false);
        vpGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        vpGo.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(vpGo);

        var contentGo = new GameObject("Content",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(vpGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot     = new Vector2(0.5f, 1);
        contentRt.offsetMin = new Vector2(40, 0);
        contentRt.offsetMax = new Vector2(-40, 0);

        var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment       = TextAnchor.UpperCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 0;
        vlg.padding = new RectOffset(0, 0, 30, 30);

        contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = vpGo.GetComponent<RectTransform>();
        sr.content  = contentRt;

        return scrollGo;
    }

    // ── Section ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a labelled white card section. Returns the card body (add children to its transform).
    /// </summary>
    private static GameObject CreateSection(Transform parent, string headerText)
    {
        // Gray section header label
        var hdrGo = new GameObject("Header_" + headerText,
            typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        hdrGo.transform.SetParent(parent, false);
        var ht = hdrGo.GetComponent<Text>();
        ht.text      = headerText.ToUpper();
        ht.font      = GetFont();
        ht.fontSize  = 24;
        ht.fontStyle = FontStyle.Bold;
        ht.color     = ColorSectionHdr;
        ht.alignment = TextAnchor.LowerLeft;
        var hle = hdrGo.GetComponent<LayoutElement>();
        hle.minHeight       = 50;
        hle.preferredHeight = 50;
        hdrGo.GetComponent<RectTransform>().offsetMin = new Vector2(8, 0);

        // White card body
        var sectionGo = new GameObject("Section_" + headerText,
            typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        sectionGo.transform.SetParent(parent, false);
        sectionGo.GetComponent<Image>().color = ColorSectionBg;

        var vlg = sectionGo.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 0;
        vlg.padding = new RectOffset(28, 28, 20, 20);

        sectionGo.AddComponent<LayoutElement>().minHeight = 60;
        return sectionGo;
    }

    // ── Text ──────────────────────────────────────────────────────────────────

    private static Text CreateLabel(Transform parent, string text, int fontSize,
        FontStyle style, Color color, TextAnchor alignment)
    {
        var go = new GameObject("Label",
            typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text      = text;
        t.font      = GetFont();
        t.fontSize  = fontSize;
        t.fontStyle = style;
        t.color     = color;
        t.alignment = alignment;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        var le = go.GetComponent<LayoutElement>();
        le.minHeight       = fontSize + 20;
        le.preferredHeight = fontSize + 20;
        return t;
    }

    private static void CreateFieldLabel(Transform parent, string text)
    {
        var go = new GameObject("FieldLabel",
            typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text     = text;
        t.font     = GetFont();
        t.fontSize = 24;
        t.color    = ColorGray;
        var le = go.GetComponent<LayoutElement>();
        le.minHeight       = 36;
        le.preferredHeight = 36;
    }

    // ── Input fields ──────────────────────────────────────────────────────────

    /// Standard full-width input field for use inside a section VLG.
    private static InputField CreateInputField(Transform parent, string value)
    {
        var go = new GameObject("InputField",
            typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.97f);
        var le = go.GetComponent<LayoutElement>();
        le.minHeight       = 64;
        le.preferredHeight = 64;
        return WireInputField(go, value, "Enter value...");
    }

    /// Flexible-width input field for use inside a HorizontalLayoutGroup row.
    private static InputField CreateRowInputField(Transform parent, string value, string placeholder)
    {
        var go = new GameObject("InputField",
            typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.97f);
        var le = go.GetComponent<LayoutElement>();
        le.flexibleWidth   = 1;
        le.minHeight       = 64;
        le.preferredHeight = 64;
        return WireInputField(go, value, placeholder);
    }

    private static InputField WireInputField(GameObject go, string value, string placeholder)
    {
        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var tc = textGo.GetComponent<Text>();
        tc.font            = GetFont();
        tc.fontSize        = 26;
        tc.color           = Color.black;
        tc.alignment       = TextAnchor.MiddleLeft;
        tc.supportRichText = false;
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(16, 4);
        textRt.offsetMax = new Vector2(-16, -4);

        var phGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        phGo.transform.SetParent(go.transform, false);
        var ph = phGo.GetComponent<Text>();
        ph.font      = GetFont();
        ph.fontSize  = 26;
        ph.fontStyle = FontStyle.Italic;
        ph.color     = new Color(0.7f, 0.7f, 0.7f);
        ph.alignment = TextAnchor.MiddleLeft;
        ph.text      = placeholder;
        var phRt = phGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(16, 4);
        phRt.offsetMax = new Vector2(-16, -4);

        var input = go.GetComponent<InputField>();
        input.textComponent = tc;
        input.placeholder   = ph;
        input.text          = value;
        return input;
    }

    // ── Buttons ───────────────────────────────────────────────────────────────

    /// Large action button with a bold title and a smaller description subtitle.
    private static void CreateActionButton(Transform parent, string title, string description,
        Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + title,
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding        = new RectOffset(24, 24, 16, 16);
        vlg.spacing        = 4;
        vlg.childAlignment = TextAnchor.MiddleLeft;

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(go.transform, false);
        var tt = titleGo.GetComponent<Text>();
        tt.text          = title;
        tt.font          = GetFont();
        tt.fontSize      = 30;
        tt.fontStyle     = FontStyle.Bold;
        tt.color         = Color.white;
        tt.raycastTarget = false;
        titleGo.AddComponent<LayoutElement>().minHeight = 36;

        var descGo = new GameObject("Description", typeof(RectTransform), typeof(Text));
        descGo.transform.SetParent(go.transform, false);
        var dt = descGo.GetComponent<Text>();
        dt.text          = description;
        dt.font          = GetFont();
        dt.fontSize      = 22;
        dt.color         = new Color(1, 1, 1, 0.8f);
        dt.raycastTarget = false;
        descGo.AddComponent<LayoutElement>().minHeight = 28;

        var le = go.GetComponent<LayoutElement>();
        le.minHeight       = 100;
        le.preferredHeight = 100;
    }

    /// Simple single-label button.
    private static void CreateSimpleButton(Transform parent, string label, Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label,
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var t = textGo.GetComponent<Text>();
        t.text          = label;
        t.font          = GetFont();
        t.fontSize      = 28;
        t.fontStyle     = FontStyle.Bold;
        t.color         = Color.white;
        t.alignment     = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        StretchToParent(textGo.GetComponent<RectTransform>());

        var le = go.GetComponent<LayoutElement>();
        le.minHeight       = 70;
        le.preferredHeight = 70;
    }

    /// Two equally-wide buttons side by side.
    private static void CreateHorizontalButtonPair(Transform parent,
        string l1, Color c1, UnityEngine.Events.UnityAction a1,
        string l2, Color c2, UnityEngine.Events.UnityAction a2)
    {
        var rowGo = new GameObject("ButtonRow",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        rowGo.transform.SetParent(parent, false);
        var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = false;
        hlg.spacing = 12;
        var le = rowGo.GetComponent<LayoutElement>();
        le.minHeight       = 70;
        le.preferredHeight = 70;
        CreateSimpleButton(rowGo.transform, l1, c1, a1);
        CreateSimpleButton(rowGo.transform, l2, c2, a2);
    }

    // ── Version bar ───────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a thin bar pinned above the tab bar showing the SDK version.
    /// Visible on all screens. Stores a reference in <see cref="_versionLabel"/>.
    /// </summary>
    private void BuildVersionBar(Transform parent, float tabBarHeight, float barHeight)
    {
        var barGo = new GameObject("VersionBar", typeof(RectTransform), typeof(Image));
        barGo.transform.SetParent(parent, false);
        barGo.GetComponent<Image>().color = new Color(0.20f, 0.20f, 0.22f, 0.88f);

        var rt = barGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(0f,   tabBarHeight);
        rt.offsetMax = new Vector2(0f,   tabBarHeight + barHeight);

        var labelGo = new GameObject("VersionLabel", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(barGo.transform, false);
        var t = labelGo.GetComponent<Text>();
        t.font      = GetFont();
        t.fontSize  = 20;
        t.color     = new Color(1f, 1f, 1f, 0.70f);
        t.alignment = TextAnchor.MiddleCenter;
        t.text      = Xsolla.Offerwall.XsollaOfferwall.GetVersion();
        StretchToParent(labelGo.GetComponent<RectTransform>());

        _versionLabel = t;
    }

    // ── Log area ──────────────────────────────────────────────────────────────

    private static GameObject CreateLogArea(Transform parent)
    {
        var scrollGo = new GameObject("LogScroll",
            typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(LayoutElement));
        scrollGo.transform.SetParent(parent, false);
        scrollGo.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.97f);

        var sr = scrollGo.GetComponent<ScrollRect>();
        sr.horizontal   = false;
        sr.movementType = ScrollRect.MovementType.Elastic;

        var vpGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        vpGo.transform.SetParent(scrollGo.transform, false);
        vpGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        vpGo.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(vpGo);

        var contentGo = new GameObject("Content",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(vpGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot     = new Vector2(0, 1);
        contentRt.offsetMin = new Vector2(12, 0);
        contentRt.offsetMax = new Vector2(-12, 0);

        var cvlg = contentGo.GetComponent<VerticalLayoutGroup>();
        cvlg.childControlWidth      = true;
        cvlg.childControlHeight     = true;
        cvlg.childForceExpandWidth  = true;
        cvlg.childForceExpandHeight = false;
        contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textGo = new GameObject("LogText",
            typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
        textGo.transform.SetParent(contentGo.transform, false);
        var lt = textGo.GetComponent<Text>();
        lt.font      = GetFont();
        lt.fontSize  = 22;
        lt.color     = new Color(0.25f, 0.25f, 0.3f);
        lt.alignment = TextAnchor.UpperLeft;
        lt.horizontalOverflow = HorizontalWrapMode.Wrap;
        lt.verticalOverflow   = VerticalWrapMode.Overflow;
        textGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        sr.viewport = vpGo.GetComponent<RectTransform>();
        sr.content  = contentRt;

        var le = scrollGo.GetComponent<LayoutElement>();
        le.minHeight       = 300;
        le.preferredHeight = 300;

        return scrollGo;
    }
}
