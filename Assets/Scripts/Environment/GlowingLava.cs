using UnityEngine;

/// <summary>
/// Parıldayan lav/ateş sprite'ı
/// HDR renk ile Bloom efektine tepki verir
/// </summary>
public class GlowingLava : MonoBehaviour
{
    [Header("Parlama Ayarları")]
    [Tooltip("Temel parlama rengi")]
    public Color glowColor = new Color(1f, 0.3f, 0.05f, 1f);
    
    [Range(1f, 10f)]
    [Tooltip("Parlama yoğunluğu - yüksek değer = daha parlak (Bloom için)")]
    public float glowIntensity = 3f;
    
    [Header("Nabız Efekti")]
    public bool enablePulse = true;
    [Range(0.5f, 5f)]
    public float pulseSpeed = 1.5f;
    [Range(0.5f, 1f)]
    public float pulseMinIntensity = 0.7f;

    [Header("Renk Değişimi")]
    public bool enableColorShift = true;
    public Color secondaryColor = new Color(1f, 0.5f, 0.1f, 1f);
    [Range(0.1f, 2f)]
    public float colorShiftSpeed = 0.5f;

    private SpriteRenderer spriteRenderer;
    private Material glowMaterial;
    private float pulseTimer;
    private float colorTimer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Parlayan material oluştur
        CreateGlowMaterial();
    }

    void CreateGlowMaterial()
    {
        // URP Particle shader ile additive blending
        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        glowMaterial = new Material(shader);
        spriteRenderer.material = glowMaterial;
        
        UpdateGlow();
    }

    void Update()
    {
        if (glowMaterial == null) return;

        pulseTimer += Time.deltaTime * pulseSpeed;
        colorTimer += Time.deltaTime * colorShiftSpeed;

        UpdateGlow();
    }

    void UpdateGlow()
    {
        // Nabız hesapla
        float pulse = 1f;
        if (enablePulse)
        {
            pulse = Mathf.Lerp(pulseMinIntensity, 1f, (Mathf.Sin(pulseTimer) + 1f) / 2f);
        }

        // Renk değişimi
        Color currentColor = glowColor;
        if (enableColorShift)
        {
            float t = (Mathf.Sin(colorTimer) + 1f) / 2f;
            currentColor = Color.Lerp(glowColor, secondaryColor, t);
        }

        // HDR renk - Bloom için yüksek değerler
        Color hdrColor = currentColor * glowIntensity * pulse;
        
        spriteRenderer.color = hdrColor;
    }

    void OnValidate()
    {
        if (spriteRenderer != null)
        {
            UpdateGlow();
        }
    }
}
