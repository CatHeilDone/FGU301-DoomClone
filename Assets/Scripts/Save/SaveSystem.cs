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
    // đồng bộ cloud (CloudSaveSync)
    public string uid;              // tài khoản Firebase sở hữu file này; rỗng = save cũ từ trước khi dùng Firebase
    public string savedAtUtc;       // lần ghi gần nhất, ISO 8601 UTC, so sánh được dạng chuỗi
    public bool pendingUpload;      // còn thay đổi chưa đẩy lên Firestore
}

/// <summary>
/// Lưu kết quả mỗi ván vào file JSON riêng cho từng tài khoản:
/// Application.persistentDataPath/saves/&lt;user&gt;.json
/// File này là bộ nhớ đệm: khi đã đăng nhập Firebase, mỗi lần ghi sẽ được
/// CloudSaveSync đẩy lên Firestore, và khi đăng nhập thì tải bản trên cloud về.
/// </summary>
public static class SaveSystem
{
    public const int MaxRecentRuns = 10;

    /// <summary>Main Menu bật cờ này khi bấm CONTINUE, DoomScene đọc rồi tắt đi.</summary>
    public static bool ContinueRequested;

    public static string CurrentUser => AuthService.IsLoggedIn ? AuthService.CurrentUser : "guest";

    static string Folder => Path.Combine(Application.persistentDataPath, "saves");

    public static string PathFor(string user) => Path.Combine(Folder, SafeName(user) + ".json");

    public static bool HasLocal(string user) => File.Exists(PathFor(string.IsNullOrEmpty(user) ? "guest" : user));

    /// <summary>Đọc file trên máy (CloudSaveSync dùng; code game cứ gọi Load).</summary>
    public static PlayerSave LoadLocal(string user) => Load(user);

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

    /// <summary>Ghi file trên máy rồi đẩy lên cloud chạy ngầm (nếu đang đăng nhập Firebase).</summary>
    static bool Write(PlayerSave save)
    {
        bool sync = CloudSaveSync.ShouldSync(save.user);
        save.savedAtUtc = DateTime.UtcNow.ToString("o");
        save.pendingUpload = sync;
        if (sync) save.uid = AuthService.Uid;

        bool ok = WriteLocal(save);
        if (ok && sync) CloudSaveSync.Push(save.user);
        return ok;
    }

    /// <summary>
    /// Đổi tên file save trên máy thành &lt;user&gt;.&lt;tag&gt;.bak.json để giữ lại, không xoá.
    /// Dùng khi file trên máy thuộc một tài khoản Firebase khác trùng tên đăng nhập.
    /// </summary>
    public static void BackupLocal(string user, string tag)
    {
        string path = PathFor(user);
        if (!File.Exists(path)) return;
        string backup = Path.Combine(Folder, SafeName(user) + "." + SafeName(tag) + ".bak.json");
        try
        {
            if (File.Exists(backup)) File.Delete(backup);
            File.Move(path, backup);
            Debug.Log("[Save] File của tài khoản khác được cất sang " + backup);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Save] Không cất được " + path + ": " + e.Message);
        }
    }

    /// <summary>Chỉ ghi file trên máy, không đụng tới cloud.</summary>
    public static bool WriteLocal(PlayerSave save)
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
