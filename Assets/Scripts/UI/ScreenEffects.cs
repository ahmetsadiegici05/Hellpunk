using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Post-processing benzeri görsel efektler (vignette, color tint, flash).
/// Screen Space Overlay Canvas'a eklenir.
/// </summary>
public class ScreenEffects : MonoBehaviour
{
    public static ScreenEffects Instance { get; private set; }
    
    [Header("Vignette")]
    [SerializeField] private bool enableVignette = true;
    [SerializeField] private float vignetteIntensity = 0.3f;
    [SerializeField] private Color vignetteColor = new Color(0, 0, 0, 1);
    
    [Header("Damage Flash")]
    [SerializeField] private Color damageColor = new Color(1f, 0f, 0f, 0.3f);
    [SerializeField] private float damageFlashDuration = 0.15f;
    
    [Header("Heal Flash")]
    [SerializeField] private Color healColor = new Color(0.3f, 1f, 0.3f, 0.25f);
    [SerializeField] private float healFlashDuration = 0.3f;
    
    private Canvas canvas;
    private Image vignetteImage;
    private Image flashImage;
    private Coroutine flashCoroutine;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        SetupCanvas();
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    
    private void SetupCanvas()
    {
        // Canvas
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        
        // Canvas Scaler
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Vignette Image
        if (enableVignette)
        {
            CreateVignette();
        }
        
        // Flash Image
        CreateFlashImage();
    }
    
    private void CreateVignette()
    {
        GameObject vignetteObj = new GameObject("Vignette");
        vignetteObj.transform.SetParent(transform, false);
        
        vignetteImage = vignetteObj.AddComponent<Image>();
        vignetteImage.sprite = CreateVignetteSprite();
        vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, vignetteIntensity);
        vignetteImage.raycastTarget = false;
        
        RectTransform rect = vignetteObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    
    private void CreateFlashImage()
    {
        GameObject flashObj = new GameObject("Flash");
        flashObj.transform.SetParent(transform, false);
        
        flashImage = flashObj.AddComponent<Image>();
        flashImage.color = new Color(1, 1, 1, 0);
        flashImage.raycastTarget = false;
        
        RectTransform rect = flashObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
    
    private Sprite CreateVignetteSprite()
    {
        int size = 256;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDist = dist / maxDist;
                
                // Eliptik vignette (kenarlar daha koyu)
                float alpha = Mathf.Pow(normalizedDist, 2f);
                alpha = Mathf.Clamp01(alpha);
                
                pixels[y * size + x] = new Color(0, 0, 0, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    
    /// <summary>
    /// Hasar flash efekti
    /// </summary>
    public void FlashDamage()
    {
        Flash(damageColor, damageFlashDuration);
    }
    
    /// <summary>
    /// İyileşme flash efekti
    /// </summary>
    public void FlashHeal()
    {
        Flash(healColor, healFlashDuration);
    }
    
    /// <summary>
    /// Özel renk ve süreyle flash
    /// </summary>
    public void Flash(Color color, float duration)
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        
        flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
    }
    
    private IEnumerator FlashCoroutine(Color color, float duration)
    {
        if (flashImage == null) yield break;
        
        float elapsed = 0f;
        flashImage.color = color;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
            Color c = color;
            c.a = color.a * (1f - t);
            flashImage.color = c;
            
            yield return null;
        }
        
        flashImage.color = new Color(1, 1, 1, 0);
    }
    
    /// <summary>
    /// Vignette yoğunluğunu değiştir (can azaldığında artırılabilir)
    /// </summary>
    public void SetVignetteIntensity(float intensity)
    {
        if (vignetteImage != null)
        {
            Color c = vignetteImage.color;
            c.a = intensity;
            vignetteImage.color = c;
        }
    }
    
    /// <summary>
    /// Düşük can vignette efekti
    /// </summary>
    public void UpdateHealthVignette(float healthPercent)
    {
        if (vignetteImage == null) return;
        
        // Can düştükçe kırmızı vignette artar
        float intensity = vignetteIntensity;
        Color color = vignetteColor;
        
        if (healthPercent < 0.3f)
        {
            float dangerFactor = 1f - (healthPercent / 0.3f);
            color = Color.Lerp(vignetteColor, new Color(0.5f, 0f, 0f, 1f), dangerFactor);
            intensity = Mathf.Lerp(vignetteIntensity, 0.5f, dangerFactor);
            
            // Pulse efekti
            intensity += Mathf.Sin(Time.time * 3f) * 0.1f * dangerFactor;
        }
        
        vignetteImage.color = new Color(color.r, color.g, color.b, intensity);
    }
}
