using UnityEngine;
using System.Collections.Generic;

public static class CheckpointData
{
    public static Vector3 LastCheckpointPosition;
    public static bool HasCheckpoint = false;

    // Gameplay unlocks
    public static bool SpikeheadShootingUnlocked = false;
    
    // Öldürülen düşmanların ID'leri (sahne bazlı)
    private static HashSet<string> killedEnemies = new HashSet<string>();
    
    // Checkpoint'teki oyuncu canı
    public static float CheckpointHealth = -1f;
    public static float CheckpointMaxHealth = -1f;

    /// <summary>
    /// Düşmanı öldürüldü olarak işaretle
    /// </summary>
    public static void MarkEnemyAsKilled(string enemyId)
    {
        if (!string.IsNullOrEmpty(enemyId))
        {
            killedEnemies.Add(enemyId);
            Debug.Log($"[CheckpointData] Düşman öldürüldü olarak işaretlendi: {enemyId}");
        }
    }
    
    /// <summary>
    /// Düşman daha önce öldürüldü mü kontrol et
    /// </summary>
    public static bool IsEnemyKilled(string enemyId)
    {
        return !string.IsNullOrEmpty(enemyId) && killedEnemies.Contains(enemyId);
    }
    
    /// <summary>
    /// Checkpoint kaydedildiğinde mevcut öldürülmüş düşmanları sakla
    /// </summary>
    public static void SaveCheckpointEnemyState()
    {
        // Mevcut state zaten HashSet'te saklanıyor
        Debug.Log($"[CheckpointData] Checkpoint kaydedildi - {killedEnemies.Count} düşman öldürülmüş");
    }
    
    /// <summary>
    /// Oyuncu canını checkpoint için kaydet
    /// </summary>
    public static void SaveCheckpointHealth(float current, float max)
    {
        CheckpointHealth = current;
        CheckpointMaxHealth = max;
    }

    public static void ResetData()
    {
        HasCheckpoint = false;
        LastCheckpointPosition = Vector3.zero;
        SpikeheadShootingUnlocked = false;
        killedEnemies.Clear();
        CheckpointHealth = -1f;
        CheckpointMaxHealth = -1f;
        Debug.Log("[CheckpointData] Tüm veriler sıfırlandı");
    }
    
    /// <summary>
    /// Sadece düşman ölümlerini temizle (yeni level için)
    /// </summary>
    public static void ClearKilledEnemies()
    {
        killedEnemies.Clear();
    }
}
