
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    [SerializeField] private Slider progressBar;

    [SerializeField] private float minimumLoadingTime = 2f;

    private void Start()
    {
        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
        }

        StartCoroutine(LoadTargetScene());
    }

    private IEnumerator LoadTargetScene()
    {
        float startTime = Time.realtimeSinceStartup;

        yield return null;

        string targetScene =
            PlayerPrefs.GetString("TargetScene", "DoomScene");

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(targetScene);

        operation.allowSceneActivation = false;

        float displayedProgress = 0f;

        while (true)
        {
            float elapsed =
                Time.realtimeSinceStartup - startTime;

            float timeProgress = Mathf.Clamp01(
                elapsed / minimumLoadingTime
            );

            float actualProgress = Mathf.Clamp01(
                operation.progress / 0.9f
            );

            // Do not display 100% until the scene is ready
            float targetProgress = Mathf.Min(
                timeProgress,
                actualProgress
            );

            // Smoothly animate the slider
            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                targetProgress,
                Time.unscaledDeltaTime * 0.5f
            );

            if (progressBar != null)
            {
                progressBar.value = displayedProgress;
            }

            bool sceneReady = operation.progress >= 0.9f;
            bool minimumTimePassed =
                elapsed >= minimumLoadingTime;

            if (sceneReady && minimumTimePassed)
            {
                break;
            }

            yield return null;
        }

        // Smoothly finish the bar at 100%
        while (displayedProgress < 1f)
        {
            displayedProgress = Mathf.MoveTowards(
                displayedProgress,
                1f,
                Time.unscaledDeltaTime * 0.5f
            );

            if (progressBar != null)
            {
                progressBar.value = displayedProgress;
            }

            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);

        operation.allowSceneActivation = true;
    }
}
