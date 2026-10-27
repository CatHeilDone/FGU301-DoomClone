using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonClickSound : MonoBehaviour
{
    public static ButtonClickSound Instance;

    public AudioSource source;
    public AudioClip clickClip;

    void Awake()
    {
        // Keep one copy that survives scene changes
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        HookButtons();   // the scene this object starts in
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        HookButtons();
    }

    void HookButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button b in buttons)
        {
            b.onClick.RemoveListener(PlayClick);   // prevents doubles
            b.onClick.AddListener(PlayClick);
        }
    }

    public void PlayClick()
    {
        if (clickClip != null) source.PlayOneShot(clickClip);
    }

    void OnDestroy()
    {
        if (Instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}