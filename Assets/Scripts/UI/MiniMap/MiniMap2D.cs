using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 2D Side-Scroller oyunlar için optimize edilmiş Mini-Map.
/// Render texture yerine UI tabanlı çalışır, daha performanslı.
/// Şık daire ikonları ve glow efektleri ile.
/// DontDestroyOnLoad ile tüm levellarda kalıcı.
/// </summary>
public class MiniMap2D : MonoBehaviour
{
    public static MiniMap2D Instance { get; private set; }

    [Header("Mini-Map Ayarları")]
    [SerializeField] private bool enableMiniMap = true;
    [SerializeField] private bool useExistingUI = true; // TRUE = Sahnedeki UI'ı kullan, FALSE = Kod ile oluştur
    
    [Header("Mevcut UI Referansları (Sahneden Ata)")]
    [Tooltip("Sahnedeki mini-map container RectTransform")]
    [SerializeField] private RectTransform existingContainer;
    [Tooltip("Oyuncu ikonu Image")]
    [SerializeField] private Image existingPlayerIcon;
    [Tooltip("Opsiyonel: Pozisyon text")]
    [SerializeField] private TextMeshProUGUI existingPositionText;
    
    [Header("Otomatik Oluşturma Ayarları (useExistingUI=false ise)")]
    [SerializeField] private Vector2 miniMapSize = new Vector2(320, 55);
    [SerializeField] private Vector2 miniMapOffset = new Vector2(0, 20);
    [SerializeField] private MiniMapPosition position = MiniMapPosition.BottomCenter;
    
    [Header("Görünüm Aralığı")]
    [SerializeField] private float viewRangeX = 60f;
    [SerializeField] private float viewRangeY = 15f;
    
    [Header("Görsel Ayarlar (Otomatik mod için)")]
    [SerializeField] private Color backgroundColor = new Color(0.12f, 0.05f, 0.18f, 0.9f); // Koyu mor
    [SerializeField] private Color borderColor = new Color(0.4f, 0.15f, 0.5f, 0.95f); // Koyu mor çerçeve
    [SerializeField] private float borderThickness = 2f;
    [SerializeField] private Color gridColor = new Color(0.3f, 0.1f, 0.4f, 0.2f);
    [SerializeField] private bool showGrid = false; // Grid varsayılan kapalı
    
    [Header("Oyuncu İkonu")]
    [SerializeField] private Color playerColor = new Color(1f, 0.5f, 0.15f, 1f); // Turuncu (skill butonları gibi)
    [SerializeField] private Color playerGlowColor = new Color(1f, 0.6f, 0.2f, 0.5f);
    [SerializeField] private float playerIconSize = 10f;
    [SerializeField] private float playerGlowSize = 18f;
    [SerializeField] private bool playerPulse = true;
    
    [Header("Düşman İkonları")]
    [SerializeField] private Color enemyColor = new Color(0.9f, 0.2f, 0.3f, 1f); // Kırmızı
    [SerializeField] private Color enemyGlowColor = new Color(0.8f, 0.15f, 0.25f, 0.4f);
    [SerializeField] private Color bossColor = new Color(1f, 0.3f, 0.1f, 1f);
    [SerializeField] private Color bossGlowColor = new Color(1f, 0.4f, 0.15f, 0.5f);
    [SerializeField] private float enemyIconSize = 6f;
    [SerializeField] private float enemyGlowSize = 12f;
    [SerializeField] private float bossIconSize = 10f;
    [SerializeField] private float bossGlowSize = 20f;
    [SerializeField] private float enemyDetectionRange = 70f;
    
    [Header("Önemli Noktalar")]
    [SerializeField] private Color checkpointColor = new Color(0.5f, 0.7f, 1f, 1f); // Açık mavi
    [SerializeField] private Color checkpointGlowColor = new Color(0.4f, 0.6f, 0.9f, 0.4f);
    [SerializeField] private Color exitColor = new Color(0.7f, 0.5f, 0.9f, 1f); // Mor
    [SerializeField] private Color exitGlowColor = new Color(0.6f, 0.4f, 0.8f, 0.4f);
    [SerializeField] private Color collectibleColor = new Color(1f, 0.75f, 0.2f, 0.9f); // Altın
    [SerializeField] private float poiIconSize = 6f;
    [SerializeField] private float poiGlowSize = 12f;
    
    [Header("Yön Okları")]
    [SerializeField] private bool showOffscreenArrows = true;
    [SerializeField] private Color offscreenArrowColor = new Color(0.6f, 0.3f, 0.7f, 0.5f); // Koyu mor
    
    [Header("Toggle")]
    [SerializeField] private KeyCode toggleKey = KeyCode.M;
    
    [Header("Menü Gizleme")]
    [SerializeField] private bool hideInPauseMenu = true;
    [SerializeField] private bool hideInMainMenu = true;
    [SerializeField] private bool hideInGameOver = true;

    // UI Components
    private Canvas miniMapCanvas;
    private RectTransform containerRect;
    private Image backgroundImage;
    private Image borderImage;
    private Image playerIcon;
    private Image playerGlow; // Glow efekti
    private TextMeshProUGUI positionText;
    
    // Cached textures
    private Sprite circleSprite;
    private Sprite glowSprite;
    private Sprite diamondSprite;
    
    // Tracking
    private Transform playerTransform;
    private Dictionary<Transform, Image> enemyIcons = new Dictionary<Transform, Image>();
    private Dictionary<Transform, Image> enemyGlows = new Dictionary<Transform, Image>(); // Düşman glow'ları
    private Dictionary<Transform, Image> poiIcons = new Dictionary<Transform, Image>(); // Points of Interest
    private Dictionary<Transform, Image> poiGlows = new Dictionary<Transform, Image>(); // POI glow'ları
    private List<Transform> toRemove = new List<Transform>();
    private HashSet<Transform> deadEnemies = new HashSet<Transform>(); // Ölen düşmanları takip et
    
    // State
    private bool isVisible = true;
    private float pulseTimer;

    public enum MiniMapPosition
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        BottomCenter // Yeni: Alt orta
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Sahne değişiminde yeniden initialize et
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Main menu'de tamamen gizle ama yok etme
        string sceneName = scene.name.ToLower();
        if (sceneName.Contains("menu") || sceneName.Contains("main"))
        {
            if (containerRect != null)
                containerRect.gameObject.SetActive(false);
            return;
        }
        
        // Oyun sahnelerinde yeniden başlat
        StartCoroutine(ReinitializeAfterSceneLoad());
    }
    
    private System.Collections.IEnumerator ReinitializeAfterSceneLoad()
    {
        // Bir frame bekle - nesnelerin yüklenmesi için
        yield return null;
        
        // Eski referansları temizle
        ClearAllTrackedObjects();
        
        // Yeni oyuncuyu bul
        FindPlayer();
        
        // Yeni POI'ları kaydet
        RegisterPointsOfInterest();
        
        // Mini-map'i göster
        if (containerRect != null)
            containerRect.gameObject.SetActive(true);
        
        isVisible = true;
        
        Debug.Log("[MiniMap2D] Sahne değişimi sonrası yeniden başlatıldı!");
    }
    
    /// <summary>
    /// Tüm takip edilen nesneleri temizle
    /// </summary>
    private void ClearAllTrackedObjects()
    {
        // Düşman ikonlarını temizle
        foreach (var icon in enemyIcons.Values)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        enemyIcons.Clear();
        
        foreach (var glow in enemyGlows.Values)
        {
            if (glow != null) Destroy(glow.gameObject);
        }
        enemyGlows.Clear();
        
        // POI ikonlarını temizle
        foreach (var icon in poiIcons.Values)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        poiIcons.Clear();
        
        foreach (var glow in poiGlows.Values)
        {
            if (glow != null) Destroy(glow.gameObject);
        }
        poiGlows.Clear();
        
        // Dead enemies listesini temizle
        deadEnemies.Clear();
        toRemove.Clear();
        
        playerTransform = null;
    }

    private void Start()
    {
        if (enableMiniMap)
        {
            // Sprite'ları oluştur
            CreateIconSprites();
            
            if (useExistingUI && existingContainer != null)
            {
                // Mevcut UI'ı kullan
                SetupExistingUI();
            }
            else
            {
                // Kod ile oluştur
                CreateMiniMapUI();
            }
            
            FindPlayer();
            RegisterPointsOfInterest();
        }
    }
    
    /// <summary>
    /// Şık ikon sprite'ları oluştur
    /// </summary>
    private void CreateIconSprites()
    {
        // Yumuşak daire sprite
        circleSprite = CreateCircleSprite(32, 1f);
        
        // Glow sprite (daha yumuşak kenarlar)
        glowSprite = CreateGlowSprite(64);
        
        // Elmas/diamond sprite (POI'ler için)
        diamondSprite = CreateDiamondSprite(32);
    }
    
    private Sprite CreateCircleSprite(int size, float sharpness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDist = dist / radius;
                
                // Keskin kenarlı daire
                float alpha = 1f - Mathf.Clamp01((normalizedDist - (1f - 0.1f * sharpness)) / (0.1f * sharpness));
                alpha = Mathf.Pow(alpha, 0.5f); // Biraz yumuşat
                
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    private Sprite CreateGlowSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDist = dist / radius;
                
                // Yumuşak glow - exponential falloff
                float alpha = Mathf.Exp(-normalizedDist * 2.5f);
                alpha = Mathf.Clamp01(alpha);
                
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    private Sprite CreateDiamondSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float halfSize = size / 2f - 2;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Diamond şekli: |x - center| + |y - center| <= halfSize
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);
                float dist = (dx + dy) / halfSize;
                
                float alpha = 1f - Mathf.Clamp01((dist - 0.8f) / 0.2f);
                alpha = Mathf.Pow(alpha, 0.5f);
                
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    /// <summary>
    /// Sahnede hazır olan UI'ı kullan
    /// </summary>
    private void SetupExistingUI()
    {
        containerRect = existingContainer;
        playerIcon = existingPlayerIcon;
        positionText = existingPositionText;
        
        // Oyuncu ikonu rengini ve sprite'ını ayarla
        if (playerIcon != null)
        {
            playerIcon.color = playerColor;
            if (circleSprite != null) playerIcon.sprite = circleSprite;
        }
        
        // RADAR başlığı ekle (mevcut UI'ya da)
        if (containerRect != null)
        {
            CreateRadarTitle(containerRect);
        }
        
        Debug.Log("[MiniMap2D] Mevcut UI kullanılıyor!");
    }

    private void Update()
    {
        if (!enableMiniMap) return;
        
        // Menü durumlarını kontrol et
        CheckMenuState();
        
        // Toggle
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMiniMap();
        }
        
        if (!isVisible) return;
        
        // Oyuncu pozisyonunu güncelle
        UpdatePlayerIcon();
        
        // Düşmanları güncelle
        UpdateEnemyIcons();
        
        // POI'ları güncelle
        UpdatePOIIcons();
    }
    
    /// <summary>
    /// Pause, Main Menu, Game Over durumlarında mini-map'i gizle
    /// </summary>
    private void CheckMenuState()
    {
        bool shouldHide = false;
        
        // Pause menu kontrolü (UIManager üzerinden)
        if (hideInPauseMenu && UIManager.Instance != null && UIManager.Instance.IsPaused)
        {
            shouldHide = true;
        }
        
        // Game Over kontrolü (UIManager üzerinden)
        if (hideInGameOver && UIManager.Instance != null && UIManager.Instance.IsGameOver)
        {
            shouldHide = true;
        }
        
        // Main Menu kontrolü (Scene adına göre)
        if (hideInMainMenu)
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene.ToLower().Contains("menu") || currentScene.ToLower().Contains("main"))
            {
                shouldHide = true;
            }
        }
        
        // Time.timeScale = 0 ise genel olarak gizle (skill input hariç)
        // Skill input sırasında timeScale 0 olsa bile mini-map açık kalmalı mı? Hayır, gizleyelim.
        if (Time.timeScale == 0f && !GuitarSkillSystem.Instance?.IsInSkillInput == true)
        {
            // Skill input değilse ve timeScale 0 ise bir menü açıktır
            shouldHide = true;
        }
        
        // Mini-map'i göster/gizle
        if (containerRect != null)
        {
            containerRect.gameObject.SetActive(!shouldHide && isVisible);
        }
    }

    private void CreateMiniMapUI()
    {
        // Eğer mevcut UI varsa, onu kullan
        if (useExistingUI && existingContainer != null)
        {
            SetupExistingUI();
            return;
        }
        
        // Ana Canvas
        GameObject canvasObj = new GameObject("MiniMap2DCanvas");
        canvasObj.transform.SetParent(transform);
        
        miniMapCanvas = canvasObj.AddComponent<Canvas>();
        miniMapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        miniMapCanvas.sortingOrder = 90;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Container
        GameObject containerObj = new GameObject("MiniMapContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);;
        containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.sizeDelta = miniMapSize;
        
        // Pozisyon ayarla
        SetPosition();
        
        // Border (dış çerçeve)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(containerObj.transform, false);
        borderImage = borderObj.AddComponent<Image>();
        borderImage.color = borderColor;
        
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-borderThickness, -borderThickness);
        borderRect.offsetMax = new Vector2(borderThickness, borderThickness);
        
        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(containerObj.transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // RADAR başlığı - şık tasarım
        CreateRadarTitle(containerObj.transform);
        
        // Grid (opsiyonel)
        if (showGrid)
        {
            CreateGrid(containerObj.transform);
        }
        
        // Oyuncu glow (önce oluştur, altta kalsın)
        GameObject playerGlowObj = new GameObject("PlayerGlow");
        playerGlowObj.transform.SetParent(containerObj.transform, false);
        playerGlow = playerGlowObj.AddComponent<Image>();
        playerGlow.color = playerGlowColor;
        playerGlow.raycastTarget = false;
        if (glowSprite != null) playerGlow.sprite = glowSprite;
        
        RectTransform playerGlowRect = playerGlowObj.GetComponent<RectTransform>();
        playerGlowRect.sizeDelta = new Vector2(playerGlowSize, playerGlowSize);
        playerGlowRect.anchoredPosition = Vector2.zero;
        
        // Oyuncu ikonu (her zaman ortada, glow'un üstünde)
        GameObject playerObj = new GameObject("PlayerIcon");
        playerObj.transform.SetParent(containerObj.transform, false);
        playerIcon = playerObj.AddComponent<Image>();
        playerIcon.color = playerColor;
        playerIcon.raycastTarget = false;
        if (circleSprite != null) playerIcon.sprite = circleSprite;
        
        RectTransform playerRect = playerObj.GetComponent<RectTransform>();
        playerRect.sizeDelta = new Vector2(playerIconSize, playerIconSize);
        playerRect.anchoredPosition = Vector2.zero; // Ortada
        
        // Pozisyon metni kaldırıldı - daha temiz görünüm
        positionText = null;
        
        Debug.Log("[MiniMap2D] Mini-map oluşturuldu!");
    }

    private void SetPosition()
    {
        switch (position)
        {
            case MiniMapPosition.TopRight:
                containerRect.anchorMin = new Vector2(1, 1);
                containerRect.anchorMax = new Vector2(1, 1);
                containerRect.pivot = new Vector2(1, 1);
                containerRect.anchoredPosition = miniMapOffset;
                break;
            case MiniMapPosition.TopLeft:
                containerRect.anchorMin = new Vector2(0, 1);
                containerRect.anchorMax = new Vector2(0, 1);
                containerRect.pivot = new Vector2(0, 1);
                containerRect.anchoredPosition = new Vector2(-miniMapOffset.x, miniMapOffset.y);
                break;
            case MiniMapPosition.BottomRight:
                containerRect.anchorMin = new Vector2(1, 0);
                containerRect.anchorMax = new Vector2(1, 0);
                containerRect.pivot = new Vector2(1, 0);
                containerRect.anchoredPosition = new Vector2(miniMapOffset.x, -miniMapOffset.y);
                break;
            case MiniMapPosition.BottomLeft:
                containerRect.anchorMin = new Vector2(0, 0);
                containerRect.anchorMax = new Vector2(0, 0);
                containerRect.pivot = new Vector2(0, 0);
                containerRect.anchoredPosition = new Vector2(-miniMapOffset.x, -miniMapOffset.y);
                break;
            case MiniMapPosition.BottomCenter:
                // Ekranın alt ortasında
                containerRect.anchorMin = new Vector2(0.5f, 0);
                containerRect.anchorMax = new Vector2(0.5f, 0);
                containerRect.pivot = new Vector2(0.5f, 0);
                containerRect.anchoredPosition = new Vector2(0, miniMapOffset.y);
                break;
        }
    }
    
    /// <summary>
    /// Mini-map üzerine şık "RADAR" başlığı ekle
    /// </summary>
    private void CreateRadarTitle(Transform parent)
    {
        // Başlık container - mini-map'in üstünde
        GameObject titleContainer = new GameObject("RadarTitleContainer");
        titleContainer.transform.SetParent(parent, false);
        
        RectTransform titleContainerRT = titleContainer.AddComponent<RectTransform>();
        titleContainerRT.anchorMin = new Vector2(0.5f, 1f);
        titleContainerRT.anchorMax = new Vector2(0.5f, 1f);
        titleContainerRT.pivot = new Vector2(0.5f, 0f);
        titleContainerRT.sizeDelta = new Vector2(80, 20);
        titleContainerRT.anchoredPosition = new Vector2(0, 4); // Mini-map'in 4px üstünde
        
        // Başlık arka planı
        GameObject titleBgObj = new GameObject("TitleBackground");
        titleBgObj.transform.SetParent(titleContainer.transform, false);
        
        Image titleBg = titleBgObj.AddComponent<Image>();
        titleBg.color = new Color(0.08f, 0.03f, 0.12f, 0.95f); // Koyu mor-siyah
        titleBg.raycastTarget = false;
        
        RectTransform titleBgRT = titleBgObj.GetComponent<RectTransform>();
        titleBgRT.anchorMin = Vector2.zero;
        titleBgRT.anchorMax = Vector2.one;
        titleBgRT.offsetMin = Vector2.zero;
        titleBgRT.offsetMax = Vector2.zero;
        
        // Sol accent çizgi
        GameObject leftLine = new GameObject("LeftAccent");
        leftLine.transform.SetParent(titleContainer.transform, false);
        Image leftImg = leftLine.AddComponent<Image>();
        leftImg.color = new Color(0.6f, 0.4f, 0.9f, 0.8f); // Mor (SOUL CHARGES uyumlu)
        leftImg.raycastTarget = false;
        RectTransform leftRT = leftLine.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 0.2f);
        leftRT.anchorMax = new Vector2(0, 0.8f);
        leftRT.sizeDelta = new Vector2(2, 0);
        leftRT.anchoredPosition = new Vector2(4, 0);
        
        // Sağ accent çizgi
        GameObject rightLine = new GameObject("RightAccent");
        rightLine.transform.SetParent(titleContainer.transform, false);
        Image rightImg = rightLine.AddComponent<Image>();
        rightImg.color = new Color(0.6f, 0.4f, 0.9f, 0.8f); // Mor (SOUL CHARGES uyumlu)
        rightImg.raycastTarget = false;
        RectTransform rightRT = rightLine.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(1, 0.2f);
        rightRT.anchorMax = new Vector2(1, 0.8f);
        rightRT.sizeDelta = new Vector2(2, 0);
        rightRT.anchoredPosition = new Vector2(-4, 0);
        
        // RADAR yazısı - TextMeshProUGUI ile diğer UI'larla uyumlu
        GameObject titleTextObj = new GameObject("RadarText");
        titleTextObj.transform.SetParent(titleContainer.transform, false);
        
        TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "RADAR";
        titleText.fontSize = 12;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.8f, 0.7f, 1f, 0.9f); // SOUL CHARGES ile aynı mor ton
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.raycastTarget = false;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        
        RectTransform titleTextRT = titleTextObj.GetComponent<RectTransform>();
        titleTextRT.anchorMin = Vector2.zero;
        titleTextRT.anchorMax = Vector2.one;
        titleTextRT.offsetMin = Vector2.zero;
        titleTextRT.offsetMax = Vector2.zero;
    }

    private void CreateGrid(Transform parent)
    {
        // Basit grid çizgileri
        int gridLinesX = 5;
        int gridLinesY = 3;
        
        for (int i = 1; i < gridLinesX; i++)
        {
            float xPos = (miniMapSize.x / gridLinesX) * i - miniMapSize.x / 2;
            CreateGridLine(parent, new Vector2(xPos, 0), new Vector2(1, miniMapSize.y), true);
        }
        
        for (int i = 1; i < gridLinesY; i++)
        {
            float yPos = (miniMapSize.y / gridLinesY) * i - miniMapSize.y / 2;
            CreateGridLine(parent, new Vector2(0, yPos), new Vector2(miniMapSize.x, 1), false);
        }
    }

    private void CreateGridLine(Transform parent, Vector2 position, Vector2 size, bool vertical)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(parent, false);
        
        Image lineImage = lineObj.AddComponent<Image>();
        lineImage.color = gridColor;
        lineImage.raycastTarget = false;
        
        RectTransform rect = lineObj.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
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
            {
                playerTransform = player.transform;
            }
        }
    }

    private void RegisterPointsOfInterest()
    {
        // Checkpoint'ler
        GameObject[] checkpoints = GameObject.FindGameObjectsWithTag("Checkpoint");
        foreach (var cp in checkpoints)
        {
            CreatePOIIcon(cp.transform, checkpointColor, poiIconSize);
        }
        
        // Level çıkışları
        LevelExit[] exits = FindObjectsByType<LevelExit>(FindObjectsSortMode.None);
        foreach (var exit in exits)
        {
            CreatePOIIcon(exit.transform, exitColor, poiIconSize + 2);
        }
        
        Debug.Log($"[MiniMap2D] {poiIcons.Count} önemli nokta kaydedildi.");
    }

    private void CreatePOIIcon(Transform target, Color color, float size)
    {
        if (poiIcons.ContainsKey(target)) return;
        
        // POI Glow (önce oluştur, altta kalsın)
        GameObject glowObj = new GameObject($"POIGlow_{target.name}");
        glowObj.transform.SetParent(containerRect, false);
        
        Image glow = glowObj.AddComponent<Image>();
        Color glowColor = new Color(color.r, color.g, color.b, 0.4f);
        glow.color = glowColor;
        glow.raycastTarget = false;
        if (glowSprite != null) glow.sprite = glowSprite;
        
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.sizeDelta = new Vector2(poiGlowSize, poiGlowSize);
        
        poiGlows.Add(target, glow);
        
        // POI Icon (diamond şekli)
        GameObject iconObj = new GameObject($"POI_{target.name}");
        iconObj.transform.SetParent(containerRect, false);
        
        Image icon = iconObj.AddComponent<Image>();
        icon.color = color;
        icon.raycastTarget = false;
        if (diamondSprite != null) icon.sprite = diamondSprite;
        
        RectTransform rect = iconObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);
        
        poiIcons.Add(target, icon);
    }

    private Image CreateEnemyIcon(Transform enemy, bool isBoss)
    {
        // Glow önce (altta kalsın)
        GameObject glowObj = new GameObject($"EnemyGlow_{enemy.name}");
        glowObj.transform.SetParent(containerRect, false);
        
        Image glow = glowObj.AddComponent<Image>();
        glow.color = isBoss ? bossGlowColor : enemyGlowColor;
        glow.raycastTarget = false;
        if (glowSprite != null) glow.sprite = glowSprite;
        
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        float glowSize = isBoss ? bossGlowSize : enemyGlowSize;
        glowRect.sizeDelta = new Vector2(glowSize, glowSize);
        
        enemyGlows.Add(enemy, glow);
        
        // İkon (üstte)
        GameObject iconObj = new GameObject($"Enemy_{enemy.name}");
        iconObj.transform.SetParent(containerRect, false);
        
        Image icon = iconObj.AddComponent<Image>();
        icon.color = isBoss ? bossColor : enemyColor;
        icon.raycastTarget = false;
        if (circleSprite != null) icon.sprite = circleSprite;
        
        RectTransform rect = iconObj.GetComponent<RectTransform>();
        float size = isBoss ? bossIconSize : enemyIconSize;
        rect.sizeDelta = new Vector2(size, size);
        
        enemyIcons.Add(enemy, icon);
        return icon;
    }

    private void UpdatePlayerIcon()
    {
        if (playerTransform == null || playerIcon == null) return;
        
        // Pulse efekti
        if (playerPulse)
        {
            pulseTimer += Time.unscaledDeltaTime * 3f;
            float pulse = 1f + Mathf.Sin(pulseTimer) * 0.15f;
            playerIcon.rectTransform.sizeDelta = new Vector2(playerIconSize * pulse, playerIconSize * pulse);
            
            // Glow da pulse yapsın
            if (playerGlow != null)
            {
                float glowPulse = 1f + Mathf.Sin(pulseTimer * 0.8f) * 0.25f;
                playerGlow.rectTransform.sizeDelta = new Vector2(playerGlowSize * glowPulse, playerGlowSize * glowPulse);
                
                // Glow alpha da değişsin
                float glowAlpha = playerGlowColor.a * (0.7f + Mathf.Sin(pulseTimer * 1.5f) * 0.3f);
                playerGlow.color = new Color(playerGlowColor.r, playerGlowColor.g, playerGlowColor.b, glowAlpha);
            }
        }
        
        // Pozisyon metni kaldırıldı
    }

    private void UpdateEnemyIcons()
    {
        if (playerTransform == null) return;
        
        // Mevcut düşmanları kontrol et - null veya ölü olanları temizle
        toRemove.Clear();
        foreach (var kvp in enemyIcons)
        {
            // Transform null, destroyed veya dead listesinde ise kaldır
            if (kvp.Key == null || !kvp.Key.gameObject.activeInHierarchy || deadEnemies.Contains(kvp.Key))
            {
                toRemove.Add(kvp.Key);
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
                // Glow'u da temizle
                if (enemyGlows.ContainsKey(kvp.Key) && enemyGlows[kvp.Key] != null)
                    Destroy(enemyGlows[kvp.Key].gameObject);
            }
            else
            {
                // EnemyHealth kontrolü - isDead ise temizle
                EnemyHealth enemyHealth = kvp.Key.GetComponent<EnemyHealth>();
                if (enemyHealth == null || enemyHealth.IsDead)
                {
                    toRemove.Add(kvp.Key);
                    if (kvp.Value != null)
                        Destroy(kvp.Value.gameObject);
                    // Glow'u da temizle
                    if (enemyGlows.ContainsKey(kvp.Key) && enemyGlows[kvp.Key] != null)
                        Destroy(enemyGlows[kvp.Key].gameObject);
                }
            }
        }
        
        foreach (var t in toRemove)
        {
            enemyIcons.Remove(t);
            enemyGlows.Remove(t); // Glow'u da listeden kaldır
            deadEnemies.Remove(t); // Dead listesinden de temizle
        }
        
        // Yakındaki düşmanları bul ve güncelle
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in allEnemies)
        {
            // Ölü düşmanları atla
            if (enemy.IsDead || deadEnemies.Contains(enemy.transform)) continue;
            
            float distance = Vector2.Distance(playerTransform.position, enemy.transform.position);
            
            if (distance <= enemyDetectionRange)
            {
                // İkon yoksa oluştur
                if (!enemyIcons.ContainsKey(enemy.transform))
                {
                    bool isBoss = enemy.gameObject.CompareTag("Boss") || 
                                  enemy.name.ToLower().Contains("boss");
                    CreateEnemyIcon(enemy.transform, isBoss);
                }
                
                // Pozisyonu güncelle
                UpdateIconPosition(enemy.transform, enemyIcons[enemy.transform]);
            }
            else
            {
                // Menzil dışındaysa ikonu ve glow'u kaldır
                if (enemyIcons.ContainsKey(enemy.transform))
                {
                    Destroy(enemyIcons[enemy.transform].gameObject);
                    enemyIcons.Remove(enemy.transform);
                }
                if (enemyGlows.ContainsKey(enemy.transform))
                {
                    Destroy(enemyGlows[enemy.transform].gameObject);
                    enemyGlows.Remove(enemy.transform);
                }
            }
        }
    }

    private void UpdatePOIIcons()
    {
        if (playerTransform == null) return;
        
        toRemove.Clear();
        
        foreach (var kvp in poiIcons)
        {
            if (kvp.Key == null)
            {
                toRemove.Add(kvp.Key);
                if (kvp.Value != null)
                    Destroy(kvp.Value.gameObject);
            }
            else
            {
                UpdateIconPosition(kvp.Key, kvp.Value);
                
                // POI glow'u da güncelle
                if (poiGlows.ContainsKey(kvp.Key) && poiGlows[kvp.Key] != null)
                {
                    UpdateIconPosition(kvp.Key, poiGlows[kvp.Key]);
                }
            }
        }
        
        foreach (var t in toRemove)
        {
            poiIcons.Remove(t);
            if (poiGlows.ContainsKey(t))
            {
                if (poiGlows[t] != null) Destroy(poiGlows[t].gameObject);
                poiGlows.Remove(t);
            }
        }
    }

    private void UpdateIconPosition(Transform target, Image icon)
    {
        if (target == null || icon == null || playerTransform == null) return;
        
        // Oyuncuya göre relatif pozisyon
        Vector2 relativePos = (Vector2)(target.position - playerTransform.position);
        
        // Mini-map koordinatlarına çevir
        float mapX = (relativePos.x / viewRangeX) * (miniMapSize.x / 2);
        float mapY = (relativePos.y / viewRangeY) * (miniMapSize.y / 2);
        
        // Sınırları kontrol et
        float halfWidth = miniMapSize.x / 2 - 5;
        float halfHeight = miniMapSize.y / 2 - 5;
        
        bool isOffscreen = Mathf.Abs(mapX) > halfWidth || Mathf.Abs(mapY) > halfHeight;
        
        if (isOffscreen && showOffscreenArrows)
        {
            // Kenardan göster
            mapX = Mathf.Clamp(mapX, -halfWidth, halfWidth);
            mapY = Mathf.Clamp(mapY, -halfHeight, halfHeight);
            
            // Kenar rengi - daha şeffaf
            icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 0.4f);
        }
        else
        {
            mapX = Mathf.Clamp(mapX, -halfWidth, halfWidth);
            mapY = Mathf.Clamp(mapY, -halfHeight, halfHeight);
            
            // Normal renk
            icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, 1f);
        }
        
        // Düşman glow'unu da güncelle (varsa)
        if (enemyGlows.ContainsKey(target) && enemyGlows[target] != null)
        {
            enemyGlows[target].rectTransform.anchoredPosition = new Vector2(mapX, mapY);
            // Glow daha şeffaf olsun offscreen'de
            float glowAlpha = isOffscreen ? 0.2f : 0.4f;
            Color glowCol = enemyGlows[target].color;
            enemyGlows[target].color = new Color(glowCol.r, glowCol.g, glowCol.b, glowAlpha);
        }
        icon.rectTransform.anchoredPosition = new Vector2(mapX, mapY);
    }

    /// <summary>
    /// Mini-map'i göster/gizle
    /// </summary>
    public void ToggleMiniMap()
    {
        isVisible = !isVisible;
        if (containerRect != null)
        {
            containerRect.gameObject.SetActive(isVisible);
        }
    }

    /// <summary>
    /// Mini-map'i göster
    /// </summary>
    public void Show()
    {
        isVisible = true;
        if (containerRect != null)
        {
            containerRect.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// Mini-map'i gizle
    /// </summary>
    public void Hide()
    {
        isVisible = false;
        if (containerRect != null)
        {
            containerRect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Görüş aralığını değiştir (zoom)
    /// </summary>
    public void SetViewRange(float rangeX, float rangeY)
    {
        viewRangeX = rangeX;
        viewRangeY = rangeY;
    }

    /// <summary>
    /// Düşman öldüğünde çağrılır (otomatik temizlik için)
    /// </summary>
    public void OnEnemyDeath(Transform enemy)
    {
        if (enemy == null) return;
        
        // Ölü düşmanlar listesine ekle (tekrar eklenmesini önle)
        deadEnemies.Add(enemy);
        
        // İkonu hemen kaldır
        if (enemyIcons.ContainsKey(enemy))
        {
            if (enemyIcons[enemy] != null)
            {
                Destroy(enemyIcons[enemy].gameObject);
            }
            enemyIcons.Remove(enemy);
        }
        
        // Glow'u da kaldır
        if (enemyGlows.ContainsKey(enemy))
        {
            if (enemyGlows[enemy] != null)
            {
                Destroy(enemyGlows[enemy].gameObject);
            }
            enemyGlows.Remove(enemy);
        }
        
        Debug.Log($"[MiniMap2D] Düşman radardan kaldırıldı: {enemy.name}");
    }

    /// <summary>
    /// Yeni bir POI ekle (dinamik olarak)
    /// </summary>
    public void AddPointOfInterest(Transform poi, Color color, float size = -1)
    {
        if (size < 0) size = poiIconSize;
        CreatePOIIcon(poi, color, size);
    }

    /// <summary>
    /// POI kaldır
    /// </summary>
    public void RemovePointOfInterest(Transform poi)
    {
        if (poiIcons.ContainsKey(poi))
        {
            if (poiIcons[poi] != null)
            {
                Destroy(poiIcons[poi].gameObject);
            }
            poiIcons.Remove(poi);
        }
        
        // POI glow'unu da kaldır
        if (poiGlows.ContainsKey(poi))
        {
            if (poiGlows[poi] != null)
            {
                Destroy(poiGlows[poi].gameObject);
            }
            poiGlows.Remove(poi);
        }
    }

    private void OnDestroy()
    {
        // Event'i unsubscribe et
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
