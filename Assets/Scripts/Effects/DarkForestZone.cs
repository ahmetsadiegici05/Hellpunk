using UnityEngine;

/// <summary>
/// Dark Forest alanını tanımlayan trigger zone.
/// Oyuncu bu alana girdiğinde görüş kısıtı aktif olur.
/// Level'da Collider2D (Is Trigger) ile birlikte kullanılır.
/// 
/// KULLANIM:
/// 1. Boş GameObject oluştur
/// 2. Bu script'i ekle
/// 3. BoxCollider2D veya PolygonCollider2D ekle
/// 4. Collider'ın "Is Trigger" seçeneğini işaretle
/// 5. Karanlık olmasını istediğin alanı çiz
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DarkForestZone : MonoBehaviour
{
    [Header("Karanlık Ayarları")]
    [Tooltip("Görüş genişliği (piksel cinsinden). Küçük = daha dar görüş")]
    [Range(50f, 800f)]
    [SerializeField] private float customVisionWidth = 300f;
    
    [Tooltip("Görüş yüksekliği (piksel cinsinden). Küçük = daha dar görüş")]
    [Range(50f, 500f)]
    [SerializeField] private float customVisionHeight = 200f;
    
    [Tooltip("Karanlık yoğunluğu. 1 = tamamen karanlık")]
    [Range(0.5f, 1f)]
    [SerializeField] private float customDarkIntensity = 0.95f;
    
    [Tooltip("Karanlık rengi")]
    [SerializeField] private Color customFogColor = new Color(0.02f, 0.01f, 0.05f, 1f);
    
    [Tooltip("Bu zone'un özel ayarlarını kullan")]
    [SerializeField] private bool useCustomSettings = true;
    
    [Header("Geçiş")]
    [SerializeField] private bool smoothTransition = true;
    
    [Header("Alternatif Algılama (Trigger çalışmazsa)")]
    [SerializeField] private bool useOverlapDetection = true;
    [SerializeField] private float detectionInterval = 0.1f;
    
    [Header("Debug")]
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.1f, 0.3f, 0.3f);
    [SerializeField] private bool showDebugLogs = false; // Varsayılan kapalı
    
    private bool playerInside = false;
    private float detectionTimer = 0f;
    private Collider2D zoneCollider;
    
    private void Awake()
    {
        // Collider'ı al ve trigger olduğundan emin ol
        zoneCollider = GetComponent<Collider2D>();
        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
            if (showDebugLogs)
                Debug.Log($"[DarkForestZone] Collider ayarlandı: {zoneCollider.GetType().Name}, isTrigger=true");
        }
        else
        {
            Debug.LogError("[DarkForestZone] Collider2D bulunamadı!");
        }
    }
    
    private void Start()
    {
        // DarkForestEffect'in var olduğundan emin ol
        EnsureDarkForestEffect();
        
        if (showDebugLogs)
            Debug.Log($"[DarkForestZone] Başlatıldı: {gameObject.name}, useOverlapDetection={useOverlapDetection}");
    }
    
    private void Update()
    {
        // Alternatif algılama: Trigger çalışmazsa manuel kontrol
        if (useOverlapDetection && zoneCollider != null)
        {
            detectionTimer -= Time.deltaTime;
            if (detectionTimer <= 0f)
            {
                detectionTimer = detectionInterval;
                CheckPlayerOverlap();
            }
        }
    }
    
    /// <summary>
    /// Manuel overlap kontrolü - Trigger çalışmadığında yedek olarak kullanılır
    /// </summary>
    private void CheckPlayerOverlap()
    {
        Transform playerTransform = null;
        
        // Player'ı bul - TÜM YOLLARI DENE
        if (PlayerMovement.Instance != null)
        {
            playerTransform = PlayerMovement.Instance.transform;
            if (showDebugLogs && Time.frameCount % 300 == 0)
                Debug.Log($"[DarkForestZone] Player bulundu: PlayerMovement.Instance");
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                if (showDebugLogs && Time.frameCount % 300 == 0)
                    Debug.Log($"[DarkForestZone] Player bulundu: Tag ile");
            }
        }
        
        if (playerTransform == null)
        {
            // Her 5 saniyede bir uyarı ver
            if (Time.frameCount % 300 == 0 && showDebugLogs)
                Debug.LogWarning("[DarkForestZone] Player bulunamadı! PlayerMovement.Instance ve 'Player' tag'li obje yok.");
            return;
        }
        
        // Player bu zone içinde mi kontrol et
        Vector2 playerPos = playerTransform.position;
        bool isInside = zoneCollider.OverlapPoint(playerPos);
        
        // Her saniye pozisyon bilgisi
        if (showDebugLogs && Time.frameCount % 60 == 0)
        {
            Bounds bounds = zoneCollider.bounds;
            Debug.Log($"[DarkForestZone] CHECK: Player({playerPos.x:F1}, {playerPos.y:F1}) | Zone({bounds.min.x:F1},{bounds.min.y:F1})-({bounds.max.x:F1},{bounds.max.y:F1}) | Inside={isInside} | State={playerInside}");
        }
        
        // Durum değişikliği
        if (isInside && !playerInside)
        {
            Debug.Log($"[DarkForestZone] ★★★ OYUNCU ZONE'A GİRDİ! ★★★");
            playerInside = true;
            OnPlayerEnter();
        }
        else if (!isInside && playerInside)
        {
            Debug.Log($"[DarkForestZone] ★★★ OYUNCU ZONE'DAN ÇIKTI! ★★★");
            playerInside = false;
            OnPlayerExit();
        }
    }
    
    private void EnsureDarkForestEffect()
    {
        // SimpleDarkOverlay kullan
        if (SimpleDarkOverlay.Instance == null)
        {
            GameObject darkObj = new GameObject("SimpleDarkOverlay");
            darkObj.AddComponent<SimpleDarkOverlay>();
            if (showDebugLogs)
                Debug.Log("[DarkForestZone] SimpleDarkOverlay oluşturuldu!");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (showDebugLogs)
            Debug.Log($"[DarkForestZone] OnTriggerEnter2D çağrıldı! other={other.name}, tag={other.tag}");
        
        // Player kontrolü - tag veya component ile
        bool isPlayer = other.CompareTag("Player") || 
                        other.GetComponent<PlayerMovement>() != null ||
                        other.GetComponentInParent<PlayerMovement>() != null;
        
        if (!isPlayer) return;
        
        if (showDebugLogs)
            Debug.Log("[DarkForestZone] PLAYER TETİKLEDİ!");
        
        playerInside = true;
        OnPlayerEnter();
    }
    
    private void OnPlayerEnter()
    {
        if (showDebugLogs)
            Debug.Log($"[DarkForestZone] OnPlayerEnter() ÇAĞRILDI!");
        
        EnsureDarkForestEffect();
        
        // Bir frame bekle - Instance'ın oluşmasını garantile
        StartCoroutine(ActivateEffectDelayed());
    }
    
    private System.Collections.IEnumerator ActivateEffectDelayed()
    {
        // Instance'ın oluşması için 1 frame bekle
        yield return null;
        
        // SimpleDarkOverlay kullan
        if (SimpleDarkOverlay.Instance == null)
        {
            EnsureDarkForestEffect();
            yield return null;
        }
        
        if (SimpleDarkOverlay.Instance != null)
        {
            if (useCustomSettings)
            {
                SimpleDarkOverlay.Instance.SetVisionSize(customVisionWidth, customVisionHeight);
                SimpleDarkOverlay.Instance.SetDarkIntensity(customDarkIntensity);
                SimpleDarkOverlay.Instance.SetDarkColor(customFogColor);
            }
            
            SimpleDarkOverlay.Instance.Activate();
            if (showDebugLogs)
                Debug.Log($"[DarkForestZone] SimpleDarkOverlay AKTİF! Width={customVisionWidth}, Height={customVisionHeight}, Intensity={customDarkIntensity}");
        }
        else
        {
            Debug.LogError("[DarkForestZone] SimpleDarkOverlay oluşturulamadı!");
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        // Player kontrolü - tag veya component ile
        bool isPlayer = other.CompareTag("Player") || 
                        other.GetComponent<PlayerMovement>() != null ||
                        other.GetComponentInParent<PlayerMovement>() != null;
        
        if (!isPlayer) return;
        
        playerInside = false;
        OnPlayerExit();
    }
    
    private void OnPlayerExit()
    {
        // SimpleDarkOverlay kullan
        if (SimpleDarkOverlay.Instance != null)
        {
            SimpleDarkOverlay.Instance.Deactivate();
            if (showDebugLogs)
                Debug.Log($"[DarkForestZone] SimpleDarkOverlay KAPALI!");
        }
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col is BoxCollider2D box)
        {
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
        }
        else if (col is CircleCollider2D circle)
        {
            Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius);
        }
        else if (col is PolygonCollider2D)
        {
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // Seçildiğinde görüş alanını göster (elips)
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.3f);
        
        float width = useCustomSettings ? customVisionWidth : 300f;
        float height = useCustomSettings ? customVisionHeight : 200f;
        // Ekran pikselinden world unit'e çevir (yaklaşık)
        float worldWidth = width / 100f;
        float worldHeight = height / 100f;
        
        // Elips çiz (wire sphere yerine matrix ile)
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, new Vector3(worldWidth, worldHeight, 1f));
        Gizmos.DrawWireSphere(Vector3.zero, 1f);
        Gizmos.matrix = oldMatrix;
    }
}
