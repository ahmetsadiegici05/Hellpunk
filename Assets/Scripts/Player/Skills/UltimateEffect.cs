using UnityEngine;
using System.Collections;

/// <summary>
/// Ultimate skill efekti - alan hasarı ve görsel efekt
/// </summary>
public class UltimateEffect : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private float effectDuration = 1.5f;
    [SerializeField] private float maxScale = 3f;
    [SerializeField] private float expandSpeed = 5f;

    [Header("Fire Effect")]
    [SerializeField] private Color fireColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private ParticleSystem fireParticles;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        StartCoroutine(EffectSequence());
    }

    private IEnumerator EffectSequence()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = Vector3.one * maxScale;

        // Genişle
        while (elapsed < effectDuration * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (effectDuration * 0.3f);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            
            if (spriteRenderer != null)
            {
                Color c = fireColor;
                c.a = 1f - t * 0.3f;
                spriteRenderer.color = c;
            }
            
            yield return null;
        }

        // Bekle
        yield return new WaitForSeconds(effectDuration * 0.4f);

        // Fade out
        elapsed = 0f;
        while (elapsed < effectDuration * 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (effectDuration * 0.3f);
            
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(0.7f, 0f, t);
                spriteRenderer.color = c;
            }
            
            yield return null;
        }

        Destroy(gameObject);
    }
}
