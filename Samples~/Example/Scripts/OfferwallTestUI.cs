 using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Xsolla.Offerwall;

public class OfferwallTestUI : MonoBehaviour
{
    [Header("Offerwall Settings")]
    [SerializeField] private string placementId = ""; //INSERT_PLACEMENT_ID
    [SerializeField] private string userId = ""; //INSERT_USER_ID

    [Header("Privacy Policy")]
    [SerializeField] private bool subjectToGDPR = true;
    [SerializeField] private string userConsent = "1";
    [SerializeField] private bool belowConsentAge = false;
    [SerializeField] private string usPrivacy = "1YNN";

    [Header("Custom Parameters")]
    [SerializeField] private string customParamKey = "campaign";
    [SerializeField] private string customParamValue = "unity_test";

    private Text _statusLabel;
    private Text _logText;
    private ScrollRect _logScrollRect;
    private InputField _placementIdInput;
    private InputField _userIdInput;
    private string _logMessages = "";

    private static readonly Color ColorBackground = new Color(0.94f, 0.94f, 0.96f);
    private static readonly Color ColorSectionBg = Color.white;
    private static readonly Color ColorSectionHeader = new Color(0.4f, 0.4f, 0.45f);
    private static readonly Color ColorBlue = new Color(0.2f, 0.5f, 1f);
    private static readonly Color ColorPurple = new Color(0.55f, 0.35f, 0.85f);
    private static readonly Color ColorOrange = new Color(0.95f, 0.6f, 0.2f);
    private static readonly Color ColorRed = new Color(0.9f, 0.3f, 0.3f);
    private static readonly Color ColorGray = new Color(0.55f, 0.55f, 0.6f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindObjectOfType<OfferwallTestUI>() != null) return;
        var go = new GameObject("OfferwallTestUI");
        go.AddComponent<OfferwallTestUI>();
    }

    private void Awake()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        var canvasGo = new GameObject("OfferwallTestCanvas");
        canvasGo.transform.SetParent(transform);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();

        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            // Support both Legacy and New Input System — use reflection so this
            // sample compiles without a hard dependency on com.unity.inputsystem.
            var newInputModule = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (newInputModule != null)
                esGo.AddComponent(newInputModule);
            else
                esGo.AddComponent<StandaloneInputModule>();
        }

        // Background
        var bgGo = CreatePanel(canvasGo.transform, "Background", ColorBackground);
        Stretch(bgGo);

        // Safe area panel
        var safeArea = CreatePanel(canvasGo.transform, "SafeArea", new Color(0, 0, 0, 0));
        Stretch(safeArea);
        safeArea.AddComponent<SafeAreaFitter>();

        // Main scroll view
        var scrollGo = CreateScrollView(safeArea.transform, "MainScroll");
        Stretch(scrollGo);
        var contentParent = scrollGo.transform.Find("Viewport/Content");

        // Title
        CreateLabel(contentParent, "Offerwall Test App", 42, FontStyle.Bold, Color.black, TextAnchor.MiddleCenter);
        CreateSpacer(contentParent, 20);

        // SDK Status section
        var statusSection = CreateSection(contentParent, "SDK Status");
        _statusLabel = CreateLabel(statusSection.transform, "Ready", 28, FontStyle.Normal, ColorGray, TextAnchor.MiddleLeft);
        CreateSpacer(contentParent, 20);

        // Configuration section
        var configSection = CreateSection(contentParent, "Configuration");
        CreateFieldLabel(configSection.transform, "Placement ID");
        _placementIdInput = CreateInputField(configSection.transform, placementId);
        CreateSpacer(configSection.transform, 12);
        CreateFieldLabel(configSection.transform, "User ID");
        _userIdInput = CreateInputField(configSection.transform, userId);
        CreateSpacer(contentParent, 20);

        // Presentation section
        var presentSection = CreateSection(contentParent, "Presentation");
        CreateActionButton(presentSection.transform, "Show Offerwall", "Default settings — full screen", ColorBlue, ShowBasicOfferwall);
        CreateSpacer(contentParent, 20);

        // Privacy section
        var privacySection = CreateSection(contentParent, "Privacy");
        CreateActionButton(privacySection.transform, "With Privacy Policy", "GDPR + CCPA params", ColorPurple, ShowWithPrivacyPolicy);
        CreateSpacer(contentParent, 20);

        // Custom Parameters section
        var paramsSection = CreateSection(contentParent, "Custom Parameters");
        CreateActionButton(paramsSection.transform, "With Custom Params", "campaign=test & source=unity_test_app", ColorOrange, ShowWithCustomParams);
        CreateSpacer(contentParent, 20);

        // Controls section
        var controlsSection = CreateSection(contentParent, "Controls");
        CreateActionButton(controlsSection.transform, "Dismiss Offerwall", "Programmatically dismiss", ColorRed, DismissOfferwall);
        CreateSpacer(contentParent, 20);

        // Log section
        var logSection = CreateSection(contentParent, "Log");
        var logScrollGo = CreateLogArea(logSection.transform);
        _logText = logScrollGo.GetComponentInChildren<Text>();
        _logScrollRect = logScrollGo.GetComponent<ScrollRect>();
        CreateSpacer(logSection.transform, 12);
        CreateSimpleButton(logSection.transform, "Clear Log", ColorGray, () => { _logMessages = ""; _logText.text = ""; });

        CreateSpacer(contentParent, 40);
    }

    // ── SDK Actions ──

    private void ShowBasicOfferwall()
    {
        SyncInputFields();
        var settings = new OfferwallSettings(placementId, userId);
        Log("Showing basic offerwall...");
        _statusLabel.text = "Opening...";
        XsollaOfferwall.Show(settings, OnDismissed);
    }

    private void ShowWithPrivacyPolicy()
    {
        SyncInputFields();
        var settings = new OfferwallSettings(placementId, userId)
        {
            PrivacyPolicy = new OfferwallPrivacyPolicy
            {
                SubjectToGDPR = subjectToGDPR,
                UserConsent = userConsent,
                BelowConsentAge = belowConsentAge,
                UsPrivacy = usPrivacy
            }
        };
        Log("Showing with privacy policy...");
        _statusLabel.text = "Opening (privacy)...";
        XsollaOfferwall.Show(settings, OnDismissed);
    }

    private void ShowWithCustomParams()
    {
        SyncInputFields();
        var settings = new OfferwallSettings(placementId, userId)
        {
            CustomParameters = new Dictionary<string, string>
            {
                { customParamKey, customParamValue },
                { "source", "unity_test_app" }
            }
        };
        Log("Showing with custom params...");
        _statusLabel.text = "Opening (custom)...";
        XsollaOfferwall.Show(settings, OnDismissed);
    }

    private void DismissOfferwall()
    {
        XsollaOfferwall.Dismiss();
        Log("Dismiss called");
        _statusLabel.text = "Dismissed";
    }

    private void OnDismissed(string error)
    {
        if (error != null)
        {
            Log($"ERROR: {error}");
            _statusLabel.text = "Error";
        }
        else
        {
            Log("Offerwall dismissed successfully");
            _statusLabel.text = "Dismissed";
        }
    }

    private void SyncInputFields()
    {
        if (_placementIdInput != null) placementId = _placementIdInput.text;
        if (_userIdInput != null) userId = _userIdInput.text;
    }

    private void Log(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        _logMessages += $"[{timestamp}] {message}\n";
        if (_logText != null) _logText.text = _logMessages;
        if (_logScrollRect != null)
            Canvas.ForceUpdateCanvases();
        Debug.Log($"[OfferwallTest] {message}");
    }

    // ── UI Builder Helpers ──

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

    private static GameObject CreateScrollView(Transform parent, string name)
    {
        var scrollGo = new GameObject(name, typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(parent, false);
        scrollGo.GetComponent<Image>().color = new Color(0, 0, 0, 0);
        var scrollRect = scrollGo.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 40;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        viewportGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(viewportGo);

        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.offsetMin = new Vector2(40, 0);
        contentRt.offsetMax = new Vector2(-40, 0);

        var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 0;
        vlg.padding = new RectOffset(0, 0, 30, 30);

        var csf = contentGo.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
        scrollRect.content = contentRt;

        return scrollGo;
    }

    private static GameObject CreateSection(Transform parent, string headerText)
    {
        // Section header label
        CreateSectionHeader(parent, headerText);

        // Section body with background
        var sectionGo = new GameObject("Section_" + headerText, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        sectionGo.transform.SetParent(parent, false);
        sectionGo.GetComponent<Image>().color = ColorSectionBg;

        var vlg = sectionGo.GetComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 0;
        vlg.padding = new RectOffset(28, 28, 20, 20);

        var le = sectionGo.AddComponent<LayoutElement>();
        le.minHeight = 60;

        return sectionGo;
    }

    private static void CreateSectionHeader(Transform parent, string text)
    {
        var go = new GameObject("Header_" + text, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var t = go.GetComponent<Text>();
        t.text = text.ToUpper();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 24;
        t.fontStyle = FontStyle.Bold;
        t.color = ColorSectionHeader;
        t.alignment = TextAnchor.LowerLeft;

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 50;
        le.preferredHeight = 50;

        go.GetComponent<RectTransform>().offsetMin = new Vector2(8, 0);
    }

    private static Text CreateLabel(Transform parent, string text, int fontSize, FontStyle style, Color color, TextAnchor alignment)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.color = color;
        t.alignment = alignment;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = fontSize + 20;
        le.preferredHeight = fontSize + 20;

        return t;
    }

    private static void CreateFieldLabel(Transform parent, string text)
    {
        var go = new GameObject("FieldLabel", typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 24;
        t.color = ColorGray;

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 36;
        le.preferredHeight = 36;
    }

    private static InputField CreateInputField(Transform parent, string value)
    {
        var go = new GameObject("InputField", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.97f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var textComp = textGo.GetComponent<Text>();
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComp.fontSize = 26;
        textComp.color = Color.black;
        textComp.alignment = TextAnchor.MiddleLeft;
        textComp.supportRichText = false;
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(16, 4);
        textRt.offsetMax = new Vector2(-16, -4);

        var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholderGo.transform.SetParent(go.transform, false);
        var phText = placeholderGo.GetComponent<Text>();
        phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        phText.fontSize = 26;
        phText.fontStyle = FontStyle.Italic;
        phText.color = new Color(0.7f, 0.7f, 0.7f);
        phText.alignment = TextAnchor.MiddleLeft;
        phText.text = "Enter value...";
        var phRt = placeholderGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(16, 4);
        phRt.offsetMax = new Vector2(-16, -4);

        var inputField = go.GetComponent<InputField>();
        inputField.textComponent = textComp;
        inputField.placeholder = phText;
        inputField.text = value;

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 64;
        le.preferredHeight = 64;

        return inputField;
    }

    private static void CreateActionButton(Transform parent, string title, string description, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + title, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;

        var button = go.GetComponent<Button>();
        var cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        button.colors = cb;

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(24, 24, 16, 16);
        vlg.spacing = 4;
        vlg.childAlignment = TextAnchor.MiddleLeft;

        // Title text
        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleGo.transform.SetParent(go.transform, false);
        var titleText = titleGo.GetComponent<Text>();
        titleText.text = title;
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 30;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = Color.white;
        titleText.raycastTarget = false;
        var titleLe = titleGo.AddComponent<LayoutElement>();
        titleLe.minHeight = 36;

        // Description text
        var descGo = new GameObject("Description", typeof(RectTransform), typeof(Text));
        descGo.transform.SetParent(go.transform, false);
        var descText = descGo.GetComponent<Text>();
        descText.text = description;
        descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 22;
        descText.color = new Color(1, 1, 1, 0.8f);
        descText.raycastTarget = false;
        var descLe = descGo.AddComponent<LayoutElement>();
        descLe.minHeight = 28;

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 100;
        le.preferredHeight = 100;

        button.onClick.AddListener(onClick);
    }

    private static void CreateSimpleButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;

        var button = go.GetComponent<Button>();
        var cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f);
        button.colors = cb;

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var textComp = textGo.GetComponent<Text>();
        textComp.text = label;
        textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComp.fontSize = 28;
        textComp.fontStyle = FontStyle.Bold;
        textComp.color = Color.white;
        textComp.alignment = TextAnchor.MiddleCenter;
        textComp.raycastTarget = false;
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var le = go.GetComponent<LayoutElement>();
        le.minHeight = 70;
        le.preferredHeight = 70;

        button.onClick.AddListener(onClick);
    }

    private static GameObject CreateLogArea(Transform parent)
    {
        var scrollGo = new GameObject("LogScroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image), typeof(LayoutElement));
        scrollGo.transform.SetParent(parent, false);
        scrollGo.GetComponent<Image>().color = new Color(0.95f, 0.95f, 0.97f);

        var scrollRect = scrollGo.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;

        var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        viewportGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.01f);
        viewportGo.GetComponent<Mask>().showMaskGraphic = false;
        Stretch(viewportGo);

        var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0, 1);
        contentRt.offsetMin = new Vector2(12, 0);
        contentRt.offsetMax = new Vector2(-12, 0);

        var contentVlg = contentGo.GetComponent<VerticalLayoutGroup>();
        contentVlg.childControlWidth = true;
        contentVlg.childControlHeight = true;
        contentVlg.childForceExpandWidth = true;
        contentVlg.childForceExpandHeight = false;

        var csf = contentGo.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var textGo = new GameObject("LogText", typeof(RectTransform), typeof(Text), typeof(ContentSizeFitter));
        textGo.transform.SetParent(contentGo.transform, false);
        var logText = textGo.GetComponent<Text>();
        logText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        logText.fontSize = 22;
        logText.color = new Color(0.25f, 0.25f, 0.3f);
        logText.alignment = TextAnchor.UpperLeft;
        logText.horizontalOverflow = HorizontalWrapMode.Wrap;
        logText.verticalOverflow = VerticalWrapMode.Overflow;

        var textCsf = textGo.GetComponent<ContentSizeFitter>();
        textCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportGo.GetComponent<RectTransform>();
        scrollRect.content = contentRt;

        var le = scrollGo.GetComponent<LayoutElement>();
        le.minHeight = 300;
        le.preferredHeight = 300;

        return scrollGo;
    }

    private static void CreateSpacer(Transform parent, float height)
    {
        var go = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        go.GetComponent<LayoutElement>().minHeight = height;
    }
}

public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rt;
    private Rect _lastSafeArea;

    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (Screen.safeArea == _lastSafeArea) return;
        _lastSafeArea = Screen.safeArea;

        var anchorMin = _lastSafeArea.position;
        var anchorMax = _lastSafeArea.position + _lastSafeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
    }
}
