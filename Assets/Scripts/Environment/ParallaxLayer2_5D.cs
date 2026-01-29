using UnityEngine;

/// <summary>
/// 2.5D Görsel Derinlik Sistemi - Adım 1
/// Her arka plan katmanına ekle, Z pozisyonuna göre otomatik parallax hesaplar.
/// Uzaktaki objeler daha yavaş hareket eder = derinlik hissi.
/// </summary>
public class ParallaxLayer2_5D : MonoBehaviour
{
    [Header("Derinlik Ayarları")]
    [Tooltip("Bu katmanın Z derinliği. Büyük değer = daha uzak = daha yavaş hareket")]
    [SerializeField] private float depth = 10f;
    
    [Tooltip("Parallax yoğunluğu çarpanı (1 = normal, 0.5 = yarı etki)")]
    [Range(0f, 2f)]
    [SerializeField] private float parallaxMultiplier = 1f;
    
    [Header("Eksen Kontrolü")]
    [SerializeField] private bool parallaxX = true;
    [SerializeField] private bool parallaxY = true;
    
    [Header("Sonsuz Tekrar (Opsiyonel)")]
    [SerializeField] private bool infiniteScrollX = false;
    [SerializeField] private bool infiniteScrollY = false;
    
    [Header("Derinlik Renk Efekti")]
    [Tooltip("Uzaktaki katmanları soluklaştır")]
    [SerializeField] private bool applyDepthTint = false;
    [SerializeField] private Color nearColor = Color.white;
    [SerializeField] private Color farColor = new Color(0.7f, 0.75f, 0.85f, 1f); // Hafif mavi-gri
    [SerializeField] private float maxTintDepth = 50f;
    
    // Internal
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    private Vector3 startPosition;
    private SpriteRenderer spriteRenderer;
    private float textureUnitSizeX;
    private float textureUnitSizeY;
    
    /// <summary>
    /// Depth değerine göre parallax faktörü hesapla.
    /// depth=0 → parallax=1 (kamerayla birlikte hareket, ön plan)
    /// depth=10 → parallax≈0.5 (yarı hızda)
    /// depth=∞ → parallax→0 (sabit, çok uzak)
    /// </summary>
    private float ParallaxFactorX => parallaxX ? (1f - (1f / (1f + depth * 0.1f))) * parallaxMultiplier : 0f;
    private float ParallaxFactorY => parallaxY ? (1f - (1f / (1f + depth * 0.1f))) * parallaxMultiplier : 0f;
    
    private void Start()
    {
        cameraTransform = Camera.main?.transform;
        if (cameraTransform == null)
        {
            Debug.LogError("ParallaxLayer2_5D: Main Camera bulunamadı!");
            enabled = false;
            return;
        }
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        startPosition = transform.position;
        lastCameraPosition = cameraTransform.position;
        
        // Texture boyutlarını hesapla (infinite scroll için)
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Sprite sprite = spriteRenderer.sprite;
            textureUnitSizeX = sprite.bounds.size.x * transform.lossyScale.x;
            textureUnitSizeY = sprite.bounds.size.y * transform.lossyScale.y;
        }
        
        // Başlangıç rengi uygula
        if (applyDepthTint && spriteRenderer != null)
        {
            ApplyDepthTint();
        }
        
        // Z pozisyonunu depth'e göre ayarla (sorting için)
        Vector3 pos = transform.position;
        pos.z = depth;
        transform.position = pos;
    }
    
    private void LateUpdate()
    {
        if (cameraTransform == null) return;
        
        // Kamera hareketi
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        
        // Parallax uygula (depth'e göre hesaplanmış faktörle)
        float moveX = deltaMovement.x * ParallaxFactorX;
        float moveY = deltaMovement.y * ParallaxFactorY;
        
        transform.position += new Vector3(moveX, moveY, 0);
        
        lastCameraPosition = cameraTransform.position;
        
        // Sonsuz scroll
        if (infiniteScrollX && textureUnitSizeX > 0)
        {
            float distX = cameraTransform.position.x - transform.position.x;
            if (Mathf.Abs(distX) >= textureUnitSizeX)
            {
                float offsetX = distX > 0 ? textureUnitSizeX : -textureUnitSizeX;
                transform.position += new Vector3(offsetX, 0, 0);
            }
        }
        
        if (infiniteScrollY && textureUnitSizeY > 0)
        {
            float distY = cameraTransform.position.y - transform.position.y;
            if (Mathf.Abs(distY) >= textureUnitSizeY)
            {
                float offsetY = distY > 0 ? textureUnitSizeY : -textureUnitSizeY;
                transform.position += new Vector3(0, offsetY, 0);
            }
        }
    }
    
    private void ApplyDepthTint()
    {
        if (spriteRenderer == null) return;
        
        float t = Mathf.Clamp01(depth / maxTintDepth);
        spriteRenderer.color = Color.Lerp(nearColor, farColor, t);
    }
    
    /// <summary>
    /// Runtime'da depth değiştirmek için
    /// </summary>
    public void SetDepth(float newDepth)
    {
        depth = newDepth;
        
        Vector3 pos = transform.position;
        pos.z = depth;
        transform.position = pos;
        
        if (applyDepthTint)
        {
            ApplyDepthTint();
        }
    }
    
    /// <summary>
    /// Editor'da görselleştirme
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Derinlik göstergesi
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Vector3 pos = transform.position;
        pos.z = depth;
        
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector3 size = spriteRenderer.sprite.bounds.size;
            size.x *= transform.lossyScale.x;
            size.y *= transform.lossyScale.y;
            size.z = 0.1f;
            Gizmos.DrawWireCube(pos, size);
        }
        
        // Z-depth çizgisi
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, pos);
    }
}
