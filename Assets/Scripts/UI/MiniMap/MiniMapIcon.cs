using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mini-map üzerindeki tek bir ikon. Takip ettiği objenin pozisyonunu gösterir.
/// </summary>
public class MiniMapIcon : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    [SerializeField] private bool enablePulse = false;
    [SerializeField] private bool rotateWithTarget = true;
    
    // Components
    private Image iconImage;
    private RectTransform rectTransform;
    private Transform target;
    private MiniMapIconType iconType;
    private float baseScale;
    
    // Animation
    private float pulseTimer;
    
    /// <summary>
    /// İkonu başlat
    /// </summary>
    public void Initialize(Transform targetTransform, MiniMapIconType type, Sprite sprite, Color color, float scale)
    {
        target = targetTransform;
        iconType = type;
        baseScale = scale;
        
        // RectTransform ayarla
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }
        
        // Image component ekle
        iconImage = gameObject.AddComponent<Image>();
        
        // Sprite varsa kullan, yoksa varsayılan şekil oluştur
        if (sprite != null)
        {
            iconImage.sprite = sprite;
        }
        else
        {
            // Varsayılan ikon (daire veya kare)
            CreateDefaultIcon(type);
        }
        
        iconImage.color = color;
        iconImage.raycastTarget = false;
        
        // Boyut ayarla
        float size = GetIconSize(type) * scale;
        rectTransform.sizeDelta = new Vector2(size, size);
        
        // Oyuncu için pulse efekti
        if (type == MiniMapIconType.Player || type == MiniMapIconType.Boss)
        {
            enablePulse = true;
        }
    }

    private void CreateDefaultIcon(MiniMapIconType type)
    {
        // Basit bir kare/daire şekli oluştur
        // Unity'nin varsayılan UI sprite'ını kullan
        iconImage.sprite = null; // Varsayılan beyaz kare
        
        // Tip bazlı şekil (outline vs filled)
        switch (type)
        {
            case MiniMapIconType.Player:
                // Oyuncu için ok şekli (yukarı bakan üçgen gibi)
                // Sprite olmadan sadece renk kullanıyoruz
                break;
            case MiniMapIconType.Enemy:
            case MiniMapIconType.Boss:
                // Düşmanlar için dolu kare
                break;
            case MiniMapIconType.Checkpoint:
            case MiniMapIconType.Exit:
                // Önemli noktalar için daha büyük
                break;
        }
    }

    private float GetIconSize(MiniMapIconType type)
    {
        switch (type)
        {
            case MiniMapIconType.Player:
                return 20f;
            case MiniMapIconType.Enemy:
                return 12f;
            case MiniMapIconType.Boss:
                return 25f;
            case MiniMapIconType.Checkpoint:
                return 15f;
            case MiniMapIconType.Exit:
                return 18f;
            case MiniMapIconType.Collectible:
                return 10f;
            default:
                return 12f;
        }
    }

    private void Update()
    {
        if (target == null)
        {
            // Hedef yok ise kendini yok et
            Destroy(gameObject);
            return;
        }
        
        // Pulse animasyonu
        if (enablePulse)
        {
            pulseTimer += Time.unscaledDeltaTime * pulseSpeed;
            float pulse = 1f + Mathf.Sin(pulseTimer) * pulseAmount;
            float size = GetIconSize(iconType) * baseScale * pulse;
            rectTransform.sizeDelta = new Vector2(size, size);
        }
    }

    /// <summary>
    /// İkon pozisyonunu güncelle (MiniMapSystem tarafından çağrılır)
    /// </summary>
    public void UpdatePosition(Camera miniMapCamera, RectTransform miniMapRect)
    {
        if (target == null || miniMapCamera == null || miniMapRect == null) return;
        
        // Hedefin dünya pozisyonunu mini-map koordinatlarına çevir
        Vector3 targetPos = target.position;
        
        // Kamera pozisyonuna göre relatif pozisyon
        Vector3 camPos = miniMapCamera.transform.position;
        float orthoSize = miniMapCamera.orthographicSize;
        
        // Dünya koordinatlarından mini-map koordinatlarına
        float relativeX = (targetPos.x - camPos.x) / (orthoSize * 2f);
        float relativeY = (targetPos.z - camPos.z) / (orthoSize * 2f); // 2D oyun için z yerine y kullanılabilir
        
        // 2D oyun için y eksenini kullan (yukarıdan bakış)
        // Eğer 2D side-scroller ise x ve y kullan
        relativeY = (targetPos.y - camPos.y + miniMapCamera.transform.position.y) / (orthoSize * 2f);
        
        // Mini-map boyutuna göre pozisyon
        float mapWidth = miniMapRect.sizeDelta.x;
        float mapHeight = miniMapRect.sizeDelta.y;
        
        // Pozisyonu sınırla (mini-map dışına çıkmasın)
        relativeX = Mathf.Clamp(relativeX, -0.5f, 0.5f);
        relativeY = Mathf.Clamp(relativeY, -0.5f, 0.5f);
        
        // Anchored position ayarla
        rectTransform.anchoredPosition = new Vector2(
            relativeX * mapWidth,
            relativeY * mapHeight
        );
        
        // Rotasyon (opsiyonel - oyuncu yönünü göster)
        if (rotateWithTarget && iconType == MiniMapIconType.Player)
        {
            // 2D oyun için sprite'ın baktığı yönü kullan
            SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                float rotation = sr.flipX ? 90f : -90f;
                rectTransform.localRotation = Quaternion.Euler(0, 0, rotation);
            }
        }
    }

    /// <summary>
    /// İkon rengini değiştir
    /// </summary>
    public void SetColor(Color color)
    {
        if (iconImage != null)
        {
            iconImage.color = color;
        }
    }

    /// <summary>
    /// İkonu göster/gizle
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>
    /// Pulse efektini aç/kapat
    /// </summary>
    public void SetPulse(bool enabled)
    {
        enablePulse = enabled;
        if (!enabled)
        {
            // Varsayılan boyuta dön
            float size = GetIconSize(iconType) * baseScale;
            rectTransform.sizeDelta = new Vector2(size, size);
        }
    }
}
