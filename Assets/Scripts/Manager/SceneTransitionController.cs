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

        // 2. Kamera Animasyonu (Varsa)
        CameraController camController = FindFirstObjectByType<CameraController>();
        if (camController != null)
        {
            bool animComplete = false;
            StartCoroutine(camController.PlayLevelExitAnimation(() => { animComplete = true; }));
            
            // Animasyon bitene kadar bekle (timeout koyalım ne olur ne olmaz)
            float timeout = 3f;
            while (!animComplete && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // Kamera yoksa direkt kısa bir bekleme
            yield return new WaitForSeconds(0.5f);
        }

        // 3. Fade Out (Siyaha döner)
        yield return StartCoroutine(FadeRoutine(0f, 1f, 0.5f, Color.black));

        // 4. Sahneyi Yükle
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // Yükleme bitene kadar bekle
        while (op.progress < 0.9f)
        {
            yield return null;
        }
        
        op.allowSceneActivation = true;

        // Sahne değişmesini bekle (SceneLoaded eventi yerine frame bekleme)
        yield return new WaitForEndOfFrame(); // Eski sahne sonu
        yield return new WaitForEndOfFrame(); // Yeni sahne başı

        // Yeni sahne için TimeScale düzelt
        Time.timeScale = 1f;

        // 5. Fade In (Siyahtan açılır)
        yield return StartCoroutine(FadeRoutine(1f, 0f, 1f, Color.black));

        // İşlem bitti, etkileşimi aç
        if (fadeImage != null) fadeImage.raycastTarget = false;
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
