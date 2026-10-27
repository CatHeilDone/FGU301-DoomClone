using System;
using UnityEngine;

/// <summary>
/// Điểm số, combo và style rank kiểu DMC / ULTRAKILL.
/// Gắn trên object "Gameplay Systems" trong DoomScene cùng với EnemySpawner.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    [Serializable]
    public struct StyleRank
    {
        public string letter;
        public string name;
        public Color color;
        public float multiplier;

        public StyleRank(string letter, string name, string hex, float multiplier)
        {
            this.letter = letter;
            this.name = name;
            ColorUtility.TryParseHtmlString(hex, out color);
            this.multiplier = multiplier;
        }
    }

    public static readonly StyleRank[] Ranks =
    {
        new StyleRank("D",   "DRIP",         "#8AA4AB", 1.0f),
        new StyleRank("C",   "SPLASH",       "#3FD6E8", 1.2f),
        new StyleRank("B",   "SOAKED",       "#4EA1FF", 1.4f),
        new StyleRank("A",   "TORRENT",      "#7D8CFF", 1.7f),
        new StyleRank("S",   "STORM",        "#F2C230", 2.0f),
        new StyleRank("SS",  "SUPERSOAKER",  "#FF9A3C", 2.5f),
        new StyleRank("SSS", "SSSUNAMI",     "#FF5A4F", 3.0f),
    };

    public static ScoreManager Instance { get; private set; }

    [Header("Điểm")]
    public int killScore = 100;
    public float comboWindow = 3f;      // hạ enemy tiếp trong khoảng này thì tăng combo
    public int maxCombo = 8;

    [Header("Style: ngưỡng từng rank (D, C, B, A, S, SS, SSS)")]
    // Mỗi rank cần nhiều style hơn rank trước, nên lên cao khó dần
    public float[] rankThresholds = { 0f, 120f, 300f, 550f, 900f, 1350f, 1900f };
    public float maxStyle = 2400f;

    [Header("Style: điểm cộng")]
    public float hitStyle = 3f;          // mỗi 1 sát thương bắn trúng
    public float killStyle = 30f;
    public float comboStyleStep = 10f;   // combo x2 +10, x3 +20...
    public float comboStyleMax = 50f;    // ...tối đa +50 mỗi kill
    public float longShotStyle = 15f;
    public float pointBlankStyle = 12f;
    public float closeCallStyle = 25f;
    public float waveClearStyle = 60f;
    [Range(0.5f, 1f)]
    public float gainFalloff = 0.85f;    // mỗi rank nhận ít style hơn: x0.85 mỗi bậc
    public float longShotDistance = 12f;
    public float pointBlankDistance = 3f;

    [Header("Style: tụt")]
    public float holdTime = 1.2f;        // vừa cộng style thì giữ nguyên chừng này giây rồi mới tụt
    public float decayBase = 10f;        // style tụt mỗi giây ở rank D
    public float decayPerRankStep = 8f;  // mỗi rank cao hơn tụt thêm chừng này mỗi giây
    public bool dropRankOnHit = true;    // bị đánh: rơi về đầu rank thấp hơn

    public int Score { get; private set; }
    public int BestScore { get; private set; }
    public bool IsNewBest { get; private set; }
    public int Combo { get; private set; }
    public int Kills { get; private set; }
    public float Style { get; private set; }
    public float TimeAlive { get; private set; }
    public int HighestRank { get; private set; } = -1;
    public bool IsGameOver { get; private set; }

    /// <summary>-1 là chưa có rank (meter ẩn).</summary>
    public int RankIndex
    {
        get
        {
            if (Style <= 0f) return -1;
            int rank = 0;
            for (int i = 1; i < Ranks.Length && i < rankThresholds.Length; i++)
                if (Style >= rankThresholds[i]) rank = i;
            return rank;
        }
    }

    /// <summary>Tiến độ 0..1 trong rank hiện tại.</summary>
    public float RankProgress
    {
        get
        {
            int rank = RankIndex;
            if (rank < 0) return 0f;
            float from = rankThresholds[rank];
            float to = rank + 1 < rankThresholds.Length ? rankThresholds[rank + 1] : maxStyle;
            return Mathf.Clamp01((Style - from) / Mathf.Max(1f, to - from));
        }
    }

    public float RankMultiplier => RankIndex < 0 ? 1f : Ranks[RankIndex].multiplier;

    public event Action<string, bool> StyleEvent;   // nhãn, true = thưởng / false = phạt
    public event Action<int, int> RankChanged;      // rank cũ, rank mới
    public event Action ScoreChanged;
    public event Action GameOver;

    float lastKillTime = -999f;
    float lastGainTime = -999f;
    int lastRank = -1;
    Transform player;
    PlayerHealth playerHealth;

    /// <summary>File lưu sau khi ván kết thúc (null khi đang chơi).</summary>
    public PlayerSave LastSave { get; private set; }
    /// <summary>false nếu ghi file lưu bị lỗi.</summary>
    public bool RunSaved { get; private set; }

    /// <summary>true nếu ván này được chơi tiếp từ bản lưu giữa ván.</summary>
    public bool LoadedFromSave { get; private set; }

    static string CurrentUser => SaveSystem.CurrentUser;

    /// <summary>Kỷ lục đã lưu của một tài khoản, dùng cho Main Menu.</summary>
    public static int GetBestScore(string user) => SaveSystem.Load(user).bestScore;

    /// <summary>12,450 — luôn dùng dấu phẩy, không phụ thuộc ngôn ngữ của máy.</summary>
    public static string FormatScore(int value) => value.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);

    void Awake()
    {
        Instance = this;
        BestScore = SaveSystem.Load(CurrentUser).bestScore;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        playerHealth = FindAnyObjectByType<PlayerHealth>();
        var move = FindAnyObjectByType<PlayerMove>();
        player = move ? move.transform : playerHealth ? playerHealth.transform : null;
    }

    void Update()
    {
        if (IsGameOver) return;

        TimeAlive += Time.deltaTime;

        if (Combo > 0 && Time.time - lastKillTime > comboWindow)
        {
            Combo = 0;
            ScoreChanged?.Invoke();
        }

        if (Style > 0f && Time.time - lastGainTime > holdTime)
        {
            float decay = decayBase + decayPerRankStep * Mathf.Max(RankIndex, 0);
            Style = Mathf.Max(0f, Style - decay * Time.deltaTime);
            CheckRank();
        }
    }

    public void EnemyHit(Enemy enemy, float damage)
    {
        if (IsGameOver) return;
        AddStyle(hitStyle * damage);
    }

    public void EnemyKilled(Enemy enemy)
    {
        if (IsGameOver) return;

        Combo = Time.time - lastKillTime <= comboWindow ? Mathf.Min(Combo + 1, maxCombo) : 1;
        lastKillTime = Time.time;
        Kills++;

        AddStyle(killStyle, "KILL");

        if (Combo >= 2)
        {
            float bonus = Mathf.Min(comboStyleStep * (Combo - 1), comboStyleMax);
            string label = Combo == 2 ? "DOUBLE KILL" : Combo == 3 ? "TRIPLE KILL" : "MULTI KILL x" + Combo;
            AddStyle(bonus, label);
        }

        if (player && enemy)
        {
            float dist = Vector3.Distance(player.position, enemy.transform.position);
            if (dist >= longShotDistance) AddStyle(longShotStyle, "LONG SHOT");
            else if (dist <= pointBlankDistance) AddStyle(pointBlankStyle, "POINT BLANK");
        }

        if (playerHealth && playerHealth.HealthPercent < 0.25f)
            AddStyle(closeCallStyle, "CLOSE CALL");

        // Rank tính sau khi cộng style, nên kill đẩy lên rank mới được nhân luôn
        Score += Mathf.RoundToInt(killScore * Combo * RankMultiplier);
        ScoreChanged?.Invoke();
    }

    public void PlayerDamaged(int damage)
    {
        if (IsGameOver || damage <= 0) return;

        // Bị đánh: rơi về đầu rank thấp hơn một bậc (D thì về 0)
        int rank = RankIndex;
        if (dropRankOnHit) Style = rank >= 1 ? rankThresholds[rank - 1] : 0f;
        else Style = Mathf.Max(0f, Style - (rank >= 1 ? rankThresholds[rank] - rankThresholds[rank - 1] : Style));
        Combo = 0;
        StyleEvent?.Invoke("DAMAGED", false);
        CheckRank();
        ScoreChanged?.Invoke();
    }

    public void WaveCleared(int wave)
    {
        if (IsGameOver) return;

        AddStyle(waveClearStyle, "WAVE " + wave + " CLEAR");
        Score += 250 * wave;
        ScoreChanged?.Invoke();
    }

    public void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        var run = new RunRecord
        {
            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            score = Score,
            kills = Kills,
            timeAlive = TimeAlive,
            wave = EnemySpawner.Instance ? EnemySpawner.Instance.Wave : 0,
            maxRank = HighestRank,
        };
        RunSaved = SaveSystem.RecordRun(CurrentUser, run, out var save, LoadedFromSave);
        LastSave = save;
        IsNewBest = run.newBest;
        BestScore = save.bestScore;

        GameOver?.Invoke();
    }

    /// <summary>Khôi phục điểm khi load ván đang dở.</summary>
    public void RestoreRun(SavedRun run)
    {
        Score = run.score;
        Kills = run.kills;
        TimeAlive = run.timeAlive;
        Style = Mathf.Clamp(run.style, 0f, maxStyle);
        HighestRank = Mathf.Max(HighestRank, run.highestRank);
        Combo = 0;
        lastKillTime = -999f;
        LoadedFromSave = true;
        CheckRank();
        ScoreChanged?.Invoke();
    }

    void AddStyle(float amount, string label = null)
    {
        // Rank càng cao càng khó lên: x0.85 mỗi bậc (SSS chỉ nhận ~38%)
        amount *= Mathf.Pow(gainFalloff, Mathf.Max(RankIndex, 0));
        Style = Mathf.Min(Style + amount, maxStyle);
        lastGainTime = Time.time;
        if (label != null) StyleEvent?.Invoke(label, true);
        CheckRank();
    }

    void CheckRank()
    {
        int rank = RankIndex;
        if (rank == lastRank) return;

        int old = lastRank;
        lastRank = rank;
        if (rank > HighestRank) HighestRank = rank;
        RankChanged?.Invoke(old, rank);
    }

    public bool IsVictory { get; private set; }

    /// <summary>Hạ boss cuối: kết thúc ván với màn hình YOU WIN.</summary>
    public void TriggerVictory()
    {
        if (IsGameOver) return;
        IsVictory = true;
        TriggerGameOver();
    }
}
