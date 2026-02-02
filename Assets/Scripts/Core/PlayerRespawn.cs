using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private void Start()
    {
        // Eğer daha önce bir checkpoint alındıysa, oyuncuyu oraya taşı
        if (CheckpointData.HasCheckpoint)
        {
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
            
            Debug.Log($"[PlayerRespawn] Checkpoint pozisyonuna taşındı: {CheckpointData.LastCheckpointPosition}");
        }
    }
}
