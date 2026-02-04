using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Sandıktan ability çıktığında görsel efekt gösterir
/// Heal/Fireball ikonu UI'a doğru uçar (SoulParticleEffect gibi)
/// </summary>
public class AbilityPickupEffect : MonoBehaviour
{
    public static AbilityPickupEffect Instance { get; private set; }
    
    [Header("Heal Renkleri")]
    [SerializeField] private Color healCoreColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color healMainColor = new Color(0.3f, 1f, 0.5f, 0.9f);
    [SerializeField] private Color healEndColor = new Color(0.2f, 0.8f, 0.4f, 0f);
    
    [Header("Fireball Renkleri")]
    [SerializeField] private Color fireballCoreColor = new Color(1f, 1f, 0.8f, 1f);
    [SerializeField] private Color fireballMainColor = new Color(1f, 0.5f, 0.1f, 0.9f);
    [SerializeField] private Color fireballEndColor = new Color(1f, 0.3f, 0f, 0f);
    
    [Header("Efekt Ayarları")]
    [SerializeField] private int particleCount = 12;
    [SerializeField] private float particleSpeed = 5f;
    [SerializeField] private float particleLifetime = 1f;
    [SerializeField] private float particleSize = 0.2f;
    
    [Header("İkon Uçuş Ayarları")]
    [SerializeField] private float iconFlyDuration = 1f;
    [SerializeField] private float iconStartSize = 80f;
    [SerializeField] private float iconEndSize = 40f;
    
    private ParticleSystem burstParticles;
    private Camera mainCamera;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        CreateParticleSystem();
        mainCamera = Camera.main;
    }
    
    private void CreateParticleSystem()
    {
        // Ana burst efekti - SoulParticleEffect'e benzer
        GameObject burstObj = new GameObject("AbilityBurstParticles");
        burstObj.transform.SetParent(transform);
        
        burstParticles = burstObj.AddComponent<ParticleSystem>();
        
        var main = burstParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(particleLifetime * 0.5f, particleLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleSpeed * 0.3f, particleSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.5f, particleSize);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f;
        main.playOnAwake = false;
        
        var emission = burstParticles.emission;
        emission.rateOverTime = 0;
        
        var shape = burstParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;
        
        var colorOverLifetime = burstParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        var sizeOverLifetime = burstParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(0.8f, 1.2f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        var noise = burstParticles.noise;
        noise.enabled = true;
        noise.strength = 1f;
        noise.frequency = 2f;
        noise.scrollSpeed = 0.5f;
        
        var trails = burstParticles.trails;
        trails.enabled = true;
        trails.lifetime = 0.3f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        
        var lights = burstParticles.lights;
        lights.enabled = true;
        lights.ratio = 0.1f;
        lights.intensity = 0.5f;
        lights.range = 1f;
        
        var renderer = burstObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.trailMaterial = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 15;
    }
    
    /// <summary>
    /// Heal ability pickup efekti
    /// </summary>
    public void PlayHealEffect(Vector3 position)
    {
        PlayEffect(position, healCoreColor, healMainColor, healEndColor);
        StartCoroutine(FlyIconToUI(position, "Heal", healMainColor));
    }
    
    /// <summary>
    /// Fireball ability pickup efekti
    /// </summary>
    public void PlayFireballEffect(Vector3 position)
    {
        PlayEffect(position, fireballCoreColor, fireballMainColor, fireballEndColor);
        StartCoroutine(FlyIconToUI(position, "Fireball", fireballMainColor));
    }
    
    private void PlayEffect(Vector3 position, Color coreColor, Color mainColor, Color endColor)
    {
        if (burstParticles == null) return;
        
        burstParticles.transform.position = position;
        
        var colorOverLifetime = burstParticles.colorOverLifetime;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(coreColor, 0f),
                new GradientColorKey(mainColor, 0.2f),
                new GradientColorKey(endColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var main = burstParticles.main;
        main.startColor = mainColor;
        
        burstParticles.Emit(particleCount);
        
        StartCoroutine(SecondWave(position, coreColor, mainColor, endColor));
    }
    
    private IEnumerator SecondWave(Vector3 position, Color coreColor, Color mainColor, Color endColor)
    {
        yield return new WaitForSeconds(0.15f);
        
        if (burstParticles == null) yield break;
        
        burstParticles.transform.position = position + Random.insideUnitSphere * 0.2f;
        burstParticles.Emit(particleCount / 2);
    }
    
    /// <summary>
    /// Ability ikonunu sandıktan UI'a uçurur
    /// </summary>
    private IEnumerator FlyIconToUI(Vector3 worldPos, string skillName, Color iconColor)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (mainCamera == null) yield break;
        
        // Canvas oluştur
        GameObject canvasObj = new GameObject("AbilityIconFlyCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        canvasObj.AddComponent<CanvasScaler>();
        
        // İkon GameObject
        GameObject iconObj = new GameObject("FlyingIcon");
        iconObj.transform.SetParent(canvasObj.transform, false);
        
        // Background (daire)
        Image bgImage = iconObj.AddComponent<Image>();
        bgImage.sprite = CreateCircleSprite(64);
        bgImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(iconStartSize, iconStartSize);
        
        // İkon sprite'ı
        GameObject spriteObj = new GameObject("IconSprite");
        spriteObj.transform.SetParent(iconObj.transform, false);
        Image iconImage = spriteObj.AddComponent<Image>();
        iconImage.sprite = CreateSkillIconSprite(skillName, 64);
        iconImage.color = iconColor;
        
        RectTransform spriteRect = spriteObj.GetComponent<RectTransform>();
        spriteRect.anchorMin = Vector2.zero;
        spriteRect.anchorMax = Vector2.one;
        spriteRect.sizeDelta = new Vector2(-16, -16);
        spriteRect.anchoredPosition = Vector2.zero;
        
        // Glow efekti
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(iconObj.transform, false);
        glowObj.transform.SetAsFirstSibling();
        Image glowImage = glowObj.AddComponent<Image>();
        glowImage.sprite = CreateCircleSprite(64);
        glowImage.color = new Color(iconColor.r, iconColor.g, iconColor.b, 0.4f);
        
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 0.5f);
        glowRect.anchorMax = new Vector2(0.5f, 0.5f);
        glowRect.sizeDelta = new Vector2(iconStartSize * 1.5f, iconStartSize * 1.5f);
        
        // Başlangıç pozisyonu (world -> screen)
        Vector3 startScreenPos = mainCamera.WorldToScreenPoint(worldPos);
        iconRect.position = startScreenPos;
        
        // Hedef pozisyon - sağ alt köşedeki skill UI (heal: index 0, fireball: index 1)
        int skillIndex = skillName == "Heal" ? 0 : 1;
        Vector2 targetPos = new Vector2(
            Screen.width - 20f - (skillIndex * 70f) - 28f, // Skill spacing = 70
            60f + 28f // Biraz yukarı
        );
        
        // Uçuş animasyonu
        float elapsed = 0f;
        Vector3 velocity = Vector3.zero;
        
        while (elapsed < iconFlyDuration)
        {
            float t = elapsed / iconFlyDuration;
            
            // Bezier eğrisi - yukarı doğru yay çiz
            float curveHeight = Mathf.Sin(t * Mathf.PI) * 150f;
            Vector3 currentTarget = Vector3.Lerp(startScreenPos, new Vector3(targetPos.x, targetPos.y, 0), t);
            currentTarget.y += curveHeight;
            
            // Smooth hareket
            iconRect.position = Vector3.SmoothDamp(iconRect.position, currentTarget, ref velocity, 0.1f);
            
            // Boyut küçülsün
            float size = Mathf.Lerp(iconStartSize, iconEndSize, t);
            iconRect.sizeDelta = new Vector2(size, size);
            glowRect.sizeDelta = new Vector2(size * 1.5f, size * 1.5f);
            
            // Glow pulse
            float pulse = 0.3f + Mathf.Sin(elapsed * 10f) * 0.15f;
            glowImage.color = new Color(iconColor.r, iconColor.g, iconColor.b, pulse);
            
            // Parlaklık artışı
            float brightness = Mathf.Lerp(1f, 1.5f, t);
            iconImage.color = iconColor * brightness;
            
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Son flash efekti
        float flashTime = 0f;
        while (flashTime < 0.2f)
        {
            float flash = 1f - (flashTime / 0.2f);
            glowImage.color = new Color(1f, 1f, 1f, flash);
            glowRect.sizeDelta = new Vector2(iconEndSize * (1.5f + flash), iconEndSize * (1.5f + flash));
            flashTime += Time.unscaledDeltaTime;
            yield return null;
        }
        
        Destroy(canvasObj);
    }
    
    private Sprite CreateCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < radius)
                {
                    float alpha = Mathf.Clamp01((radius - dist) / 2f);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
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
    
    private Sprite CreateSkillIconSprite(string skillName, int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        switch (skillName)
        {
            case "Heal":
                // Artı işareti
                DrawPlus(pixels, size, center, size * 0.35f, size * 0.12f);
                break;
            case "Fireball":
                // Ateş topu
                DrawFireball(pixels, size, center, size * 0.4f);
                break;
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    
    private void DrawPlus(Color[] pixels, int size, Vector2 center, float length, float thickness)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                
                if (Mathf.Abs(pos.y) <= thickness && Mathf.Abs(pos.x) <= length)
                {
                    pixels[y * size + x] = Color.white;
                }
                else if (Mathf.Abs(pos.x) <= thickness && Mathf.Abs(pos.y) <= length)
                {
                    pixels[y * size + x] = Color.white;
                }
            }
        }
    }
    
    private void DrawFireball(Color[] pixels, int size, Vector2 center, float radius)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                
                // Ana ateş topu (daire)
                Vector2 fireballCenter = new Vector2(radius * 0.15f, 0);
                float fireballDist = (pos - fireballCenter).magnitude;
                float fireballRadius = radius * 0.5f;
                
                if (fireballDist <= fireballRadius)
                {
                    float alpha = 1f;
                    if (fireballDist > fireballRadius - 2f) 
                        alpha = (fireballRadius - fireballDist) / 2f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
                    continue;
                }
                
                // Kuyruk (sol tarafa doğru)
                float tailStartX = -radius * 0.3f;
                float tailEndX = -radius * 0.85f;
                
                if (pos.x >= tailEndX && pos.x <= tailStartX)
                {
                    float progress = (pos.x - tailEndX) / (tailStartX - tailEndX);
                    float tailWidth = radius * 0.4f * progress;
                    
                    if (Mathf.Abs(pos.y) <= tailWidth)
                    {
                        float alpha = progress * (1f - Mathf.Abs(pos.y) / tailWidth);
                        pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha * 0.8f));
                    }
                }
            }
        }
    }
}
