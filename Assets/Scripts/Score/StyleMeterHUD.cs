using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD điểm (góc trên trái) + style meter kiểu DMC / ULTRAKILL (bên phải) + thông báo đợt enemy.
/// Tự dựng UI bằng code, không cần prefab.
/// </summary>
public class StyleMeterHUD : MonoBehaviour
{
    const int FeedLines = 6;
    const float FeedLifetime = 3f;
    const float FeedFade = 0.6f;

    struct FeedItem
    {
        public string text;
        public bool positive;
        public float time;
    }

    ScoreManager score;
    EnemySpawner spawner;

    // Điểm
    TextMeshProUGUI scoreText, comboText, bestText;
    float shownScore;
    float comboPunch;

    // Style meter
    CanvasGroup meterGroup;
    RectTransform letterRt;
    TextMeshProUGUI letterText, letterShadow, rankNameText, multText;
    RectTransform barFill;
    Image barFillImg, meterBg;
    readonly TextMeshProUGUI[] feedTexts = new TextMeshProUGUI[FeedLines];
    readonly List<FeedItem> feed = new List<FeedItem>();
    float rankPunch, flash;
    Vector2 letterBasePos;

    // Thông báo đợt
    TextMeshProUGUI waveText;
    float waveShowUntil;

    void Start()
    {
        score = ScoreManager.Instance;
        spawner = EnemySpawner.Instance;
        Build();

        if (score)
        {
            score.StyleEvent += OnStyleEvent;
            score.RankChanged += OnRankChanged;
            score.ScoreChanged += OnScoreChanged;
            score.GameOver += OnGameOver;
            OnScoreChanged();
        }
        if (spawner) spawner.WaveStarted += OnWaveStarted;
    }

    void OnDestroy()
    {
        if (score)
        {
            score.StyleEvent -= OnStyleEvent;
            score.RankChanged -= OnRankChanged;
            score.ScoreChanged -= OnScoreChanged;
            score.GameOver -= OnGameOver;
        }
        if (spawner) spawner.WaveStarted -= OnWaveStarted;
    }

    void Build()
    {
        var canvas = UIFactory.CreateCanvas("Style HUD", 40, transform);
        var root = canvas.transform;

        // ---- Điểm, góc trên trái ----
        var scoreBox = UIFactory.Rect("Score", root, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -36), new Vector2(520, 150));
        UIFactory.Text("Label", UIFactory.Rect("", scoreBox, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(300, 30)),
            "SCORE", 22, UIFactory.Hazard, TextAlignmentOptions.TopLeft);
        scoreText = UIFactory.Text("Value", UIFactory.Rect("", scoreBox, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -26), new Vector2(360, 70)),
            "0", 64, UIFactory.Fg, TextAlignmentOptions.TopLeft, FontStyles.Bold | FontStyles.Italic);
        comboText = UIFactory.Text("Combo", UIFactory.Rect("", scoreBox, new Vector2(0, 1), new Vector2(0, 1), new Vector2(330, -40), new Vector2(160, 50)),
            "", 40, UIFactory.Hazard, TextAlignmentOptions.TopLeft, FontStyles.Bold | FontStyles.Italic);
        bestText = UIFactory.Text("Best", UIFactory.Rect("", scoreBox, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, -100), new Vector2(400, 30)),
            "", 22, UIFactory.Muted, TextAlignmentOptions.TopLeft);

        // ---- Style meter, bên phải ----
        var meter = UIFactory.Rect("Style Meter", root, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-36, 120), new Vector2(420, 400));
        meterGroup = meter.gameObject.AddComponent<CanvasGroup>();
        meterGroup.alpha = 0f;
        meterGroup.blocksRaycasts = false;

        meterBg = UIFactory.Panel("Background", meter, new Color(0f, 0f, 0f, 0.55f));
        var edge = UIFactory.Rect("Edge", meter, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(6, 400));
        edge.anchorMin = new Vector2(0, 0); edge.anchorMax = new Vector2(0, 1); edge.sizeDelta = new Vector2(6, 0);
        edge.gameObject.AddComponent<Image>().color = UIFactory.Water;

        letterBasePos = new Vector2(30, -10);
        letterRt = UIFactory.Rect("Rank Letter", meter, new Vector2(0, 1), new Vector2(0, 1), letterBasePos, new Vector2(240, 150));
        letterRt.localRotation = Quaternion.Euler(0, 0, 6f);
        letterShadow = UIFactory.Text("Shadow", UIFactory.Rect("", letterRt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(6, -6), new Vector2(240, 150)),
            "D", 150, new Color(0, 0, 0, 0.7f), TextAlignmentOptions.TopLeft, FontStyles.Bold | FontStyles.Italic);
        letterText = UIFactory.Text("Letter", UIFactory.Rect("", letterRt, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(240, 150)),
            "D", 150, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Bold | FontStyles.Italic);

        rankNameText = UIFactory.Text("Rank Name", UIFactory.Rect("", meter, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -160), new Vector2(370, 44)),
            "DRIP", 38, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Bold | FontStyles.Italic);
        multText = UIFactory.Text("Multiplier", UIFactory.Rect("", meter, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-20, -22), new Vector2(160, 30)),
            "x1.0", 26, UIFactory.Muted, TextAlignmentOptions.TopRight, FontStyles.Bold);

        var barBg = UIFactory.Rect("Bar", meter, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -212), new Vector2(360, 14));
        barBg.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);
        barFill = UIFactory.Rect("Fill", barBg, new Vector2(0, 0), new Vector2(0, 0.5f), Vector2.zero, Vector2.zero);
        barFill.anchorMin = new Vector2(0, 0);
        barFill.anchorMax = new Vector2(0, 1);
        barFill.offsetMin = barFill.offsetMax = Vector2.zero;
        barFillImg = barFill.gameObject.AddComponent<Image>();

        for (int i = 0; i < FeedLines; i++)
        {
            var rt = UIFactory.Rect("", meter, new Vector2(0, 1), new Vector2(0, 1), new Vector2(34, -240 - i * 26), new Vector2(370, 26));
            feedTexts[i] = UIFactory.Text("Feed " + i, rt, "", 22, Color.white, TextAlignmentOptions.TopLeft, FontStyles.Bold);
        }

        // ---- Thông báo đợt, giữa trên ----
        waveText = UIFactory.Text("Wave Banner", UIFactory.Rect("", root, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(900, 90)),
            "", 72, UIFactory.Hurt, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
    }

    void Update()
    {
        if (!score) return;
        float dt = Time.unscaledDeltaTime;

        // Điểm chạy dần lên
        shownScore = Mathf.MoveTowards(shownScore, score.Score, Mathf.Max(400f, (score.Score - shownScore) * 6f) * dt);
        scoreText.text = Mathf.RoundToInt(shownScore).ToString("N0");

        comboPunch = Mathf.MoveTowards(comboPunch, 0f, dt * 4f);
        comboText.rectTransform.localScale = Vector3.one * (1f + comboPunch * 0.5f);

        UpdateMeter(dt);
        UpdateFeed();

        float waveLeft = waveShowUntil - Time.unscaledTime;
        waveText.alpha = Mathf.Clamp01(waveLeft / 0.5f);
    }

    void UpdateMeter(float dt)
    {
        int rank = score.RankIndex;
        float targetAlpha = rank >= 0 && !score.IsGameOver ? 1f : 0f;
        meterGroup.alpha = Mathf.MoveTowards(meterGroup.alpha, targetAlpha, dt * 4f);
        if (rank < 0) return;

        var info = ScoreManager.Ranks[rank];
        Color c = Color.Lerp(info.color, UIFactory.Hurt, flash);

        letterText.text = info.letter;
        letterShadow.text = info.letter;
        letterText.color = c;
        rankNameText.text = info.name;
        rankNameText.color = c;
        multText.text = "x" + info.multiplier.ToString("0.0");
        barFillImg.color = c;
        barFill.anchorMax = new Vector2(score.RankProgress, 1f);
        meterBg.color = new Color(c.r * 0.15f, c.g * 0.15f, c.b * 0.15f, 0.6f);

        rankPunch = Mathf.MoveTowards(rankPunch, 0f, dt * 3f);
        flash = Mathf.MoveTowards(flash, 0f, dt * 2.5f);
        letterRt.localScale = Vector3.one * (1f + rankPunch * 0.6f);

        // Từ S trở lên chữ rung, rank càng cao rung càng mạnh
        float shake = Mathf.Max(0, rank - 3) * 2.2f + rankPunch * 8f;
        letterRt.anchoredPosition = letterBasePos + Random.insideUnitCircle * shake;
    }

    void UpdateFeed()
    {
        float now = Time.unscaledTime;
        feed.RemoveAll(f => now - f.time > FeedLifetime);

        for (int i = 0; i < FeedLines; i++)
        {
            var t = feedTexts[i];
            if (i >= feed.Count)
            {
                t.text = "";
                continue;
            }

            var item = feed[feed.Count - 1 - i];   // mới nhất ở trên
            float age = now - item.time;
            float alpha = Mathf.Clamp01((FeedLifetime - age) / FeedFade);
            Color c = item.positive ? UIFactory.Fg : UIFactory.Hurt;
            c.a = alpha;
            t.color = c;
            t.text = (item.positive ? "+ " : "- ") + item.text;
        }
    }

    void OnStyleEvent(string label, bool positive)
    {
        feed.Add(new FeedItem { text = label, positive = positive, time = Time.unscaledTime });
        if (feed.Count > FeedLines) feed.RemoveAt(0);
        if (!positive) flash = 1f;
    }

    void OnRankChanged(int oldRank, int newRank)
    {
        if (newRank > oldRank) rankPunch = 1f;
        else flash = 1f;
    }

    void OnScoreChanged()
    {
        comboText.text = score.Combo >= 2 ? "x" + score.Combo : "";
        if (score.Combo >= 2) comboPunch = 1f;
        bestText.text = "BEST " + Mathf.Max(score.BestScore, score.Score).ToString("N0");
    }

    void OnWaveStarted(int wave)
    {
        waveText.text = "WAVE " + wave;
        waveShowUntil = Time.unscaledTime + 2.2f;
    }

    void OnGameOver()
    {
        waveShowUntil = 0f;
    }
}
