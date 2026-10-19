using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dựng bố cục mặc định cho StyleHUD và GameOverScreen rồi nối sẵn các ô trong Inspector.
/// Editor dùng nó để tạo prefab (Tools > DoomClone > Create HUD Prefabs);
/// lúc chạy game chỉ dùng khi chưa có prefab trong Resources/UI.
/// </summary>
public static class HUDBuilder
{
    const FontStyles BoldItalic = FontStyles.Bold | FontStyles.Italic;

    public static GameObject BuildStyleHUD()
    {
        var canvas = UIFactory.CreateCanvas("StyleHUD", 40, null);
        var root = canvas.transform;
        var hud = canvas.gameObject.AddComponent<StyleMeterHUD>();

        // ---- Điểm, góc trên trái ----
        var scoreBox = UIFactory.Rect("Score", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -36), new Vector2(520, 150));
        UIFactory.Text("Label", Child(scoreBox, Vector2.zero, new Vector2(300, 30)), "SCORE", 22, UIFactory.Hazard, TextAlignmentOptions.TopLeft);
        hud.scoreText = UIFactory.Text("Value", Child(scoreBox, new Vector2(0, -26), new Vector2(360, 70)), "0", 64, UIFactory.Fg, TextAlignmentOptions.TopLeft, BoldItalic);
        hud.comboText = UIFactory.Text("Combo", Child(scoreBox, new Vector2(330, -40), new Vector2(160, 50)), "", 40, UIFactory.Hazard, TextAlignmentOptions.TopLeft, BoldItalic);
        hud.bestText = UIFactory.Text("Best", Child(scoreBox, new Vector2(0, -100), new Vector2(400, 30)), "BEST 0", 22, UIFactory.Muted, TextAlignmentOptions.TopLeft);

        // ---- Style meter, bên phải ----
        var meter = UIFactory.Rect("Style Meter", root, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-36, 120), new Vector2(420, 400));
        hud.meterGroup = meter.gameObject.AddComponent<CanvasGroup>();
        hud.meterGroup.blocksRaycasts = false;
        hud.meterGroup.interactable = false;

        hud.meterBackground = UIFactory.Panel("Background", meter, new Color(0f, 0f, 0f, 0.55f));

        var edge = UIFactory.Rect("Edge", meter, Vector2.zero, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        edge.anchorMin = new Vector2(0, 0);
        edge.anchorMax = new Vector2(0, 1);
        edge.sizeDelta = new Vector2(6, 0);
        edge.gameObject.AddComponent<Image>().color = UIFactory.Water;

        hud.letterRoot = UIFactory.Rect("Rank Letter", meter, new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -10), new Vector2(240, 150));
        hud.letterRoot.localRotation = Quaternion.Euler(0, 0, 6f);
        hud.letterShadow = UIFactory.Text("Shadow", Child(hud.letterRoot, new Vector2(6, -6), new Vector2(240, 150)), "S", 150, new Color(0, 0, 0, 0.7f), TextAlignmentOptions.TopLeft, BoldItalic);
        hud.letterText = UIFactory.Text("Letter", Child(hud.letterRoot, Vector2.zero, new Vector2(240, 150)), "S", 150, ScoreManager.Ranks[4].color, TextAlignmentOptions.TopLeft, BoldItalic);

        hud.rankNameText = UIFactory.Text("Rank Name", Child(meter, new Vector2(34, -160), new Vector2(370, 44)), "STORM", 38, ScoreManager.Ranks[4].color, TextAlignmentOptions.TopLeft, BoldItalic);
        hud.multiplierText = UIFactory.Text("Multiplier", UIFactory.Rect("", meter, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -22), new Vector2(160, 30)),
            "x2.0", 26, UIFactory.Muted, TextAlignmentOptions.TopRight);

        var barBg = UIFactory.Rect("Bar", meter, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -212), new Vector2(360, 14));
        barBg.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        hud.barFill = UIFactory.Rect("Fill", barBg, Vector2.zero, new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        hud.barFill.anchorMin = new Vector2(0, 0);
        hud.barFill.anchorMax = new Vector2(0.6f, 1);
        hud.barFill.offsetMin = hud.barFill.offsetMax = Vector2.zero;
        hud.barFillImage = hud.barFill.gameObject.AddComponent<Image>();
        hud.barFillImage.color = ScoreManager.Ranks[4].color;

        string[] samples = { "+ LONG SHOT", "+ DOUBLE KILL", "+ KILL", "", "", "" };
        hud.feedLines = new TextMeshProUGUI[samples.Length];
        for (int i = 0; i < samples.Length; i++)
            hud.feedLines[i] = UIFactory.Text("Feed " + i, Child(meter, new Vector2(34, -240 - i * 26), new Vector2(370, 26)), samples[i], 22, UIFactory.Fg, TextAlignmentOptions.TopLeft);

        // ---- Thông báo đợt, giữa trên ----
        hud.waveText = UIFactory.Text("Wave Banner", UIFactory.Rect("", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(900, 90)),
            "WAVE 1", 72, UIFactory.Hurt, TextAlignmentOptions.Center, BoldItalic);

        return canvas.gameObject;
    }

    public static GameObject BuildGameOverScreen()
    {
        var canvas = UIFactory.CreateCanvas("GameOverScreen", 100, null);
        var ui = canvas.gameObject.AddComponent<GameOverUI>();

        // Panel con để bật/tắt, GameOverUI nằm ở Canvas nên vẫn chạy khi panel tắt
        var panelRt = UIFactory.Rect("Panel", canvas.transform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        UIFactory.Stretch(panelRt);
        ui.panel = panelRt.gameObject.AddComponent<CanvasGroup>();
        var root = panelRt.transform;

        var bg = UIFactory.Panel("Background", root, new Color(0.06f, 0.01f, 0.01f, 0.86f));
        bg.raycastTarget = true;   // chặn click xuống game

        ui.title = UIFactory.Rect("", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1400, 200));
        UIFactory.Text("YOU LOSE", ui.title, "YOU LOSE", 180, UIFactory.Hurt, TextAlignmentOptions.Center, BoldItalic);

        ui.newRecord = UIFactory.Text("New Record", UIFactory.Rect("", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(800, 50)),
            "NEW RECORD!", 42, UIFactory.Hazard, TextAlignmentOptions.Center, BoldItalic).gameObject;

        ui.scoreValue = Stat(root, "SCORE", "12,450", new Vector2(-300, 10), 76, UIFactory.Fg);
        ui.bestValue = Stat(root, "BEST", "18,900", new Vector2(300, 10), 76, UIFactory.Muted);
        ui.killsValue = Stat(root, "KILLS", "14", new Vector2(-300, -110), 44, UIFactory.Fg);
        ui.timeValue = Stat(root, "TIME", "03:41", new Vector2(0, -110), 44, UIFactory.Fg);
        ui.rankValue = Stat(root, "MAX STYLE", "S", new Vector2(300, -110), 44, ScoreManager.Ranks[4].color);

        ui.retryButton = UIFactory.Button(root, "RETRY", new Vector2(-170, -250), new Vector2(300, 80), UIFactory.Water, UIFactory.Ink);
        ui.menuButton = UIFactory.Button(root, "MAIN MENU", new Vector2(170, -250), new Vector2(300, 80), new Color(1f, 1f, 1f, 0.12f), UIFactory.Fg);

        UIFactory.Text("Hint", UIFactory.Rect("", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(800, 30)),
            "ENTER  retry     ESC  menu", 22, UIFactory.Muted, TextAlignmentOptions.Center, FontStyles.Normal);

        return canvas.gameObject;
    }

    static RectTransform Child(RectTransform parent, Vector2 pos, Vector2 size)
    {
        return UIFactory.Rect("", parent, new Vector2(0, 1), new Vector2(0, 1), pos, size);
    }

    static TextMeshProUGUI Stat(Transform root, string label, string sample, Vector2 pos, float size, Color color)
    {
        var box = UIFactory.Rect("Stat " + label, root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(420, size + 40));
        UIFactory.Text("Label", UIFactory.Rect("", box, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(420, 30)),
            label, 22, UIFactory.Muted, TextAlignmentOptions.Center);
        return UIFactory.Text("Value", UIFactory.Rect("", box, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(420, size + 10)),
            sample, size, color, TextAlignmentOptions.Center, BoldItalic);
    }
}
