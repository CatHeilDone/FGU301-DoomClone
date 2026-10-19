using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tự gắn hệ thống điểm, đợt enemy, style HUD và màn hình YOU LOSE
/// vào mọi scene có PlayerHealth, nên không phải sửa file scene.
/// UI lấy từ prefab trong Resources/UI. Nếu đã kéo prefab vào scene thì dùng cái trong scene.
/// </summary>
public static class GameplayBootstrap
{
    public const string StyleHUDPath = "UI/StyleHUD";
    public const string GameOverPath = "UI/GameOverScreen";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (ScoreManager.Instance && ScoreManager.Instance.gameObject.scene == scene) return;
        if (!Object.FindFirstObjectByType<PlayerHealth>()) return;

        // sceneLoaded chạy trước Start của các object trong scene,
        // nên Enemy.Start đã thấy EnemySpawner.Instance
        var systems = new GameObject("Gameplay Systems");
        systems.AddComponent<ScoreManager>();
        systems.AddComponent<EnemySpawner>();

        if (!Object.FindFirstObjectByType<StyleMeterHUD>(FindObjectsInactive.Include))
            Spawn(StyleHUDPath, HUDBuilder.BuildStyleHUD);
        if (!Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include))
            Spawn(GameOverPath, HUDBuilder.BuildGameOverScreen);
    }

    static void Spawn(string resourcePath, System.Func<GameObject> fallback)
    {
        var prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab)
        {
            Object.Instantiate(prefab).name = prefab.name;
            return;
        }

        Debug.LogWarning("[HUD] Chưa có prefab Resources/" + resourcePath
                         + ", đang dựng tạm bằng code. Chạy Tools > DoomClone > Create HUD Prefabs để tạo prefab.");
        fallback();
    }
}
