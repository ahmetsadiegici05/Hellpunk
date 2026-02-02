using UnityEngine;

/// <summary>
/// Coin toplama efekti - parlama ve yükselen parçacıklar
/// CoinUI'dan çağrılır
/// </summary>
public class CoinCollectEffect : MonoBehaviour
{
    public static CoinCollectEffect Instance { get; private set; }
    
    [Header("Parçacık Ayarları")]
    [SerializeField] private Color coinColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private int particleCount = 8;
    [SerializeField] private float particleSpeed = 3f;
    [SerializeField] private float particleSize = 0.1f;
    
    [Header("Parlama")]
    [SerializeField] private Color glowColor = new Color(1f, 0.9f, 0.4f, 0.6f);
    [SerializeField] private float glowDuration = 0.3f;
    
    private ParticleSystem coinParticles;
    private ParticleSystem glowParticles;
    private Material particleMaterial;
    
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
        CreateParticleMaterial();
        CreateCoinParticles();
        CreateGlowParticles();
    }
    
    private void CreateParticleMaterial()
    {
        // ParticleHelper kullan
        particleMaterial = ParticleHelper.CreateAdditiveMaterial(ParticleHelper.GetSparkTexture());
    }
    
    private void CreateCoinParticles()
    {
        GameObject particleObj = new GameObject("CoinParticles");
        particleObj.transform.SetParent(transform);
        
        coinParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = coinParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleSpeed * 0.5f, particleSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.5f, particleSize);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.5f; // Yukarı yüzme
        main.startColor = coinColor;
        main.playOnAwake = false;
        
        var emission = coinParticles.emission;
        emission.rateOverTime = 0;
        
        // Şekil - yukarı doğru koni
        var shape = coinParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 30f;
        shape.radius = 0.15f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        
        // Boyut
        var sizeOverLifetime = coinParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(0.5f, 1.2f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk
        var colorOverLifetime = coinParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(coinColor, 0.2f),
                new GradientColorKey(coinColor * 0.8f, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 20;
        renderer.material = particleMaterial;
    }
    
    private void CreateGlowParticles()
    {
        GameObject glowObj = new GameObject("GlowParticles");
        glowObj.transform.SetParent(transform);
        
        glowParticles = glowObj.AddComponent<ParticleSystem>();
        
        var main = glowParticles.main;
        main.startLifetime = glowDuration;
        main.startSpeed = 0f;
        main.startSize = 1.5f;
        main.maxParticles = 10;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = glowColor;
        main.playOnAwake = false;
        
        var emission = glowParticles.emission;
        emission.rateOverTime = 0;
        
        // Boyut - hızlı büyüyüp kaybolma
        var sizeOverLifetime = glowParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 1.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk - hızlı solma
        var colorOverLifetime = glowParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(glowColor, 0f),
                new GradientColorKey(glowColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var renderer = glowObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 19;
        renderer.material = particleMaterial;
    }
    
    /// <summary>
    /// Coin toplama efektini oynat
    /// </summary>
    public void PlayCoinEffect(Vector3 position)
    {
        // Parçacıklar
        if (coinParticles != null)
        {
            coinParticles.transform.position = position;
            coinParticles.Emit(particleCount);
        }
        
        // Parlama
        if (glowParticles != null)
        {
            glowParticles.transform.position = position;
            glowParticles.Emit(1);
        }
    }
    
    /// <summary>
    /// Büyük coin toplama (daha çok parçacık)
    /// </summary>
    public void PlayBigCoinEffect(Vector3 position)
    {
        if (coinParticles != null)
        {
            coinParticles.transform.position = position;
            coinParticles.Emit(particleCount * 2);
        }
        
        if (glowParticles != null)
        {
            glowParticles.transform.position = position;
            glowParticles.Emit(1);
        }
    }
}
