using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] string loginScene = "Login";
    [SerializeField] string loadingScene = "LoadingScene";

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
