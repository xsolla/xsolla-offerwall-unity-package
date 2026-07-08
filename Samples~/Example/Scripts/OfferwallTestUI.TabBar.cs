using UnityEngine;
using UnityEngine.UI;

public partial class OfferwallTestUI
{
    private void BuildTabBar(Transform parent, float height)
    {
        // 1px separator line above the bar
        var lineGo = new GameObject("TabSeparator", typeof(RectTransform), typeof(Image));
        lineGo.transform.SetParent(parent, false);
        lineGo.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.83f);
        var lineRt = lineGo.GetComponent<RectTransform>();
        lineRt.anchorMin = new Vector2(0, 0);
        lineRt.anchorMax = new Vector2(1, 0);
        lineRt.pivot     = new Vector2(0.5f, 0);
        lineRt.offsetMin = new Vector2(0, height);
        lineRt.offsetMax = new Vector2(0, height + 2);

        // Bar background
        var barGo = new GameObject("TabBar",
            typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
        barGo.transform.SetParent(parent, false);
        barGo.GetComponent<Image>().color = Color.white;

        var rt = barGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot     = new Vector2(0.5f, 0);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = new Vector2(0, height);

        var hlg = barGo.GetComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 1; // thin divider between tabs

        (_homeTabBg,     _homeTabLabel)     = BuildTabButton(barGo.transform, "Home",     ShowHomeTab);
        (_settingsTabBg, _settingsTabLabel) = BuildTabButton(barGo.transform, "Settings", ShowSettingsTab);
    }

    private (Image bg, Text label) BuildTabButton(
        Transform parent, string label, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject("Tab_" + label,
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var img = go.GetComponent<Image>();
        img.color = ColorTabInactive;

        go.GetComponent<Button>().onClick.AddListener(onClick);

        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment       = TextAnchor.MiddleCenter;
        vlg.childControlWidth    = true;
        vlg.childControlHeight   = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(0, 0, 16, 10);

        var textGo = new GameObject("Label",
            typeof(RectTransform), typeof(Text), typeof(LayoutElement));
        textGo.transform.SetParent(go.transform, false);
        var t = textGo.GetComponent<Text>();
        t.text          = label;
        t.font          = GetFont();
        t.fontSize      = 28;
        t.fontStyle     = FontStyle.Bold;
        t.color         = ColorGray;
        t.alignment     = TextAnchor.MiddleCenter;
        t.raycastTarget = false;
        textGo.GetComponent<LayoutElement>().minHeight = 40;

        return (img, t);
    }

    private void ShowHomeTab()
    {
        _mainScreenGo.SetActive(true);
        _settingsScreenGo.SetActive(false);
        ApplyTabState(_homeTabBg,     _homeTabLabel,     active: true);
        ApplyTabState(_settingsTabBg, _settingsTabLabel, active: false);
    }

    private void ShowSettingsTab()
    {
        _mainScreenGo.SetActive(false);
        _settingsScreenGo.SetActive(true);
        ApplyTabState(_homeTabBg,     _homeTabLabel,     active: false);
        ApplyTabState(_settingsTabBg, _settingsTabLabel, active: true);
    }

    private static void ApplyTabState(Image bg, Text label, bool active)
    {
        bg.color    = active ? ColorTabActive   : ColorTabInactive;
        label.color = active ? Color.white      : ColorGray;
    }
}
