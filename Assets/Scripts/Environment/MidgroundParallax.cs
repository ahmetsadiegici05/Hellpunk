using UnityEngine;

/// <summary>
/// Basit ve stabil Midground Parallax
/// Sadece X ekseninde parallax, Y sabit kalır
/// Sonsuz yatay tekrar destekler
/// </summary>
public class MidgroundParallax : MonoBehaviour
{
    [Header("Parallax Ayarları")]
    [Range(0f, 1f)]
    [Tooltip("0 = kamerayla aynı hızda, 1 = sabit durur. Midground için 0.5-0.7 önerilir")]
    public float parallaxAmount = 0.5f;

    [Header("Görünüm")]
    [Tooltip("Sorting Order - negatif = arkada")]
    public int sortingOrder = -50;
    
    [Range(0f, 1f)]
    [Tooltip("Şeffaflık")]
    public float alpha = 1f;

    [Header("Yatay Tekrar")]
    [Tooltip("Sonsuz yatay tekrar için aç")]
    public bool infiniteScrollX = true;
    
    [Tooltip("Kaç adet tile oluşturulsun (sağ ve sol)")]
    public int tilesPerSide = 2;

    private Transform cameraTransform;
    private SpriteRenderer spriteRenderer;
    private float spriteWidth;
    private float startPosX;
    private float startPosY;
    private GameObject[] tiles;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogError("MidgroundParallax: SpriteRenderer veya Sprite yok!");
            enabled = false;
            return;
        }

        // Sorting ve alpha ayarla
        spriteRenderer.sortingOrder = sortingOrder;
        Color c = spriteRenderer.color;
        c.a = alpha;
        spriteRenderer.color = c;

        // Boyutları hesapla
        spriteWidth = spriteRenderer.bounds.size.x;
        startPosX = transform.position.x;
        startPosY = transform.position.y; // Y sabit kalacak

        // Tile'ları oluştur
        if (infiniteScrollX)
        {
            CreateHorizontalTiles();
        }
    }

    void CreateHorizontalTiles()
    {
        int totalTiles = tilesPerSide * 2; // Sol ve sağ için
        tiles = new GameObject[totalTiles];
        
        int index = 0;
        for (int i = -tilesPerSide; i < tilesPerSide; i++)
        {
            if (i == 0) continue; // Merkez zaten var
            
            GameObject tile = new GameObject($"MidTile_{i}");
            tile.transform.SetParent(transform.parent); // Parent'ın altına, bu objenin değil
            tile.transform.position = new Vector3(
                transform.position.x + (i * spriteWidth),
                transform.position.y,
                transform.position.z
            );
            tile.transform.localScale = transform.localScale;

            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = spriteRenderer.sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = spriteRenderer.color;
            
            // Tile'a da parallax ekle (ama tile oluşturmasın)
            MidgroundTile tileScript = tile.AddComponent<MidgroundTile>();
            tileScript.Initialize(this, i);

            if (index < tiles.Length)
                tiles[index++] = tile;
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Sadece X parallax - Y sabit kalır (titreme yok!)
        float distanceX = cameraTransform.position.x * parallaxAmount;
        float newPosX = startPosX + distanceX;

        transform.position = new Vector3(newPosX, startPosY, transform.position.z);

        // Sonsuz scroll için pozisyon sıfırlama
        if (infiniteScrollX)
        {
            float relativeX = cameraTransform.position.x * (1 - parallaxAmount);
            
            // Kamera çok uzaklaştığında pozisyonu sıfırla
            if (Mathf.Abs(transform.position.x - cameraTransform.position.x) > spriteWidth * tilesPerSide)
            {
                startPosX += Mathf.Sign(cameraTransform.position.x - transform.position.x) * spriteWidth;
            }
        }
    }

    void OnDestroy()
    {
        // Tile'ları temizle
        if (tiles != null)
        {
            foreach (var tile in tiles)
            {
                if (tile != null)
                    Destroy(tile);
            }
        }
    }
}

/// <summary>
/// Midground tile'ları için yardımcı script
/// Ana objeyi takip eder
/// </summary>
public class MidgroundTile : MonoBehaviour
{
    private MidgroundParallax parent;
    private int offset;
    private float spriteWidth;
    private SpriteRenderer sr;

    public void Initialize(MidgroundParallax parentScript, int tileOffset)
    {
        parent = parentScript;
        offset = tileOffset;
        sr = GetComponent<SpriteRenderer>();
        spriteWidth = sr.bounds.size.x;
    }

    void LateUpdate()
    {
        if (parent == null) return;

        // Parent'ın pozisyonunu takip et, offset kadar kaydır
        Vector3 parentPos = parent.transform.position;
        transform.position = new Vector3(
            parentPos.x + (offset * spriteWidth),
            parentPos.y,
            parentPos.z
        );
    }
}
