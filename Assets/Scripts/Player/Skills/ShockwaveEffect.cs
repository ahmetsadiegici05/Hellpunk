using UnityEngine;
using System.Collections;

/// <summary>
/// Shockwave görsel efekti - genişleyen dalga
/// </summary>
public class ShockwaveEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float expandDuration = 0.5f;
    [SerializeField] private float maxScale = 8f;
    [SerializeField] private Color startColor = new Color(1f, 0.9f, 0.3f, 0.8f);
    [SerializeField] private Color endColor = new Color(1f, 0.6f, 0.1f, 0f);

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Sprite yoksa oluştur
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateRingSprite();
        }
        
        spriteRenderer.color = startColor;
        spriteRenderer.sortingOrder = 50;
    }

    private void Start()
    {
        StartCoroutine(ExpandAndFade());
    }

    private IEnumerator ExpandAndFade()
    {
        float elapsed = 0f;
        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 endScale = Vector3.one * maxScale;

        transform.localScale = startScale;

        while (elapsed < expandDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / expandDuration;
            
            // Ease out - hızlı başla, yavaş bitir
            float smoothT = 1f - Mathf.Pow(1f - t, 3f);
            
            transform.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            
            yield return null;
        }

        Destroy(gameObject);
    }

    private Sprite CreateRingSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerRadius = size / 2f - 2f;
        float innerRadius = outerRadius - 8f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                
                if (dist <= outerRadius && dist >= innerRadius)
                {
                    // Smooth edges
                    float alpha = 1f;
                    if (dist > outerRadius - 2f)
                        alpha = (outerRadius - dist) / 2f;
                    else if (dist < innerRadius + 2f)
                        alpha = (dist - innerRadius) / 2f;
                    
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
