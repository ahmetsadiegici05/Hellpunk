using UnityEngine;

/// <summary>
/// Cehennem atmosferi için ateş/lav efektleri
/// Yukarıdan düşen ateş topları ve parıldayan ember parçacıkları
/// </summary>
public class HellFireEffects : MonoBehaviour
{
    [Header("Düşen Ateş Topları")]
    [Tooltip("Ateş topu efektini aktif et")]
    public bool enableFireballs = true;
    [Range(0.5f, 5f)]
    public float fireballInterval = 2f;
    [Range(1f, 10f)]
    public float fireballSpeed = 4f;
    [Range(0.3f, 2f)]
    public float fireballSize = 0.8f;
    [Tooltip("Parlama yoğunluğu (HDR)")]
    [Range(1f, 10f)]
    public float fireballGlowIntensity = 3f;

    [Header("Ember Parçacıkları (Kıvılcımlar)")]
    public bool enableEmbers = true;
    [Range(10, 200)]
    public int emberCount = 50;
    [Range(0.5f, 5f)]
    public float emberSpeed = 1.5f;

    [Header("Lav Parıldaması")]
    public bool enableLavaGlow = true;
    [Range(0.5f, 3f)]
    public float lavaPulseSpeed = 1f;
    [Range(1f, 5f)]
    public float lavaGlowIntensity = 2f;

    [Header("Renkler (HDR için yüksek değerler kullan)")]
    public Color fireColor = new Color(1f, 0.4f, 0.1f, 1f);
    public Color emberColor = new Color(1f, 0.6f, 0.2f, 1f);
    public Color lavaColor = new Color(1f, 0.2f, 0.05f, 1f);

    [Header("Spawn Alanı")]
    public float spawnWidth = 30f;
    public float spawnHeight = 15f;
    public float despawnY = -10f;
    
    [Header("Derinlik (Arka Plan)")]
    [Tooltip("Z pozisyonu - yüksek değer = daha uzakta (20-50 arası background için)")]
    public float zDepth = 30f;
    [Tooltip("Sorting Order - negatif = arkada")]
    public int sortingOrder = -80;
    [Tooltip("Uzaklık nedeniyle boyut çarpanı (0.3 = %30 boyut)")]
    [Range(0.1f, 1f)]
    public float distanceScale = 0.4f;
    
    [Header("Sprite (Opsiyonel)")]
    [Tooltip("Özel ateş topu sprite'ı - boş bırakılırsa yuvarlak oluşturulur")]
    public Sprite fireballSprite;
    public Sprite emberSprite;
    
    [Header("Kamera Takibi")]
    [Tooltip("Efektler kamerayı takip etsin - tüm level boyunca devam eder")]
    public bool followCamera = true;
    private Transform cameraTransform;

    private ParticleSystem fireballPS;
    private ParticleSystem emberPS;
    private ParticleSystem lavaGlowPS;

    void Start()
    {
        // Ana kamerayı bul
        if (followCamera && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        if (enableFireballs) CreateFireballSystem();
        if (enableEmbers) CreateEmberSystem();
        if (enableLavaGlow) CreateLavaGlowSystem();
    }
    
    void LateUpdate()
    {
        // Kamerayı takip et - efektler her zaman kamera üstünde spawn olsun
        if (followCamera && cameraTransform != null)
        {
            Vector3 newPos = new Vector3(
                cameraTransform.position.x,
                cameraTransform.position.y,
                0
            );
            transform.position = newPos;
        }
    }

    void CreateFireballSystem()
    {
        GameObject fireballObj = new GameObject("Fireballs");
        fireballObj.transform.SetParent(transform);
        fireballObj.transform.localPosition = new Vector3(0, spawnHeight / 2, zDepth);

        fireballPS = fireballObj.AddComponent<ParticleSystem>();
        var main = fireballPS.main;
        main.loop = true;
        main.startLifetime = spawnHeight / fireballSpeed + 2f;
        main.startSpeed = fireballSpeed * distanceScale; // Uzakta daha yavaş görünsün
        main.startSize = fireballSize * distanceScale; // Uzakta daha küçük görünsün
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.3f;

        // HDR Renk - Bloom için yüksek intensity
        Color hdrFireColor = fireColor * fireballGlowIntensity;
        main.startColor = new ParticleSystem.MinMaxGradient(hdrFireColor);

        // Emission
        var emission = fireballPS.emission;
        emission.rateOverTime = 1f / fireballInterval;

        // Shape - üstten yatay çizgi
        var shape = fireballPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(spawnWidth, 0.1f, 0.1f);
        shape.rotation = new Vector3(90, 0, 0); // Aşağı doğru

        // Velocity - aşağı doğru
        var velocity = fireballPS.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(-fireballSpeed);

        // Size over lifetime - biraz küçülsün
        var sizeOverLife = fireballPS.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.8f, 0.8f);
        sizeCurve.AddKey(1f, 0.3f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Color over lifetime - parlama efekti
        var colorOverLife = fireballPS.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(fireColor, 0.3f),
                new GradientColorKey(fireColor * 0.5f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = gradient;

        // Trail kapalı - sadece ateş topu
        var trails = fireballPS.trails;
        trails.enabled = false;

        // Renderer - HER ZAMAN yuvarlak texture kullan
        var renderer = fireballObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        // Her zaman yuvarlak circle texture kullan (kare sorununu önle)
        Material fireballMat = CreateCircleMaterial(fireColor * fireballGlowIntensity);
        
        // Eğer sprite varsa, onun texture'ını kullan ama yine de circle material ile
        if (fireballSprite != null && fireballSprite.texture != null)
        {
            fireballMat = CreateCleanSpriteMaterial(fireballSprite, fireColor * fireballGlowIntensity);
        }
        
        renderer.material = fireballMat;

        // Sub-emitter için kıvılcımlar
        CreateFireballSparks(fireballObj);
    }

    void CreateFireballSparks(GameObject parent)
    {
        GameObject sparksObj = new GameObject("FireballSparks");
        sparksObj.transform.SetParent(parent.transform);
        sparksObj.transform.localPosition = Vector3.zero;

        ParticleSystem sparksPS = sparksObj.AddComponent<ParticleSystem>();
        var main = sparksPS.main;
        main.loop = true;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new ParticleSystem.MinMaxGradient(emberColor * 3f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.2f; // Hafif yukarı

        var emission = sparksPS.emission;
        emission.rateOverTime = 0;

        // Fireball'dan emit et
        var subEmitters = fireballPS.subEmitters;
        subEmitters.enabled = true;
        subEmitters.AddSubEmitter(sparksPS, ParticleSystemSubEmitterType.Birth, ParticleSystemSubEmitterProperties.InheritNothing);

        var renderer = sparksObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateCircleMaterial(emberColor * 4f); // Yuvarlak texture
        renderer.sortingOrder = -29;
    }

    void CreateEmberSystem()
    {
        GameObject emberObj = new GameObject("Embers");
        emberObj.transform.SetParent(transform);
        emberObj.transform.localPosition = Vector3.zero;

        emberPS = emberObj.AddComponent<ParticleSystem>();
        var main = emberPS.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, emberSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
        main.maxParticles = emberCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.1f; // Hafif yukarı uçsun

        // HDR renk
        Color hdrEmberColor = emberColor * 4f;
        main.startColor = new ParticleSystem.MinMaxGradient(hdrEmberColor, hdrEmberColor * 0.7f);

        var emission = emberPS.emission;
        emission.rateOverTime = emberCount / 3f;

        // Shape - geniş alan
        var shape = emberPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(spawnWidth, spawnHeight, 0.1f);

        // Velocity - yavaşça yukarı ve yana
        var velocity = emberPS.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.3f, 1f);

        // Noise - organik hareket
        var noise = emberPS.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.5f;

        // Color over lifetime - yanıp sönme
        var colorOverLife = emberPS.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(emberColor, 0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(emberColor * 0.5f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.8f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = gradient;

        // Renderer - yuvarlak tanecikler
        var renderer = emberObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateCircleMaterial(hdrEmberColor); // Yuvarlak texture
        renderer.sortingOrder = -25;
    }

    void CreateLavaGlowSystem()
    {
        GameObject lavaObj = new GameObject("LavaGlow");
        lavaObj.transform.SetParent(transform);
        lavaObj.transform.localPosition = new Vector3(0, despawnY + 2f, 0);

        lavaGlowPS = lavaObj.AddComponent<ParticleSystem>();
        var main = lavaGlowPS.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f;

        // HDR renk - çok parlak
        Color hdrLavaColor = lavaColor * lavaGlowIntensity * 2f;
        main.startColor = new ParticleSystem.MinMaxGradient(hdrLavaColor);

        var emission = lavaGlowPS.emission;
        emission.rateOverTime = 10f;

        // Shape - alt çizgi (lav yüzeyi)
        var shape = lavaGlowPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(spawnWidth, 0.5f, 0.1f);

        // Velocity
        var velocity = lavaGlowPS.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(1f, 3f);

        // Size over lifetime
        var sizeOverLife = lavaGlowPS.sizeOverLifetime;
        sizeOverLife.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.5f);
        curve.AddKey(0.5f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

        // Color - fade out
        var colorOverLife = lavaGlowPS.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(lavaColor, 0f),
                new GradientColorKey(lavaColor * 1.5f, 0.3f),
                new GradientColorKey(lavaColor * 0.3f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = gradient;

        // Renderer - yuvarlak lav tanecikleri
        var renderer = lavaObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateCircleMaterial(hdrLavaColor); // Yuvarlak texture
        renderer.sortingOrder = -35;
    }

    Material CreateGlowMaterial(Color hdrColor)
    {
        // Additive blending ile parlayan material
        Material mat = new Material(Shader.Find("Sprites/Default"));
        
        // URP için particle shader dene
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader != null)
        {
            mat = new Material(particleShader);
            mat.SetColor("_BaseColor", hdrColor);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Additive
        }
        else
        {
            mat.SetColor("_Color", hdrColor);
        }
        
        mat.renderQueue = 3000;
        return mat;
    }

    Material CreateCircleMaterial(Color hdrColor)
    {
        // Yuvarlak parlayan texture oluştur
        Texture2D circleTexture = CreateCircleTexture(64);
        
        Material mat = new Material(Shader.Find("Sprites/Default"));
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader != null)
        {
            mat = new Material(particleShader);
            mat.SetTexture("_BaseMap", circleTexture);
            mat.SetColor("_BaseColor", hdrColor);
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
        }
        else
        {
            mat.mainTexture = circleTexture;
            mat.SetColor("_Color", hdrColor);
        }
        
        mat.renderQueue = 3000;
        return mat;
    }
    
    Material CreateSpriteMaterial(Sprite sprite, Color hdrColor)
    {
        // Sprite için transparent cutout material
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material mat;
        
        if (particleShader != null)
        {
            mat = new Material(particleShader);
            mat.SetTexture("_BaseMap", sprite.texture);
            mat.SetColor("_BaseColor", hdrColor);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Additive
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.SetFloat("_Cutoff", 0.1f);
        }
        else
        {
            // Fallback - Particles/Alpha Blended shader
            Shader alphaBlend = Shader.Find("Particles/Alpha Blended");
            if (alphaBlend != null)
            {
                mat = new Material(alphaBlend);
            }
            else
            {
                mat = new Material(Shader.Find("Sprites/Default"));
            }
            mat.mainTexture = sprite.texture;
            mat.SetColor("_TintColor", hdrColor);
        }
        
        mat.renderQueue = 3000;
        return mat;
    }
    
    Material CreateCleanSpriteMaterial(Sprite sprite, Color hdrColor)
    {
        // Sprite'ı düzgün şeffaf arka planla göster
        Material mat;
        
        // Önce Particles/Alpha Blended dene (en iyi sonuç)
        Shader alphaBlendShader = Shader.Find("Particles/Alpha Blended");
        if (alphaBlendShader != null)
        {
            mat = new Material(alphaBlendShader);
            mat.mainTexture = sprite.texture;
            mat.SetColor("_TintColor", new Color(hdrColor.r, hdrColor.g, hdrColor.b, 0.5f));
            mat.renderQueue = 3000;
            return mat;
        }
        
        // URP varsa onu dene
        Shader urpShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpShader != null)
        {
            mat = new Material(urpShader);
            mat.SetTexture("_BaseMap", sprite.texture);
            mat.SetColor("_BaseColor", hdrColor);
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 1); // Alpha blend (Additive değil!)
            mat.SetFloat("_ColorMode", 0);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
            return mat;
        }
        
        // Son çare - Legacy/Particles/Alpha Blended
        Shader legacyShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (legacyShader != null)
        {
            mat = new Material(legacyShader);
            mat.mainTexture = sprite.texture;
            mat.SetColor("_TintColor", hdrColor);
            mat.renderQueue = 3000;
            return mat;
        }
        
        // En son - Sprites/Default
        mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = sprite.texture;
        mat.color = hdrColor;
        mat.renderQueue = 3000;
        return mat;
    }
    
    Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                
                if (distance < radius)
                {
                    // Kenardan merkeze doğru parlama (gradient)
                    float alpha = 1f - (distance / radius);
                    alpha = Mathf.Pow(alpha, 0.5f); // Daha yumuşak geçiş
                    
                    // Merkez daha parlak
                    float brightness = Mathf.Pow(1f - (distance / radius), 2f);
                    Color color = Color.Lerp(Color.white, new Color(1f, 0.6f, 0.2f), distance / radius);
                    color.a = alpha;
                    
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        return texture;
    }

    void OnDrawGizmosSelected()
    {
        // Spawn alanını göster
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnWidth, spawnHeight, 0.1f));
        
        // Lav çizgisi
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawLine(
            transform.position + new Vector3(-spawnWidth/2, despawnY, 0),
            transform.position + new Vector3(spawnWidth/2, despawnY, 0)
        );
    }
}
