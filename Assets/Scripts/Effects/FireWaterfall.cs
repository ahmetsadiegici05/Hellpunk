using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Hareketli ve parlak ateş şelalesi efekti
/// ParticleHelper kullanarak yumuşak parçacıklar oluşturur - kare görünüm yok
/// </summary>
public class FireWaterfall : MonoBehaviour
{
    [Header("Şelale Ayarları")]
    [SerializeField] private float width = 2f;
    [SerializeField] private float height = 5f;
    [SerializeField] private float particleSpeed = 3f;
    [SerializeField] private int particleCount = 150;
    
    [Header("Renk Ayarları")]
    [SerializeField] private Color fireColorBright = new Color(1f, 0.95f, 0.5f); // Parlak sarı
    [SerializeField] private Color fireColorMid = new Color(1f, 0.6f, 0.1f); // Turuncu
    [SerializeField] private Color fireColorEnd = new Color(0.9f, 0.25f, 0.05f); // Kırmızı
    
    [Header("Işık Ayarları")]
    [SerializeField] private bool enableLight = true;
    [SerializeField] private float lightIntensity = 2f;
    [SerializeField] private float lightRange = 6f;
    [SerializeField] private Color lightColor = new Color(1f, 0.6f, 0.2f);
    [SerializeField] private float lightFlickerSpeed = 10f;
    [SerializeField] private float lightFlickerAmount = 0.4f;
    
    [Header("Kıvılcım Ayarları")]
    [SerializeField] private bool enableSparks = true;
    [SerializeField] private int sparkCount = 40;
    
    [Header("Hasar Ayarları")]
    [SerializeField] private bool dealsDamage = true;
    [SerializeField] private float damagePerTick = 0.5f;
    [SerializeField] private float damageTickRate = 0.8f; // Her 0.8 saniyede bir hasar
    [SerializeField] private float minimumHealthLeft = 1f; // Oyuncuyu öldürmez, minimum bu kadar can bırakır
    
    private ParticleSystem mainFireSystem;
    private ParticleSystem innerFireSystem;
    private ParticleSystem sparkSystem;
    private ParticleSystem glowSystem;
    private Light2D fireLight;
    private Light2D topLight;
    private float baseLightIntensity;
    private float flickerOffset;
    
    private Material additiveMaterial;
    private Material glowMaterial;
    
    private BoxCollider2D damageCollider;
    private float lastDamageTime;
    
    private void Start()
    {
        flickerOffset = Random.Range(0f, 100f);
        
        // ParticleHelper'dan material al - yumuşak kenarlar için
        additiveMaterial = ParticleHelper.CreateAdditiveMaterial(ParticleHelper.GetGlowTexture());
        glowMaterial = ParticleHelper.CreateAdditiveMaterial(ParticleHelper.GetSoftCircleTexture());
        
        CreateMainFire();
        CreateInnerFire();
        
        if (enableSparks)
            CreateSparkSystem();
            
        CreateGlowSystem();
        
        if (enableLight)
            CreateLights();
            
        if (dealsDamage)
            CreateDamageCollider();
    }
    
    private void Update()
    {
        // Işık titremesi - organik
        if (fireLight != null && enableLight)
        {
            float noise1 = Mathf.PerlinNoise(Time.time * lightFlickerSpeed + flickerOffset, 0f);
            float noise2 = Mathf.PerlinNoise(0f, Time.time * lightFlickerSpeed * 0.7f + flickerOffset);
            float flicker = (noise1 + noise2) * 0.5f;
            
            fireLight.intensity = baseLightIntensity + (flicker - 0.5f) * lightFlickerAmount * 2f;
            
            if (topLight != null)
            {
                topLight.intensity = baseLightIntensity * 0.6f + (1f - flicker) * lightFlickerAmount;
            }
        }
    }
    
    private void CreateMainFire()
    {
        GameObject fireObj = new GameObject("MainFire");
        fireObj.transform.SetParent(transform);
        fireObj.transform.localPosition = Vector3.zero;
        
        mainFireSystem = fireObj.AddComponent<ParticleSystem>();
        
        var main = mainFireSystem.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(height / particleSpeed * 0.7f, height / particleSpeed * 0.85f); // Daha tutarlı lifetime
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleSpeed * 0.95f, particleSpeed * 1.05f); // Daha az hız varyasyonu
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
        main.maxParticles = particleCount * 5; // Daha fazla particle
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.2f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        
        // Renk gradyanı - ERKEN fadeout
        var colorOverLifetime = mainFireSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(fireColorBright, 0f),
                new GradientColorKey(fireColorMid, 0.35f),
                new GradientColorKey(fireColorEnd, 0.65f),
                new GradientColorKey(new Color(0.4f, 0.1f, 0.02f), 0.85f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.7f, 0f),
                new GradientAlphaKey(0.85f, 0.15f),
                new GradientAlphaKey(0.6f, 0.5f),
                new GradientAlphaKey(0.2f, 0.75f),
                new GradientAlphaKey(0f, 0.88f) // Çok erken fadeout - kare görünmeden kaybolsun
            }
        );
        colorOverLifetime.color = gradient;
        
        // Emission - yüksek ve sabit
        var emission = mainFireSystem.emission;
        emission.rateOverTime = particleCount * 1.5f; // Daha yoğun
        
        // Shape - üstten aşağı
        var shape = mainFireSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(width, 0.15f, 1f);
        shape.rotation = new Vector3(90f, 0f, 0f);
        
        // Size over lifetime - SABIT boyut
        var sizeOverLifetime = mainFireSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(new Keyframe(0f, 0.75f, 0f, 0.8f));
        sizeCurve.AddKey(new Keyframe(0.2f, 1f, 0f, 0f));
        sizeCurve.AddKey(new Keyframe(0.6f, 1.05f, 0f, 0f));
        sizeCurve.AddKey(new Keyframe(0.85f, 1f, 0f, 0f));
        sizeCurve.AddKey(new Keyframe(1f, 0.9f, 0f, 0f)); // Neredeyse hiç küçülmesin
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Noise - hafif organik hareket (yoğunluğu bozmayacak şekilde)
        var noise = mainFireSystem.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.1f, 0.2f); // Daha az dağılma
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.3f;
        noise.damping = true;
        
        // Rotation
        var rotationOverLifetime = mainFireSystem.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        
        // Renderer - ParticleHelper material kullan
        var renderer = fireObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 10;
        renderer.material = additiveMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
    }
    
    private void CreateInnerFire()
    {
        GameObject innerObj = new GameObject("InnerFire");
        innerObj.transform.SetParent(transform);
        innerObj.transform.localPosition = Vector3.zero;
        
        innerFireSystem = innerObj.AddComponent<ParticleSystem>();
        
        var main = innerFireSystem.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(height / particleSpeed * 0.5f, height / particleSpeed * 0.65f); // Daha uzun ömür
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleSpeed * 0.97f, particleSpeed * 1.03f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.maxParticles = particleCount * 2; // Daha fazla
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.15f;
        
        // Daha parlak iç renk
        var colorOverLifetime = innerFireSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient innerGradient = new Gradient();
        innerGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.85f), 0f), // Neredeyse beyaz
                new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0.3f),
                new GradientColorKey(new Color(1f, 0.65f, 0.2f), 0.55f),
                new GradientColorKey(new Color(1f, 0.4f, 0.05f), 0.8f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.85f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(0.5f, 0.55f),
                new GradientAlphaKey(0.1f, 0.8f),
                new GradientAlphaKey(0f, 0.9f)
            }
        );
        colorOverLifetime.color = innerGradient;
        
        // Emission - yoğun
        var emission = innerFireSystem.emission;
        emission.rateOverTime = particleCount; // Artırıldı
        
        // Shape - daha dar merkez
        var shape = innerFireSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(width * 0.4f, 0.1f, 1f);
        shape.rotation = new Vector3(90f, 0f, 0f);
        
        // Size over lifetime
        var sizeOverLifetime = innerFireSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.8f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(0.7f, 1f);
        sizeCurve.AddKey(1f, 0.85f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Noise - minimal
        var noise = innerFireSystem.noise;
        noise.enabled = true;
        noise.strength = 0.08f;
        noise.frequency = 1f;
        
        // Renderer
        var renderer = innerObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 11;
        renderer.material = additiveMaterial;
    }
    
    private void CreateSparkSystem()
    {
        GameObject sparkObj = new GameObject("Sparks");
        sparkObj.transform.SetParent(transform);
        sparkObj.transform.localPosition = Vector3.zero;
        
        sparkSystem = sparkObj.AddComponent<ParticleSystem>();
        
        var main = sparkSystem.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.maxParticles = sparkCount * 2;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.4f;
        main.startColor = new Color(1f, 0.95f, 0.7f, 1f);
        
        // Emission
        var emission = sparkSystem.emission;
        emission.rateOverTime = sparkCount;
        
        // Shape
        var shape = sparkSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(width * 0.7f, height * 0.7f, 1f);
        shape.position = new Vector3(0, -height * 0.35f, 0);
        
        // Color
        var colorOverLifetime = sparkSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient sparkGradient = new Gradient();
        sparkGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.9f), 0f),
                new GradientColorKey(new Color(1f, 0.7f, 0.3f), 0.5f),
                new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.7f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = sparkGradient;
        
        // Noise
        var noise = sparkSystem.noise;
        noise.enabled = true;
        noise.strength = 1.5f;
        noise.frequency = 3f;
        
        // Trail - kıvılcım izi
        var trails = sparkSystem.trails;
        trails.enabled = true;
        trails.lifetime = 0.15f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, 0f);
        trails.inheritParticleColor = true;
        
        // Renderer
        var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 13;
        renderer.material = ParticleHelper.CreateAdditiveMaterial(ParticleHelper.GetSparkTexture());
        renderer.trailMaterial = additiveMaterial;
    }
    
    private void CreateGlowSystem()
    {
        // Alt parlama
        GameObject glowObj = new GameObject("BottomGlow");
        glowObj.transform.SetParent(transform);
        glowObj.transform.localPosition = new Vector3(0, -height, 0);
        
        glowSystem = glowObj.AddComponent<ParticleSystem>();
        
        var main = glowSystem.main;
        main.startLifetime = 0.25f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2f);
        main.maxParticles = 15;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        // Emission
        var emission = glowSystem.emission;
        emission.rateOverTime = 15;
        
        // Shape
        var shape = glowSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Rectangle;
        shape.scale = new Vector3(width * 1.2f, 0.2f, 1f);
        
        // Color
        var colorOverLifetime = glowSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        
        Gradient glowGradient = new Gradient();
        glowGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.65f, 0.15f), 0f),
                new GradientColorKey(new Color(1f, 0.4f, 0.05f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.5f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = glowGradient;
        
        // Size
        var sizeOverLifetime = glowSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        
        AnimationCurve glowSizeCurve = new AnimationCurve();
        glowSizeCurve.AddKey(0f, 1f);
        glowSizeCurve.AddKey(1f, 1.3f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, glowSizeCurve);
        
        // Renderer
        var renderer = glowObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 9;
        renderer.material = glowMaterial;
    }
    
    private void CreateLights()
    {
        // Ana ışık - ortada
        GameObject lightObj = new GameObject("FireLight");
        lightObj.transform.SetParent(transform);
        lightObj.transform.localPosition = new Vector3(0, -height / 2f, 0);
        
        fireLight = lightObj.AddComponent<Light2D>();
        fireLight.lightType = Light2D.LightType.Point;
        fireLight.color = lightColor;
        fireLight.intensity = lightIntensity;
        fireLight.pointLightOuterRadius = lightRange;
        fireLight.pointLightInnerRadius = lightRange * 0.2f;
        fireLight.falloffIntensity = 0.5f;
        
        baseLightIntensity = lightIntensity;
        
        // Üst ışık
        GameObject topLightObj = new GameObject("TopLight");
        topLightObj.transform.SetParent(transform);
        topLightObj.transform.localPosition = Vector3.zero;
        
        topLight = topLightObj.AddComponent<Light2D>();
        topLight.lightType = Light2D.LightType.Point;
        topLight.color = new Color(1f, 0.8f, 0.4f);
        topLight.intensity = lightIntensity * 0.6f;
        topLight.pointLightOuterRadius = lightRange * 0.5f;
        topLight.pointLightInnerRadius = lightRange * 0.1f;
    }
    
    private void CreateDamageCollider()
    {
        // Hasar collider'ı ayrı bir child objeye koy
        GameObject damageZone = new GameObject("DamageZone");
        damageZone.transform.SetParent(transform);
        damageZone.transform.localPosition = new Vector3(0, -height / 2f, 0);
        
        damageCollider = damageZone.AddComponent<BoxCollider2D>();
        damageCollider.isTrigger = true;
        damageCollider.size = new Vector2(width, height);
        damageCollider.offset = Vector2.zero;
        
        // Trigger event'lerini almak için bu script'i damage zone'a da ekle
        FireWaterfallDamageZone damageScript = damageZone.AddComponent<FireWaterfallDamageZone>();
        damageScript.Initialize(this);
        
        // Player ile fiziksel çarpışmayı engelle
        StartCoroutine(DisablePlayerCollision());
    }
    
    private System.Collections.IEnumerator DisablePlayerCollision()
    {
        yield return null; // Bir frame bekle
        
        // Player'ı bul ve collider çarpışmasını kapat
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider != null && damageCollider != null)
            {
                Physics2D.IgnoreCollision(damageCollider, playerCollider, true);
            }
        }
    }
    
    // Player spawn olduğunda da çarpışmayı kapat
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && damageCollider != null)
        {
            Physics2D.IgnoreCollision(damageCollider, other, true);
        }
    }
    
    public void ApplyDamageToPlayer(Collider2D other)
    {
        if (!dealsDamage) return;
        
        if (other.CompareTag("Player"))
        {
            if (Time.time - lastDamageTime >= damageTickRate)
            {
                lastDamageTime = Time.time;
                
                Health playerHealth = other.GetComponent<Health>();
                if (playerHealth != null)
                {
                    // Oyuncuyu öldürme - minimum can bırak
                    if (playerHealth.currentHealth > minimumHealthLeft)
                    {
                        float maxDamage = playerHealth.currentHealth - minimumHealthLeft;
                        float actualDamage = Mathf.Min(damagePerTick, maxDamage);
                        
                        if (actualDamage > 0)
                        {
                            playerHealth.TakeDamage(actualDamage);
                            
                            if (DamageVignette.Instance != null)
                            {
                                DamageVignette.Instance.FlashDamage();
                            }
                        }
                    }
                }
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireCube(transform.position + Vector3.down * height / 2f, new Vector3(width, height, 0.1f));
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.down * height / 2f, 0.3f);
    }
}
