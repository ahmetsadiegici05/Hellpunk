using UnityEngine;

public class RotationResetter : MonoBehaviour
{
    [SerializeField] private Transform rotationRoot;

    private void Awake()
    {
        ApplySavedRotation();
    }

    public void ApplySavedRotation()
    {
        if (rotationRoot == null)
            return;

        // Önce CheckpointData'dan kontrol et (öncelikli)
        float z = 0f;
        
        if (CheckpointData.HasCheckpoint && CheckpointData.LastCheckpointRotationZ != 0f)
        {
            // Checkpoint varsa, checkpoint rotasyonunu kullan
            z = CheckpointData.LastCheckpointRotationZ;
            Debug.Log($"[RotationResetter] Checkpoint rotasyonu uygulandı: {z}");
        }
        else if (GameManager.Instance != null)
        {
            // Checkpoint yoksa GameManager'dan al
            z = GameManager.Instance.lastTransformRotationValue;
            Debug.Log($"[RotationResetter] GameManager rotasyonu uygulandı: {z}");
        }
        
        rotationRoot.rotation = Quaternion.Euler(0f, 0f, z);
        
        // GameManager'ı da güncelle
        if (GameManager.Instance != null)
            GameManager.Instance.lastTransformRotationValue = z;
    }
}
