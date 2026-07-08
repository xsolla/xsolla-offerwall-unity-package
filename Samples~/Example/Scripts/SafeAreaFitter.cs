using UnityEngine;

/// <summary>
/// Resizes its RectTransform each frame to match the device safe area,
/// keeping UI clear of notches, home indicators, and status bars.
/// </summary>
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rt;
    private Rect          _lastSafeArea;

    private void Awake() => _rt = GetComponent<RectTransform>();

    private void Update()
    {
        if (Screen.safeArea == _lastSafeArea) return;
        _lastSafeArea = Screen.safeArea;

        var anchorMin = _lastSafeArea.position;
        var anchorMax = _lastSafeArea.position + _lastSafeArea.size;
        anchorMin.x /= Screen.width;  anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;  anchorMax.y /= Screen.height;

        _rt.anchorMin = anchorMin;
        _rt.anchorMax = anchorMax;
    }
}
