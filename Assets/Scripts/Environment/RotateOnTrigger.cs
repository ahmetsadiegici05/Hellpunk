using UnityEngine;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;

public class RotateOnTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string triggerTag = "Player";

    [Header("Rotate Edilecek Ana Obje")]
    [SerializeField] private Transform rotationRoot;

    [Header("Rotation Settings")]
    [SerializeField] private float rotateZAmount = 90f;
    [SerializeField] private float rotateDuration = 1.5f;

    [Header("QTE Settings")]
    [SerializeField] private int requiredPressCount = 10;
    [SerializeField] private GameObject qtePanel; // ekranda açılacak panel

    private bool hasRotated = false;
    private Transform player;
    private Rigidbody2D playerRb;
    private Health playerHealth;

    private int currentPressCount = 0;
    private bool qteActive = false;

    public ShakeData rotationShakeData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasRotated) return;

        if (other.CompareTag(triggerTag))
        {
            hasRotated = true;

            player = other.transform;
            playerRb = player.GetComponent<Rigidbody2D>();
            playerHealth = player.GetComponent<Health>();

            StartCoroutine(RotateSequence());
        }
    }

    private void Update()
    {
        if (!qteActive) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            currentPressCount++;
        }
    }

    private IEnumerator RotateSequence()
    {
        PlayerMovement.Instance.lockMovement = true;
        CameraShakerHandler.Shake(rotationShakeData);

        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        currentPressCount = 0;
        qteActive = true;

        if (qtePanel != null)
            qtePanel.SetActive(true);

        float elapsed = 0f;

        Quaternion startRot = rotationRoot.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, rotateZAmount);

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotateDuration;

            rotationRoot.rotation = Quaternion.Lerp(startRot, endRot, t);

            // oyuncuyu trigger merkezinde tut
            player.position = transform.position;

            yield return null;
        }

        rotationRoot.rotation = endRot;

        qteActive = false;

        if (qtePanel != null)
            qtePanel.SetActive(false);

        // rotation değerini kaydet
        if (GameManager.Instance != null)
            GameManager.Instance.lastTransformRotationValue = rotationRoot.eulerAngles.z;

        // ❌ QTE BAŞARISIZ → GAME OVER
        if (currentPressCount < requiredPressCount)
        {
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(playerHealth.currentHealth + 999f);
            }
            yield break;
        }

        // ✅ BAŞARILI
        PlayerMovement.Instance.lockMovement = false;
    }
}
