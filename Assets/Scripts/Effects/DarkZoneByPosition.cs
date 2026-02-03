using UnityEngine;

/// <summary>
/// Pozisyon bazlı karanlık bölge - Trigger gerektirmez!
/// GameObject'in Transform pozisyonunu baz alır.
/// 
/// KULLANIM:
/// 1. Boş GameObject oluştur (isim: "DarkZone")
/// 2. Bu script'i ekle
/// 3. GameObject'i sahneye yerleştir (X pozisyonu = bölgenin merkezi)
/// 4. Inspector'dan zoneWidth ile bölge genişliğini ayarla
/// 5. Objeyi sürükleyerek bölgeyi hareket ettirebilirsin!
/// </summary>
public class DarkZoneByPosition : MonoBehaviour
{
    [Header("Bölge Ayarları")]
    [Tooltip("Bölgenin toplam genişliği. Objenin X pozisyonu merkez olur.")]
    [SerializeField] private float zoneWidth = 40f;
    
    [Tooltip("Eski sistem için - Transform pozisyonunu yoksay ve bu değerleri kullan")]
    [SerializeField] private bool useManualCoordinates = false;
    
    [Tooltip("Manuel mod: Başlangıç X koordinatı")]
    [SerializeField] private float manualStartX = 10f;
    
    [Tooltip("Manuel mod: Bitiş X koordinatı")]
    [SerializeField] private float manualEndX = 50f;
    
    [Header("Karanlık Ayarları")]
    [Tooltip("Görüş genişliği (piksel). Küçük = daha dar görüş")]
    [Range(50f, 800f)]
    [SerializeField] private float visionWidth = 300f;
    
    [Tooltip("Görüş yüksekliği (piksel). Küçük = daha dar görüş")]
    [Range(50f, 500f)]
    [SerializeField] private float visionHeight = 200f;
    
    [Tooltip("Karanlık yoğunluğu. 1 = tamamen karanlık")]
    [Range(0.5f, 1f)]
    [SerializeField] private float darkIntensity = 0.95f;
    
    [Tooltip("Karanlık rengi")]
    [SerializeField] private Color darkColor = new Color(0.02f, 0.01f, 0.05f, 1f);
    
    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0.5f, 0f, 0.5f, 0.3f);
    [SerializeField] private bool showDebugLogs = false;
    
    // Hesaplanan sınırlar
    private float StartX => useManualCoordinates ? manualStartX : transform.position.x - zoneWidth / 2f;
    private float EndX => useManualCoordinates ? manualEndX : transform.position.x + zoneWidth / 2f;
    
    // State
    private bool playerInZone = false;
    private Transform playerTransform;
    
    private void Start()
    {
        // SimpleDarkOverlay'in varlığını garantile
        EnsureDarkOverlay();
        
        Debug.Log($"[DarkZoneByPosition] Başlatıldı: X={StartX:F1} - {EndX:F1} (Genişlik: {zoneWidth})");
    }
    
    private void Update()
    {
        // Player'ı bul
        if (playerTransform == null)
        {
            FindPlayer();
            if (playerTransform == null) return;
        }
        
        // Oyuncunun X pozisyonunu kontrol et
        float playerX = playerTransform.position.x;
        bool isInZone = playerX >= StartX && playerX <= EndX;
        
        // Durum değişikliği
        if (isInZone && !playerInZone)
        {
            // Bölgeye girdi
            playerInZone = true;
            ActivateDarkness();
            
            if (showDebugLogs)
                Debug.Log($"[DarkZoneByPosition] Oyuncu KARANLIK BÖLGEYE GİRDİ! X={playerX:F1}");
        }
        else if (!isInZone && playerInZone)
        {
            // Bölgeden çıktı
            playerInZone = false;
            DeactivateDarkness();
            
            if (showDebugLogs)
                Debug.Log($"[DarkZoneByPosition] Oyuncu karanlık bölgeden ÇIKTI! X={playerX:F1}");
        }
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
                playerTransform = player.transform;
        }
    }
    
    private void EnsureDarkOverlay()
    {
        if (SimpleDarkOverlay.Instance == null)
        {
            GameObject darkObj = new GameObject("SimpleDarkOverlay");
            darkObj.AddComponent<SimpleDarkOverlay>();
            Debug.Log("[DarkZoneByPosition] SimpleDarkOverlay oluşturuldu!");
        }
    }
    
    private void ActivateDarkness()
    {
        EnsureDarkOverlay();
        
        if (SimpleDarkOverlay.Instance != null)
        {
            SimpleDarkOverlay.Instance.SetVisionSize(visionWidth, visionHeight);
            SimpleDarkOverlay.Instance.SetDarkIntensity(darkIntensity);
            SimpleDarkOverlay.Instance.SetDarkColor(darkColor);
            SimpleDarkOverlay.Instance.Activate();
        }
        
        // Şık bildirim göster
        EnsureNotification();
        if (DarkZoneNotification.Instance != null)
        {
            DarkZoneNotification.Instance.ShowEnterNotification();
        }
    }
    
    private void EnsureNotification()
    {
        if (DarkZoneNotification.Instance == null)
        {
            GameObject notifObj = new GameObject("DarkZoneNotification");
            notifObj.AddComponent<DarkZoneNotification>();
        }
    }
    
    private void DeactivateDarkness()
    {
        if (SimpleDarkOverlay.Instance != null)
        {
            SimpleDarkOverlay.Instance.Deactivate();
        }
    }
    
    // Scene view'da görselleştirme
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        Gizmos.color = gizmoColor;
        
        // Hesaplanan sınırları kullan
        float currentStartX = StartX;
        float currentEndX = EndX;
        
        // Bölgeyi çiz (dikey çizgiler ve dolgu)
        float height = 20f; // Görsel yükseklik
        float y = transform.position.y;
        
        // Sol sınır çizgisi
        Gizmos.DrawLine(new Vector3(currentStartX, y - height/2, 0), new Vector3(currentStartX, y + height/2, 0));
        
        // Sağ sınır çizgisi
        Gizmos.DrawLine(new Vector3(currentEndX, y - height/2, 0), new Vector3(currentEndX, y + height/2, 0));
        
        // Dolgu kutusu
        Vector3 center = new Vector3((currentStartX + currentEndX) / 2f, y, 0);
        Vector3 size = new Vector3(currentEndX - currentStartX, height, 0.1f);
        Gizmos.DrawCube(center, size);
        
        // Kenar çizgileri (daha belirgin)
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
        Gizmos.DrawWireCube(center, size);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Seçildiğinde daha belirgin göster
        Gizmos.color = new Color(1f, 0f, 1f, 0.5f);
        
        float currentStartX = StartX;
        float currentEndX = EndX;
        
        float height = 20f;
        float y = transform.position.y;
        
        Vector3 center = new Vector3((currentStartX + currentEndX) / 2f, y, 0);
        Vector3 size = new Vector3(currentEndX - currentStartX, height, 0.1f);
        Gizmos.DrawWireCube(center, size);
        
        // Sınır noktalarını göster
        Gizmos.DrawSphere(new Vector3(currentStartX, y, 0), 0.5f);
        Gizmos.DrawSphere(new Vector3(currentEndX, y, 0), 0.5f);
    }
}
