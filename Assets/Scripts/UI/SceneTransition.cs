using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Sahne geçişlerinde fade efekti
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [Header("Transition Settings")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Optional Loading Screen")]
    [SerializeField] private GameObject loadingScreen;
    [SerializeField] private UnityEngine.UI.Slider loadingBar;

    private bool isTransitioning = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Başlangıçta fade overlay'i ayarla
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
            StartCoroutine(FadeIn());
        }
    }

    /// <summary>
    /// Yeni sahneye geçiş yap
    /// </summary>
    public void LoadScene(string sceneName)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene(sceneName));
        }
    }

    /// <summary>
    /// Yeni sahneye geçiş yap (index ile)
    /// </summary>
    public void LoadScene(int sceneIndex)
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene(sceneIndex));
        }
    }

    private IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;

        // Fade out
        yield return StartCoroutine(FadeOut());

        // Loading screen göster
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        // Async sahne yükle
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            if (loadingBar != null)
            {
                loadingBar.value = progress;
            }

            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        // Loading screen gizle
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }

        // Fade in
        yield return StartCoroutine(FadeIn());

        isTransitioning = false;
    }

    private IEnumerator TransitionToScene(int sceneIndex)
    {
        isTransitioning = true;

        yield return StartCoroutine(FadeOut());

        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);
        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);

            if (loadingBar != null)
            {
                loadingBar.value = progress;
            }

            if (asyncLoad.progress >= 0.9f)
            {
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }

        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false);
        }

        yield return StartCoroutine(FadeIn());

        isTransitioning = false;
    }

    private IEnumerator FadeOut()
    {
        if (fadeCanvasGroup == null) yield break;

        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeCurve.Evaluate(elapsed / fadeDuration);
            fadeCanvasGroup.alpha = t;
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeIn()
    {
        if (fadeCanvasGroup == null) yield break;

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = fadeCurve.Evaluate(elapsed / fadeDuration);
            fadeCanvasGroup.alpha = 1f - t;
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Statik metod - herhangi bir yerden çağırılabilir
    /// </summary>
    public static void TransitionTo(string sceneName)
    {
        if (Instance != null)
        {
            Instance.LoadScene(sceneName);
        }
        else
        {
            // Fallback - direkt yükle
            SceneManager.LoadScene(sceneName);
        }
    }
}
