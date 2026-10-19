using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Màu chung và hàm tiện ích dựng UI, HUDBuilder dùng để tạo bố cục prefab.</summary>
public static class UIFactory
{
    public static readonly Color Ink = new Color32(0x0B, 0x14, 0x17, 0xFF);
    public static readonly Color Water = new Color32(0x3F, 0xD6, 0xE8, 0xFF);
    public static readonly Color Hazard = new Color32(0xF2, 0xC2, 0x30, 0xFF);
    public static readonly Color Hurt = new Color32(0xFF, 0x5A, 0x4F, 0xFF);
    public static readonly Color Muted = new Color32(0x8A, 0xA4, 0xAB, 0xFF);
    public static readonly Color Fg = new Color32(0xE3, 0xEE, 0xF0, 0xFF);

    public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    public static Image Panel(string name, Transform parent, Color color)
    {
        var rt = Rect(name, parent, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(rt);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI Text(string name, RectTransform rt, string text, float size, Color color,
                                       TextAlignmentOptions align, FontStyles style = FontStyles.Bold)
    {
        rt.name = name;
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = style;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static Button Button(Transform parent, string label, Vector2 pos, Vector2 size,
                                Color bg, Color fg, UnityAction onClick = null)
    {
        var rt = Rect("Button " + label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = bg;

        var btn = rt.gameObject.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.selectedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        btn.colors = colors;
        if (onClick != null) btn.onClick.AddListener(onClick);

        var textRt = Rect("Label", rt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Text("Label", textRt, label, 34, fg, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
        return btn;
    }
}
