using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Chuyển cảnh mượt: màn hình mờ dần sang màu nền → nạp scene mới → hiện dần lên.
/// Tự tạo khi game bắt đầu (không cần đặt vào scene) và hiện dần scene đầu tiên
/// ngay sau splash của Unity, để không bị "bật" thẳng vào màn hình Login.
/// Dùng: SceneFader.LoadScene("MainMenu");
/// </summary>
public class SceneFader : MonoBehaviour
{
    public static Color fadeColor = new Color32(0x11, 0x12, 0x14, 0xFF);   // trùng màu nền Login
    public static float fadeOutTime = 0.35f;
    public static float fadeInTime = 0.5f;
    public static float firstSceneFadeIn = 0.8f;

    static SceneFader instance;

    CanvasGroup group;
    bool loading;
    Coroutine fadeIn;   // lần hiện dần đầu game, dừng lại nếu chuyển cảnh ngay

    public static bool IsLoading => instance && instance.loading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void FadeInFirstScene()
    {
        var fader = Get();
        fader.group.alpha = 1f;
        fader.fadeIn = fader.StartCoroutine(fader.Fade(1f, 0f, firstSceneFadeIn));
    }

    /// <summary>Mờ dần, nạp scene, rồi hiện dần. Gọi lần nữa trong lúc đang chuyển thì bỏ qua.</summary>
    public static void LoadScene(string sceneName)
    {
        var fader = Get();
        if (fader.loading) return;
        if (fader.fadeIn != null) { fader.StopCoroutine(fader.fadeIn); fader.fadeIn = null; }
        fader.StartCoroutine(fader.LoadRoutine(sceneName));
    }

    static SceneFader Get()
    {
        if (instance) return instance;

        var go = new GameObject("[SceneFader]", typeof(RectTransform));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SceneFader>();

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;   // trên mọi UI khác
        go.AddComponent<GraphicRaycaster>();

        var img = new GameObject("Fade", typeof(RectTransform)).AddComponent<Image>();
        img.transform.SetParent(go.transform, false);
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        img.color = fadeColor;

        instance.group = go.AddComponent<CanvasGroup>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;
        return instance;
    }

    IEnumerator LoadRoutine(string sceneName)
    {
        loading = true;
        yield return Fade(group.alpha, 1f, fadeOutTime);

        var op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone) yield return null;
        yield return null;   // đợi Start() của scene mới chạy xong rồi mới hiện lên

        loading = false;
        yield return Fade(1f, 0f, fadeInTime);
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        group.blocksRaycasts = true;   // chặn bấm nút trong lúc chuyển cảnh
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            // SmoothStep cho nhịp vào / ra mềm hơn tuyến tính
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
        group.alpha = to;
        group.blocksRaycasts = to > 0.01f;
    }
}
