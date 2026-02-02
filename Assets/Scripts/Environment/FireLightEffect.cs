using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Sabit duran ama animasyonlu ateş ışığı efekti.
/// Sprite gerektirmez - tamamen kod ile oluşturulur.
/// Sahneye boş obje ekleyip bu script'i atayın.
/// </summary>
public class FireLightEffect : MonoBehaviour
{
    [Header("Işık Ayarları")]
    [SerializeField] private Color lightColor = new Color(1f, 0.6f, 0.2f); // Turuncu-sarı
    [SerializeField] private float baseIntensity = 1.5f;
    [SerializeField] private float intensityFlicker = 0.4f; // Titreşim miktarı
    [SerializeField] private float flickerSpeed = 8f;
    [SerializeField] private float lightRadius = 3f;
    
    [Header("Ateş Parçacıkları")]
    [SerializeField] private bool useParticles = true;
    [SerializeField] private int particleCount = 15;
    [SerializeField] private float particleSize = 0.15f;
    [SerializeField] private float particleSpeed = 1.5f;
    [SerializeField] private float particleLifetime = 0.8f;
    
    [Header("Kıvılcımlar")]
    [SerializeField] private bool useSparks = true;
    [SerializeField] private int sparkCount = 5;
    
    [Header("Kaynak Görünümü (Çatlak/Delik)")]
    [SerializeField] private bool useSourceEffect = true;
    [SerializeField] private Color sourceGlowColor = new Color(1f, 0.3f, 0.05f, 0.9f);
    [SerializeField] private float sourceWidth = 0.8f;
    [SerializeField] private float sourceHeight = 0.15f;
    
    private Light2D fireLight;
    private ParticleSystem fireParticles;
    private ParticleSystem sparkParticles;
    private GameObject sourceGlow;
    private float flickerOffset;
    private Texture2D circleTexture;
    private Material particleMaterial;
    
    private void Start()
    {
        flickerOffset = Random.Range(0f, 100f); // Her ateş farklı titreşsin
        
        CreateCircleTexture();
        CreateLight();
        
        if (useParticles)
            CreateFireParticles();
            
        if (useSparks)
            CreateSparkParticles();
            
        if (useSourceEffect)
            CreateSourceGlow();
    }
    
    private void CreateCircleTexture()
    {
        // ParticleHelper kullanarak texture ve material oluştur
        circleTexture = ParticleHelper.GetGlowTexture();
        particleMaterial = ParticleHelper.CreateAdditiveMaterial(circleTexture);
    }
    
    private void Update()
    {
        // Işık titreşimi
        if (fireLight != null)
        {
            float noise1 = Mathf.PerlinNoise(Time.time * flickerSpeed + flickerOffset, 0f);
            float noise2 = Mathf.PerlinNoise(0f, Time.time * flickerSpeed * 1.3f + flickerOffset);
            float flicker = (noise1 + noise2) * 0.5f; // 0-1 arası
            
            fireLight.intensity = baseIntensity + (flicker - 0.5f) * intensityFlicker * 2f;
            
            // Hafif renk değişimi
            float colorShift = Mathf.PerlinNoise(Time.time * 3f + flickerOffset, 10f) * 0.1f;
            fireLight.color = new Color(
                lightColor.r,
                lightColor.g - colorShift,
                lightColor.b
            );
        }
    }
    
    private void CreateLight()
    {
        GameObject lightObj = new GameObject("FireLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = Vector3.zero;
        
        fireLight = lightObj.AddComponent<Light2D>();
        fireLight.lightType = Light2D.LightType.Point;
        fireLight.color = lightColor;
        fireLight.intensity = baseIntensity;
        fireLight.pointLightOuterRadius = lightRadius;
        fireLight.pointLightInnerRadius = lightRadius * 0.3f;
        fireLight.falloffIntensity = 0.5f;
    }
    
    private void CreateFireParticles()
    {
        GameObject particleObj = new GameObject("FireParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;
        
        fireParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = fireParticles.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.maxParticles = particleCount * 3;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f; // Yukarı git
        
        // Renk gradyanı
        var colorOverLifetime = fireParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),   // Sarı başlangıç
                new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.4f), // Turuncu
                new GradientColorKey(new Color(0.8f, 0.2f, 0.05f), 1f) // Kırmızı bitiş
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(1f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Boyut değişimi
        var sizeOverLifetime = fireParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Emisyon
        var emission = fireParticles.emission;
        emission.rateOverTime = particleCount;
        
        // Şekil - küçük koni
        var shape = fireParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.1f;
        shape.rotation = new Vector3(-90f, 0f, 0f); // Yukarı bak
        
        // Renderer ayarları
        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 10;
        
        // Yuvarlak particle material kullan
        renderer.material = particleMaterial;
    }
    
    private void CreateSparkParticles()
    {
        GameObject sparkObj = new GameObject("SparkParticles");
        sparkObj.transform.SetParent(transform);
        sparkObj.transform.localPosition = Vector3.zero;
        
        sparkParticles = sparkObj.AddComponent<ParticleSystem>();
        
        var main = sparkParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.maxParticles = sparkCount * 5;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;
        main.startColor = new Color(1f, 0.8f, 0.3f);
        
        // Emisyon - aralıklı
        var emission = sparkParticles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, (short)sparkCount, (short)sparkCount, -1, 0.3f)
        });
        
        // Şekil
        var shape = sparkParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f;
        shape.radius = 0.05f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        
        // Renk solma
        var colorOverLifetime = sparkParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Renderer - yuvarlak particle kullan
        var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 11;
        renderer.material = particleMaterial;
    }
    
    private void CreateSourceGlow()
    {
        // Ana kaynak objesi
        sourceGlow = new GameObject("SourceGlow");
        sourceGlow.transform.SetParent(transform);
        sourceGlow.transform.localPosition = Vector3.zero;
        
        // Merkez parlama - çatlağın içi
        GameObject centerGlow = new GameObject("CenterGlow");
        centerGlow.transform.SetParent(sourceGlow.transform);
        centerGlow.transform.localPosition = Vector3.zero;
        
        SpriteRenderer centerRenderer = centerGlow.AddComponent<SpriteRenderer>();
        centerRenderer.sprite = CreateOvalSprite(64, 16); // Yatay oval
        centerRenderer.color = sourceGlowColor;
        centerRenderer.sortingOrder = 5;
        centerRenderer.material = new Material(Shader.Find("Sprites/Default"));
        centerGlow.transform.localScale = new Vector3(sourceWidth, sourceHeight, 1f);
        
        // Dış parlama halkası
        GameObject outerGlow = new GameObject("OuterGlow");
        outerGlow.transform.SetParent(sourceGlow.transform);
        outerGlow.transform.localPosition = Vector3.zero;
        
        SpriteRenderer outerRenderer = outerGlow.AddComponent<SpriteRenderer>();
        outerRenderer.sprite = CreateOvalSprite(64, 16);
        outerRenderer.color = new Color(sourceGlowColor.r, sourceGlowColor.g * 0.5f, 0f, 0.4f);
        outerRenderer.sortingOrder = 4;
        outerGlow.transform.localScale = new Vector3(sourceWidth * 1.5f, sourceHeight * 2f, 1f);
        
        // Titreşim için referans sakla
        StartCoroutine(AnimateSourceGlow(centerRenderer, outerRenderer));
    }
    
    private System.Collections.IEnumerator AnimateSourceGlow(SpriteRenderer center, SpriteRenderer outer)
    {
        Color originalCenterColor = center.color;
        Color originalOuterColor = outer.color;
        
        while (true)
        {
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed * 0.5f + flickerOffset, 20f);
            float intensity = 0.8f + noise * 0.4f;
            
            center.color = new Color(
                originalCenterColor.r,
                originalCenterColor.g * intensity,
                originalCenterColor.b,
                originalCenterColor.a * intensity
            );
            
            outer.color = new Color(
                originalOuterColor.r,
                originalOuterColor.g,
                originalOuterColor.b,
                originalOuterColor.a * (0.6f + noise * 0.4f)
            );
            
            // Hafif boyut değişimi
            float scaleNoise = 1f + (noise - 0.5f) * 0.1f;
            center.transform.localScale = new Vector3(sourceWidth * scaleNoise, sourceHeight, 1f);
            
            yield return null;
        }
    }
    
    private Sprite CreateOvalSprite(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[width * height];
        Vector2 center = new Vector2(width / 2f, height / 2f);
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Oval mesafe hesaplama
                float dx = (x - center.x) / (width / 2f);
                float dy = (y - center.y) / (height / 2f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                float alpha = 1f - Mathf.Clamp01(dist);
                alpha = alpha * alpha * alpha; // Daha yumuşak kenarlar
                
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }
    
    // Editor'da görsel yardım
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, lightRadius);
    }
}
