using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Dark Forest görüş kısıtı efekti.
/// Tüm ekran karanlık, oyuncunun etrafında daire şeklinde görüş alanı.
/// </summary>
public class DarkForestEffect : MonoBehaviour
{
    public static DarkForestEffect Instance { get; private set; }
    
    [Header("Efekt Ayarları")]
    [SerializeField] private float visionRadius = 300f;
    [SerializeField] private Color fogColor = new Color(0.01f, 0.005f, 0.02f, 0.97f);
    
    [Header("Geçiş")]
    [SerializeField] private float transitionDuration = 0.8f;
    
    // UI
    private Canvas fogCanvas;
    private CanvasGroup canvasGroup;
    private RawImage fullScreenFog;
    private Texture2D fogTexture;
    
    // State
    private bool isActive = false;
    private float currentAlpha = 0f;
    private Coroutine transitionCoroutine;
    private Transform playerTransform;
    private Camera mainCamera;
    
    // Cache
    private Vector2 lastPlayerScreenPos;
    private int texSize = 128; // Düşük çözünürlük = performans
    private bool uiCreated = false;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[DarkForest] Instance oluşturuldu!");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        if (uiCreated) return;
        
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("[DarkForest] Camera.main bulunamadı!");
            return;
        }
        
        CreateUI();
        SetVisibility(false);
        uiCreated = true;
        Debug.Log("[DarkForest] UI oluşturuldu! F10 ile test et.");
    }
    
    private void Update()
    {
        // UI henüz oluşturulmadıysa oluştur
        if (!uiCreated)
        {
            InitializeUI();
        }
        
        // DEBUG: F10 ile karanlık modu aç/kapat
        if (Input.GetKeyDown(KeyCode.F10))
        {
            if (!uiCreated) InitializeUI();
            
            if (isActive)
                DeactivateEffect();
            else
                ActivateEffect();
            Debug.Log($"[DarkForest] F10 - Aktif: {!isActive}");
        }
    }
    
    private void LateUpdate()
    {
        if (!isActive || mainCamera == null) return;
        
        if (playerTransform == null)
            FindPlayer();
        
        if (playerTransform == null) return;
        
        // Oyuncu ekran pozisyonu
        Vector3 screenPos = mainCamera.WorldToScreenPoint(playerTransform.position);
        Vector2 normalizedPos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
        
        // Pozisyon değiştiyse texture güncelle
        if (Vector2.Distance(normalizedPos, lastPlayerScreenPos) > 0.005f)
        {
            lastPlayerScreenPos = normalizedPos;
            UpdateFogTexture(normalizedPos);
        }
    }
    
    private void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("DarkForestCanvas");
        canvasObj.transform.SetParent(transform);
        
        fogCanvas = canvasObj.AddComponent<Canvas>();
        fogCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fogCanvas.sortingOrder = 40;
        
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Full screen fog image
        GameObject fogObj = new GameObject("FogOverlay");
        fogObj.transform.SetParent(canvasObj.transform, false);
        
        fullScreenFog = fogObj.AddComponent<RawImage>();
        fullScreenFog.raycastTarget = false;
        
        RectTransform rect = fullScreenFog.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Fog texture oluştur
        CreateFogTexture();
        fullScreenFog.texture = fogTexture;
    }
    
    private void CreateFogTexture()
    {
        fogTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        
        // Başlangıçta tamamen karanlık
        Color[] pixels = new Color[texSize * texSize];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = fogColor;
        
        fogTexture.SetPixels(pixels);
        fogTexture.Apply();
    }
    
    private void UpdateFogTexture(Vector2 playerNormalizedPos)
    {
        if (fogTexture == null) return;
        
        // Oyuncu pozisyonunu texture koordinatına çevir
        Vector2 playerTexPos = new Vector2(
            playerNormalizedPos.x * texSize,
            playerNormalizedPos.y * texSize
        );
        
        // Görüş yarıçapını texture boyutuna göre ölçekle
        float scaledRadius = (visionRadius / Screen.height) * texSize;
        float softEdge = scaledRadius * 0.4f;
        
        Color[] pixels = new Color[texSize * texSize];
        
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), playerTexPos);
                
                float alpha;
                if (dist < scaledRadius - softEdge)
                {
                    alpha = 0f; // Görünür alan
                }
                else if (dist > scaledRadius + softEdge)
                {
                    alpha = 1f; // Karanlık alan
                }
                else
                {
                    // Yumuşak geçiş
                    float t = (dist - (scaledRadius - softEdge)) / (softEdge * 2f);
                    alpha = Mathf.SmoothStep(0f, 1f, t);
                }
                
                pixels[y * texSize + x] = new Color(fogColor.r, fogColor.g, fogColor.b, alpha * fogColor.a);
            }
        }
        
        fogTexture.SetPixels(pixels);
        fogTexture.Apply();
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
    
    // ========== PUBLIC API ==========
    
    public void ActivateEffect()
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        
        // UI yoksa oluştur
        if (fogCanvas == null)
        {
            Debug.Log("[DarkForest] UI oluşturuluyor...");
            CreateUI();
        }
        
        // Önce player'ı bul
        if (playerTransform == null)
            FindPlayer();
        
        // Kamerayı kontrol et
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        isActive = true;
        SetVisibility(true);
        
        // İlk texture güncellemesi
        if (playerTransform != null && mainCamera != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(playerTransform.position);
            Vector2 normalizedPos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
            lastPlayerScreenPos = normalizedPos;
            UpdateFogTexture(normalizedPos);
        }
        else
        {
            // Player bulunamadıysa merkeze varsayılan fog
            Debug.LogWarning("[DarkForest] Player veya kamera bulunamadı, merkeze fog uygulanıyor");
            UpdateFogTexture(new Vector2(0.5f, 0.5f));
        }
        
        // Radar sistemini başlat (varsa)
        EnsureRadarSystem();
        
        transitionCoroutine = StartCoroutine(FadeIn());
        Debug.Log("[DarkForest] Efekt AKTİF!");
    }
    
    public void DeactivateEffect()
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        
        transitionCoroutine = StartCoroutine(FadeOut());
        Debug.Log("[DarkForest] Efekt DEAKTİF!");
    }
    
    public void SetEffectImmediate(bool active)
    {
        if (transitionCoroutine != null)
            StopCoroutine(transitionCoroutine);
        
        isActive = active;
        currentAlpha = active ? 1f : 0f;
        if (canvasGroup != null)
            canvasGroup.alpha = currentAlpha;
        SetVisibility(active);
        
        // Radar sistemini başlat/kapat
        if (active)
            EnsureRadarSystem();
    }
    
    /// <summary>
    /// Radar sisteminin var olduğundan emin ol
    /// </summary>
    private void EnsureRadarSystem()
    {
        if (EnemyRadarSystem.Instance == null)
        {
            GameObject radarObj = new GameObject("EnemyRadarSystem");
            radarObj.AddComponent<EnemyRadarSystem>();
            Debug.Log("[DarkForest] Radar sistemi oluşturuldu!");
        }
    }
    
    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        float startAlpha = currentAlpha;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentAlpha = Mathf.Lerp(startAlpha, 1f, elapsed / transitionDuration);
            if (canvasGroup != null)
                canvasGroup.alpha = currentAlpha;
            yield return null;
        }
        
        currentAlpha = 1f;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }
    
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float startAlpha = currentAlpha;
        
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentAlpha = Mathf.Lerp(startAlpha, 0f, elapsed / transitionDuration);
            if (canvasGroup != null)
                canvasGroup.alpha = currentAlpha;
            yield return null;
        }
        
        currentAlpha = 0f;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        
        isActive = false;
        SetVisibility(false);
    }
    
    private void SetVisibility(bool visible)
    {
        if (fogCanvas != null)
            fogCanvas.gameObject.SetActive(visible);
    }
    
    public void SetVisionRadius(float radius) => visionRadius = radius;
    public void SetFogColor(Color color) => fogColor = color;
    
    public bool IsActive => isActive;
    public float CurrentIntensity => currentAlpha;
    public float VisionRadius => visionRadius;
    
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        if (fogTexture != null)
            Destroy(fogTexture);
    }
}
