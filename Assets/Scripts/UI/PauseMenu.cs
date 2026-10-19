using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Esc để dừng / chơi tiếp. Panel có Resume, Save, Load, Main Menu.
/// Gắn trên Canvas PauseScreen trong DoomScene; không mở được khi đang ở màn hình YOU LOSE.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    public string mainMenuScene = "MainMenu";

    [Header("UI")]
    public CanvasGroup panel;               // component này phải nằm ngoài panel
    public Button resumeButton;
    public Button saveButton;
    public Button loadButton;
    public Button menuButton;
    public TextMeshProUGUI statusText;      // "Saved 14:32", "No saved game"...
    public Color statusColor = new Color32(0x8A, 0xA4, 0xAB, 0xFF);
    public Color errorColor = new Color32(0xFF, 0x5A, 0x4F, 0xFF);

    SaveGameController saver;

    void Awake()
    {
        IsPaused = false;   // static, nên reset mỗi lần vào scene
    }

    void OnDestroy()
    {
        IsPaused = false;
    }

    void Start()
    {
        saver = FindAnyObjectByType<SaveGameController>();
        if (panel) panel.gameObject.SetActive(false);
        if (resumeButton) resumeButton.onClick.AddListener(Resume);
        if (saveButton) saveButton.onClick.AddListener(SaveClicked);
        if (loadButton) loadButton.onClick.AddListener(LoadClicked);
        if (menuButton) menuButton.onClick.AddListener(GoToMenu);
    }

    void Update()
    {
        var score = ScoreManager.Instance;
        if (score && score.IsGameOver) return;   // Esc lúc này thuộc về màn hình YOU LOSE

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (panel) panel.gameObject.SetActive(true);
        ShowLastSave();
        if (resumeButton && EventSystem.current) EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (panel) panel.gameObject.SetActive(false);
    }

    void SaveClicked()
    {
        if (!saver) return;
        bool ok = saver.SaveNow();
        SetStatus(ok ? "Game saved  ·  " + System.DateTime.Now.ToString("HH:mm") : "Could not save the game", !ok);
    }

    void LoadClicked()
    {
        if (!saver) return;
        if (saver.LoadNow()) Resume();
        else SetStatus("No saved game for this account", true);
    }

    void GoToMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    void ShowLastSave()
    {
        var save = SaveSystem.Load(SaveSystem.CurrentUser);
        if (loadButton) loadButton.interactable = save.hasSavedRun;
        SetStatus(save.hasSavedRun
            ? "Last save: " + save.savedRun.date + "  ·  " + ScoreManager.FormatScore(save.savedRun.score) + " pts"
            : "No saved game yet", false);
    }

    void SetStatus(string text, bool error)
    {
        if (!statusText) return;
        statusText.text = text;
        statusText.color = error ? errorColor : statusColor;
    }
}
