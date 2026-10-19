using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Một ván chơi đã kết thúc.</summary>
[Serializable]
public class RunRecord
{
    public string date;         // giờ máy, dạng "2026-10-19 14:32"
    public int score;
    public int kills;
    public float timeAlive;     // giây
    public int wave;
    public int maxRank = -1;    // chỉ số trong ScoreManager.Ranks, -1 là chưa có rank
    public bool newBest;
}

/// <summary>Ván đang chơi dở, lưu từ menu Pause hoặc phím F5 để chơi tiếp.</summary>
[Serializable]
public class SavedRun
{
    public string date;
    public string scene;
    // điểm
    public int score;
    public int kills;
    public float style;
    public float timeAlive;
    public int highestRank = -1;
    public int wave;
    // người chơi
    public int health;
    public int armor;
    public int ammo;
    public bool hasYellow, hasOrange, hasBlue, hasPurple;
    public Vector3 position;
    public float lookAngle;
}

/// <summary>Dữ liệu lưu của một tài khoản.</summary>
[Serializable]
public class PlayerSave
{
    public string user;
    public int bestScore;
    public int gamesPlayed;
    public int totalKills;
    public float totalPlayTime;
    public int highestRank = -1;
    public int highestWave;
    public List<RunRecord> recentRuns = new List<RunRecord>();   // mới nhất ở đầu
    public bool hasSavedRun;
    public SavedRun savedRun = new SavedRun();
}

/// <summary>
/// Lưu kết quả mỗi ván vào file JSON riêng cho từng tài khoản:
/// Application.persistentDataPath/saves/&lt;user&gt;.json
/// </summary>
public static class SaveSystem
{
    public const int MaxRecentRuns = 10;

    /// <summary>Main Menu bật cờ này khi bấm CONTINUE, DoomScene đọc rồi tắt đi.</summary>
    public static bool ContinueRequested;

    public static string CurrentUser => AuthService.IsLoggedIn ? AuthService.CurrentUser : "guest";

    static string Folder => Path.Combine(Application.persistentDataPath, "saves");

    public static string PathFor(string user) => Path.Combine(Folder, SafeName(user) + ".json");

    public static PlayerSave Load(string user)
    {
        user = string.IsNullOrEmpty(user) ? "guest" : user;
        PlayerSave save = null;
        string path = PathFor(user);

        try
        {
            if (File.Exists(path))
                save = JsonUtility.FromJson<PlayerSave>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Save] Không đọc được " + path + ": " + e.Message);
        }

        if (save == null) save = new PlayerSave();
        save.user = user;
        if (save.recentRuns == null) save.recentRuns = new List<RunRecord>();
        if (save.savedRun == null) save.savedRun = new SavedRun();

        // Kỷ lục cũ trước đây chỉ nằm trong PlayerPrefs, gộp vào để không mất
        int legacyBest = PlayerPrefs.GetInt("best_score_" + user, 0);
        if (legacyBest > save.bestScore) save.bestScore = legacyBest;

        return save;
    }

    /// <summary>Ghi đè ván đang dở. Trả về false nếu ghi file lỗi.</summary>
    public static bool SaveRun(string user, SavedRun run)
    {
        var save = Load(user);
        save.hasSavedRun = true;
        save.savedRun = run;
        return Write(save);
    }

    /// <summary>
    /// Cộng một ván đã kết thúc vào file lưu. clearSavedRun = true khi ván này
    /// được chơi tiếp từ bản lưu, để không CONTINUE lại một ván đã chết.
    /// Trả về false nếu ghi file lỗi.
    /// </summary>
    public static bool RecordRun(string user, RunRecord run, out PlayerSave save, bool clearSavedRun = false)
    {
        save = Load(user);
        if (clearSavedRun) save.hasSavedRun = false;

        run.newBest = run.score > save.bestScore;
        if (run.newBest) save.bestScore = run.score;
        save.gamesPlayed++;
        save.totalKills += run.kills;
        save.totalPlayTime += run.timeAlive;
        save.highestRank = Mathf.Max(save.highestRank, run.maxRank);
        save.highestWave = Mathf.Max(save.highestWave, run.wave);

        save.recentRuns.Insert(0, run);
        if (save.recentRuns.Count > MaxRecentRuns)
            save.recentRuns.RemoveRange(MaxRecentRuns, save.recentRuns.Count - MaxRecentRuns);

        return Write(save);
    }

    static bool Write(PlayerSave save)
    {
        string path = PathFor(save.user);
        string temp = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Folder);
            // Ghi ra file tạm rồi mới thay, để tắt game giữa chừng không làm hỏng file cũ
            File.WriteAllText(temp, JsonUtility.ToJson(save, true));
            if (File.Exists(path)) File.Replace(temp, path, null);
            else File.Move(temp, path);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Save] Không ghi được " + path + ": " + e.Message);
            return false;
        }
    }

    static string SafeName(string user)
    {
        foreach (char c in Path.GetInvalidFileNameChars()) user = user.Replace(c, '_');
        return user;
    }
}
