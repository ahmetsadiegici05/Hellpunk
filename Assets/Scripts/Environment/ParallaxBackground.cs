using UnityEngine;

/// <summary>
/// 2D Platformer için optimize edilmiş X + Y Parallax arka plan sistemi
/// Sonsuz yatay ve dikey tekrar destekler
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Ayarları")]
    [Range(0f, 1f)] 
    [Tooltip("0 = kamerayla birlikte hareket (ön plan), 1 = sabit durur (çok uzak). Background: 0.9, Midground: 0.5")]
    public float parallaxEffectX = 0.9f;
    [Range(0f, 1f)] 
    [Tooltip("Dikey parallax. Genelde yataydan düşük tutulur.")]
    public float parallaxEffectY = 0.5f;

    [Header("Ölçekleme")]
    [Tooltip("Kapalıyken scale değiştirmez, açıkken kameraya göre otomatik scale yapar")]
    public bool autoScaleToCamera = false;
    public float manualScale = 5f;
    [Tooltip("Auto scale açıkken ekstra çarpan")]
    public float extraScale = 1.2f;
    
    [Header("Pozisyon")]
    [Tooltip("Açıkken başlangıçta kamera pozisyonuna taşır, kapalıyken mevcut pozisyonu korur")]
    public bool followCameraOnStart = true;
    [Tooltip("Y offset - negatif = aşağı, pozitif = yukarı")]
    public float yOffset = 0f;

    [Header("Derinlik Ayarları")]
    [Tooltip("Z pozisyonu - büyük değer = daha uzak")]
    public float zDepth = 20f;
    [Tooltip("Sorting Layer adı (Background, Midground, vb.)")]
    public string sortingLayerName = "Default";
    [Tooltip("Sorting Order - negatif değerler daha arkada. Background için -100, Midground için -50 önerilir")]
    public int sortingOrder = -100;

    [Header("Şeffaflık (Midground için)")]
    [Range(0f, 1f)]
    [Tooltip("0 = tamamen şeffaf, 1 = tamamen opak. Midground için 0.6-0.8 önerilir")]
    public float alpha = 1f;

    [Header("Tile Tekrar")]
    [Tooltip("Açıkken 3x3 tile oluşturur (sonsuz arka plan için). Midground için KAPALI tutun!")]
    public bool createTiles = true;

    private Transform cameraTransform;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;

    private float textureUnitSizeX;
    private float textureUnitSizeY;
    private Vector3 lastCameraPosition;
    private float appliedScale;

    void Start()
    {
        mainCamera = Camera.main;
        cameraTransform = mainCamera.transform;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogError("ParallaxBackground: SpriteRenderer veya Sprite yok!");
            enabled = false;
            return;
        }

        // Sorting Layer ve Order ayarla
        ApplySortingSettings();

        // Scale ayarla (autoScaleToCamera açıksa)
        if (autoScaleToCamera)
        {
            CalculateAndApplyScale();
        }
        else
        {
            // Manuel scale - mevcut scale'i koru, sadece texture size hesapla
            Sprite sprite = spriteRenderer.sprite;
            appliedScale = transform.localScale.x;
            textureUnitSizeX = sprite.bounds.size.x * appliedScale;
            textureUnitSizeY = sprite.bounds.size.y * appliedScale;
        }

        // Pozisyon ayarla
        if (followCameraOnStart)
        {
            transform.position = new Vector3(
                cameraTransform.position.x,
                cameraTransform.position.y + yOffset,
                zDepth
            );
        }
        else
        {
            // Sadece Z derinliğini ayarla, X ve Y koru
            Vector3 pos = transform.position;
            pos.z = zDepth;
            transform.position = pos;
        }

        lastCameraPosition = cameraTransform.position;

        // Tile oluştur (opsiyonel)
        if (createTiles)
        {
            CreateTiles2D();
        }
    }

    void ApplySortingSettings()
    {
        // Sorting Layer kontrolü - layer yoksa Default kullan
        if (!string.IsNullOrEmpty(sortingLayerName) && SortingLayer.NameToID(sortingLayerName) != 0)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
        }
        else
        {
            spriteRenderer.sortingLayerName = "Default";
        }
        
        spriteRenderer.sortingOrder = sortingOrder;
        
        // Alpha/şeffaflık uygula
        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
        
        Debug.Log($"[ParallaxBackground] {gameObject.name} - Layer: {spriteRenderer.sortingLayerName}, Order: {sortingOrder}, Alpha: {alpha}");
    }

    void CalculateAndApplyScale()
    {
        Sprite sprite = spriteRenderer.sprite;
        float spriteHeight = sprite.bounds.size.y;
        float spriteWidth = sprite.bounds.size.x;

        if (autoScaleToCamera)
        {
            float cameraHeight = mainCamera.orthographicSize * 2f;
            appliedScale = (cameraHeight / spriteHeight) * extraScale;
        }
        else
        {
            appliedScale = manualScale;
        }

        transform.localScale = new Vector3(appliedScale, appliedScale, 1f);

        textureUnitSizeX = spriteWidth * appliedScale;
        textureUnitSizeY = spriteHeight * appliedScale;
    }

    void CreateTiles2D()
    {
        // 3x3 grid (-1,0,1) x (-1,0,1)
        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (x == 0 && y == 0) continue;

                GameObject tile = new GameObject($"Tile_{x}_{y}");
                tile.transform.SetParent(transform);
                tile.transform.localPosition = new Vector3(
                    x * textureUnitSizeX / appliedScale,
                    y * textureUnitSizeY / appliedScale,
                    0
                );
                tile.transform.localScale = Vector3.one;

                SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
                sr.sprite = spriteRenderer.sprite;
                sr.sortingLayerID = spriteRenderer.sortingLayerID;
                sr.sortingOrder = spriteRenderer.sortingOrder;
                sr.color = spriteRenderer.color;
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        // Parallax efekti: background kameradan DAHA YAVAŞ hareket etmeli
        // parallaxEffectX = 0 → background kamerayla aynı hızda (sabit kalır ekranda)
        // parallaxEffectX = 1 → background hiç hareket etmez (dünyada sabit)
        // parallaxEffectX = 0.5 → background kameranın yarı hızında hareket eder
        float parallaxX = deltaMovement.x * (1 - parallaxEffectX);
        float parallaxY = deltaMovement.y * (1 - parallaxEffectY);

        transform.position += new Vector3(parallaxX, parallaxY, 0);

        lastCameraPosition = cameraTransform.position;

        float distX = cameraTransform.position.x - transform.position.x;
        if (Mathf.Abs(distX) >= textureUnitSizeX)
        {
            float offsetX = distX > 0 ? textureUnitSizeX : -textureUnitSizeX;
            transform.position += new Vector3(offsetX, 0, 0);
        }

        float distY = cameraTransform.position.y - transform.position.y;
        if (Mathf.Abs(distY) >= textureUnitSizeY)
        {
            float offsetY = distY > 0 ? textureUnitSizeY : -textureUnitSizeY;
            transform.position += new Vector3(0, offsetY, 0);
        }
    }
}
