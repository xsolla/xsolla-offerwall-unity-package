using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Offerwall SDK test UI — bootstrapped at runtime, no scene setup required.
/// Split across partial class files:
///   OfferwallTestUI.cs            — fields, colours, Awake, BuildUI
///   OfferwallTestUI.TabBar.cs     — bottom tab bar and navigation
///   OfferwallTestUI.HomeScreen.cs — home screen + custom params UI
///   OfferwallTestUI.SettingsScreen.cs — settings screen
///   OfferwallTestUI.SDKActions.cs — SDK method calls (Connect, Show …)
///   OfferwallTestUI.UIHelpers.cs  — static UI builder helpers
///   SafeAreaFitter.cs             — SafeAreaFitter MonoBehaviour
/// </summary>
public partial class OfferwallTestUI : MonoBehaviour
{
    // ── Inspector fields ──────────────────────────────────────────────────────

    [Header("Offerwall Settings")]
    [SerializeField] private string placementId = ""; //INSERT_PLACEMENT_ID
    [SerializeField] private string userId      = ""; //INSERT_USER_ID

    [Header("Privacy Policy Defaults")]
    [SerializeField] private string userConsent = "1";
    [SerializeField] private string usPrivacy   = "1YNN";

    // ── Runtime privacy state — null means "Not Set" ──────────────────────────

    private bool? _subjectToGDPR;
    private bool? _belowConsentAge;

    // ── Custom parameters ─────────────────────────────────────────────────────

    private readonly Dictionary<string, string> _customParams = new Dictionary<string, string>();

    // ── UI refs — Custom Params ───────────────────────────────────────────────

    private Transform  _customParamsListParent;
    private InputField _addKeyInput;
    private InputField _addValueInput;

    // ── UI refs — Home screen ─────────────────────────────────────────────────

    private Text       _statusLabel;
    private Text       _connectStatusLabel;
    private Text       _attStatusLabel;
    private Text       _logText;
    private ScrollRect _logScrollRect;
    private InputField _placementIdInput;

    // ── UI refs — Settings screen ─────────────────────────────────────────────

    private InputField              _userIdInput;
    private readonly List<string>   _publisherUserIds = new List<string>();
    private Transform               _publisherUserIdsListParent;
    private InputField              _addPublisherUserIdInput;
    private InputField              _userConsentInput;
    private InputField              _usPrivacyInput;
    private Text                    _androidDeviceIdLabel;
    private System.Action<bool?>                                 _refreshGdpr;
    private System.Action<bool?>                                 _refreshBelowAge;
    private System.Action<Xsolla.Offerwall.OfferwallLogLevel?>   _refreshLogLevel;
    private System.Action<Xsolla.Offerwall.OfferwallOrientation> _refreshOrientation;

    // ── Version UI ────────────────────────────────────────────────────────────

    private Text _versionLabel;

    // ── Navigation ────────────────────────────────────────────────────────────

    private GameObject _mainScreenGo;
    private GameObject _settingsScreenGo;
    private Image      _homeTabBg;
    private Text       _homeTabLabel;
    private Image      _settingsTabBg;
    private Text       _settingsTabLabel;

    // ── Log state ─────────────────────────────────────────────────────────────

    private string _logMessages = "";

    // ── Colours ───────────────────────────────────────────────────────────────

    private static readonly Color ColorBackground  = new Color(0.94f, 0.94f, 0.96f);
    private static readonly Color ColorSectionBg   = Color.white;
    private static readonly Color ColorSectionHdr  = new Color(0.40f, 0.40f, 0.45f);
    private static readonly Color ColorBlue        = new Color(0.20f, 0.50f, 1.00f);
    private static readonly Color ColorGreen       = new Color(0.15f, 0.65f, 0.45f);
    private static readonly Color ColorPurple      = new Color(0.55f, 0.35f, 0.85f);
    private static readonly Color ColorRed         = new Color(0.90f, 0.30f, 0.30f);
    private static readonly Color ColorGray        = new Color(0.55f, 0.55f, 0.60f);
    private static readonly Color ColorTabActive   = new Color(0.20f, 0.50f, 1.00f);
    private static readonly Color ColorTabInactive = new Color(0.95f, 0.95f, 0.97f);
    private static readonly Color ColorSegActive   = new Color(0.20f, 0.50f, 1.00f);
    private static readonly Color ColorSegInactive = new Color(0.82f, 0.82f, 0.85f);

    // ── Bootstrap ─────────────────────────────────────────────────────────────

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindObjectOfType<OfferwallTestUI>() != null) return;
        new GameObject("OfferwallTestUI").AddComponent<OfferwallTestUI>();
    }

    private void Awake() => BuildUI();

    // ── Top-level build ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas
        var canvasGo = new GameObject("OfferwallTestCanvas");
        canvasGo.transform.SetParent(transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Event system
        if (FindObjectOfType<EventSystem>() == null)
        {
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            var nim = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (nim != null) esGo.AddComponent(nim);
            else             esGo.AddComponent<StandaloneInputModule>();
        }

        // Full-screen background
        var bgGo = CreatePanel(canvasGo.transform, "Background", ColorBackground);
        Stretch(bgGo);

        // Safe-area panel
        var safeAreaGo = CreatePanel(canvasGo.transform, "SafeArea", new Color(0, 0, 0, 0));
        Stretch(safeAreaGo);
        safeAreaGo.AddComponent<SafeAreaFitter>();

        const float TabH        = 120f;
        const float VersionBarH = 48f;

        // Tab bar — pinned to bottom of safe area
        BuildTabBar(safeAreaGo.transform, TabH);

        // Version bar — sits just above the tab bar, always visible
        BuildVersionBar(safeAreaGo.transform, TabH, VersionBarH);

        // Content area — fills safe area above the version bar
        var contentGo = new GameObject("ContentArea", typeof(RectTransform));
        contentGo.transform.SetParent(safeAreaGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = Vector2.zero;
        contentRt.anchorMax = Vector2.one;
        contentRt.offsetMin = new Vector2(0, TabH + VersionBarH);
        contentRt.offsetMax = Vector2.zero;

        BuildMainScreen(contentGo.transform);
        BuildSettingsScreen(contentGo.transform);

        UpdateVersionLabel();
        ShowHomeTab();
    }
}
