using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD điểm (góc trên trái) + style meter kiểu DMC / ULTRAKILL (bên phải) + thông báo đợt enemy.
/// Gắn trên Canvas StyleHUD trong DoomScene, các ô bên dưới kéo thả trong Inspector.
/// </summary>
public class StyleMeterHUD : MonoBehaviour
{
    [Header("Điểm")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI bestText;

    [Header("Style meter")]
    public CanvasGroup meterGroup;
    public Image meterBackground;
    public RectTransform letterRoot;        // xoay, phóng to, rung cả cụm chữ rank
    public TextMeshProUGUI letterText;
    public TextMeshProUGUI letterShadow;
    public TextMeshProUGUI rankNameText;
    public TextMeshProUGUI multiplierText;
    public RectTransform barFill;           // anchorMax.x = tiến độ trong rank
    public Image barFillImage;
    public TextMeshProUGUI[] feedLines;     // dòng 0 ở trên cùng = mới nhất

    [Header("Thông báo đợt")]
    public TextMeshProUGUI waveText;

    [Header("Tinh chỉnh")]
    public float feedLifetime = 3f;
    public float feedFade = 0.6f;
    public float shakePerRank = 2.2f;       // từ S trở lên chữ rank rung
    public float waveBannerTime = 2.2f;
    public Color bonusColor = new Color32(0xE3, 0xEE, 0xF0, 0xFF);
    public Color damageColor = new Color32(0xFF, 0x5A, 0x4F, 0xFF);

    struct FeedItem
    {
        public string text;
        public bool positive;
        public float time;
    }

    ScoreManager score;
    EnemySpawner spawner;
    readonly List<FeedItem> feed = new List<FeedItem>();
    float shownScore, comboPunch, rankPunch, flash, waveShowUntil;
    Vector2 letterBasePos;

    void Start()
    {
        score = ScoreManager.Instance;
        spawner = EnemySpawner.Instance;

        if (letterRoot) letterBasePos = letterRoot.anchoredPosition;
        if (meterGroup) meterGroup.alpha = 0f;
        if (waveText) waveText.alpha = 0f;
        foreach (var line in feedLines) if (line) line.text = "";

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

    void Update()
    {
        if (!score) return;
        float dt = Time.unscaledDeltaTime;

        // Điểm chạy dần lên
        shownScore = Mathf.MoveTowards(shownScore, score.Score, Mathf.Max(400f, (score.Score - shownScore) * 6f) * dt);
        if (scoreText) scoreText.text = ScoreManager.FormatScore(Mathf.RoundToInt(shownScore));

        comboPunch = Mathf.MoveTowards(comboPunch, 0f, dt * 4f);
        if (comboText) comboText.rectTransform.localScale = Vector3.one * (1f + comboPunch * 0.5f);

        UpdateMeter(dt);
        UpdateFeed();

        if (waveText) waveText.alpha = Mathf.Clamp01((waveShowUntil - Time.unscaledTime) / 0.5f);
    }

    void UpdateMeter(float dt)
    {
        int rank = score.RankIndex;
        if (meterGroup)
        {
            float targetAlpha = rank >= 0 && !score.IsGameOver ? 1f : 0f;
            meterGroup.alpha = Mathf.MoveTowards(meterGroup.alpha, targetAlpha, dt * 4f);
        }
        if (rank < 0) return;

        var info = ScoreManager.Ranks[rank];
        Color c = Color.Lerp(info.color, damageColor, flash);

        if (letterText) { letterText.text = info.letter; letterText.color = c; }
        if (letterShadow) letterShadow.text = info.letter;
        if (rankNameText) { rankNameText.text = info.name; rankNameText.color = c; }
        if (multiplierText) multiplierText.text = "x" + info.multiplier.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        if (barFillImage) barFillImage.color = c;
        if (barFill) barFill.anchorMax = new Vector2(score.RankProgress, barFill.anchorMax.y);
        if (meterBackground) meterBackground.color = new Color(c.r * 0.15f, c.g * 0.15f, c.b * 0.15f, 0.6f);

        rankPunch = Mathf.MoveTowards(rankPunch, 0f, dt * 3f);
        flash = Mathf.MoveTowards(flash, 0f, dt * 2.5f);

        if (letterRoot)
        {
            letterRoot.localScale = Vector3.one * (1f + rankPunch * 0.6f);
            float shake = Mathf.Max(0, rank - 3) * shakePerRank + rankPunch * 8f;
            letterRoot.anchoredPosition = letterBasePos + Random.insideUnitCircle * shake;
        }
    }

    void UpdateFeed()
    {
        float now = Time.unscaledTime;
        feed.RemoveAll(f => now - f.time > feedLifetime);

        for (int i = 0; i < feedLines.Length; i++)
        {
            var t = feedLines[i];
            if (!t) continue;
            if (i >= feed.Count)
            {
                t.text = "";
                continue;
            }

            var item = feed[feed.Count - 1 - i];   // mới nhất ở trên
            Color c = item.positive ? bonusColor : damageColor;
            c.a = Mathf.Clamp01((feedLifetime - (now - item.time)) / feedFade);
            t.color = c;
            t.text = (item.positive ? "+ " : "- ") + item.text;
        }
    }

    void OnStyleEvent(string label, bool positive)
    {
        feed.Add(new FeedItem { text = label, positive = positive, time = Time.unscaledTime });
        if (feed.Count > feedLines.Length) feed.RemoveAt(0);
        if (!positive) flash = 1f;
    }

    void OnRankChanged(int oldRank, int newRank)
    {
        if (newRank > oldRank) rankPunch = 1f;
        else flash = 1f;
    }

    void OnScoreChanged()
    {
        if (comboText) comboText.text = score.Combo >= 2 ? "x" + score.Combo : "";
        if (score.Combo >= 2) comboPunch = 1f;
        if (bestText) bestText.text = "BEST " + ScoreManager.FormatScore(Mathf.Max(score.BestScore, score.Score));
    }

    void OnWaveStarted(int wave)
    {
        if (waveText) waveText.text = "WAVE " + wave;
        waveShowUntil = Time.unscaledTime + waveBannerTime;
    }

    void OnGameOver()
    {
        waveShowUntil = 0f;
    }
}
