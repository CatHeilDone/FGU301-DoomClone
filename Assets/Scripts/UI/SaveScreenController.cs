using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    public static bool IsPaused = false;

    public GameObject pausePanel;

    void Start()
    {
        Resume();
    }

    void Update()
    {
        if (CanvasManager.Instance.winPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused) Resume();
            else Pause();
        }
    }

    public void Pause()
    {
        IsPaused = true;
        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        IsPaused = false;
        pausePanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}