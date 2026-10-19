using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Màn hình YOU LOSE: điểm, kỷ lục, rank cao nhất, nút chơi lại / về menu.
/// Hiện khi ScoreManager báo GameOver (PlayerHealth hết máu).
/// </summary>
public class GameOverUI : MonoBehaviour
{
    public string mainMenuScene = "MainMenu";

    ScoreManager score;
    CanvasGroup group;
    RectTransform title;
    TextMeshProUGUI scoreValue, bestValue, killsValue, timeValue, rankValue, recordText;
    Button retryButton;
    float shownAt = -1f;

    void Start()
    {
        score = ScoreManager.Instance;
        Build();
        group.gameObject.SetActive(false);
        if (score) score.GameOver += Show;
    }

    void OnDestroy()
    {
        if (score) score.GameOver -= Show;
    }

    void Build()
    {
        var canvas = UIFactory.CreateCanvas("Game Over", 100, transform);
        group = canvas.gameObject.AddComponent<CanvasGroup>();
        var root = canvas.transform;

        var bg = UIFactory.Panel("Background", root, new Color(0.06f, 0.01f, 0.01f, 0.86f));
        bg.raycastTarget = true;

        title = UIFactory.Rect("", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1400, 200));
        UIFactory.Text("YOU LOSE", title, "YOU LOSE", 180, UIFactory.Hurt, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);

        recordText = UIFactory.Text("New Record", UIFactory.Rect("", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 110), new Vector2(800, 50)),
            "NEW RECORD!", 42, UIFactory.Hazard, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);

        scoreValue = Stat(root, "SCORE", new Vector2(-300, 10), 76, UIFactory.Fg);
        bestValue = Stat(root, "BEST", new Vector2(300, 10), 76, UIFactory.Muted);
        killsValue = Stat(root, "KILLS", new Vector2(-300, -110), 44, UIFactory.Fg);
        timeValue = Stat(root, "TIME", new Vector2(0, -110), 44, UIFactory.Fg);
        rankValue = Stat(root, "MAX STYLE", new Vector2(300, -110), 44, UIFactory.Fg);

        retryButton = UIFactory.Button(root, "RETRY", new Vector2(-170, -250), new Vector2(300, 80), UIFactory.Water, UIFactory.Ink, Retry);
        UIFactory.Button(root, "MAIN MENU", new Vector2(170, -250), new Vector2(300, 80), new Color(1f, 1f, 1f, 0.12f), UIFactory.Fg, GoToMenu);

        UIFactory.Text("Hint", UIFactory.Rect("", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -330), new Vector2(800, 30)),
            "ENTER  retry     ESC  menu", 22, UIFactory.Muted, TextAlignmentOptions.Center, FontStyles.Normal);
    }

    TextMeshProUGUI Stat(Transform root, string label, Vector2 pos, float size, Color color)
    {
        var box = UIFactory.Rect("Stat " + label, root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(420, size + 40));
        UIFactory.Text("Label", UIFactory.Rect("", box, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(420, 30)),
            label, 22, UIFactory.Muted, TextAlignmentOptions.Center);
        return UIFactory.Text("Value", UIFactory.Rect("", box, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(420, size + 10)),
            "0", size, color, TextAlignmentOptions.Center, FontStyles.Bold | FontStyles.Italic);
    }

    void Show()
    {
        // Dừng game và trả chuột cho người chơi
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        DisablePlayerControl();
        EnsureEventSystem();

        scoreValue.text = score.Score.ToString("N0");
        bestValue.text = score.BestScore.ToString("N0");
        killsValue.text = score.Kills.ToString();
        int seconds = Mathf.FloorToInt(score.TimeAlive);
        timeValue.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        if (score.HighestRank >= 0)
        {
            var rank = ScoreManager.Ranks[score.HighestRank];
            rankValue.text = rank.letter;
            rankValue.color = rank.color;
        }
        else
        {
            rankValue.text = "-";
        }
        recordText.gameObject.SetActive(score.IsNewBest);
        bestValue.color = score.IsNewBest ? UIFactory.Hazard : UIFactory.Muted;

        group.gameObject.SetActive(true);
        group.alpha = 0f;
        shownAt = Time.unscaledTime;
        EventSystem.current?.SetSelectedGameObject(retryButton.gameObject);
    }

    void Update()
    {
        if (shownAt < 0f) return;

        float t = Time.unscaledTime - shownAt;
        group.alpha = Mathf.Clamp01(t / 0.6f);
        // Chữ YOU LOSE đập vào rồi thu nhỏ về
        title.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, Mathf.Clamp01(t / 0.35f));

        if (t < 0.5f) return;   // tránh bấm nhầm lúc vừa chết
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.R))
            Retry();
        else if (Input.GetKeyDown(KeyCode.Escape))
            GoToMenu();
    }

    void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void GoToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    static void DisablePlayerControl()
    {
        foreach (var c in FindObjectsByType<PlayerMove>(FindObjectsSortMode.None)) c.enabled = false;
        foreach (var c in FindObjectsByType<MouseLook>(FindObjectsSortMode.None)) c.enabled = false;
        foreach (var c in FindObjectsByType<Gun>(FindObjectsSortMode.None)) c.enabled = false;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current) return;
        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        go.transform.SetParent(null);
    }
}
