using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Màn hình YOU LOSE: điểm, kỷ lục, rank cao nhất, nút chơi lại / về menu.
/// Gắn trên Canvas GameOverScreen trong DoomScene, các ô bên dưới kéo thả trong Inspector.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    public string mainMenuScene = "MainMenu";

    [Header("UI")]
    public CanvasGroup panel; // bật/tắt cả màn hình, component này phải nằm ngoài panel
    public RectTransform title; // chữ YOU LOSE, có hiệu ứng đập vào
    public GameObject newRecord;
    public TextMeshProUGUI scoreValue;
    public TextMeshProUGUI bestValue;
    public TextMeshProUGUI killsValue;
    public TextMeshProUGUI timeValue;
    public TextMeshProUGUI rankValue;
    public Button retryButton;
    public Button menuButton;
    public TextMeshProUGUI savedText; // "RUN SAVED · GAME #12 ..." (tùy chọn)
    public Color savedColor = new Color32(0x8A, 0xA4, 0xAB, 0xFF);
    public Color saveErrorColor = new Color32(0xFF, 0x5A, 0x4F, 0xFF);
    public Color bestColor = new Color32(0x8A, 0xA4, 0xAB, 0xFF);
    public Color newRecordColor = new Color32(0xF2, 0xC2, 0x30, 0xFF);

    [Header("Màn hình thắng (khi hạ boss)")]
    public Color winColor = new Color32(0xF2, 0xC2, 0x30, 0xFF);
    TextMeshProUGUI titleText;
    string loseText;
    Color loseColor;

    ScoreManager score;
    float shownAt = -1f;

    void Start()
    {
        score = ScoreManager.Instance;
        if (title)
        {
            titleText = title.GetComponentInChildren<TextMeshProUGUI>(true);
            if (titleText)
            {
                loseText = titleText.text;
                loseColor = titleText.color;
            }
        }
        if (panel)
            panel.gameObject.SetActive(false);
        if (retryButton)
            retryButton.onClick.AddListener(Retry);
        if (menuButton)
            menuButton.onClick.AddListener(GoToMenu);
        if (score)
            score.GameOver += Show;
    }

    void OnDestroy()
    {
        if (score)
            score.GameOver -= Show;
    }

    void Show()
    {
        // Dừng game và trả chuột cho người chơi
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        DisablePlayerControl();
        EnsureEventSystem();

        if (scoreValue)
            scoreValue.text = ScoreManager.FormatScore(score.Score);
            
        if (titleText)
        {
            bool win = score.IsVictory;
            titleText.text = win ? "YOU WIN" : loseText;
            titleText.color = win ? winColor : loseColor;
        }
        if (bestValue)
        {
            bestValue.text = ScoreManager.FormatScore(score.BestScore);
            bestValue.color = score.IsNewBest ? newRecordColor : bestColor;
        }
        if (killsValue)
            killsValue.text = score.Kills.ToString();
        if (timeValue)
        {
            int seconds = Mathf.FloorToInt(score.TimeAlive);
            timeValue.text = (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }
        if (rankValue)
        {
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
        }
        if (newRecord)
            newRecord.SetActive(score.IsNewBest);
        if (savedText)
        {
            var save = score.LastSave;
            if (score.RunSaved && save != null)
            {
                savedText.text =
                    "RUN SAVED  ·  GAME #"
                    + save.gamesPlayed
                    + "  ·  "
                    + ScoreManager.FormatScore(save.totalKills)
                    + " TOTAL KILLS";
                savedText.color = savedColor;
            }
            else
            {
                savedText.text = "COULD NOT SAVE THIS RUN";
                savedText.color = saveErrorColor;
            }
        }

        if (panel)
        {
            panel.gameObject.SetActive(true);
            panel.alpha = 0f;
        }
        shownAt = Time.unscaledTime;
        if (retryButton && EventSystem.current)
            EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
    }

    void Update()
    {
        if (shownAt < 0f)
            return;

        float t = Time.unscaledTime - shownAt;
        if (panel)
            panel.alpha = Mathf.Clamp01(t / 0.6f);
        // Chữ YOU LOSE đập vào rồi thu nhỏ về
        if (title)
            title.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, Mathf.Clamp01(t / 0.35f));

        if (t < 0.5f)
            return; // tránh bấm nhầm lúc vừa chết
        if (
            Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetKeyDown(KeyCode.R)
        )
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
        foreach (var c in FindObjectsByType<PlayerMove>())
            c.enabled = false;
        foreach (var c in FindObjectsByType<MouseLook>())
            c.enabled = false;
        foreach (var c in FindObjectsByType<Gun>())
            c.enabled = false;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current)
            return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }
}
