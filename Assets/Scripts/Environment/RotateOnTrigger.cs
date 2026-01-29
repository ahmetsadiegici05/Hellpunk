using UnityEngine;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;

public class RotateOnTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string triggerTag = "Player";

    [Header("Rotate Edilecek Ana Obje")]
    [SerializeField] private Transform rotationRoot; // RotationObjects

    [Header("Rotation Settings")]
    [SerializeField] private float rotateZAmount = 90f;
    [SerializeField] private float rotateDuration = 1.5f;

    private bool hasRotated = false;
    private Transform player;
    private Rigidbody2D playerRb;
    public ShakeData rotationShakeData;


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasRotated) return;

        if (other.CompareTag(triggerTag))
        {
            hasRotated = true;

            player = other.transform;
            playerRb = player.GetComponent<Rigidbody2D>();

            StartCoroutine(RotateSequence());
        }
    }

    private IEnumerator RotateSequence()
    {
        PlayerMovement.Instance.lockMovement = true;
        CameraShakerHandler.Shake(rotationShakeData);

        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        float elapsed = 0f;

        Quaternion startRot = rotationRoot.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, rotateZAmount);

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotateDuration;

            rotationRoot.rotation = Quaternion.Lerp(startRot, endRot, t);

            player.position = transform.position;

            yield return null;
        }

        rotationRoot.rotation = endRot;

        PlayerMovement.Instance.lockMovement = false;
    }
}
