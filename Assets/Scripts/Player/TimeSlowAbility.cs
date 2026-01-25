using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Oyuncuya zaman yavaşlatma yeteneği verir.
/// Sağ mouse click ile aktifleşir, skill kombinasyonlarını girmek için kullanılır.
/// </summary>
public class TimeSlowAbility : MonoBehaviour
{
    [Header("Ability Settings")]
    [SerializeField] private KeyCode activateKey = KeyCode.E; // E tuşu ile zaman yavaşlatma
    [SerializeField] private float slowMotionScale = 0.3f;      // Zaman ne kadar yavaşlasın (0.3 = %30 hız)
    [SerializeField] private float slowDuration = 10f;          // 10 saniye sürsün (gerçek zamanda)
    [SerializeField] private float cooldownTime = 15f;          // Tekrar kullanmak için bekleme

    [Header("Visual Effects")]
    [SerializeField] private bool useVisualEffects = true;
    [SerializeField] private Color slowMotionTint = new Color(0.7f, 0.85f, 1f, 1f); // Hafif mavi ton
    [SerializeField] private float vignetteIntensity = 0.3f;

    [Header("UI (Optional)")]
    [SerializeField] private Image cooldownFillImage;           // Cooldown göstergesi
    [SerializeField] private Image abilityIconImage;            // Ability ikonu

    [Header("Audio (Optional)")]
    [Tooltip("Time slow ses kontrolürü - Inspector'dan atanmazsa otomatik bulunur")]
    [SerializeField] private TimeSlowAudioController audioController;
    
    [Header("Legacy Audio (Deprecated)")]
    [SerializeField] private AudioSource activateSound;
    [SerializeField] private AudioSource deactivateSound;

    private float cooldownTimer = 0f;
    private bool isSlowMotionActive = false;
    private float originalFixedDeltaTime;
    private SpriteRenderer[] allSpriteRenderers;
    private Color[] originalColors;
    private float currentElapsed = 0f; // Aktif süre takibi
    
    // Vignette UI
    private Canvas vignetteCanvas;
    private UnityEngine.UI.Image vignetteImage;
    private float currentVignetteAlpha = 0f;

    // Static instance for easy access
    public static TimeSlowAbility Instance { get; private set; }
    
    public bool IsSlowMotionActive => isSlowMotionActive;
    public float CooldownProgress => cooldownTimer <= 0 ? 1f : 1f - (cooldownTimer / cooldownTime);
    public float CooldownRemaining => cooldownTimer;  // Kalan saniye
    public bool IsReady => cooldownTimer <= 0f && !isSlowMotionActive;
    
    /// <summary>
    /// Time slow aktifken kalan süre (0-10 saniye)
    /// </summary>
    public float ActiveTimeRemaining => isSlowMotionActive ? Mathf.Max(0, slowDuration - currentElapsed) : 0f;
    
    /// <summary>
    /// Time slow toplam süresi (10 saniye)
    /// </summary>
    public float ActiveDuration => slowDuration;
    
    /// <summary>
    /// Time slow aktifken oyuncunun normal hızda kalması için kullanılacak çarpan.
    /// Örn: timeScale=0.3 iken bu değer ~3.33 olur, böylece oyuncu normal hızda kalır.
    /// </summary>
    public float PlayerTimeCompensation => isSlowMotionActive ? (1f / Time.timeScale) : 1f;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
            
        originalFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Start()
    {        // TimeSlowAudioController'\u0131 bul (atanmad\u0131ysa)
        if (audioController == null)
        {
            audioController = TimeSlowAudioController.Instance;
            if (audioController == null)
                audioController = FindFirstObjectByType<TimeSlowAudioController>();
        }
        
        // Vignette UI oluştur
        CreateVignetteUI();
        
        // Sahnedeki tüm sprite'ları bul (görsel efekt için)
        if (useVisualEffects)
        {
            allSpriteRenderers = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            originalColors = new Color[allSpriteRenderers.Length];
            for (int i = 0; i < allSpriteRenderers.Length; i++)
            {
                originalColors[i] = allSpriteRenderers[i].color;
            }
        }
    }

    private void Update()
    {
        // Cooldown timer
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.unscaledDeltaTime; // unscaled çünkü zaman yavaşken de çalışmalı
        }

        // UI güncelle
        UpdateUI();
        
        // NOT: E tuşu ile direkt aktivasyon kaldırıldı
        // Artık GuitarSkillSystem üzerinden R tuşu + ok kombinasyonu ile aktifleşiyor
    }
    
    /// <summary>
    /// GuitarSkillSystem'dan çağrılır - skill kombinasyonu başarılı olunca
    /// </summary>
    public void ActivateFromSkillSystem()
    {
        if (!IsReady) return;
        
        StartCoroutine(SlowMotionRoutine());
    }

    private IEnumerator SlowMotionRoutine()
    {
        isSlowMotionActive = true;
        currentElapsed = 0f; // Süre sıfırla

        // TimeSlowAudioController Integration
        if (audioController != null)
            audioController.StartTimeSlowSequence();

        // Zaman yavaşlat
        Time.timeScale = slowMotionScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * slowMotionScale;

        // Görsel efekt - renk tonu
        if (useVisualEffects)
            ApplySlowMotionVisuals(true);

        // Süre boyunca bekle (unscaled time kullan)
        while (currentElapsed < slowDuration)
        {
            currentElapsed += Time.unscaledDeltaTime;
            
            // Süre bitmeden önce hafif "fade out" efekti
            if (currentElapsed > slowDuration * 0.7f)
            {
                float fadeProgress = (currentElapsed - slowDuration * 0.7f) / (slowDuration * 0.3f);
                Time.timeScale = Mathf.Lerp(slowMotionScale, 1f, fadeProgress);
                Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
            }
            
            yield return null;
        }

        // Normal zamana dön
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;

        // Görsel efekti kaldır
        if (useVisualEffects)
            ApplySlowMotionVisuals(false);

        // TimeSlowAudioController Integration
        if (audioController != null)
            audioController.StopTimeSlowSequence();

        isSlowMotionActive = false;
        cooldownTimer = cooldownTime;
    }

    private void ApplySlowMotionVisuals(bool activate)
    {
        // Vignette efekti
        if (activate)
            StartCoroutine(FadeVignette(vignetteIntensity, 0.3f));
        else
            StartCoroutine(FadeVignette(0f, 0.5f));
        
        if (allSpriteRenderers == null) return;

        for (int i = 0; i < allSpriteRenderers.Length; i++)
        {
            if (allSpriteRenderers[i] == null) continue;

            if (activate)
            {
                // Mavi ton uygula
                Color original = originalColors[i];
                allSpriteRenderers[i].color = new Color(
                    original.r * slowMotionTint.r,
                    original.g * slowMotionTint.g,
                    original.b * slowMotionTint.b,
                    original.a
                );
            }
            else
            {
                // Orijinal renge dön
                allSpriteRenderers[i].color = originalColors[i];
            }
        }
    }
    
    private void CreateVignetteUI()
    {
        // Canvas oluştur
        GameObject canvasObj = new GameObject("TimeSlowVignetteCanvas");
        vignetteCanvas = canvasObj.AddComponent<Canvas>();
        vignetteCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        vignetteCanvas.sortingOrder = 50; // Diğer UI'ların altında
        
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        
        // Vignette image oluştur
        GameObject imgObj = new GameObject("VignetteImage");
        imgObj.transform.SetParent(canvasObj.transform, false);
        
        vignetteImage = imgObj.AddComponent<UnityEngine.UI.Image>();
        vignetteImage.sprite = CreateVignetteSprite(512);
        vignetteImage.color = new Color(0f, 0f, 0f, 0f); // Başlangıçta görünmez
        vignetteImage.raycastTarget = false;
        
        // Full screen yap
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
        float maxDist = size * 0.7f; // Kenarlardan başlayan vignette
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                
                // Merkeze yakın = saydam, kenarlara yakın = opak
                float alpha = 0f;
                if (dist > maxDist * 0.5f)
                {
                    // Smooth gradient
                    float t = (dist - maxDist * 0.5f) / (maxDist * 0.5f);
                    alpha = Mathf.Clamp01(t * t); // Quadratic ease-in
                }
                
                pixels[y * size + x] = new Color(0f, 0f, 0.05f, alpha); // Hafif mavi-siyah
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    
    private IEnumerator FadeVignette(float targetAlpha, float duration)
    {
        if (vignetteImage == null) yield break;
        
        float startAlpha = currentVignetteAlpha;
        float time = 0f;
        
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / duration;
            t = t * t * (3f - 2f * t); // Smoothstep
            
            currentVignetteAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            vignetteImage.color = new Color(0f, 0f, 0.05f, currentVignetteAlpha);
            
            yield return null;
        }
        
        currentVignetteAlpha = targetAlpha;
        vignetteImage.color = new Color(0f, 0f, 0.05f, currentVignetteAlpha);
    }

    private void UpdateUI()
    {
        if (cooldownFillImage != null)
        {
            cooldownFillImage.fillAmount = CooldownProgress;
            
            // Hazır değilse gri yap
            cooldownFillImage.color = IsReady ? Color.white : Color.gray;
        }

        if (abilityIconImage != null)
        {
            // Aktifken parlat
            if (isSlowMotionActive)
                abilityIconImage.color = Color.cyan;
            else if (IsReady)
                abilityIconImage.color = Color.white;
            else
                abilityIconImage.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        }
    }

    private void OnDestroy()
    {
        // Oyun bittiğinde zaman normal kalsın
        Time.timeScale = 1f;
        Time.fixedDeltaTime = originalFixedDeltaTime;
    }

    private void OnDisable()
    {
        // Disable olduğunda da normal zaman
        if (isSlowMotionActive)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
            isSlowMotionActive = false;
        }
    }

}
