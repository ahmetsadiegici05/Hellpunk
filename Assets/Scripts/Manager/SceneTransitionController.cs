using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// "Spin & Zoom" Transition
/// Kamerayı ve dünyayı döndürerek (vortex etkisi) sahneden uzaklaştırır.
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

    [Header("Spin Settings")]
    [SerializeField] private float duration = 1.0f;
    [SerializeField] private float zoomAmount = 0.05f; // En son ne kadar küçülsün
    [SerializeField] private float spinRotations = 2f; // Kaç tam tur dönsün

    private Canvas overlayCanvas;
    private Image blackoutImage;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            CreateOverlay();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void CreateOverlay()
    {
        if (overlayCanvas != null) return;

        GameObject canvasObj = new GameObject("TransitionOverlay");
        canvasObj.transform.SetParent(transform);
        
        overlayCanvas = canvasObj.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 9999;
        
        GameObject imgObj = new GameObject("Blackout");
        imgObj.transform.SetParent(overlayCanvas.transform);
        
        blackoutImage = imgObj.AddComponent<Image>();
        blackoutImage.color = Color.clear;
        blackoutImage.raycastTarget = false; // Tıklamayı engellemesin
        
        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void LoadSceneWithTransition(string sceneName)
    {
        if (overlayCanvas == null) CreateOverlay();
        StartCoroutine(SpinRoutine(sceneName));
    }

    private IEnumerator SpinRoutine(string sceneName)
    {
        // --- STEP 1: EXIT SCENE (SPIN IN) ---
        Time.timeScale = 0f; // Fizik ve inputları dondur
        
        Camera cam = Camera.main;
        float startSize = 5f;
        Quaternion startRot = Quaternion.identity;

        // Mevcut kamera bilgilerini al
        if (cam != null)
        {
            startSize = cam.orthographicSize;
            startRot = cam.transform.rotation;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            
            // Ease In Cubic (yavaş hızlanarak başla)
            float curve = t * t * t;

            if (cam != null)
            {
                // Kamerayı döndür (Z ekseninde)
                float currentRotAngle = curve * 360f * spinRotations;
                cam.transform.rotation = startRot * Quaternion.Euler(0, 0, currentRotAngle);
                
                // Kamerayı merkeze doğru zoom'la (Vortex etkisi için)
                cam.orthographicSize = Mathf.Lerp(startSize, zoomAmount, curve);
            }

            // Sonlara doğru ekranı karart (Geçiş anındaki çirkinliği gizlemek için)
            if (t > 0.7f && blackoutImage != null)
            {
                float fadeT = (t - 0.7f) / 0.3f;
                blackoutImage.color = new Color(0, 0, 0, fadeT);
            }

            yield return null;
        }

        // Tam karartma
        if (blackoutImage != null) blackoutImage.color = Color.black;


        // --- STEP 2: LOAD NEW SCENE ---
        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        
        while (op.progress < 0.9f)
            yield return null;
            
        op.allowSceneActivation = true;
        yield return null; // Aktifleşme için bir frame bekle
        
        // --- STEP 3: ENTER SCENE (SPIN OUT) ---
        
        // Yeni sahnenin kamerasını bul
        cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        
        if (cam != null)
        {
            // Yeni kameranın orijinal ayarlarını sakla
            float targetSize = cam.orthographicSize;
            Quaternion targetRot = cam.transform.rotation;
            
            // Başlangıç durumunu ayarla (Dönmüş ve küçük)
            cam.orthographicSize = zoomAmount;
            cam.transform.rotation = targetRot * Quaternion.Euler(0, 0, 360f * spinRotations); // Spun state
            
            timer = 0f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);
                
                // Ease Out Cubic (hızlı başla, yavaşlayarak dur)
                float curve = 1f - Mathf.Pow(1f - t, 3f);
                
                // Dönüşü geri al
                float currentRotAngle = (1f - curve) * 360f * spinRotations;
                cam.transform.rotation = targetRot * Quaternion.Euler(0, 0, currentRotAngle);
                
                // Zoom'u geri aç
                cam.orthographicSize = Mathf.Lerp(zoomAmount, targetSize, curve);
                
                // Ekran karartmasını aç
                if (t < 0.4f && blackoutImage != null)
                {
                    // İlk %40'lık kısımda aç
                    blackoutImage.color = Color.Lerp(Color.black, Color.clear, t / 0.4f);
                }
                else if (blackoutImage != null)
                {
                    blackoutImage.color = Color.clear;
                }

                yield return null;
            }
            
            // Garantileme
            cam.transform.rotation = targetRot;
            cam.orthographicSize = targetSize;
        }
        else
        {
            // Kamera yoksa sadece blackout'u kaldır
            if (blackoutImage != null) blackoutImage.color = Color.clear;
        }
        
        Time.timeScale = 1f; // Oyunu devam ettir
    }
}
