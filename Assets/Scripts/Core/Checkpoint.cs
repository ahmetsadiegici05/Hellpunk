using UnityEngine;
using System.Collections;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private bool unlockSpikeheadShooting;
    [SerializeField] private bool saveEnemyDeaths = true; // Öldürülen düşmanları kaydet
    private bool activated;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (GameManager.Instance.isRotation)
            return;

        if (collision.CompareTag("Player") && !activated)
        {
            activated = true;

            CheckpointData.LastCheckpointPosition = transform.position;
            CheckpointData.HasCheckpoint = true;

            StartCoroutine(HasCheckpoint());

            if (unlockSpikeheadShooting)
                CheckpointData.SpikeheadShootingUnlocked = true;
            
            // Oyuncu canını kaydet
            Health playerHealth = collision.GetComponent<Health>();
            if (playerHealth != null)
            {
                CheckpointData.SaveCheckpointHealth(playerHealth.currentHealth, playerHealth.maxHealth);
            }
            
            // Öldürülen düşmanları kaydet
            if (saveEnemyDeaths)
            {
                CheckpointData.SaveCheckpointEnemyState();
            }

            if (spriteRenderer != null)
                spriteRenderer.color = activeColor;

            Debug.Log($"Checkpoint Activated! Pozisyon: {transform.position}");
        }
    }

    IEnumerator HasCheckpoint()
    {
        yield return new WaitForSeconds(5.2f);
        GameManager.Instance.lastCheckpointPosition = transform.position;
        GameManager.Instance.hasCheckpoint = true;
    }
}
