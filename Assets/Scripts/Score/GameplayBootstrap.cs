using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Tự gắn hệ thống điểm, style meter, đợt enemy và màn hình YOU LOSE
/// vào mọi scene có PlayerHealth, nên không phải sửa file scene.
/// </summary>
public static class GameplayBootstrap
{
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
        var go = new GameObject("Gameplay Systems");
        go.AddComponent<ScoreManager>();
        go.AddComponent<EnemySpawner>();
        go.AddComponent<StyleMeterHUD>();
        go.AddComponent<GameOverUI>();
    }
}
