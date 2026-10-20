using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] string loginScene = "Login";
    [SerializeField] string loadingScene = "LoadingScene";

    [Header("Thông tin người chơi (tùy chọn)")]
    [SerializeField] TMP_Text playerNameText;
    [SerializeField] TMP_Text bestScoreText;
    [SerializeField] TMP_Text gamesPlayedText;

    [Header("Các ván gần đây (tùy chọn)")]
    [SerializeField] TMP_Text[] recentRunRows;     // dòng 0 = ván mới nhất
    [SerializeField] TMP_Text recentRunsEmpty;     // hiện khi chưa chơi ván nào

    [Header("Chơi tiếp ván đã lưu (tùy chọn)")]
    [SerializeField] GameObject continueButton;    // ẩn khi không có ván đang dở
    [SerializeField] TMP_Text continueInfo;        // "WAVE 3 · 4,210 PTS · 19/10 14:32"
    [SerializeField] string gameScene = "DoomScene";

    void Start()
    {
        // Chưa đăng nhập thì đưa về màn hình Login.
        if (!AuthService.IsLoggedIn)
        {
            SceneManager.LoadScene(loginScene);
            return;
        }

        // Mở khóa chuột cho menu (game FPS thường khóa chuột khi chơi).
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        var save = SaveSystem.Load(AuthService.CurrentUser);
        if (playerNameText) playerNameText.text = AuthService.CurrentUser.ToUpperInvariant();
        if (bestScoreText) bestScoreText.text = ScoreManager.FormatScore(save.bestScore);
        if (gamesPlayedText) gamesPlayedText.text = save.gamesPlayed.ToString();
        ShowRecentRuns(save);

        if (continueButton) continueButton.SetActive(save.hasSavedRun);
        if (continueInfo && save.hasSavedRun)
        {
            var run = save.savedRun;
            // "2026-10-19 14:32" -> "19/10 14:32" cho vừa một dòng
            string when = System.DateTime.TryParseExact(run.date, "yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var d) ? d.ToString("dd/MM HH:mm") : run.date;
            continueInfo.text = "WAVE " + run.wave + "  ·  " + ScoreManager.FormatScore(run.score) + " PTS  ·  " + when;
        }
    }

    /// <summary>Nút CONTINUE: vào game rồi load ván đang dở.</summary>
    public void ContinueGame()
    {
        var save = SaveSystem.Load(AuthService.CurrentUser);
        if (!save.hasSavedRun) return;
        SaveSystem.ContinueRequested = true;
        LoadScene(string.IsNullOrEmpty(save.savedRun.scene) ? gameScene : save.savedRun.scene);
    }

    void ShowRecentRuns(PlayerSave save)
    {
        if (recentRunRows == null) return;
        for (int i = 0; i < recentRunRows.Length; i++)
        {
            var row = recentRunRows[i];
            if (!row) continue;
            bool has = i < save.recentRuns.Count;
            row.gameObject.SetActive(has);
            if (has) row.text = FormatRun(save.recentRuns[i]);
        }
        if (recentRunsEmpty) recentRunsEmpty.gameObject.SetActive(save.recentRuns.Count == 0);
    }

    // "12,450   S   14   03:41" — cột canh bằng thẻ <pos>, khớp với dòng tiêu đề "Columns" trong scene
    static string FormatRun(RunRecord run)
    {
        string rank = "-";
        if (run.maxRank >= 0 && run.maxRank < ScoreManager.Ranks.Length)
        {
            var r = ScoreManager.Ranks[run.maxRank];
            rank = "<color=#" + ColorUtility.ToHtmlStringRGB(r.color) + ">" + r.letter + "</color>";
        }
        int seconds = Mathf.FloorToInt(run.timeAlive);
        string time = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        string score = "<b>" + ScoreManager.FormatScore(run.score) + "</b>" + (run.newBest ? " <size=65%><color=#C07A20>NEW</color></size>" : "");
        return score + "<pos=50%><b>" + rank + "</b><pos=68%>" + run.kills + "<pos=84%>" + time;
    }

    public void LoadScene(string sceneName)
    {
        // Chuyển qua màn hình Loading, LoadingManager sẽ đọc TargetScene để load.
        PlayerPrefs.SetString("TargetScene", sceneName);
        SceneManager.LoadScene(loadingScene);
    }

    public void Logout()
    {
        AuthService.Logout();
        SceneManager.LoadScene(loginScene);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
