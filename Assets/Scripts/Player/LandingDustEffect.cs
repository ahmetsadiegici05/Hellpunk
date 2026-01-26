using UnityEngine;
using System.Collections;

/// <summary>
/// Oyuncu yere düşerken toz efekti oluşturur.
/// Oyuncuya eklenir.
/// </summary>
public class LandingDustEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float minFallSpeed = 5f;
    [SerializeField] private float dustDuration = 0.4f;
    [SerializeField] private int particleCount = 6;
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Appearance")]
    [SerializeField] private Color dustColor = new Color(0.8f, 0.75f, 0.6f, 0.6f);
    [SerializeField] private float particleSize = 0.15f;
    [SerializeField] private float spreadSpeed = 3f;
    
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private bool wasGrounded = true;
    private float lastYVelocity;
    private Sprite dustSprite;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        dustSprite = CreateDustSprite();
    }
    
    private void Update()
    {
        if (rb == null) return;
        
        bool isGrounded = IsGrounded();
        
        // Yere iniş kontrolü
        if (!wasGrounded && isGrounded && lastYVelocity < -minFallSpeed)
        {
            float intensity = Mathf.Clamp01(Mathf.Abs(lastYVelocity) / 15f);
            SpawnDust(intensity);
            
            // Ekran sarsıntısı
            if (ScreenShake.Instance != null && intensity > 0.5f)
            {
                ScreenShake.Instance.ShakeLight();
            }
        }
        
        wasGrounded = isGrounded;
        lastYVelocity = rb.linearVelocity.y;
    }
    
    private void SpawnDust(float intensity)
    {
        Vector3 spawnPos = transform.position;
        
        // Ayak pozisyonunu bul
        if (boxCollider != null)
        {
            spawnPos.y -= boxCollider.bounds.extents.y;
        }
        
        int count = Mathf.RoundToInt(particleCount * intensity);
        
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(AnimateDustParticle(spawnPos, i, count, intensity));
        }
    }
    
    private IEnumerator AnimateDustParticle(Vector3 startPos, int index, int total, float intensity)
    {
        // Partikül oluştur
        GameObject particle = new GameObject("DustParticle");
        SpriteRenderer sr = particle.AddComponent<SpriteRenderer>();
        sr.sprite = dustSprite;
        sr.color = dustColor;
        sr.sortingOrder = 5;
        
        // Başlangıç pozisyonu ve yön
        float angle = (index / (float)total) * 180f; // 0-180 derece (sol-sağ)
        float radians = angle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Abs(Mathf.Sin(radians)) * 0.3f);
        
        // Rastgelelik ekle
        direction.x += Random.Range(-0.2f, 0.2f);
        direction.y += Random.Range(0f, 0.2f);
        
        particle.transform.position = startPos + new Vector3(Random.Range(-0.1f, 0.1f), 0, 0);
        float size = particleSize * Random.Range(0.7f, 1.3f) * intensity;
        particle.transform.localScale = Vector3.one * size;
        
        float elapsed = 0f;
        float duration = dustDuration * Random.Range(0.8f, 1.2f);
        float speed = spreadSpeed * intensity * Random.Range(0.8f, 1.2f);
        
        Vector3 velocity = direction * speed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Hareket (yavaşlayarak)
            velocity *= 0.95f;
            velocity.y -= 2f * Time.deltaTime; // Hafif yerçekimi
            particle.transform.position += (Vector3)velocity * Time.deltaTime;
            
            // Fade out ve shrink
            Color c = dustColor;
            c.a = dustColor.a * (1f - t);
            sr.color = c;
            
            particle.transform.localScale = Vector3.one * size * (1f - t * 0.5f);
            
            yield return null;
        }
        
        Destroy(particle);
    }
    
    private bool IsGrounded()
    {
        if (boxCollider == null) return false;
        
        Vector2 origin = (Vector2)transform.position - new Vector2(0, boxCollider.bounds.extents.y);
        Vector2 size = new Vector2(boxCollider.bounds.size.x * 0.9f, 0.1f);
        
        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, 0.1f, groundLayer);
        return hit.collider != null;
    }
    
    private Sprite CreateDustSprite()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / (size / 2f));
                alpha = Mathf.Pow(alpha, 1.5f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
