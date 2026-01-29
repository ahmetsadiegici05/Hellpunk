using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Smooth Midground Parallax
/// Kamera takibi ile pürüzsüz hareket
/// </summary>
[ExecuteInEditMode]
public class MidgroundController : MonoBehaviour
{
    [Header("Parallax")]
    [Range(0f, 0.95f)]
    [Tooltip("0 = kamerayla aynı (parallax yok), 0.5 = yarı hızda, 0.9 = çok yavaş")]
    public float parallaxFactor = 0.5f;

    [Header("Tile Ayarları")]
    [Tooltip("Toplam kaç tile (tek sayı: 5, 7, 9)")]
    public int totalTiles = 7;
    
    [Header("Görünüm")]
    public int sortingOrder = -50;
    
    [Tooltip("Tile'ların rengi - Inspector'dan değiştirin")]
    public Color tileColor = Color.white;
    
    [Header("Pozisyon")]
    public float yPosition = -2f;
    public float zDepth = 10f;

    private Camera mainCamera;
    private SpriteRenderer mainRenderer;
    private float spriteWidth;
    private List<SpriteRenderer> allTiles = new List<SpriteRenderer>();
    private bool initialized = false;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (initialized) return;
        
        mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        mainRenderer = GetComponent<SpriteRenderer>();
        if (mainRenderer == null || mainRenderer.sprite == null)
        {
            Debug.LogError("[MidgroundController] SpriteRenderer veya Sprite yok!");
            enabled = false;
            return;
        }

        // Sorting ayarla
        mainRenderer.sortingOrder = sortingOrder;
        
        // Sprite genişliği (scale dahil)
        spriteWidth = mainRenderer.sprite.bounds.size.x * transform.localScale.x;

        // Eski tile'ları temizle
        ClearOldTiles();
        
        // Tile'ları oluştur
        CreateAllTiles();
        
        initialized = true;
    }

    void ClearOldTiles()
    {
        // Eski child'ları sil
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
        allTiles.Clear();
    }

    void CreateAllTiles()
    {
        // Ana renderer'ı ekle
        allTiles.Add(mainRenderer);

        int sidesCount = (totalTiles - 1) / 2;

        for (int i = 1; i <= sidesCount; i++)
        {
            // Sağ tile
            allTiles.Add(CreateTile(i));
            // Sol tile  
            allTiles.Add(CreateTile(-i));
        }
    }

    SpriteRenderer CreateTile(int index)
    {
        GameObject tile = new GameObject($"Tile_{index}");
        tile.transform.SetParent(transform);
        // Önemli: localPosition kullan ve SCALE'i hesaba kat
        tile.transform.localPosition = new Vector3(
            index * mainRenderer.sprite.bounds.size.x, // Scale zaten parent'ta
            0, 
            0
        );
        tile.transform.localScale = Vector3.one;
        tile.transform.localRotation = Quaternion.identity;

        SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
        sr.sprite = mainRenderer.sprite;
        sr.sortingOrder = sortingOrder;
        sr.color = tileColor;

        return sr;
    }

    void LateUpdate()
    {
        if (mainCamera == null) 
        {
            mainCamera = Camera.main;
            return;
        }
        
        if (!initialized) Initialize();

        // Kamera X pozisyonu
        float camX = mainCamera.transform.position.x;
        
        // Parallax hesapla:
        // parallaxFactor = 0 → midground kamerayla aynı hızda gider (ekranda sabit)
        // parallaxFactor = 0.5 → midground kameranın yarı hızında gider
        // parallaxFactor = 1 → midground hiç hareket etmez (dünyada sabit)
        
        // Midground'un X pozisyonu = kamera pozisyonu * parallaxFactor
        // Bu şekilde kamera sağa gidince midground SOLA KAYAR (parallax efekti)
        float newX = camX * parallaxFactor;
        
        // Pozisyonu güncelle (Y ve Z sabit)
        transform.position = new Vector3(newX, yPosition, zDepth);
        
        // Tüm tile'ların rengini senkronize tut
        SyncColors();
    }
    
    void SyncColors()
    {
        // Ana renderer'ın rengini tileColor ile senkronize et
        if (mainRenderer != null && mainRenderer.color != tileColor)
        {
            mainRenderer.color = tileColor;
        }
        
        // Tüm tile'ları güncelle
        foreach (var sr in allTiles)
        {
            if (sr != null && sr.color != tileColor)
            {
                sr.color = tileColor;
            }
        }
    }

    // Inspector'da değer değişince güncelle
    void OnValidate()
    {
        if (!Application.isPlaying) return;
        
        mainRenderer = GetComponent<SpriteRenderer>();
        if (mainRenderer != null)
        {
            mainRenderer.sortingOrder = sortingOrder;
            mainRenderer.color = tileColor;
            
            foreach (var sr in allTiles)
            {
                if (sr != null)
                {
                    sr.sortingOrder = sortingOrder;
                    sr.color = tileColor;
                }
            }
        }
        
        transform.position = new Vector3(transform.position.x, yPosition, zDepth);
    }
    
    /// <summary>
    /// Dışarıdan renk değiştirmek için kullan
    /// </summary>
    public void SetColor(Color newColor)
    {
        tileColor = newColor;
        SyncColors();
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        float width = sr.sprite.bounds.size.x * transform.localScale.x;
        float height = sr.sprite.bounds.size.y * transform.localScale.y;
        
        Gizmos.color = new Color(0.5f, 0, 0.5f, 0.3f);
        
        int sides = (totalTiles - 1) / 2;
        for (int i = -sides; i <= sides; i++)
        {
            Vector3 center = new Vector3(transform.position.x + i * width, yPosition, zDepth);
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0.1f));
        }
    }
#endif
}
