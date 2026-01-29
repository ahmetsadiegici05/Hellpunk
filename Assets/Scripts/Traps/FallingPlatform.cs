using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class FallingPlatform : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float fallDelay = 0.5f;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private bool canRespawn = true;

    [Header("Shake Effect")]
    [SerializeField] private float shakeAmount = 0.05f;
    [SerializeField] private float shakeDuration = 0.3f;

    private Rigidbody2D rb;
    private Vector3 startPosition;
    private bool triggered = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 1f;

        startPosition = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (triggered) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            triggered = true;
            StartCoroutine(FallSequence());
        }
    }

    private IEnumerator FallSequence()
    {
        yield return StartCoroutine(Shake());

        yield return new WaitForSeconds(fallDelay);

        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    private IEnumerator Shake()
    {
        float elapsed = 0f;
        Vector3 originalPos = transform.position;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = originalPos + (Vector3)Random.insideUnitCircle * shakeAmount;
            yield return null;
        }

        transform.position = originalPos;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!canRespawn) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(Respawn());
        }
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        transform.position = startPosition;
        triggered = false;
    }
}
