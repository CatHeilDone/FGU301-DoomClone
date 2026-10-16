using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] string loginScene = "Login";

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
        SceneManager.LoadScene(sceneName);
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