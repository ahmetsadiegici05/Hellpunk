using UnityEngine;

/// <summary>
/// PortalPoint objesine eklenir.
/// Eğer parent rotasyondan etkileniyorsa, rotasyon tamamlandıktan sonra portal spawn eder.
/// Aksi halde sahne yüklendiğinde normal şekilde spawn edilir.
/// </summary>
public class PortalPointTrigger : MonoBehaviour
{
    [Header("Spawn Ayarları")]
    [Tooltip("True ise rotasyon tamamlandıktan sonra spawn eder")]
    [SerializeField] private bool waitForRotation = true;
    
    [Tooltip("Spawn gecikmesi (saniye)")]
    [SerializeField] private float spawnDelay = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = false;
    
    private bool hasSpawned = false;
    private GameObject spawnedPortal;
    
    private void Start()
    {
        // Tag'i ayarla
        if (gameObject.tag != "PortalPoint")
        {
            gameObject.tag = "PortalPoint";
        }
        
        // Eğer rotasyon beklemiyorsa, normal spawn sistemi çalışacak (GameManager)
        // Eğer rotasyon bekliyorsa, OnBecameVisible veya manuel trigger kullan
        if (!waitForRotation)
        {
            if (debugMode) Debug.Log($"[PortalPointTrigger] {name}: Normal spawn modu (GameManager tarafından spawn edilecek)");
            return;
        }
        
        // Rotasyon sonrası spawn için GameManager'ın spawn etmesini engelle
        // Bu noktayı kendi kendine spawn edecek
        gameObject.tag = "PortalPointDelayed"; // Geçici tag değiştir
        
        if (debugMode) Debug.Log($"[PortalPointTrigger] {name}: Delayed spawn modu, rotasyon bekliyor");
    }
    
    /// <summary>
    /// Rotasyon tamamlandığında çağrılır (RotateOnTrigger tarafından veya manuel)
    /// </summary>
    public void OnRotationComplete()
    {
        if (hasSpawned) return;
        
        if (debugMode) Debug.Log($"[PortalPointTrigger] {name}: Rotasyon tamamlandı, portal spawn ediliyor...");
        
        Invoke(nameof(SpawnPortal), spawnDelay);
    }
    
    /// <summary>
    /// Obje görünür olduğunda (kamera görüş alanına girdiğinde)
    /// </summary>
    private void OnBecameVisible()
    {
        if (!waitForRotation || hasSpawned) return;
        
        // Görünür olunca spawn et
        if (debugMode) Debug.Log($"[PortalPointTrigger] {name}: Görünür oldu, portal spawn ediliyor...");
        
        Invoke(nameof(SpawnPortal), spawnDelay);
    }
    
    /// <summary>
    /// Portal spawn eder
    /// </summary>
    private void SpawnPortal()
    {
        if (hasSpawned) return;
        hasSpawned = true;
        
        // GameManager'dan portal prefab'ını al
        if (GameManager.Instance == null || GameManager.Instance.portalPrefab == null)
        {
            Debug.LogWarning($"[PortalPointTrigger] {name}: GameManager veya portalPrefab bulunamadı!");
            return;
        }
        
        // Spawn şansını kontrol et
        if (Random.value > GameManager.Instance.PortalSpawnChance)
        {
            if (debugMode) Debug.Log($"[PortalPointTrigger] {name}: Spawn şansı tutmadı, portal spawn edilmedi");
            return;
        }
        
        spawnedPortal = Instantiate(
            GameManager.Instance.portalPrefab,
            transform.position,
            Quaternion.identity,
            transform
        );
        
        Debug.Log($"[PortalPointTrigger] {name}: Portal spawn edildi! Pozisyon: {transform.position}");
    }
    
    private void OnDestroy()
    {
        // Portal'ı da yok et
        if (spawnedPortal != null)
        {
            Destroy(spawnedPortal);
        }
    }
    
    /// <summary>
    /// Editor'da gizmo göster
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = hasSpawned ? Color.green : (waitForRotation ? Color.yellow : Color.cyan);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // İkon
        Gizmos.color = Color.magenta;
        Gizmos.DrawIcon(transform.position, "Portal", true);
    }
}
