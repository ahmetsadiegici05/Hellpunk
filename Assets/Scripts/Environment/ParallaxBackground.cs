using UnityEngine;

/// <summary>
/// 2D Platformer için optimize edilmiş Parallax arka plan sistemi
/// Yatay panoramik görseller için ideal (1920x1080, 2048x512 vb.)
/// </summary>
public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Ayarları")]
    [Tooltip("Yatay parallax efekt. 0 = sabit, 1 = kamerayla aynı hızda")]
    [Range(0f, 1f)]
    public float parallaxEffectX = 0.5f;
    
    [Tooltip("Dikey parallax efekt (genellikle 0 veya çok düşük)")]
    [Range(0f, 1f)]
    public float parallaxEffectY = 0f;
    
    [Header("Ölçekleme")]
    [Tooltip("Kamera yüksekliğine göre otomatik ölçekle")]
    public bool autoScaleToCamera = true;
    
    [Tooltip("Manuel ölçek (autoScale kapalıysa)")]
    public float manualScale = 5f;
    
    [Tooltip("Ekstra ölçek çarpanı")]
    public float extraScale = 1.2f;
    
    private Transform cameraTransform;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;
    private float textureUnitSizeX;
    private Vector3 lastCameraPosition;
    
    // Sadece yatay 3 tile (sol, merkez, sağ)
    private SpriteRenderer[] tiles;
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
        
        // Ölçeği hesapla ve uygula
        CalculateAndApplyScale();
        
        // Başlangıç pozisyonu
        transform.position = new Vector3(
            cameraTransform.position.x,
            cameraTransform.position.y,
            transform.position.z
        );
        
        lastCameraPosition = cameraTransform.position;
        
        // Sadece yatay 3 tile oluştur (sol, merkez, sağ)
        CreateHorizontalTiles();
        
        Debug.Log($"ParallaxBackground: Scale={appliedScale}, Tile genişliği={textureUnitSizeX}");
    }
    
    void CalculateAndApplyScale()
    {
        Sprite sprite = spriteRenderer.sprite;
        float spriteHeight = sprite.bounds.size.y;
        float spriteWidth = sprite.bounds.size.x;
        
        if (autoScaleToCamera)
        {
            // Kamera yüksekliğini tamamen kaplasın
            float cameraHeight = mainCamera.orthographicSize * 2f;
            appliedScale = (cameraHeight / spriteHeight) * extraScale;
        }
        else
        {
            appliedScale = manualScale;
        }
        
        transform.localScale = new Vector3(appliedScale, appliedScale, 1f);
        
        // Tile genişliğini hesapla
        textureUnitSizeX = spriteWidth * appliedScale;
    }
    
    void CreateHorizontalTiles()
    {
        // Sadece 3 tile: sol (-1), merkez (0), sağ (+1)
        tiles = new SpriteRenderer[3];
        
        for (int i = 0; i < 3; i++)
        {
            int x = i - 1; // -1, 0, 1
            
            if (x == 0)
            {
                // Merkez = ana sprite
                tiles[i] = spriteRenderer;
            }
            else
            {
                // Yatay kopya oluştur
                GameObject tileObj = new GameObject($"Tile_{x}");
                tileObj.transform.SetParent(transform);
                tileObj.transform.localPosition = new Vector3(
                    x * textureUnitSizeX / appliedScale, 
                    0, 
                    0
                );
                tileObj.transform.localScale = Vector3.one;
                tileObj.transform.localRotation = Quaternion.identity;
                
                SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
                sr.sprite = spriteRenderer.sprite;
                sr.sortingLayerID = spriteRenderer.sortingLayerID;
                sr.sortingOrder = spriteRenderer.sortingOrder;
                sr.color = spriteRenderer.color;
                
                tiles[i] = sr;
            }
        }
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;
        
        // Kamera hareket miktarı
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        
        // Parallax hareketi (yatay ağırlıklı)
        transform.position += new Vector3(
            deltaMovement.x * parallaxEffectX,
            deltaMovement.y * parallaxEffectY,
            0
        );
        
        lastCameraPosition = cameraTransform.position;
        
        // Sonsuz yatay tekrar için pozisyon kontrolü
        float distX = cameraTransform.position.x - transform.position.x;
        
        if (Mathf.Abs(distX) >= textureUnitSizeX)
        {
            float offsetX = (distX > 0) ? textureUnitSizeX : -textureUnitSizeX;
            transform.position += new Vector3(offsetX, 0, 0);
        }
    }
    
    void OnDestroy()
    {
        // Child objeler otomatik silinir
    }
    
    void OnDrawGizmosSelected()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;
        
        float scale = autoScaleToCamera ? extraScale : manualScale;
        float w = sr.sprite.bounds.size.x * transform.localScale.x;
        float h = sr.sprite.bounds.size.y * transform.localScale.y;
        
        // Merkez
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, new Vector3(w, h, 0));
        
        // Yatay komşular
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireCube(transform.position + new Vector3(-w, 0, 0), new Vector3(w, h, 0));
        Gizmos.DrawWireCube(transform.position + new Vector3(w, 0, 0), new Vector3(w, h, 0));
    }
}
