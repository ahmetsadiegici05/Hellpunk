using UnityEngine;

/// <summary>
/// İyileşme efekti - yeşil parçacıklar ve parlama
/// Can kazanıldığında çağrılır
/// </summary>
public class HealVFX : MonoBehaviour
{
    public static HealVFX Instance { get; private set; }
    
    [Header("Parçacık Ayarları")]
    [SerializeField] private Color healColor = new Color(0.3f, 1f, 0.4f, 1f);
    [SerializeField] private int particleCount = 12;
    [SerializeField] private float particleSpeed = 2f;
    [SerializeField] private float particleSize = 0.12f;
    
    private ParticleSystem healParticles;
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
        CreateHealParticles();
        CreateGlowParticles();
    }
    
    private void CreateParticleMaterial()
    {
        // ParticleHelper kullan
        particleMaterial = ParticleHelper.CreateAdditiveMaterial(ParticleHelper.GetGlowTexture());
    }
    
    private void CreateHealParticles()
    {
        GameObject particleObj = new GameObject("HealParticles");
        particleObj.transform.SetParent(transform);
        
        healParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = healParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(particleSpeed * 0.5f, particleSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.5f, particleSize);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.8f; // Yukarı yüz
        main.startColor = healColor;
        main.playOnAwake = false;
        
        var emission = healParticles.emission;
        emission.rateOverTime = 0;
        
        // Şekil - etrafı saran küre
        var shape = healParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;
        
        // Boyut
        var sizeOverLifetime = healParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk
        var colorOverLifetime = healParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(healColor, 0.2f),
                new GradientColorKey(new Color(0.2f, 0.8f, 0.3f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(1f, 0.3f),
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
        GameObject glowObj = new GameObject("HealGlow");
        glowObj.transform.SetParent(transform);
        
        glowParticles = glowObj.AddComponent<ParticleSystem>();
        
        var main = glowParticles.main;
        main.startLifetime = 0.4f;
        main.startSpeed = 0f;
        main.startSize = 2f;
        main.maxParticles = 5;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(healColor.r, healColor.g, healColor.b, 0.4f);
        main.playOnAwake = false;
        
        var emission = glowParticles.emission;
        emission.rateOverTime = 0;
        
        // Boyut
        var sizeOverLifetime = glowParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 1.2f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk
        var colorOverLifetime = glowParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(healColor, 0f),
                new GradientColorKey(healColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.5f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var renderer = glowObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 19;
        renderer.material = particleMaterial;
    }
    
    /// <summary>
    /// İyileşme efektini oynat
    /// </summary>
    public void PlayHealEffect(Vector3 position)
    {
        if (healParticles != null)
        {
            healParticles.transform.position = position;
            healParticles.Emit(particleCount);
        }
        
        if (glowParticles != null)
        {
            glowParticles.transform.position = position;
            glowParticles.Emit(1);
        }
    }
    
    /// <summary>
    /// Transform üzerinde efekt (oyuncu için)
    /// </summary>
    public void PlayHealEffect(Transform target)
    {
        if (target != null)
            PlayHealEffect(target.position);
    }
}
