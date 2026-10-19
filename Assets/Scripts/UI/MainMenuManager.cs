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

        if (playerNameText) playerNameText.text = AuthService.CurrentUser.ToUpperInvariant();
        if (bestScoreText) bestScoreText.text = ScoreManager.FormatScore(ScoreManager.GetBestScore(AuthService.CurrentUser));
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
