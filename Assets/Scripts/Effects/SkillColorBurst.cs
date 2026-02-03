using UnityEngine;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Skill kullanıldığında ekranda gelişmiş renk patlaması efekti
/// Vignette + Radial burst + Parçacık efektleri
/// </summary>
public class SkillColorBurst : MonoBehaviour
{
    public static SkillColorBurst Instance { get; private set; }
    
    [Header("Renk Ayarları")]
    [SerializeField] private Color healColor = new Color(0.1f, 1f, 0.5f, 1f);          // Parlak yeşil
    [SerializeField] private Color fireballColor = new Color(1f, 0.4f, 0.1f, 1f);      // Ateş turuncusu
    [SerializeField] private Color timeSlowColor = new Color(0.4f, 0.6f, 1f, 1f);      // Buz mavisi
    [SerializeField] private Color ultimateColor = new Color(1f, 0.1f, 0.3f, 1f);      // Koyu kırmızı
    [SerializeField] private Color shockwaveColor = new Color(1f, 0.9f, 0.2f, 1f);     // Elektrik sarısı
    
    [Header("Efekt Ayarları")]
    [SerializeField] private float burstDuration = 0.5f;
    [SerializeField] private float vignetteIntensity = 0.6f;
    [SerializeField] private float pulseCount = 2f; // Nabız sayısı
    [SerializeField] private float innerRingSize = 0.3f;
    [SerializeField] private float outerRingSize = 0.8f;
    
    [Header("Parçacık Ayarları")]
    [SerializeField] private int particleCount = 12;
    [SerializeField] private float particleSpeed = 800f;
    [SerializeField] private float particleSize = 8f;
    
    private Canvas burstCanvas;
    private RawImage vignetteImage;
    private RawImage ringImage;
    private Image[] particles;
    private RectTransform canvasRect;
    private Coroutine currentBurst;
    
    // Shader özellikleri
    private Material vignetteMaterial;
    private Material ringMaterial;
    
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
        CreateBurstUI();
    }
    
    private void CreateBurstUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("SkillColorBurstCanvas");
        canvasObj.transform.SetParent(transform);
        
        burstCanvas = canvasObj.AddComponent<Canvas>();
        burstCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        burstCanvas.sortingOrder = 80;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasRect = canvasObj.GetComponent<RectTransform>();
        
        // Vignette efekti (kenarlarda yoğun, ortada şeffaf)
        CreateVignetteImage(canvasObj.transform);
        
        // Ring efekti (genişleyen halka)
        CreateRingImage(canvasObj.transform);
        
        // Parçacıklar
        CreateParticles(canvasObj.transform);
    }
    
    private void CreateVignetteImage(Transform parent)
    {
        GameObject vignetteObj = new GameObject("Vignette");
        vignetteObj.transform.SetParent(parent, false);
        
        vignetteImage = vignetteObj.AddComponent<RawImage>();
        vignetteImage.raycastTarget = false;
        
        // Vignette texture oluştur
        Texture2D vignetteTex = CreateVignetteTexture(256);
        vignetteImage.texture = vignetteTex;
        vignetteImage.color = Color.clear;
        
        RectTransform rect = vignetteObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-100, -100); // Biraz taşsın
        rect.offsetMax = new Vector2(100, 100);
    }
    
    private void CreateRingImage(Transform parent)
    {
        GameObject ringObj = new GameObject("Ring");
        ringObj.transform.SetParent(parent, false);
        
        ringImage = ringObj.AddComponent<RawImage>();
        ringImage.raycastTarget = false;
        
        // Ring texture oluştur
        Texture2D ringTex = CreateRingTexture(256);
        ringImage.texture = ringTex;
        ringImage.color = Color.clear;
        
        RectTransform rect = ringObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(800, 800);
    }
    
    private void CreateParticles(Transform parent)
    {
        particles = new Image[particleCount];
        
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particleObj = new GameObject($"Particle_{i}");
            particleObj.transform.SetParent(parent, false);
            
            Image particle = particleObj.AddComponent<Image>();
            particle.raycastTarget = false;
            particle.color = Color.clear;
            
            // Soft circle texture
            Texture2D particleTex = CreateSoftCircleTexture(32);
            particle.sprite = Sprite.Create(particleTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            
            RectTransform rect = particleObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(particleSize, particleSize);
            
            particles[i] = particle;
        }
    }
    
    private Texture2D CreateVignetteTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                // Kenarlar dolu, orta şeffaf (vignette)
                float alpha = Mathf.Pow(dist, 2f); // Quadratic falloff
                alpha = Mathf.Clamp01(alpha);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }
    
    private Texture2D CreateRingTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                // İnce halka - 0.7-0.9 arasında görünür
                float ringWidth = 0.15f;
                float ringCenter = 0.8f;
                float alpha = 1f - Mathf.Abs(dist - ringCenter) / ringWidth;
                alpha = Mathf.Clamp01(alpha);
                alpha = Mathf.Pow(alpha, 0.5f); // Soft edges
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        return tex;
    }
    
    private Texture2D CreateSoftCircleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float alpha = 1f - Mathf.Pow(dist, 1.5f);
                alpha = Mathf.Clamp01(alpha);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        tex.Apply();
        return tex;
    }
    
    /// <summary>
    /// Skill tipine göre gelişmiş renk patlaması
    /// </summary>
    public void TriggerBurst(GuitarSkillSystem.SkillType skillType)
    {
        Color burstColor = GetColorForSkill(skillType);
        
        if (currentBurst != null)
            StopCoroutine(currentBurst);
            
        currentBurst = StartCoroutine(AdvancedBurstCoroutine(burstColor, skillType));
    }
    
    /// <summary>
    /// Skill tipine göre renk patlaması + kamera sarsıntısı
    /// </summary>
    public void TriggerBurstWithShake(GuitarSkillSystem.SkillType skillType, float shakeIntensity = 0.1f)
    {
        TriggerBurst(skillType);
        
        if (ScreenShake.Instance != null)
        {
            if (shakeIntensity < 0.1f)
                ScreenShake.Instance.ShakeLight();
            else if (shakeIntensity < 0.2f)
                ScreenShake.Instance.ShakeMedium();
            else
                ScreenShake.Instance.ShakeHeavy();
        }
    }
    
    /// <summary>
    /// Özel renk ile patlama
    /// </summary>
    public void TriggerCustomBurst(Color color, float duration = -1f)
    {
        if (currentBurst != null)
            StopCoroutine(currentBurst);
            
        float dur = duration > 0 ? duration : burstDuration;
        currentBurst = StartCoroutine(AdvancedBurstCoroutine(color, GuitarSkillSystem.SkillType.None, dur));
    }
    
    private Color GetColorForSkill(GuitarSkillSystem.SkillType skillType)
    {
        return skillType switch
        {
            GuitarSkillSystem.SkillType.Heal => healColor,
            GuitarSkillSystem.SkillType.Fireball => fireballColor,
            GuitarSkillSystem.SkillType.TimeSlow => timeSlowColor,
            GuitarSkillSystem.SkillType.Ultimate => ultimateColor,
            GuitarSkillSystem.SkillType.Shockwave => shockwaveColor,
            _ => Color.white
        };
    }
    
    private IEnumerator AdvancedBurstCoroutine(Color color, GuitarSkillSystem.SkillType skillType, float duration = -1f)
    {
        float dur = duration > 0 ? duration : burstDuration;
        
        // Parçacıkları başlat
        StartCoroutine(AnimateParticles(color, dur));
        
        // Ring animasyonunu başlat
        StartCoroutine(AnimateRing(color, dur));
        
        // Vignette animasyonu
        float elapsed = 0f;
        
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / dur;
            
            // Pulse efekti - birden fazla nabız
            float pulse = Mathf.Sin(t * Mathf.PI * pulseCount) * (1f - t);
            pulse = Mathf.Max(0, pulse);
            
            // Vignette alpha
            float vignetteAlpha = pulse * vignetteIntensity;
            
            if (vignetteImage != null)
            {
                vignetteImage.color = new Color(color.r, color.g, color.b, vignetteAlpha);
            }
            
            yield return null;
        }
        
        // Temizle
        if (vignetteImage != null)
            vignetteImage.color = Color.clear;
            
        currentBurst = null;
    }
    
    private IEnumerator AnimateRing(Color color, float duration)
    {
        if (ringImage == null) yield break;
        
        RectTransform ringRect = ringImage.GetComponent<RectTransform>();
        float startSize = 100f;
        float endSize = 2000f;
        
        float elapsed = 0f;
        
        while (elapsed < duration * 0.8f) // Ring daha hızlı genişler
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (duration * 0.8f);
            
            // Ease out
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            
            // Boyut
            float size = Mathf.Lerp(startSize, endSize, smoothT);
            ringRect.sizeDelta = new Vector2(size, size);
            
            // Alpha - başta parlak, sonra söner
            float alpha = (1f - t) * 0.7f;
            ringImage.color = new Color(color.r, color.g, color.b, alpha);
            
            yield return null;
        }
        
        ringImage.color = Color.clear;
        ringRect.sizeDelta = new Vector2(800, 800);
    }
    
    private IEnumerator AnimateParticles(Color color, float duration)
    {
        if (particles == null) yield break;
        
        // Parçacık yönlerini ve hızlarını belirle
        Vector2[] directions = new Vector2[particleCount];
        float[] speeds = new float[particleCount];
        Vector2[] startPositions = new Vector2[particleCount];
        
        for (int i = 0; i < particleCount; i++)
        {
            float angle = (360f / particleCount) * i + Random.Range(-15f, 15f);
            directions[i] = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );
            speeds[i] = particleSpeed * Random.Range(0.7f, 1.3f);
            startPositions[i] = directions[i] * 50f; // Merkezden biraz uzakta başla
            
            // Başlangıç pozisyonu
            particles[i].rectTransform.anchoredPosition = startPositions[i];
            particles[i].rectTransform.sizeDelta = new Vector2(particleSize, particleSize) * Random.Range(0.8f, 1.5f);
        }
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
            for (int i = 0; i < particleCount; i++)
            {
                if (particles[i] == null) continue;
                
                // Pozisyon - hızlanarak dışa doğru
                float dist = speeds[i] * t * t; // Quadratic acceleration
                particles[i].rectTransform.anchoredPosition = startPositions[i] + directions[i] * dist;
                
                // Alpha - önce belir, sonra kaybol
                float alpha;
                if (t < 0.2f)
                    alpha = t / 0.2f; // Fade in
                else
                    alpha = 1f - ((t - 0.2f) / 0.8f); // Fade out
                    
                alpha = Mathf.Clamp01(alpha) * 0.9f;
                
                // Glow rengi (biraz daha parlak)
                Color glowColor = Color.Lerp(color, Color.white, 0.3f);
                particles[i].color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
                
                // Boyut küçülsün
                float scale = 1f - t * 0.5f;
                particles[i].rectTransform.localScale = Vector3.one * scale;
            }
            
            yield return null;
        }
        
        // Temizle
        foreach (var particle in particles)
        {
            if (particle != null)
            {
                particle.color = Color.clear;
                particle.rectTransform.anchoredPosition = Vector2.zero;
                particle.rectTransform.localScale = Vector3.one;
            }
        }
    }
}
