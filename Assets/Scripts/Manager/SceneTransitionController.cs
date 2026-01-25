using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Sahne geçişlerini ve animasyonlarını yöneten kontrolcü.
/// </summary>
public class SceneTransitionController : MonoBehaviour
{
    private static SceneTransitionController instance;
    public static SceneTransitionController Instance
    {
        get
        {
            if (instance == null)
            {
                var existing = FindFirstObjectByType<SceneTransitionController>();
                if (existing != null)
                    instance = existing;
                else
                {
                    GameObject go = new GameObject("SceneTransitionController");
                    instance = go.AddComponent<SceneTransitionController>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private Canvas transitionCanvas;
    private Image fadeImage;

    // Singleton yönetimi
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateTransitionUI();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void CreateTransitionUI()
    {
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform);
        
        transitionCanvas = canvasObj.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 999; // En üstte

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(canvasObj.transform);
        
        fadeImage = imgObj.AddComponent<Image>();
        fadeImage.color = Color.clear; // Başlangıçta görünmez
        
        RectTransform rect = imgObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        // Touch/Click geçirmemesi için
        fadeImage.raycastTarget = false; 
    }

    /// <summary>
    /// Level geçişini başlatır: Kamera animasyonu -> Fade Out -> Sahne Yükle -> Fade In
    /// </summary>
    public void LoadSceneWithTransition(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        // 1. Raycast'i aç ki oyuncu hareket edemesin
        if (fadeImage != null) fadeImage.raycastTarget = true;

        // 2. Beyaz Flash efekti (çok hızlı)
        yield return StartCoroutine(FlashEffect());

        // 3. Kamera Animasyonu (kısa zoom out)
        CameraController camController = FindFirstObjectByType<CameraController>();
        if (camController != null)
        {
            bool animComplete = false;
            StartCoroutine(camController.PlayLevelExitAnimation(() => { animComplete = true; }));
            
            // Animasyon bitene kadar bekle (timeout koyalım)
            float timeout = 1f;
            while (!animComplete && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        // 4. Fade Out (Siyaha döner) - daha hızlı
        yield return StartCoroutine(FadeRoutine(0f, 1f, 0.3f, Color.black));

        // 5. Sahneyi Yükle
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // Yükleme bitene kadar bekle
        while (op.progress < 0.9f)
        {
            yield return null;
        }
        
        op.allowSceneActivation = true;

        // Sahne değişmesini bekle
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Yeni sahne için TimeScale düzelt
        Time.timeScale = 1f;

        // 6. Fade In (Siyahtan açılır) - smooth
        yield return StartCoroutine(FadeRoutine(1f, 0f, 0.8f, Color.black));

        // İşlem bitti, etkileşimi aç
        if (fadeImage != null) fadeImage.raycastTarget = false;
    }
    
    /// <summary>
    /// Hızlı beyaz flash efekti - level geçişinde parlama
    /// </summary>
    private IEnumerator FlashEffect()
    {
        if (fadeImage == null) CreateTransitionUI();
        
        float flashDuration = 0.15f;
        float time = 0f;
        
        // Beyaza flash
        while (time < flashDuration)
        {
            time += Time.deltaTime;
            float t = time / flashDuration;
            float alpha = Mathf.Sin(t * Mathf.PI); // 0 -> 1 -> 0 eğrisi
            fadeImage.color = new Color(1f, 1f, 1f, alpha * 0.7f);
            yield return null;
        }
        
        fadeImage.color = Color.clear;
    }

    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float duration, Color color)
    {
        if (fadeImage == null) CreateTransitionUI();
        
        Color c = color;
        float time = 0f;

        fadeImage.color = new Color(c.r, c.g, c.b, startAlpha);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            // Smoothstep
            t = t * t * (3f - 2f * t);
            
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }

        fadeImage.color = new Color(c.r, c.g, c.b, targetAlpha);
    }
}
