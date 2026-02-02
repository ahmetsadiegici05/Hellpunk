using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Oyuncu hasar aldığında ekran kenarlarında kırmızı yanıp sönme efekti
/// </summary>
public class DamageVignette : MonoBehaviour
{
    public static DamageVignette Instance { get; private set; }
    
    [Header("Efekt Ayarları")]
    [SerializeField] private Color damageColor = new Color(0.8f, 0f, 0f, 0.4f);
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private float lowHealthPulseSpeed = 2f;
    [SerializeField] private float lowHealthThreshold = 0.3f; // %30 can altında pulse
    
    private Canvas vignetteCanvas;
    private Image vignetteImage;
    private float currentAlpha = 0f;
    private bool isLowHealth = false;
    private Coroutine pulseCoroutine;
    
    private void Awake()
    {
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
    }
    
    private void Start()
    {
        CreateVignetteUI();
    }
    
    private void CreateVignetteUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("DamageVignetteCanvas");
        canvasObj.transform.SetParent(transform);
        vignetteCanvas = canvasObj.AddComponent<Canvas>();
        vignetteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        vignetteCanvas.sortingOrder = 90;
        
        canvasObj.AddComponent<CanvasScaler>();
        
        // Vignette image
        GameObject imgObj = new GameObject("DamageVignetteImage");
        imgObj.transform.SetParent(canvasObj.transform, false);
        
        vignetteImage = imgObj.AddComponent<Image>();
        vignetteImage.sprite = CreateVignetteSprite(256);
        vignetteImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, 0f);
        vignetteImage.raycastTarget = false;
        
        // Full screen
        RectTransform rect = imgObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }
    
    private Sprite CreateVignetteSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size * 0.7f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                
                float alpha = 0f;
                if (dist > maxDist * 0.4f)
                {
                    float t = (dist - maxDist * 0.4f) / (maxDist * 0.6f);
                    alpha = Mathf.Clamp01(t * t);
                }
                
                pixels[y * size + x] = new Color(1f, 0f, 0f, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    
    /// <summary>
    /// Hasar alındığında çağır
    /// </summary>
    public void FlashDamage()
    {
        StopAllCoroutines();
        StartCoroutine(DamageFlashRoutine());
    }
    
    /// <summary>
    /// Can durumunu güncelle (düşük canda pulse için)
    /// </summary>
    public void UpdateHealthStatus(float healthPercent)
    {
        bool wasLowHealth = isLowHealth;
        isLowHealth = healthPercent <= lowHealthThreshold;
        
        if (isLowHealth && !wasLowHealth)
        {
            // Düşük cana düştü - pulse başlat
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            pulseCoroutine = StartCoroutine(LowHealthPulse());
        }
        else if (!isLowHealth && wasLowHealth)
        {
            // Can yükseldi - pulse durdur
            if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
            StartCoroutine(FadeOut());
        }
    }
    
    private IEnumerator DamageFlashRoutine()
    {
        if (vignetteImage == null) yield break;
        
        // Hızlı flash in
        float elapsed = 0f;
        float flashInDuration = flashDuration * 0.3f;
        
        while (elapsed < flashInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flashInDuration;
            currentAlpha = Mathf.Lerp(0f, damageColor.a, t);
            vignetteImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, currentAlpha);
            yield return null;
        }
        
        // Yavaş flash out
        elapsed = 0f;
        float flashOutDuration = flashDuration * 0.7f;
        float startAlpha = currentAlpha;
        float targetAlpha = isLowHealth ? damageColor.a * 0.3f : 0f;
        
        while (elapsed < flashOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flashOutDuration;
            t = t * t; // Ease out
            currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            vignetteImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, currentAlpha);
            yield return null;
        }
        
        currentAlpha = targetAlpha;
        vignetteImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, currentAlpha);
    }
    
    private IEnumerator LowHealthPulse()
    {
        if (vignetteImage == null) yield break;
        
        while (isLowHealth)
        {
            float pulse = (Mathf.Sin(Time.unscaledTime * lowHealthPulseSpeed * Mathf.PI) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(damageColor.a * 0.15f, damageColor.a * 0.35f, pulse);
            vignetteImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, alpha);
            yield return null;
        }
    }
    
    private IEnumerator FadeOut()
    {
        if (vignetteImage == null) yield break;
        
        float startAlpha = currentAlpha;
        float elapsed = 0f;
        
        while (elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime;
            currentAlpha = Mathf.Lerp(startAlpha, 0f, elapsed / 0.3f);
            vignetteImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, currentAlpha);
            yield return null;
        }
        
        currentAlpha = 0f;
        vignetteImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, 0f);
    }
    
    /// <summary>
    /// Tüm efektleri sıfırla - respawn/restart durumlarında çağır
    /// </summary>
    public void ResetVignette()
    {
        StopAllCoroutines();
        
        isLowHealth = false;
        currentAlpha = 0f;
        
        if (vignetteImage != null)
        {
            vignetteImage.color = new Color(damageColor.r, damageColor.g, damageColor.b, 0f);
        }
        
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }
}
