using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    [Header("Rotation Root (Level 2 için)")]
    [SerializeField] private Transform rotationRoot; // Inspector'dan atanmalı veya otomatik bulunmalı
    
    private void Start()
    {
        // Eğer daha önce bir checkpoint alındıysa, oyuncuyu oraya taşı
        if (CheckpointData.HasCheckpoint)
        {
            // Önce level rotasyonunu ayarla (checkpoint'te kaydedilen rotasyona)
            ApplyCheckpointRotation();
            
            // Sonra oyuncuyu pozisyona taşı
            transform.position = CheckpointData.LastCheckpointPosition;
            
            // Checkpoint'teki canı yükle
            if (CheckpointData.CheckpointHealth > 0)
            {
                Health playerHealth = GetComponent<Health>();
                if (playerHealth != null)
                {
                    playerHealth.currentHealth = CheckpointData.CheckpointHealth;
                    
                    // UI'ı güncelle
                    if (ScreenEffects.Instance != null)
                        ScreenEffects.Instance.UpdateHealthVignette(playerHealth.currentHealth / playerHealth.maxHealth);
                    
                    Debug.Log($"[PlayerRespawn] Checkpoint canı yüklendi: {CheckpointData.CheckpointHealth}");
                }
            }
            
            Debug.Log($"[PlayerRespawn] Checkpoint pozisyonuna taşındı: {CheckpointData.LastCheckpointPosition}, Rotasyon: {CheckpointData.LastCheckpointRotationZ}");
        }
    }
    
    /// <summary>
    /// Checkpoint'te kaydedilen level rotasyonunu uygula
    /// </summary>
    private void ApplyCheckpointRotation()
    {
        // RotationRoot'u bul (eğer atanmamışsa)
        if (rotationRoot == null)
        {
            // "RotationRoot" veya "Level" adlı objeyi ara
            GameObject rotRoot = GameObject.Find("RotationRoot");
            if (rotRoot == null) rotRoot = GameObject.Find("Level");
            if (rotRoot == null) rotRoot = GameObject.Find("LevelRoot");
            if (rotRoot == null) rotRoot = GameObject.Find("World");
            
            if (rotRoot != null)
                rotationRoot = rotRoot.transform;
        }
        
        if (rotationRoot != null && CheckpointData.LastCheckpointRotationZ != 0f)
        {
            // Level'ı checkpoint'teki rotasyona döndür
            rotationRoot.rotation = Quaternion.Euler(0f, 0f, CheckpointData.LastCheckpointRotationZ);
            
            // GameManager'a da bildir
            if (GameManager.Instance != null)
                GameManager.Instance.lastTransformRotationValue = CheckpointData.LastCheckpointRotationZ;
                
            Debug.Log($"[PlayerRespawn] Level rotasyonu uygulandı: {CheckpointData.LastCheckpointRotationZ}");
        }
    }
}
