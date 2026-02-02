using UnityEngine;
using System.Collections;

/// <summary>
/// Level 2'de boss'u aktive eder
/// Oyuncu trigger'a girdiğinde boss spawn olur veya aktive edilir
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
        
        // Boss başlangıçta inactive olmalı
        if (bossObject != null && bossObject.activeSelf)
        {
            bossObject.SetActive(false);
            Debug.Log("[BossActivator] Boss başlangıçta devre dışı bırakıldı");
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!activateOnTrigger || bossActivated) return;
        
        if (other.CompareTag("Player"))
        {
            bossActivated = true;
            StartCoroutine(ActivateBossSequence());
        }
    }
    
    private IEnumerator ActivateBossSequence()
    {
        Debug.Log("[BossActivator] Boss aktivasyon sekansı başladı!");
        
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
        
        // Boss'u aktive et veya spawn et
        if (bossObject != null)
        {
            bossObject.SetActive(true);
            Debug.Log("[BossActivator] Boss aktive edildi: " + bossObject.name);
            
            // Spawn efekti
            if (EnemySpawnEffect.Instance != null)
            {
                EnemySpawnEffect.Instance.PlaySpawnEffect(bossObject.transform.position, bossObject.transform);
            }
        }
        else if (bossPrefab != null && bossSpawnPoint != null)
        {
            // Prefab'dan spawn et
            GameObject spawnedBoss = Instantiate(bossPrefab, bossSpawnPoint.position, Quaternion.identity);
            Debug.Log("[BossActivator] Boss spawn edildi: " + spawnedBoss.name);
            
            // Spawn efekti
            if (EnemySpawnEffect.Instance != null)
            {
                EnemySpawnEffect.Instance.PlaySpawnEffect(bossSpawnPoint.position, spawnedBoss.transform);
            }
        }
        else
        {
            Debug.LogError("[BossActivator] Boss object veya prefab atanmamış!");
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
        // Ekranda "BOSS FIGHT" yazısı göster (opsiyonel)
        Debug.Log("[BossActivator] BOSS FIGHT!");
        
        // UI'da göstermek için UIManager kullanılabilir
        // UIManager.Instance?.ShowBossWarning();
    }
    
    /// <summary>
    /// Manuel olarak boss'u aktive et (başka scriptlerden çağrılabilir)
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
                Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);
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
