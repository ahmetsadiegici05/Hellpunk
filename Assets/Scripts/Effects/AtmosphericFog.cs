using UnityEngine;

/// <summary>
/// Atmosferik sis/duman efekti - Arka planda yavaşça hareket eden sis
/// Sahneye ekleyin ve kameranın çocuğu yapın
/// </summary>
public class AtmosphericFog : MonoBehaviour
{
    public static AtmosphericFog Instance { get; private set; }
    
    [Header("Sis Ayarları")]
    [SerializeField] private Color fogColor = new Color(0.3f, 0.25f, 0.35f, 0.4f); // Daha görünür mor-gri
    [SerializeField] private int fogLayerCount = 3;
    [SerializeField] private float fogSpeed = 0.3f;
    [SerializeField] private float fogScale = 20f;
    
    [Header("Parçacık Ayarları")]
    [SerializeField] private int particleCount = 50;
    [SerializeField] private float particleSize = 5f;
    [SerializeField] private float particleLifetime = 10f;
    
    private ParticleSystem[] fogLayers;
    private Camera mainCamera;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        CreateFogLayers();
    }
    
    private void LateUpdate()
    {
        // Kamerayı takip et
        if (mainCamera != null)
        {
            transform.position = new Vector3(
                mainCamera.transform.position.x,
                mainCamera.transform.position.y,
                0f
            );
        }
    }
    
    private void CreateFogLayers()
    {
        fogLayers = new ParticleSystem[fogLayerCount];
        
        for (int i = 0; i < fogLayerCount; i++)
        {
            float layerDepth = 5f + (i * 2f); // Kameranın önünde, pozitif z (2D'de kameraya yakın)
            float layerAlpha = fogColor.a * (1f - i * 0.15f); // Katmanlar arası solma
            float layerSpeed = fogSpeed * (1f + i * 0.3f);
            
            fogLayers[i] = CreateFogLayer($"FogLayer_{i}", layerDepth, layerAlpha, layerSpeed);
        }
    }
    
    private ParticleSystem CreateFogLayer(string name, float zOffset, float alpha, float speed)
    {
        GameObject fogObj = new GameObject(name);
        fogObj.transform.SetParent(transform);
        fogObj.transform.localPosition = Vector3.zero; // Z pozisyonunu sıfırla, sorting ile kontrol edelim
        
        ParticleSystem ps = fogObj.AddComponent<ParticleSystem>();
        
        var main = ps.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = speed;
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.5f, particleSize * 1.5f);
        main.maxParticles = particleCount * 2;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(fogColor.r, fogColor.g, fogColor.b, alpha);
        main.loop = true;
        main.prewarm = true;
        
        // Emisyon
        var emission = ps.emission;
        emission.rateOverTime = particleCount / particleLifetime;
        
        // Şekil - Geniş dikdörtgen alan
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(fogScale * 2f, fogScale, 1f);
        
        // Hareket - yatay sürüklenme
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-speed * 0.5f, speed * 0.5f);
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-speed * 0.2f, speed * 0.2f);
        
        // Boyut değişimi
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.5f, 1f);
        sizeCurve.AddKey(1f, 0.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk solma
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(fogColor, 0f),
                new GradientColorKey(fogColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.8f, 0.15f),
                new GradientAlphaKey(0.8f, 0.85f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Döndürme
        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.2f, 0.2f);
        
        // Renderer
        var renderer = fogObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Default"; // Default layer kullan
        renderer.sortingOrder = 100; // Ön planda görünür olsun
        
        // Material - ParticleHelper kullan
        ParticleHelper.ApplyMaterial(renderer, false, ParticleHelper.GetGlowTexture());
        
        return ps;
    }
    
    /// <summary>
    /// Sis yoğunluğunu ayarla (0-1)
    /// </summary>
    public void SetFogIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        
        foreach (var layer in fogLayers)
        {
            if (layer != null)
            {
                var main = layer.main;
                Color c = main.startColor.color;
                main.startColor = new Color(c.r, c.g, c.b, fogColor.a * intensity);
            }
        }
    }
}
