using UnityEngine;
using System.Collections;

/// <summary>
/// Heal skill görsel efekti - karakterin etrafında iyileştirme parıltısı
/// </summary>
public class HealEffect : MonoBehaviour
{
    [Header("Effect Settings")]
    [SerializeField] private float effectDuration = 1f;
    [SerializeField] private Color healColor = new Color(0.3f, 1f, 0.3f, 0.8f);

    [Header("Particle Settings")]
    [SerializeField] private int particleCount = 20;
    [SerializeField] private float riseSpeed = 2f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        StartCoroutine(HealSequence());
    }

    private IEnumerator HealSequence()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        
        // Başlangıç
        if (spriteRenderer != null)
        {
            spriteRenderer.color = healColor;
        }

        // Parıltı efekti - yukarı doğru hareket ve fade
        while (elapsed < effectDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / effectDuration;

            // Yukarı hareket
            transform.localPosition += Vector3.up * riseSpeed * Time.deltaTime * (1f - t);

            // Fade out
            if (spriteRenderer != null)
            {
                Color c = healColor;
                c.a = Mathf.Lerp(healColor.a, 0f, t);
                spriteRenderer.color = c;
            }

            // Scale pulse
            float pulse = 1f + Mathf.Sin(t * Mathf.PI * 4) * 0.2f;
            transform.localScale = startScale * pulse * (1f - t * 0.5f);

            yield return null;
        }

        Destroy(gameObject);
    }
}
