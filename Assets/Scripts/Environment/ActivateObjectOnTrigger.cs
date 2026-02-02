using UnityEngine;
using System.Collections;

public class ActivateObjectOnTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string triggerTag = "Player";

    [Header("Target Object")]
    [SerializeField] private GameObject targetObject;

    [Header("Options")]
    [SerializeField] private bool activateOnlyOnce = true;
    [SerializeField] private float activateDelay = 0.5f;

    private bool activated = false;

    private void Start()
    {
        Debug.Log($"[ActivateObjectOnTrigger] Başlatıldı - Target: {(targetObject != null ? targetObject.name : "NULL")}, Trigger Tag: {triggerTag}");
        
        // Trigger collider kontrolü
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError("[ActivateObjectOnTrigger] Collider2D bulunamadı! Trigger çalışmayacak.");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("[ActivateObjectOnTrigger] Collider isTrigger=false! Trigger olarak işaretleniyor.");
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[ActivateObjectOnTrigger] Trigger girişi: {other.name}, Tag: {other.tag}");
        
        if (activateOnlyOnce && activated)
        {
            Debug.Log("[ActivateObjectOnTrigger] Zaten aktive edilmiş, atlanıyor.");
            return;
        }

        if (other.CompareTag(triggerTag))
        {
            activated = true;
            Debug.Log($"[ActivateObjectOnTrigger] {triggerTag} algılandı! Aktivasyon başlıyor...");
            StartCoroutine(ActivateAfterDelay());
        }
    }

    private IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(activateDelay);

        if (targetObject != null)
        {
            targetObject.SetActive(true);
            Debug.Log($"[ActivateObjectOnTrigger] {targetObject.name} aktive edildi!");
            
            // Spawn efekti (boss için)
            if (EnemySpawnEffect.Instance != null)
            {
                EnemySpawnEffect.Instance.PlaySpawnEffect(targetObject.transform.position, targetObject.transform);
            }
        }
        else
        {
            Debug.LogError("[ActivateObjectOnTrigger] Target object NULL! Aktivasyon başarısız.");
        }
    }
}
