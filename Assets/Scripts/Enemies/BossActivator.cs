using UnityEngine;
using System.Collections;

/// <summary>
/// Level 2'de boss'u aktive eder
/// Oyuncu trigger'a girdiğinde boss spawn olur veya aktive edilir
/// Checkpoint desteği var - eğer checkpoint boss'un ötesindeyse, boss otomatik aktive olur
/// </summary>
public class BossActivator : MonoBehaviour
{
    [Header("Boss Settings")]
    [SerializeField] private GameObject bossObject;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private GameObject bossPrefab; // Prefab'dan spawn için (opsiyonel)
    
    [Header("Trigger Settings")]
    [SerializeField] private bool activateOnTrigger = true;
    [SerializeField] private float activationDelay = 0.5f;
    [SerializeField] private bool showWarningText = true;
    
    [Header("Checkpoint Integration")]
    [Tooltip("Bu trigger'ın X pozisyonu. Checkpoint bunun sağındaysa boss otomatik aktive olur.")]
    [SerializeField] private bool checkCheckpointPosition = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    [Header("Audio")]
    [SerializeField] private AudioClip bossIntroSound;
    
    private bool bossActivated = false;
    private AudioSource audioSource;
    
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Checkpoint kontrolü - eğer oyuncu checkpoint'ten başlıyorsa ve trigger'ın sağındaysa
        if (checkCheckpointPosition && CheckpointData.HasCheckpoint)
        {
            float triggerX = transform.position.x;
            float checkpointX = CheckpointData.LastCheckpointPosition.x;
            
            if (checkpointX > triggerX)
            {
                // Oyuncu trigger'ın sağında spawn olacak - boss'u hemen aktive et
                if (debugMode) Debug.Log($"[BossActivator] Checkpoint ({checkpointX:F1}) trigger'ın ({triggerX:F1}) sağında - Boss otomatik aktive edilecek!");
                bossActivated = true;
                
                // Boss'u hemen aktive et (delay olmadan)
                if (bossObject != null)
                {
                    bossObject.SetActive(true);
                    if (debugMode) Debug.Log("[BossActivator] ✅ Boss checkpoint nedeniyle hemen aktive edildi!");
                }
                return; // Start'ın geri kalanını atla
            }
        }
        
        // BAŞLANGIÇTA BOSS'U KAPALI TUT - trigger ile açılacak
        if (bossObject != null)
        {
            // Sadece aktifse kapat
            if (bossObject.activeSelf)
            {
                bossObject.SetActive(false);
                if (debugMode) Debug.Log("[BossActivator] Boss başlangıçta devre dışı bırakıldı, trigger bekliyor...");
            }
            else
            {
                if (debugMode) Debug.Log("[BossActivator] Boss zaten inactive");
            }
        }
        else
        {
            Debug.LogError("[BossActivator] Boss object atanmamış!");
        }
        
        // Collider kontrolü
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("[BossActivator] Trigger collider yok!");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("[BossActivator] Collider 'Is Trigger' değil! Düzeltiliyor...");
            col.isTrigger = true;
        }
        
        if (debugMode) Debug.Log($"[BossActivator] Hazır. Trigger aktif: {activateOnTrigger}, Collider: {col != null}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (debugMode) Debug.Log($"[BossActivator] OnTriggerEnter2D: {other.name}, tag: {other.tag}");
        
        if (!activateOnTrigger)
        {
            if (debugMode) Debug.Log("[BossActivator] activateOnTrigger kapalı!");
            return;
        }
        
        if (bossActivated)
        {
            if (debugMode) Debug.Log("[BossActivator] Boss zaten aktive edilmiş!");
            return;
        }
        
        if (other.CompareTag("Player"))
        {
            if (debugMode) Debug.Log("[BossActivator] ✅ PLAYER TRİGGER'A GİRDİ - BOSS AKTİVE EDİLİYOR!");
            bossActivated = true;
            StartCoroutine(ActivateBossSequence());
        }
    }
    
    // Alternatif: OnTriggerStay da kontrol et (bazen Enter çalışmıyor)
    private void OnTriggerStay2D(Collider2D other)
    {
        if (bossActivated || !activateOnTrigger) return;
        
        if (other.CompareTag("Player"))
        {
            if (debugMode) Debug.Log("[BossActivator] ✅ PLAYER TRİGGER'DA (Stay) - BOSS AKTİVE EDİLİYOR!");
            bossActivated = true;
            StartCoroutine(ActivateBossSequence());
        }
    }
    
    private IEnumerator ActivateBossSequence()
    {
        Debug.Log("[BossActivator] Boss aktivasyon sekansı başladı!");
        
        // Arena efektlerini aktive et
        if (BossArenaEffects.Instance != null)
        {
            BossArenaEffects.Instance.ActivateBossArena();
        }
        
        // Uyarı metni göster
        if (showWarningText)
        {
            ShowBossWarning();
        }
        
        // Intro sesi
        if (bossIntroSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(bossIntroSound);
        }
        
        yield return new WaitForSeconds(activationDelay);
        
        // Boss'u aktive et
        if (bossObject != null)
        {
            bossObject.SetActive(true);
            Debug.Log("[BossActivator] ✅ Boss aktive edildi: " + bossObject.name);
            
            // Spawn efekti
            if (EnemySpawnEffect.Instance != null)
            {
                EnemySpawnEffect.Instance.PlaySpawnEffect(bossObject.transform.position, bossObject.transform);
            }
        }
        else if (bossPrefab != null && bossSpawnPoint != null)
        {
            GameObject spawnedBoss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
            Debug.Log("[BossActivator] ✅ Boss spawn edildi: " + spawnedBoss.name);
        }
        else
        {
            Debug.LogError("[BossActivator] ❌ Boss object veya prefab atanmamış!");
        }
        
        // Trigger collider'ı devre dışı bırak (tekrar tetiklenmesin)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
    }
    
    private void ShowBossWarning()
    {
        Debug.Log("[BossActivator] BOSS FIGHT!");
    }
    
    /// <summary>
    /// Manuel olarak boss'u aktive et
    /// </summary>
    public void ActivateBoss()
    {
        if (!bossActivated)
        {
            bossActivated = true;
            StartCoroutine(ActivateBossSequence());
        }
    }
    
    /// <summary>
    /// Boss'un aktif olup olmadığını kontrol et
    /// </summary>
    public bool IsBossActive()
    {
        if (bossObject != null)
            return bossObject.activeSelf;
        return bossActivated;
    }
    
    private void OnDrawGizmosSelected()
    {
        // Trigger alanını göster
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            if (col is BoxCollider2D box)
            {
                Vector3 center = transform.position + (Vector3)box.offset;
                Gizmos.DrawCube(center, box.size);
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(center, box.size);
            }
        }
        
        // Boss spawn noktasını göster
        if (bossSpawnPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(bossSpawnPoint.position, 0.5f);
        }
        
        // Boss objesine çizgi çek
        if (bossObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, bossObject.transform.position);
        }
    }
}
