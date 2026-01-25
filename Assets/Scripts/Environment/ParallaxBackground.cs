using UnityEngine;

/// <summary>
/// Parallax arka plan sistemi - Kamera hareket ettikçe arka plan da hareket eder
/// ve sonsuz tekrar (infinite scrolling) sağlar. Kamera görüş alanını her zaman kaplar.
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Ayarları")]
    [Tooltip("Parallax efekt miktarı. 0 = sabit, 1 = kamerayla aynı hızda")]
    [Range(0f, 1f)]
    public float parallaxEffectX = 0.5f;
    
    [Tooltip("Dikey parallax efekt miktarı")]
    [Range(0f, 1f)]
    public float parallaxEffectY = 0.3f;
    
    [Header("Tekrarlama Ayarları")]
    [Tooltip("Arka planın yatay olarak tekrarlanmasını aktif eder")]
    public bool infiniteHorizontal = true;
    
    [Tooltip("Arka planın dikey olarak tekrarlanmasını aktif eder")]
    public bool infiniteVertical = true;
    
    [Header("Ölçek Ayarları")]
    [Tooltip("Arka planın minimum ölçeği")]
    public float minScale = 3f;
    
    [Tooltip("Ekstra ölçek çarpanı")]
    public float scaleMultiplier = 1.5f;
    
    [Tooltip("Kopyalar arası overlap (boşluk önleme)")]
    public float overlapAmount = 0.02f;
    
    [Header("Referanslar")]
    [Tooltip("Boş bırakılırsa Main Camera otomatik bulunur")]
    public Transform cameraTransform;
    
    private float spriteWidth;
    private float spriteHeight;
    private Vector3 lastCameraPosition;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    
    // Arka plan kopyaları (3x3 grid - tüm yönleri kapsar)
    private GameObject[,] copies = new GameObject[3, 3];

    void Start()
    {
        // Kamera referansı
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
        }
        
        if (cameraTransform == null)
        {
            Debug.LogError("ParallaxBackground: Kamera bulunamadı!");
            enabled = false;
            return;
        }
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning("ParallaxBackground: SpriteRenderer veya Sprite bulunamadı!");
            enabled = false;
            return;
        }
        
        // Kamera görüş alanına göre ölçekle
        AutoScaleToCamera();
        
        lastCameraPosition = cameraTransform.position;
        
        // Sprite boyutlarını al (scale uygulandıktan sonra)
        spriteWidth = spriteRenderer.bounds.size.x;
        spriteHeight = spriteRenderer.bounds.size.y;
        
        // Pozisyonu kameraya göre ayarla
        transform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y, transform.position.z);
        
        // 3x3 grid oluştur (merkez + 8 komşu)
        CreateBackgroundGrid();
        
        Debug.Log($"ParallaxBackground: Ölçek={transform.localScale}, Boyut={spriteWidth}x{spriteHeight}");
    }
    
    void AutoScaleToCamera()
    {
        if (mainCamera == null || spriteRenderer == null) return;
        
        // Kameranın görüş alanı boyutlarını hesapla
        float cameraHeight = mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * mainCamera.aspect;
        
        // Sprite'ın orijinal boyutları (pixels per unit hesaba katılarak)
        float spriteOriginalWidth = spriteRenderer.sprite.bounds.size.x;
        float spriteOriginalHeight = spriteRenderer.sprite.bounds.size.y;
        
        // Kamerayı kaplamak için gereken ölçek
        float scaleX = (cameraWidth / spriteOriginalWidth) * scaleMultiplier;
        float scaleY = (cameraHeight / spriteOriginalHeight) * scaleMultiplier;
        
        // En büyük ölçeği kullan (tam kaplama için)
        float finalScale = Mathf.Max(scaleX, scaleY, minScale);
        
        transform.localScale = new Vector3(finalScale, finalScale, 1f);
        
        Debug.Log($"ParallaxBackground: Camera size={cameraWidth}x{cameraHeight}, Sprite size={spriteOriginalWidth}x{spriteOriginalHeight}, Final scale={finalScale}");
    }
    
    void CreateBackgroundGrid()
    {
        // Overlap için boyutları biraz küçült (kopyalar üst üste binsin)
        float effectiveWidth = spriteWidth * (1f - overlapAmount);
        float effectiveHeight = spriteHeight * (1f - overlapAmount);
        
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Merkez = ana obje
                
                GameObject copy = CreateBackgroundCopy($"BG_{x}_{y}");
                copy.transform.position = transform.position + new Vector3(x * effectiveWidth, y * effectiveHeight, 0);
                copies[x + 1, y + 1] = copy;
            }
        }
    }
    
    GameObject CreateBackgroundCopy(string name)
    {
        GameObject copy = new GameObject(name);
        copy.transform.SetParent(transform.parent); // Aynı parent'a ekle
        copy.transform.localScale = transform.localScale;
        
        SpriteRenderer copyRenderer = copy.AddComponent<SpriteRenderer>();
        copyRenderer.sprite = spriteRenderer.sprite;
        copyRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        copyRenderer.sortingOrder = spriteRenderer.sortingOrder;
        copyRenderer.color = spriteRenderer.color;
        
        return copy;
    }
    
    void LateUpdate()
    {
        if (cameraTransform == null) return;
        
        // Kamera hareket miktarı
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        
        // Parallax hareketi uygula
        float moveX = deltaMovement.x * parallaxEffectX;
        float moveY = deltaMovement.y * parallaxEffectY;
        
        transform.position += new Vector3(moveX, moveY, 0);
        
        // Sonsuz tekrar kontrolü
        CheckAndRepositionGrid();
        
        lastCameraPosition = cameraTransform.position;
    }
    
    void CheckAndRepositionGrid()
    {
        float effectiveWidth = spriteWidth * (1f - overlapAmount);
        float effectiveHeight = spriteHeight * (1f - overlapAmount);
        
        // Kamera ile arka plan merkezi arasındaki mesafe
        float distX = cameraTransform.position.x - transform.position.x;
        float distY = cameraTransform.position.y - transform.position.y;
        
        // Yatay reposition
        if (infiniteHorizontal)
        {
            if (distX > effectiveWidth * 0.5f)
            {
                ShiftGrid(1, 0);
            }
            else if (distX < -effectiveWidth * 0.5f)
            {
                ShiftGrid(-1, 0);
            }
        }
        
        // Dikey reposition
        if (infiniteVertical)
        {
            if (distY > effectiveHeight * 0.5f)
            {
                ShiftGrid(0, 1);
            }
            else if (distY < -effectiveHeight * 0.5f)
            {
                ShiftGrid(0, -1);
            }
        }
    }
    
    void ShiftGrid(int shiftX, int shiftY)
    {
        float effectiveWidth = spriteWidth * (1f - overlapAmount);
        float effectiveHeight = spriteHeight * (1f - overlapAmount);
        
        // Ana objeyi taşı
        transform.position += new Vector3(shiftX * effectiveWidth, shiftY * effectiveHeight, 0);
        
        // Tüm kopyaları güncelle
        UpdateCopyPositions();
    }
    
    void UpdateCopyPositions()
    {
        float effectiveWidth = spriteWidth * (1f - overlapAmount);
        float effectiveHeight = spriteHeight * (1f - overlapAmount);
        
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                
                if (copies[x + 1, y + 1] != null)
                {
                    copies[x + 1, y + 1].transform.position = transform.position + new Vector3(x * effectiveWidth, y * effectiveHeight, 0);
                }
            }
        }
    }
    
    void OnDestroy()
    {
        // Kopyaları temizle
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (copies[x, y] != null)
                {
                    Destroy(copies[x, y]);
                }
            }
        }
    }
    
    /// <summary>
    /// Editor'da görselleştirme
    /// </summary>
    void OnDrawGizmosSelected()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float w = sr.bounds.size.x;
            float h = sr.bounds.size.y;
            
            // Merkez
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, new Vector3(w, h, 0));
            
            // Grid
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    Gizmos.DrawWireCube(transform.position + new Vector3(x * w, y * h, 0), new Vector3(w, h, 0));
                }
            }
        }
    }
}
