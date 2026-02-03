using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Ana Mini-Map sistemi. Render Texture tabanlı mini-map oluşturur.
/// Oyuncuyu, düşmanları ve önemli noktaları gösterir.
/// </summary>
public class MiniMapSystem : MonoBehaviour
{
    public static MiniMapSystem Instance { get; private set; }

    [Header("Mini-Map Settings")]
    [SerializeField] private bool enableMiniMap = true;
    [SerializeField] private Vector2 miniMapSize = new Vector2(200, 200);
    [SerializeField] private Vector2 miniMapPosition = new Vector2(-20, -20); // Sağ üst köşeden offset
    [SerializeField] private MiniMapCorner miniMapCorner = MiniMapCorner.TopRight;
    
    [Header("Camera Settings")]
    [SerializeField] private float cameraHeight = 50f;
    [SerializeField] private float cameraSize = 15f; // Orthographic size
    [SerializeField] private bool rotateWithPlayer = false;
    [SerializeField] private float smoothFollow = 5f;
    
    [Header("Zoom Settings")]
    [SerializeField] private float minZoom = 8f;
    [SerializeField] private float maxZoom = 30f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private KeyCode zoomInKey = KeyCode.Equals;
    [SerializeField] private KeyCode zoomOutKey = KeyCode.Minus;
    
    [Header("Visual Settings")]
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.8f);
    [SerializeField] private Color borderColor = new Color(0.8f, 0.6f, 0.2f, 1f); // Altın rengi border
    [SerializeField] private float borderWidth = 3f;
    [SerializeField] private bool circularMask = true;
    
    [Header("Icon Settings")]
    [SerializeField] private Sprite playerIcon;
    [SerializeField] private Sprite enemyIcon;
    [SerializeField] private Sprite bossIcon;
    [SerializeField] private Sprite checkpointIcon;
    [SerializeField] private Sprite exitIcon;
    [SerializeField] private Sprite collectibleIcon;
    [SerializeField] private float iconScale = 1f;
    
    [Header("Icon Colors")]
    [SerializeField] private Color playerColor = Color.green;
    [SerializeField] private Color enemyColor = Color.red;
    [SerializeField] private Color bossColor = new Color(1f, 0.3f, 0f); // Turuncu
    [SerializeField] private Color checkpointColor = Color.cyan;
    [SerializeField] private Color exitColor = Color.yellow;
    [SerializeField] private Color collectibleColor = new Color(1f, 0.8f, 0.2f); // Altın

    [Header("Layers")]
    [SerializeField] private LayerMask miniMapLayers; // Mini-map kamerasının göreceği layerlar

    // Components
    private Camera miniMapCamera;
    private RenderTexture miniMapTexture;
    private RawImage miniMapDisplay;
    private Image miniMapBorder;
    private Image miniMapBackground;
    private Canvas miniMapCanvas;
    private RectTransform miniMapRect;
    private GameObject miniMapContainer;
    
    // Tracking
    private Transform playerTransform;
    private Dictionary<Transform, MiniMapIcon> trackedObjects = new Dictionary<Transform, MiniMapIcon>();
    private List<Transform> objectsToRemove = new List<Transform>();
    
    // State
    private bool isInitialized = false;
    private float currentZoom;

    public enum MiniMapCorner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

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
    }

    private void Start()
    {
        if (enableMiniMap)
        {
            InitializeMiniMap();
        }
    }

    private void Update()
    {
        if (!isInitialized || !enableMiniMap) return;
        
        // Zoom kontrolü
        HandleZoom();
        
        // Kamera takibi
        FollowPlayer();
        
        // İkonları güncelle
        UpdateIcons();
    }

    private void LateUpdate()
    {
        if (!isInitialized || !enableMiniMap) return;
        
        // Ölü objeleri temizle
        CleanupDeadObjects();
    }

    /// <summary>
    /// Mini-map sistemini başlat
    /// </summary>
    public void InitializeMiniMap()
    {
        if (isInitialized) return;
        
        // Oyuncuyu bul
        FindPlayer();
        
        // Render texture oluştur
        CreateRenderTexture();
        
        // Mini-map kamerasını oluştur
        CreateMiniMapCamera();
        
        // UI elementlerini oluştur
        CreateMiniMapUI();
        
        // Mevcut objeleri kaydet
        RegisterExistingObjects();
        
        currentZoom = cameraSize;
        isInitialized = true;
        
        Debug.Log("[MiniMapSystem] Mini-map başarıyla oluşturuldu!");
    }

    private void FindPlayer()
    {
        // PlayerMovement'ı bul
        if (PlayerMovement.Instance != null)
        {
            playerTransform = PlayerMovement.Instance.transform;
        }
        else
        {
            // Alternatif: Player tag'i ile bul
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        if (playerTransform == null)
        {
            Debug.LogWarning("[MiniMapSystem] Oyuncu bulunamadı!");
        }
    }

    private void CreateRenderTexture()
    {
        int resolution = Mathf.RoundToInt(Mathf.Max(miniMapSize.x, miniMapSize.y) * 2);
        miniMapTexture = new RenderTexture(resolution, resolution, 16);
        miniMapTexture.filterMode = FilterMode.Bilinear;
        miniMapTexture.Create();
    }

    private void CreateMiniMapCamera()
    {
        // Kamera objesi oluştur
        GameObject camObj = new GameObject("MiniMapCamera");
        camObj.transform.SetParent(transform);
        
        miniMapCamera = camObj.AddComponent<Camera>();
        miniMapCamera.orthographic = true;
        miniMapCamera.orthographicSize = cameraSize;
        miniMapCamera.clearFlags = CameraClearFlags.SolidColor;
        miniMapCamera.backgroundColor = backgroundColor;
        miniMapCamera.targetTexture = miniMapTexture;
        miniMapCamera.cullingMask = miniMapLayers.value != 0 ? miniMapLayers.value : ~0; // Tüm layerlar veya seçili olanlar
        miniMapCamera.depth = -10; // Ana kameradan düşük öncelik
        
        // Yukarıdan bakan pozisyon
        if (playerTransform != null)
        {
            camObj.transform.position = playerTransform.position + Vector3.up * cameraHeight;
        }
        camObj.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Aşağı bak
    }

    private void CreateMiniMapUI()
    {
        // Canvas oluştur
        GameObject canvasObj = new GameObject("MiniMapCanvas");
        canvasObj.transform.SetParent(transform);
        
        miniMapCanvas = canvasObj.AddComponent<Canvas>();
        miniMapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        miniMapCanvas.sortingOrder = 100;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Container oluştur
        miniMapContainer = new GameObject("MiniMapContainer");
        miniMapContainer.transform.SetParent(canvasObj.transform, false);
        miniMapRect = miniMapContainer.AddComponent<RectTransform>();
        
        // Pozisyonu ayarla
        SetMiniMapPosition();
        
        // Arka plan (border için)
        GameObject borderObj = new GameObject("MiniMapBorder");
        borderObj.transform.SetParent(miniMapContainer.transform, false);
        miniMapBorder = borderObj.AddComponent<Image>();
        miniMapBorder.color = borderColor;
        
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-borderWidth, -borderWidth);
        borderRect.offsetMax = new Vector2(borderWidth, borderWidth);
        
        // Background
        GameObject bgObj = new GameObject("MiniMapBackground");
        bgObj.transform.SetParent(miniMapContainer.transform, false);
        miniMapBackground = bgObj.AddComponent<Image>();
        miniMapBackground.color = backgroundColor;
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Mini-map display (RawImage)
        GameObject displayObj = new GameObject("MiniMapDisplay");
        displayObj.transform.SetParent(miniMapContainer.transform, false);
        miniMapDisplay = displayObj.AddComponent<RawImage>();
        miniMapDisplay.texture = miniMapTexture;
        miniMapDisplay.color = Color.white;
        
        RectTransform displayRect = displayObj.GetComponent<RectTransform>();
        displayRect.anchorMin = Vector2.zero;
        displayRect.anchorMax = Vector2.one;
        displayRect.offsetMin = Vector2.zero;
        displayRect.offsetMax = Vector2.zero;
        
        // Circular mask (opsiyonel)
        if (circularMask)
        {
            ApplyCircularMask();
        }
    }

    private void SetMiniMapPosition()
    {
        miniMapRect.sizeDelta = miniMapSize;
        
        switch (miniMapCorner)
        {
            case MiniMapCorner.TopRight:
                miniMapRect.anchorMin = new Vector2(1, 1);
                miniMapRect.anchorMax = new Vector2(1, 1);
                miniMapRect.pivot = new Vector2(1, 1);
                miniMapRect.anchoredPosition = miniMapPosition;
                break;
                
            case MiniMapCorner.TopLeft:
                miniMapRect.anchorMin = new Vector2(0, 1);
                miniMapRect.anchorMax = new Vector2(0, 1);
                miniMapRect.pivot = new Vector2(0, 1);
                miniMapRect.anchoredPosition = new Vector2(-miniMapPosition.x, miniMapPosition.y);
                break;
                
            case MiniMapCorner.BottomRight:
                miniMapRect.anchorMin = new Vector2(1, 0);
                miniMapRect.anchorMax = new Vector2(1, 0);
                miniMapRect.pivot = new Vector2(1, 0);
                miniMapRect.anchoredPosition = new Vector2(miniMapPosition.x, -miniMapPosition.y);
                break;
                
            case MiniMapCorner.BottomLeft:
                miniMapRect.anchorMin = new Vector2(0, 0);
                miniMapRect.anchorMax = new Vector2(0, 0);
                miniMapRect.pivot = new Vector2(0, 0);
                miniMapRect.anchoredPosition = new Vector2(-miniMapPosition.x, -miniMapPosition.y);
                break;
        }
    }

    private void ApplyCircularMask()
    {
        // Circular mask için Mask component ekle
        var mask = miniMapContainer.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        // Mask için circle image gerekli
        var maskImage = miniMapContainer.AddComponent<Image>();
        maskImage.color = Color.white;
        
        // Circle sprite yoksa varsayılan kullan
        // Not: Gerçek projede circle sprite atanmalı
        // Şimdilik kare olarak bırakıyoruz, sprite atanınca çalışacak
    }

    private void HandleZoom()
    {
        // Klavye ile zoom
        if (Input.GetKey(zoomInKey))
        {
            currentZoom -= zoomSpeed * Time.unscaledDeltaTime;
        }
        else if (Input.GetKey(zoomOutKey))
        {
            currentZoom += zoomSpeed * Time.unscaledDeltaTime;
        }
        
        // Mouse scroll ile zoom (mini-map üzerindeyken)
        // Bu özellik opsiyonel, şimdilik klavye yeterli
        
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        
        if (miniMapCamera != null)
        {
            miniMapCamera.orthographicSize = Mathf.Lerp(
                miniMapCamera.orthographicSize, 
                currentZoom, 
                Time.unscaledDeltaTime * 5f
            );
        }
    }

    private void FollowPlayer()
    {
        if (miniMapCamera == null || playerTransform == null) return;
        
        Vector3 targetPosition = playerTransform.position + Vector3.up * cameraHeight;
        
        if (smoothFollow > 0)
        {
            miniMapCamera.transform.position = Vector3.Lerp(
                miniMapCamera.transform.position,
                targetPosition,
                Time.unscaledDeltaTime * smoothFollow
            );
        }
        else
        {
            miniMapCamera.transform.position = targetPosition;
        }
        
        // Oyuncu ile döndürme (opsiyonel)
        if (rotateWithPlayer && playerTransform != null)
        {
            float playerYRotation = playerTransform.eulerAngles.y;
            miniMapCamera.transform.rotation = Quaternion.Euler(90f, playerYRotation, 0f);
        }
    }

    private void UpdateIcons()
    {
        foreach (var kvp in trackedObjects)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                kvp.Value.UpdatePosition(miniMapCamera, miniMapRect);
            }
        }
    }

    private void CleanupDeadObjects()
    {
        objectsToRemove.Clear();
        
        foreach (var kvp in trackedObjects)
        {
            if (kvp.Key == null || kvp.Value == null)
            {
                objectsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var obj in objectsToRemove)
        {
            if (trackedObjects.TryGetValue(obj, out MiniMapIcon icon))
            {
                if (icon != null)
                {
                    Destroy(icon.gameObject);
                }
            }
            trackedObjects.Remove(obj);
        }
    }

    /// <summary>
    /// Mevcut sahnedeki objeleri kaydet
    /// </summary>
    private void RegisterExistingObjects()
    {
        // Oyuncuyu kaydet
        if (playerTransform != null)
        {
            RegisterObject(playerTransform, MiniMapIconType.Player);
        }
        
        // Düşmanları bul ve kaydet
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            // Boss kontrolü (örnek: "Boss" tag'i veya isimde "Boss" geçiyorsa)
            bool isBoss = enemy.gameObject.CompareTag("Boss") || 
                          enemy.gameObject.name.ToLower().Contains("boss");
            
            RegisterObject(enemy.transform, isBoss ? MiniMapIconType.Boss : MiniMapIconType.Enemy);
        }
        
        // Checkpoint'leri bul
        GameObject[] checkpoints = GameObject.FindGameObjectsWithTag("Checkpoint");
        foreach (var cp in checkpoints)
        {
            RegisterObject(cp.transform, MiniMapIconType.Checkpoint);
        }
        
        // Level çıkışlarını bul
        LevelExit[] exits = FindObjectsByType<LevelExit>(FindObjectsSortMode.None);
        foreach (var exit in exits)
        {
            RegisterObject(exit.transform, MiniMapIconType.Exit);
        }
        
        Debug.Log($"[MiniMapSystem] {trackedObjects.Count} obje mini-map'e kaydedildi.");
    }

    /// <summary>
    /// Yeni bir objeyi mini-map'e kaydet
    /// </summary>
    public void RegisterObject(Transform target, MiniMapIconType iconType)
    {
        if (target == null || trackedObjects.ContainsKey(target)) return;
        if (!isInitialized) return;
        
        // İkon objesi oluştur
        GameObject iconObj = new GameObject($"MiniMapIcon_{target.name}");
        iconObj.transform.SetParent(miniMapContainer.transform, false);
        
        MiniMapIcon icon = iconObj.AddComponent<MiniMapIcon>();
        icon.Initialize(target, iconType, GetIconSprite(iconType), GetIconColor(iconType), iconScale);
        
        trackedObjects.Add(target, icon);
    }

    /// <summary>
    /// Bir objeyi mini-map'ten kaldır
    /// </summary>
    public void UnregisterObject(Transform target)
    {
        if (target == null || !trackedObjects.ContainsKey(target)) return;
        
        if (trackedObjects.TryGetValue(target, out MiniMapIcon icon))
        {
            if (icon != null)
            {
                Destroy(icon.gameObject);
            }
        }
        
        trackedObjects.Remove(target);
    }

    private Sprite GetIconSprite(MiniMapIconType iconType)
    {
        switch (iconType)
        {
            case MiniMapIconType.Player: return playerIcon;
            case MiniMapIconType.Enemy: return enemyIcon;
            case MiniMapIconType.Boss: return bossIcon;
            case MiniMapIconType.Checkpoint: return checkpointIcon;
            case MiniMapIconType.Exit: return exitIcon;
            case MiniMapIconType.Collectible: return collectibleIcon;
            default: return null;
        }
    }

    private Color GetIconColor(MiniMapIconType iconType)
    {
        switch (iconType)
        {
            case MiniMapIconType.Player: return playerColor;
            case MiniMapIconType.Enemy: return enemyColor;
            case MiniMapIconType.Boss: return bossColor;
            case MiniMapIconType.Checkpoint: return checkpointColor;
            case MiniMapIconType.Exit: return exitColor;
            case MiniMapIconType.Collectible: return collectibleColor;
            default: return Color.white;
        }
    }

    /// <summary>
    /// Mini-map'i göster/gizle
    /// </summary>
    public void ToggleMiniMap()
    {
        if (miniMapContainer != null)
        {
            miniMapContainer.SetActive(!miniMapContainer.activeSelf);
        }
    }

    /// <summary>
    /// Mini-map'i göster
    /// </summary>
    public void ShowMiniMap()
    {
        if (miniMapContainer != null)
        {
            miniMapContainer.SetActive(true);
        }
    }

    /// <summary>
    /// Mini-map'i gizle
    /// </summary>
    public void HideMiniMap()
    {
        if (miniMapContainer != null)
        {
            miniMapContainer.SetActive(false);
        }
    }

    /// <summary>
    /// Zoom seviyesini ayarla
    /// </summary>
    public void SetZoom(float zoom)
    {
        currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
    }

    private void OnDestroy()
    {
        // Temizlik
        if (miniMapTexture != null)
        {
            miniMapTexture.Release();
            Destroy(miniMapTexture);
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

/// <summary>
/// Mini-map ikon tipleri
/// </summary>
public enum MiniMapIconType
{
    Player,
    Enemy,
    Boss,
    Checkpoint,
    Exit,
    Collectible,
    Custom
}
