using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BossHealth))]
public class BossHealthBar : MonoBehaviour
{
    public string bossName = "SIEU CAP BA KHI";
    [Tooltip("Để trống = font TMP mặc định (không có dấu tiếng Việt). Muốn có dấu thì kéo font hỗ trợ dấu vào đây")]
    public TMP_FontAsset fontAsset;

    [Header("Kích thước")]
    public float barWidth = 720f;
    public float barHeight = 28f;
    public float topMargin = 36f;

    [Header("Màu")]
    public Color fillColor = new Color(0.3f, 0.85f, 1f);
    public Color phase2Color = new Color(1f, 0.4f, 0.3f);
    [Tooltip("Thanh nhấp nháy màu này khi boss đang vỡ băng (nhận sát thương gấp đôi)")]
    public Color brokenColor = new Color(1f, 0.95f, 0.4f);
    public Color trailColor = new Color(1f, 1f, 1f, 0.85f);
    public float fillSpeed = 4f;
    public float trailSpeed = 0.25f;

    BossHealth health;
    BossController controller;
    GameObject canvasGO;
    CanvasGroup group;
    RectTransform fillRT, trailRT;
    Image fillImage;
    float fillTarget = 1f, fillShown = 1f, trailShown = 1f;

    void Awake()
    {
        health = GetComponent<BossHealth>();
        controller = GetComponent<BossController>();
    }

    void Start()
    {
        BuildUI();
        health.OnHealthChanged += HandleHealthChanged;
        fillTarget = health.maxHealth > 0f ? health.currentHealth / health.maxHealth : 1f;
        fillShown = trailShown = fillTarget;
    }

    void OnDestroy()
    {
        if (health) health.OnHealthChanged -= HandleHealthChanged;
        if (canvasGO) Destroy(canvasGO);
    }

    void HandleHealthChanged(float current, float max)
    {
        fillTarget = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    void Update()
    {
        if (!group) return;

        // Chỉ hiện từ lúc boss kích hoạt tới khi boss chết
        bool show = controller && controller.IsActivated && !health.IsDead;
        group.alpha = Mathf.MoveTowards(group.alpha, show ? 1f : 0f, 3f * Time.unscaledDeltaTime);

        fillShown = Mathf.MoveTowards(fillShown, fillTarget, fillSpeed * Time.unscaledDeltaTime);
        if (trailShown < fillShown) trailShown = fillShown;
        else trailShown = Mathf.MoveTowards(trailShown, fillShown, trailSpeed * Time.unscaledDeltaTime);

        SetRatio(fillRT, fillShown);
        SetRatio(trailRT, trailShown);

        Color c = health.IsPhase2 ? phase2Color : fillColor;
        if (health.IsBroken)
            c = Color.Lerp(c, brokenColor, (Mathf.Sin(Time.unscaledTime * 12f) + 1f) * 0.5f);
        fillImage.color = c;
    }

    static void SetRatio(RectTransform rt, float ratio)
    {
        if (!rt) return;
        Vector2 max = rt.anchorMax;
        max.x = Mathf.Clamp01(ratio);
        rt.anchorMax = max;
    }

    // ----- Dựng giao diện bằng code -----

    void BuildUI()
    {
        canvasGO = new GameObject("BossHealthBarCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = canvasGO.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        // Khung ngoài, nằm giữa phía trên màn hình
        var frame = NewImage("Frame", canvasGO.transform, new Color(0f, 0f, 0f, 0.8f));
        var frt = frame.rectTransform;
        frt.anchorMin = new Vector2(0.5f, 1f);
        frt.anchorMax = new Vector2(0.5f, 1f);
        frt.pivot = new Vector2(0.5f, 1f);
        frt.sizeDelta = new Vector2(barWidth, barHeight);
        frt.anchoredPosition = new Vector2(0f, -topMargin);

        var inner = NewRect("Inner", frame.transform);
        Stretch(inner, 3f);

        var back = NewImage("Back", inner, new Color(0.12f, 0.12f, 0.15f, 1f));
        Stretch(back.rectTransform, 0f);

        var trail = NewImage("Trail", inner, trailColor);
        trailRT = trail.rectTransform;
        Stretch(trailRT, 0f);

        var fill = NewImage("Fill", inner, fillColor);
        fillRT = fill.rectTransform;
        Stretch(fillRT, 0f);
        fillImage = fill;

        // Vạch đánh dấu 50% (bắt đầu pha 2)
        var mark = NewImage("Phase2Mark", inner, new Color(1f, 1f, 1f, 0.9f));
        var mrt = mark.rectTransform;
        mrt.anchorMin = new Vector2(0.5f, 0f);
        mrt.anchorMax = new Vector2(0.5f, 1f);
        mrt.sizeDelta = new Vector2(2f, 0f);

        // Tên boss ngay dưới thanh máu
        var nameRT = NewRect("BossName", canvasGO.transform);
        var tmp = nameRT.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = bossName;
        tmp.fontSize = 26f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        if (fontAsset) tmp.font = fontAsset;
        nameRT.anchorMin = new Vector2(0.5f, 1f);
        nameRT.anchorMax = new Vector2(0.5f, 1f);
        nameRT.pivot = new Vector2(0.5f, 1f);
        nameRT.sizeDelta = new Vector2(barWidth, 34f);
        nameRT.anchoredPosition = new Vector2(0f, -(topMargin + barHeight + 4f));
    }

    static RectTransform NewRect(string objName, Transform parent)
    {
        var go = new GameObject(objName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static Image NewImage(string objName, Transform parent, Color color)
    {
        var rt = NewRect(objName, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static void Stretch(RectTransform rt, float pad)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(pad, pad);
        rt.offsetMax = new Vector2(-pad, -pad);
    }
}