using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Karanlık bölgelerde düşmanları tespit eden radar sistemi.
/// DarkForestEffect aktifken otomatik olarak açılır.
/// Oyuncunun etrafındaki düşmanları radar üzerinde gösterir.
/// Sahne değişikliğinde otomatik sıfırlanır.
/// </summary>
public class EnemyRadarSystem : MonoBehaviour
{
    public static EnemyRadarSystem Instance { get; private set; }

    [Header("Radar Settings")]
    [SerializeField] private bool enableRadar = true;
    [SerializeField] private float radarRange = 15f; // Düşman tespit mesafesi
    [SerializeField] private float updateInterval = 0.1f; // Güncelleme sıklığı
    [SerializeField] private Vector2 radarSize = new Vector2(180, 180);
    [SerializeField] private Vector2 radarOffset = new Vector2(-20, -20); // Ekran kenarından offset
    
    [Header("Visual Settings")]
    [SerializeField] private Color radarBackgroundColor = new Color(0.05f, 0.15f, 0.1f, 0.85f);
    [SerializeField] private Color radarBorderColor = new Color(0.2f, 0.8f, 0.4f, 1f);
    [SerializeField] private Color radarLineColor = new Color(0.1f, 0.5f, 0.3f, 0.5f);
    [SerializeField] private Color playerBlipColor = new Color(0.3f, 1f, 0.5f, 1f);
    [SerializeField] private Color enemyBlipColor = new Color(1f, 0.3f, 0.2f, 1f);
    [SerializeField] private Color bossBlipColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private float borderWidth = 3f;
    [SerializeField] private float blipSize = 8f;
    [SerializeField] private float playerBlipSize = 10f;
    
    [Header("Sweep Effect")]
    [SerializeField] private bool enableSweep = true;
    [SerializeField] private float sweepSpeed = 90f; // Derece/saniye
    [SerializeField] private Color sweepColor = new Color(0.3f, 1f, 0.5f, 0.3f);
    
    [Header("Ping Effect")]
    [SerializeField] private bool enablePing = true;
    [SerializeField] private float pingInterval = 2f;
    [SerializeField] private float pingDuration = 1f;
    [SerializeField] private AudioClip pingSound;
    [SerializeField] [Range(0f, 1f)] private float pingSoundVolume = 0.3f;
    
    [Header("Detection")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool showOnlyInDarkZone = true; // Sadece karanlık bölgede göster
    
    // Components
    private Canvas radarCanvas;
    private CanvasGroup canvasGroup;
    private RectTransform radarContainer;
    private Image radarBackground;
    private Image radarBorder;
    private Image sweepImage;
    private Image playerBlip;
    private List<Image> enemyBlips = new List<Image>();
    private AudioSource audioSource;
    
    // State
    private Transform playerTransform;
    private bool isActive = false;
    private float sweepAngle = 0f;
    private float pingTimer = 0f;
    private float updateTimer = 0f;
    private List<Transform> detectedEnemies = new List<Transform>();
    
    // Ping ring effect
    private List<Image> pingRings = new List<Image>();
    
    // Object pooling for blips
    private const int MAX_BLIPS = 20;
    private Queue<Image> blipPool = new Queue<Image>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Sahne değişikliğini dinle
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Instance == this)
            Instance = null;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Radarı kapat ve referansları temizle
        SetRadarActive(false);
        playerTransform = null;
        detectedEnemies.Clear();
        
        Debug.Log($"[EnemyRadarSystem] Sahne değişti: {scene.name} - Radar sıfırlandı");
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        CreateRadarUI();
        FindPlayer();
        
        // Başlangıçta gizle
        SetRadarActive(false);
    }

    private void Update()
    {
        if (!enableRadar) return;
        
        // Karanlık bölge kontrolü - SimpleDarkOverlay kullan
        if (showOnlyInDarkZone)
        {
            bool shouldShow = SimpleDarkOverlay.Instance != null && SimpleDarkOverlay.Instance.IsActive;
            if (shouldShow != isActive)
            {
                SetRadarActive(shouldShow);
            }
        }
        
        if (!isActive) return;
        
        // Player kontrolü
        if (playerTransform == null)
            FindPlayer();
        
        if (playerTransform == null) return;
        
        // Güncelleme
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0f)
        {
            updateTimer = updateInterval;
            DetectEnemies();
            UpdateBlips();
        }
        
        // Sweep efekti
        if (enableSweep)
        {
            sweepAngle += sweepSpeed * Time.deltaTime;
            if (sweepAngle >= 360f) sweepAngle -= 360f;
            
            if (sweepImage != null)
                sweepImage.rectTransform.localRotation = Quaternion.Euler(0, 0, -sweepAngle);
        }
        
        // Ping efekti
        if (enablePing)
        {
            pingTimer -= Time.deltaTime;
            if (pingTimer <= 0f)
            {
                pingTimer = pingInterval;
                TriggerPing();
            }
        }
    }

    private void CreateRadarUI()
    {
        // Canvas oluştur
        GameObject canvasObj = new GameObject("RadarCanvas");
        canvasObj.transform.SetParent(transform);
        
        radarCanvas = canvasObj.AddComponent<Canvas>();
        radarCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        radarCanvas.sortingOrder = 45; // DarkForest'in üstünde
        
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Radar container (üst orta)
        GameObject containerObj = new GameObject("RadarContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        radarContainer = containerObj.AddComponent<RectTransform>();
        
        // Üst ortaya yerleştir
        radarContainer.anchorMin = new Vector2(0.5f, 1);
        radarContainer.anchorMax = new Vector2(0.5f, 1);
        radarContainer.pivot = new Vector2(0.5f, 1);
        radarContainer.anchoredPosition = new Vector2(0, radarOffset.y); // X=0 ortalamak için
        radarContainer.sizeDelta = radarSize;
        
        // Background (daire şeklinde)
        CreateRadarBackground();
        
        // Grid çizgileri
        CreateRadarGrid();
        
        // Sweep efekti
        if (enableSweep)
            CreateSweepEffect();
        
        // Border
        CreateRadarBorder();
        
        // Player blip (ortada)
        CreatePlayerBlip();
        
        // Blip pool oluştur
        CreateBlipPool();
        
        // Ping ring pool
        CreatePingRings();
    }

    private void CreateRadarBackground()
    {
        GameObject bgObj = new GameObject("RadarBackground");
        bgObj.transform.SetParent(radarContainer, false);
        
        radarBackground = bgObj.AddComponent<Image>();
        radarBackground.color = radarBackgroundColor;
        radarBackground.raycastTarget = false;
        
        // Daire sprite kullan veya mask ekle
        radarBackground.sprite = CreateCircleSprite(64);
        radarBackground.type = Image.Type.Simple;
        
        RectTransform rect = radarBackground.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void CreateRadarGrid()
    {
        // Çapraz çizgiler
        for (int i = 0; i < 4; i++)
        {
            GameObject lineObj = new GameObject($"GridLine_{i}");
            lineObj.transform.SetParent(radarContainer, false);
            
            Image line = lineObj.AddComponent<Image>();
            line.color = radarLineColor;
            line.raycastTarget = false;
            
            RectTransform rect = line.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(radarSize.x * 0.9f, 1f);
            rect.localRotation = Quaternion.Euler(0, 0, i * 45f);
        }
        
        // Konsantrik daireler
        float[] ringRadii = { 0.33f, 0.66f };
        foreach (float radiusFactor in ringRadii)
        {
            GameObject ringObj = new GameObject($"GridRing_{radiusFactor}");
            ringObj.transform.SetParent(radarContainer, false);
            
            Image ring = ringObj.AddComponent<Image>();
            ring.sprite = CreateRingSprite(32, 0.9f);
            ring.color = radarLineColor;
            ring.raycastTarget = false;
            ring.type = Image.Type.Simple;
            ring.preserveAspect = true;
            
            RectTransform rect = ring.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = radarSize * radiusFactor;
        }
    }

    private void CreateSweepEffect()
    {
        GameObject sweepObj = new GameObject("SweepEffect");
        sweepObj.transform.SetParent(radarContainer, false);
        
        sweepImage = sweepObj.AddComponent<Image>();
        sweepImage.sprite = CreateSweepSprite(64);
        sweepImage.color = sweepColor;
        sweepImage.raycastTarget = false;
        sweepImage.type = Image.Type.Filled;
        sweepImage.fillMethod = Image.FillMethod.Radial360;
        sweepImage.fillAmount = 0.15f; // 54 derece
        sweepImage.fillOrigin = (int)Image.Origin360.Top;
        
        RectTransform rect = sweepImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(5, 5);
        rect.offsetMax = new Vector2(-5, -5);
    }

    private void CreateRadarBorder()
    {
        GameObject borderObj = new GameObject("RadarBorder");
        borderObj.transform.SetParent(radarContainer, false);
        
        radarBorder = borderObj.AddComponent<Image>();
        radarBorder.sprite = CreateRingSprite(64, 0.92f);
        radarBorder.color = radarBorderColor;
        radarBorder.raycastTarget = false;
        
        RectTransform rect = radarBorder.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Outer glow efekti için ikinci border
        GameObject glowObj = new GameObject("RadarGlow");
        glowObj.transform.SetParent(radarContainer, false);
        
        Image glow = glowObj.AddComponent<Image>();
        glow.sprite = CreateRingSprite(64, 0.85f);
        glow.color = new Color(radarBorderColor.r, radarBorderColor.g, radarBorderColor.b, 0.3f);
        glow.raycastTarget = false;
        
        RectTransform glowRect = glow.rectTransform;
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(-5, -5);
        glowRect.offsetMax = new Vector2(5, 5);
    }

    private void CreatePlayerBlip()
    {
        GameObject blipObj = new GameObject("PlayerBlip");
        blipObj.transform.SetParent(radarContainer, false);
        
        playerBlip = blipObj.AddComponent<Image>();
        playerBlip.sprite = CreateTriangleSprite(16);
        playerBlip.color = playerBlipColor;
        playerBlip.raycastTarget = false;
        
        RectTransform rect = playerBlip.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(playerBlipSize, playerBlipSize);
        rect.anchoredPosition = Vector2.zero;
    }

    private void CreateBlipPool()
    {
        for (int i = 0; i < MAX_BLIPS; i++)
        {
            GameObject blipObj = new GameObject($"EnemyBlip_{i}");
            blipObj.transform.SetParent(radarContainer, false);
            
            Image blip = blipObj.AddComponent<Image>();
            blip.sprite = CreateCircleSprite(16);
            blip.color = enemyBlipColor;
            blip.raycastTarget = false;
            
            RectTransform rect = blip.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(blipSize, blipSize);
            
            blipObj.SetActive(false);
            blipPool.Enqueue(blip);
        }
    }

    private void CreatePingRings()
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject ringObj = new GameObject($"PingRing_{i}");
            ringObj.transform.SetParent(radarContainer, false);
            
            Image ring = ringObj.AddComponent<Image>();
            ring.sprite = CreateRingSprite(64, 0.9f);
            ring.color = new Color(radarBorderColor.r, radarBorderColor.g, radarBorderColor.b, 0f);
            ring.raycastTarget = false;
            
            RectTransform rect = ring.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero;
            
            pingRings.Add(ring);
        }
    }

    private void FindPlayer()
    {
        if (PlayerMovement.Instance != null)
            playerTransform = PlayerMovement.Instance.transform;
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }

    private void DetectEnemies()
    {
        detectedEnemies.Clear();
        
        if (playerTransform == null) return;
        
        // Etraftaki düşmanları bul
        Collider2D[] hits = Physics2D.OverlapCircleAll(playerTransform.position, radarRange, enemyLayer);
        
        foreach (var hit in hits)
        {
            if (hit.transform != playerTransform)
            {
                detectedEnemies.Add(hit.transform);
            }
        }
    }

    private void UpdateBlips()
    {
        // Tüm blip'leri gizle
        foreach (var blip in enemyBlips)
        {
            blip.gameObject.SetActive(false);
            blipPool.Enqueue(blip);
        }
        enemyBlips.Clear();
        
        // Player yönünü güncelle
        if (playerBlip != null && playerTransform != null)
        {
            // Player'ın baktığı yönü al
            float playerAngle = GetPlayerFacingAngle();
            playerBlip.rectTransform.localRotation = Quaternion.Euler(0, 0, -playerAngle);
        }
        
        // Tespit edilen düşmanlar için blip göster
        foreach (var enemy in detectedEnemies)
        {
            if (enemy == null) continue;
            if (blipPool.Count == 0) break;
            
            Image blip = blipPool.Dequeue();
            enemyBlips.Add(blip);
            
            // Pozisyon hesapla
            Vector2 relativePos = enemy.position - playerTransform.position;
            float distance = relativePos.magnitude;
            float normalizedDist = distance / radarRange;
            
            // Radar üzerindeki pozisyon
            Vector2 radarPos = relativePos.normalized * (radarSize.x * 0.45f * normalizedDist);
            
            blip.rectTransform.anchoredPosition = new Vector2(radarPos.x, radarPos.y);
            
            // Boss mu kontrol et (BossEffects veya isimde "boss" varsa)
            bool isBoss = enemy.GetComponent<BossEffects>() != null || 
                          enemy.name.ToLower().Contains("boss");
            
            blip.color = isBoss ? bossBlipColor : enemyBlipColor;
            blip.rectTransform.sizeDelta = new Vector2(
                isBoss ? blipSize * 1.5f : blipSize,
                isBoss ? blipSize * 1.5f : blipSize
            );
            
            // Pulse efekti (yakındaki düşmanlar için)
            if (normalizedDist < 0.3f)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.2f;
                blip.rectTransform.localScale = Vector3.one * pulse;
            }
            else
            {
                blip.rectTransform.localScale = Vector3.one;
            }
            
            blip.gameObject.SetActive(true);
        }
    }

    private float GetPlayerFacingAngle()
    {
        // PlayerMovement'tan yönü almaya çalış
        if (PlayerMovement.Instance != null)
        {
            Vector2 facing = PlayerMovement.Instance.LastMoveDirection;
            if (facing != Vector2.zero)
            {
                return Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - 90f;
            }
        }
        
        // SpriteRenderer'dan flip kontrolü
        SpriteRenderer sr = playerTransform?.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            return sr.flipX ? 90f : -90f;
        }
        
        return 0f;
    }

    private void TriggerPing()
    {
        // Ses çal
        if (pingSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pingSound, pingSoundVolume);
        }
        
        // Ping ring animasyonu
        StartCoroutine(PingRingAnimation());
    }

    private IEnumerator PingRingAnimation()
    {
        foreach (var ring in pingRings)
        {
            StartCoroutine(AnimatePingRing(ring));
            yield return new WaitForSeconds(0.15f);
        }
    }

    private IEnumerator AnimatePingRing(Image ring)
    {
        float elapsed = 0f;
        Color startColor = new Color(radarBorderColor.r, radarBorderColor.g, radarBorderColor.b, 0.5f);
        
        while (elapsed < pingDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pingDuration;
            
            // Büyüme
            float size = Mathf.Lerp(20f, radarSize.x, t);
            ring.rectTransform.sizeDelta = new Vector2(size, size);
            
            // Fade out
            float alpha = Mathf.Lerp(0.5f, 0f, t);
            ring.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            yield return null;
        }
        
        ring.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        ring.rectTransform.sizeDelta = Vector2.zero;
    }

    // ========== SPRITE OLUŞTURMA ==========

    private Sprite CreateCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    float alpha = Mathf.Clamp01(1f - (dist - radius + 2f) / 2f);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateRingSprite(int size, float innerRadius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerRadius = size / 2f - 1f;
        float inner = outerRadius * innerRadius;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= outerRadius && dist >= inner)
                {
                    float alpha = 1f;
                    // Dış kenar yumuşatma
                    if (dist > outerRadius - 1f)
                        alpha = Mathf.Clamp01(outerRadius - dist + 1f);
                    // İç kenar yumuşatma
                    else if (dist < inner + 1f)
                        alpha = Mathf.Clamp01(dist - inner + 1f);
                    
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateTriangleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        
        // Üçgen noktaları (yukarı bakan)
        Vector2 top = new Vector2(size / 2f, size - 2f);
        Vector2 bottomLeft = new Vector2(2f, 2f);
        Vector2 bottomRight = new Vector2(size - 2f, 2f);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                if (PointInTriangle(p, top, bottomLeft, bottomRight))
                {
                    pixels[y * size + x] = Color.white;
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateSweepSprite(int size)
    {
        // Basit daire sprite (filled image olarak kullanılacak)
        return CreateCircleSprite(size);
    }

    private bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        
        return !(hasNeg && hasPos);
    }

    private float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    // ========== PUBLIC API ==========

    public void SetRadarActive(bool active)
    {
        if (isActive == active) return;
        
        isActive = active;
        
        if (active)
        {
            StartCoroutine(FadeRadar(1f, 0.3f));
            pingTimer = 0.5f; // Hemen ping at
        }
        else
        {
            StartCoroutine(FadeRadar(0f, 0.3f));
        }
        
        Debug.Log($"[EnemyRadar] Radar {(active ? "AKTİF" : "KAPALI")}");
    }

    private IEnumerator FadeRadar(float targetAlpha, float duration)
    {
        if (canvasGroup == null) yield break;
        
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
    }

    /// <summary>
    /// Radar menzilini ayarla
    /// </summary>
    public void SetRadarRange(float range)
    {
        radarRange = range;
    }

    /// <summary>
    /// Manuel olarak radarı aç/kapat (karanlık bölge kontrolü bypass)
    /// </summary>
    public void ForceSetActive(bool active)
    {
        showOnlyInDarkZone = false;
        SetRadarActive(active);
    }

    /// <summary>
    /// Radar aktif mi?
    /// </summary>
    public bool IsRadarActive => isActive;
    
    /// <summary>
    /// Tespit edilen düşman sayısı
    /// </summary>
    public int DetectedEnemyCount => detectedEnemies.Count;

    private void OnDrawGizmosSelected()
    {
        if (playerTransform != null)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 0.4f, 0.3f);
            Gizmos.DrawWireSphere(playerTransform.position, radarRange);
        }
    }
}
