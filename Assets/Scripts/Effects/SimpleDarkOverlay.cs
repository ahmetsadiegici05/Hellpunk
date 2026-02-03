using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Basit karanlık overlay efekti - Hiçbir şey bağlamana gerek yok!
/// F10 ile test et.
/// Sahne değişikliğinde otomatik sıfırlanır.
/// </summary>
public class SimpleDarkOverlay : MonoBehaviour
{
    public static SimpleDarkOverlay Instance { get; private set; }
    
    [Header("Görüş Ayarları")]
    [SerializeField] private float visionWidth = 300f;  // Yatay görüş (piksel)
    [SerializeField] private float visionHeight = 200f; // Dikey görüş (piksel)
    [Range(0.5f, 1f)]
    [SerializeField] private float darkIntensity = 0.95f; // Karanlık yoğunluğu
    
    [Header("Karanlık Rengi")]
    [SerializeField] private Color darkColor = new Color(0.02f, 0.01f, 0.03f, 1f);
    
    [Header("Geçiş Hızı")]
    [SerializeField] private float fadeSpeed = 3f;
    
    [Header("Canvas Ayarları")]
    [SerializeField] private int sortingOrder = 5; // Çok düşük - tüm UI'ların altında
    
    // UI
    private Canvas canvas;
    private RawImage overlay;
    private Texture2D maskTexture;
    
    // State
    private bool isActive = false;
    private float currentAlpha = 0f;
    private float targetAlpha = 0f;
    private Transform playerTransform;
    private Camera mainCamera;
    
    // Texture
    private int texSize = 256;
    private Color[] pixels;
    
    // Public properties
    public bool IsActive => isActive;
    public float VisionWidth => visionWidth;
    public float VisionHeight => visionHeight;
    public float DarkIntensity => darkIntensity;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateOverlay();
            
            // Sahne değişikliğini dinle
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log("[SimpleDarkOverlay] Hazır! F10 ile test et.");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        // Event'i temizle
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Texture'ı temizle
        if (maskTexture != null)
            Destroy(maskTexture);
        
        if (Instance == this)
            Instance = null;
    }
    
    /// <summary>
    /// Sahne yüklendiğinde efekti sıfırla
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Efekti hemen kapat
        ForceDeactivate();
        
        // Player referansını temizle (yeni sahnede yeniden bulunacak)
        playerTransform = null;
        mainCamera = null;
        
        Debug.Log($"[SimpleDarkOverlay] Sahne değişti: {scene.name} - Efekt sıfırlandı");
    }
    
    /// <summary>
    /// Efekti anında kapat (fade olmadan)
    /// </summary>
    public void ForceDeactivate()
    {
        isActive = false;
        targetAlpha = 0f;
        currentAlpha = 0f;
        
        if (overlay != null)
            overlay.color = new Color(1, 1, 1, 0);
    }
    
    private void CreateOverlay()
    {
        // Canvas oluştur
        GameObject canvasObj = new GameObject("DarkOverlayCanvas");
        canvasObj.transform.SetParent(transform);
        
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder; // UI'ların ALTINDA (UI genelde 100+)
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Overlay image
        GameObject overlayObj = new GameObject("DarkOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        
        overlay = overlayObj.AddComponent<RawImage>();
        overlay.raycastTarget = false;
        
        // Full screen
        RectTransform rt = overlay.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // Texture oluştur
        CreateMaskTexture();
        overlay.texture = maskTexture;
        overlay.color = new Color(1, 1, 1, 0); // Başlangıçta görünmez
    }
    
    private void CreateMaskTexture()
    {
        maskTexture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
        maskTexture.filterMode = FilterMode.Bilinear;
        maskTexture.wrapMode = TextureWrapMode.Clamp;
        
        pixels = new Color[texSize * texSize];
        
        // Başlangıç: tam karanlık
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = darkColor;
        
        maskTexture.SetPixels(pixels);
        maskTexture.Apply();
    }
    
    private void Update()
    {
        // F10 ile test
        if (Input.GetKeyDown(KeyCode.F10))
        {
            if (isActive)
                Deactivate();
            else
                Activate();
            
            Debug.Log($"[SimpleDarkOverlay] F10 - Aktif: {isActive}");
        }
        
        // Alpha geçişi
        if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
            if (overlay != null)
                overlay.color = new Color(1, 1, 1, currentAlpha);
        }
    }
    
    private void LateUpdate()
    {
        if (!isActive || currentAlpha < 0.01f) return;
        
        // Kamera ve player bul
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (playerTransform == null)
            FindPlayer();
        
        if (mainCamera == null || playerTransform == null) return;
        
        // Oyuncunun ekran pozisyonunu hesapla
        Vector3 screenPos = mainCamera.WorldToScreenPoint(playerTransform.position);
        float normalizedX = screenPos.x / Screen.width;
        float normalizedY = screenPos.y / Screen.height;
        
        // Texture'ı güncelle
        UpdateMask(normalizedX, normalizedY);
    }
    
    private void FindPlayer()
    {
        if (PlayerMovement.Instance != null)
        {
            playerTransform = PlayerMovement.Instance.transform;
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }
    }
    
    private void UpdateMask(float playerX, float playerY)
    {
        // Oyuncu pozisyonunu texture koordinatına çevir
        float centerX = playerX * texSize;
        float centerY = playerY * texSize;
        
        // Görüş yarıçaplarını hesapla (ekran boyutuna göre normalize) - Elips için ayrı width/height
        float radiusX = (visionWidth / Screen.width) * texSize;
        float radiusY = (visionHeight / Screen.height) * texSize;
        float softEdge = Mathf.Min(radiusX, radiusY) * 0.4f;
        
        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dx = x - centerX;
                float dy = y - centerY;
                
                // Elips mesafesi hesapla: (dx/rx)^2 + (dy/ry)^2 <= 1 elips içinde
                float normalizedDist = Mathf.Sqrt((dx * dx) / (radiusX * radiusX) + (dy * dy) / (radiusY * radiusY));
                
                float alpha;
                float innerEdge = 1f - (softEdge / Mathf.Min(radiusX, radiusY));
                float outerEdge = 1f + (softEdge / Mathf.Min(radiusX, radiusY));
                
                if (normalizedDist < innerEdge)
                {
                    alpha = 0f; // Görünür alan (şeffaf)
                }
                else if (normalizedDist > outerEdge)
                {
                    alpha = darkIntensity; // Karanlık alan - yoğunluk kullan
                }
                else
                {
                    // Yumuşak geçiş
                    float t = (normalizedDist - innerEdge) / (outerEdge - innerEdge);
                    alpha = Mathf.SmoothStep(0f, darkIntensity, t);
                }
                
                pixels[y * texSize + x] = new Color(darkColor.r, darkColor.g, darkColor.b, alpha);
            }
        }
        
        maskTexture.SetPixels(pixels);
        maskTexture.Apply();
    }
    
    /// <summary>
    /// Karanlık efektini aç
    /// </summary>
    public void Activate()
    {
        isActive = true;
        targetAlpha = 1f;
        Debug.Log("[SimpleDarkOverlay] AÇILDI!");
    }
    
    /// <summary>
    /// Karanlık efektini kapat
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        targetAlpha = 0f;
        Debug.Log("[SimpleDarkOverlay] KAPANDI!");
    }
    
    /// <summary>
    /// Görüş boyutlarını ayarla (piksel cinsinden)
    /// </summary>
    public void SetVisionSize(float width, float height)
    {
        visionWidth = width;
        visionHeight = height;
    }
    
    /// <summary>
    /// Görüş genişliğini ayarla (piksel cinsinden)
    /// </summary>
    public void SetVisionWidth(float width)
    {
        visionWidth = width;
    }
    
    /// <summary>
    /// Görüş yüksekliğini ayarla (piksel cinsinden)
    /// </summary>
    public void SetVisionHeight(float height)
    {
        visionHeight = height;
    }
    
    /// <summary>
    /// Karanlık yoğunluğunu ayarla (0-1 arası)
    /// </summary>
    public void SetDarkIntensity(float intensity)
    {
        darkIntensity = Mathf.Clamp01(intensity);
    }
    
    /// <summary>
    /// Karanlık rengini ayarla
    /// </summary>
    public void SetDarkColor(Color color)
    {
        darkColor = color;
    }
}
