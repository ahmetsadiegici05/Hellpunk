using UnityEngine;

/// <summary>
/// Düşmana vuruş efekti - toz, kıvılcım ve impact efektleri
/// EnemyHealth.TakeDamage() içinden çağrılır
/// </summary>
public class HitEffect : MonoBehaviour
{
    public static HitEffect Instance { get; private set; }
    
    [Header("Toz Efekti")]
    [SerializeField] private Color dustColor = new Color(0.8f, 0.7f, 0.5f, 1f);
    [SerializeField] private int dustParticleCount = 8;
    [SerializeField] private float dustSpeed = 3f;
    [SerializeField] private float dustSize = 0.15f;
    
    [Header("Kıvılcım/Impact")]
    [SerializeField] private Color impactColor = new Color(1f, 0.9f, 0.6f, 1f);
    [SerializeField] private int impactParticleCount = 5;
    
    [Header("Ekran Shake")]
    [SerializeField] private bool useScreenShake = true;
    [SerializeField] private float shakeIntensity = 0.08f;
    [SerializeField] private float shakeDuration = 0.1f;
    
    private ParticleSystem dustParticleSystem;
    private ParticleSystem impactParticleSystem;
    private Material particleMaterial;
    private Camera mainCamera;
    private Vector3 originalCameraPos;
    
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
        mainCamera = Camera.main;
        CreateParticleMaterial();
        CreateDustParticleSystem();
        CreateImpactParticleSystem();
    }
    
    private void CreateParticleMaterial()
    {
        // ParticleHelper kullanarak material oluştur
        particleMaterial = ParticleHelper.CreateAlphaBlendMaterial(ParticleHelper.GetSoftCircleTexture());
    }
    
    private void CreateDustParticleSystem()
    {
        GameObject dustObj = new GameObject("DustParticles");
        dustObj.transform.SetParent(transform);
        
        dustParticleSystem = dustObj.AddComponent<ParticleSystem>();
        
        var main = dustParticleSystem.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(dustSpeed * 0.5f, dustSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(dustSize * 0.5f, dustSize);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.3f;
        main.startColor = dustColor;
        main.playOnAwake = false;
        
        // Emisyon
        var emission = dustParticleSystem.emission;
        emission.rateOverTime = 0;
        
        // Şekil - yarım küre
        var shape = dustParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.2f;
        
        // Boyut azalması
        var sizeOverLifetime = dustParticleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk solması
        var colorOverLifetime = dustParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(dustColor, 0f),
                new GradientColorKey(dustColor * 0.7f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Renderer - Alpha blend material
        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 15;
        ParticleHelper.ApplyMaterial(renderer, false); // Alpha blend for dust
    }
    
    private void CreateImpactParticleSystem()
    {
        GameObject impactObj = new GameObject("ImpactParticles");
        impactObj.transform.SetParent(transform);
        
        impactParticleSystem = impactObj.AddComponent<ParticleSystem>();
        
        var main = impactParticleSystem.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;
        main.startColor = impactColor;
        main.playOnAwake = false;
        
        // Emisyon
        var emission = impactParticleSystem.emission;
        emission.rateOverTime = 0;
        
        // Şekil - dışa doğru patlama
        var shape = impactParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 60f;
        shape.radius = 0.1f;
        
        // Renk solması
        var colorOverLifetime = impactParticleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(impactColor, 0.3f),
                new GradientColorKey(impactColor * 0.5f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Renderer - Additive material for impact sparks
        var renderer = impactObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 16;
        ParticleHelper.ApplyMaterial(renderer, true, ParticleHelper.GetSparkTexture()); // Additive for sparks
    }
    
    /// <summary>
    /// Vuruş efektini oynat
    /// </summary>
    /// <param name="position">Vuruş pozisyonu</param>
    /// <param name="hitDirection">Vuruş yönü (oyuncudan düşmana)</param>
    public void PlayHitEffect(Vector3 position, Vector2 hitDirection)
    {
        // Hit Stop efekti
        if (HitStop.Instance != null)
        {
            HitStop.Instance.NormalHit();
        }
        
        // Toz efekti
        if (dustParticleSystem != null)
        {
            dustParticleSystem.transform.position = position;
            
            // Yönü ayarla
            var shape = dustParticleSystem.shape;
            float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
            shape.rotation = new Vector3(0f, 0f, angle - 90f);
            
            dustParticleSystem.Emit(dustParticleCount);
        }
        
        // Impact efekti
        if (impactParticleSystem != null)
        {
            impactParticleSystem.transform.position = position;
            
            var shape = impactParticleSystem.shape;
            float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
            shape.rotation = new Vector3(0f, 0f, angle);
            
            impactParticleSystem.Emit(impactParticleCount);
        }
        
        // Ekran shake
        if (useScreenShake && mainCamera != null)
        {
            StartCoroutine(ScreenShake());
        }
    }
    
    /// <summary>
    /// Basit vuruş efekti (yön belirtmeden)
    /// </summary>
    public void PlayHitEffect(Vector3 position)
    {
        PlayHitEffect(position, Vector2.right);
    }
    
    /// <summary>
    /// Ağır/Kritik vuruş efekti - daha fazla parçacık ve güçlü shake
    /// </summary>
    public void PlayHeavyHitEffect(Vector3 position, Vector2 hitDirection)
    {
        // Daha fazla parçacık
        if (impactParticleSystem != null)
        {
            var shape = impactParticleSystem.shape;
            shape.rotation = new Vector3(0, 0, Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg);
            impactParticleSystem.transform.position = position;
            impactParticleSystem.Emit(15); // Normal 8, kritik 15
        }
        
        if (dustParticleSystem != null)
        {
            dustParticleSystem.transform.position = position;
            dustParticleSystem.Emit(15); // Normal 6, kritik 15
        }
        
        // Güçlü ekran sarsıntısı
        float originalIntensity = shakeIntensity;
        float originalDuration = shakeDuration;
        shakeIntensity = 0.15f; // Normal 0.08, kritik 0.15
        shakeDuration = 0.12f;  // Normal 0.08, kritik 0.12
        StartCoroutine(ScreenShake());
        shakeIntensity = originalIntensity;
        shakeDuration = originalDuration;
    }
    
    private System.Collections.IEnumerator ScreenShake()
    {
        if (mainCamera == null) yield break;
        
        float elapsed = 0f;
        Vector3 originalPos = mainCamera.transform.localPosition;
        
        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeIntensity;
            float y = Random.Range(-1f, 1f) * shakeIntensity;
            
            mainCamera.transform.localPosition = new Vector3(
                originalPos.x + x, 
                originalPos.y + y, 
                originalPos.z
            );
            
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Orijinal pozisyona dön
        mainCamera.transform.localPosition = originalPos;
    }
    
    /// <summary>
    /// Kritik vuruş için daha güçlü efekt
    /// </summary>
    public void PlayCriticalHitEffect(Vector3 position, Vector2 hitDirection)
    {
        // Normal efektin 2 katı parçacık
        if (dustParticleSystem != null)
        {
            dustParticleSystem.transform.position = position;
            dustParticleSystem.Emit(dustParticleCount * 2);
        }
        
        if (impactParticleSystem != null)
        {
            impactParticleSystem.transform.position = position;
            impactParticleSystem.Emit(impactParticleCount * 2);
        }
        
        // Daha güçlü ekran shake
        if (useScreenShake)
        {
            StartCoroutine(ScreenShake());
            StartCoroutine(ScreenShake()); // İki kere çağır daha yoğun olsun
        }
    }
}
