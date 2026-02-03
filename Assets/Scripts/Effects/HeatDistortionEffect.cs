using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Lav yakınında sıcaklık dalgalanması efekti
/// Oyuncu lava yaklaştığında ekranda distortion ve renk efekti
/// </summary>
public class HeatDistortionEffect : MonoBehaviour
{
    public static HeatDistortionEffect Instance { get; private set; }
    
    [Header("Sıcaklık Efekti Ayarları")]
    [SerializeField] private float maxHeatDistance = 8f;
    [SerializeField] private float minHeatDistance = 2f;
    [SerializeField] private Color heatTintColor = new Color(1f, 0.7f, 0.5f, 0.3f);
    [SerializeField] private float maxVignetteIntensity = 0.4f;
    [SerializeField] private float heatPulseSpeed = 2f;
    
    [Header("Parçacık Efekti")]
    [SerializeField] private bool enableHeatParticles = true;
    [SerializeField] private Color emberColor = new Color(1f, 0.5f, 0.1f, 0.8f);
    [SerializeField] private float emberSpawnRate = 5f;
    
    private Transform playerTransform;
    private Transform[] lavaSources;
    
    // UI Overlay
    private Canvas heatCanvas;
    private UnityEngine.UI.Image heatOverlay;
    private UnityEngine.UI.Image heatVignette;
    
    // Heat particles
    private ParticleSystem heatParticles;
    
    // Current heat level (0-1)
    private float currentHeatLevel = 0f;
    private float targetHeatLevel = 0f;
    
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
        CreateHeatUI();
        if (enableHeatParticles) CreateHeatParticles();
        FindPlayer();
        FindLavaSources();
    }
    
    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }
    
    private void FindLavaSources()
    {
        // Lava tag'li veya LavaController'lı objeleri bul
        GameObject[] lavaObjects = GameObject.FindGameObjectsWithTag("Lava");
        
        // LavaController'ları da bul
        LavaController[] lavaControllers = FindObjectsByType<LavaController>(FindObjectsSortMode.None);
        
        // FireWaterfall'ları da ekle
        FireWaterfall[] fireWaterfalls = FindObjectsByType<FireWaterfall>(FindObjectsSortMode.None);
        
        int totalCount = lavaObjects.Length + lavaControllers.Length + fireWaterfalls.Length;
        lavaSources = new Transform[totalCount];
        
        int index = 0;
        foreach (var obj in lavaObjects)
        {
            lavaSources[index++] = obj.transform;
        }
        foreach (var lava in lavaControllers)
        {
            if (System.Array.IndexOf(lavaSources, lava.transform) < 0)
            {
                lavaSources[index++] = lava.transform;
            }
        }
        foreach (var fire in fireWaterfalls)
        {
            lavaSources[index++] = fire.transform;
        }
    }
    
    private void CreateHeatUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("HeatEffectCanvas");
        canvasObj.transform.SetParent(transform);
        
        heatCanvas = canvasObj.AddComponent<Canvas>();
        heatCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        heatCanvas.sortingOrder = 90;
        
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        
        // Heat tint overlay
        GameObject overlayObj = new GameObject("HeatOverlay");
        overlayObj.transform.SetParent(canvasObj.transform, false);
        
        heatOverlay = overlayObj.AddComponent<UnityEngine.UI.Image>();
        heatOverlay.color = new Color(heatTintColor.r, heatTintColor.g, heatTintColor.b, 0f);
        heatOverlay.raycastTarget = false;
        
        RectTransform overlayRect = overlayObj.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        
        // Heat vignette
        GameObject vignetteObj = new GameObject("HeatVignette");
        vignetteObj.transform.SetParent(canvasObj.transform, false);
        
        heatVignette = vignetteObj.AddComponent<UnityEngine.UI.Image>();
        heatVignette.sprite = CreateVignetteSprite();
        heatVignette.color = new Color(1f, 0.3f, 0f, 0f);
        heatVignette.raycastTarget = false;
        
        RectTransform vignetteRect = vignetteObj.GetComponent<RectTransform>();
        vignetteRect.anchorMin = Vector2.zero;
        vignetteRect.anchorMax = Vector2.one;
        vignetteRect.offsetMin = Vector2.zero;
        vignetteRect.offsetMax = Vector2.zero;
    }
    
    private Sprite CreateVignetteSprite()
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size);
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float alpha = Mathf.Clamp01(dist - 0.3f) / 0.7f;
                alpha = alpha * alpha; // Daha yumuşak geçiş
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    private void CreateHeatParticles()
    {
        GameObject particleObj = new GameObject("HeatParticles");
        particleObj.transform.SetParent(transform);
        
        heatParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = heatParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.5f; // Yukarı doğru
        main.playOnAwake = false;
        main.loop = true;
        main.startColor = emberColor;
        
        var emission = heatParticles.emission;
        emission.rateOverTime = 0;
        
        var shape = heatParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3f, 0.5f, 1f);
        
        var colorOverLifetime = heatParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(emberColor, 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.1f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.1f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var noise = heatParticles.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 1f;
        
        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 5;
    }
    
    private void Update()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }
        
        if (lavaSources == null || lavaSources.Length == 0)
        {
            FindLavaSources();
        }
        
        // En yakın lav kaynağına mesafeyi hesapla
        float closestDistance = float.MaxValue;
        Transform closestLava = null;
        
        if (lavaSources != null)
        {
            foreach (var lava in lavaSources)
            {
                if (lava == null) continue;
                float dist = Vector3.Distance(playerTransform.position, lava.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closestLava = lava;
                }
            }
        }
        
        // Sıcaklık seviyesini hesapla
        if (closestDistance < maxHeatDistance)
        {
            targetHeatLevel = 1f - Mathf.InverseLerp(minHeatDistance, maxHeatDistance, closestDistance);
            
            // Heat particles pozisyonu
            if (enableHeatParticles && heatParticles != null && closestLava != null)
            {
                Vector3 particlePos = Vector3.Lerp(playerTransform.position, closestLava.position, 0.7f);
                particlePos.y = playerTransform.position.y - 0.5f;
                heatParticles.transform.position = particlePos;
                
                var emission = heatParticles.emission;
                emission.rateOverTime = emberSpawnRate * targetHeatLevel;
                
                if (!heatParticles.isPlaying && targetHeatLevel > 0.1f)
                {
                    heatParticles.Play();
                }
            }
        }
        else
        {
            targetHeatLevel = 0f;
            
            if (heatParticles != null && heatParticles.isPlaying)
            {
                heatParticles.Stop();
            }
        }
        
        // Smooth transition
        currentHeatLevel = Mathf.Lerp(currentHeatLevel, targetHeatLevel, Time.deltaTime * 3f);
        
        // UI güncelle
        UpdateHeatVisuals();
    }
    
    private void UpdateHeatVisuals()
    {
        if (heatOverlay == null || heatVignette == null) return;
        
        // Pulse efekti
        float pulse = 1f + Mathf.Sin(Time.time * heatPulseSpeed) * 0.1f * currentHeatLevel;
        
        // Overlay alpha
        float overlayAlpha = currentHeatLevel * heatTintColor.a * pulse;
        heatOverlay.color = new Color(heatTintColor.r, heatTintColor.g, heatTintColor.b, overlayAlpha);
        
        // Vignette alpha
        float vignetteAlpha = currentHeatLevel * maxVignetteIntensity * pulse;
        heatVignette.color = new Color(1f, 0.3f, 0f, vignetteAlpha);
    }
    
    /// <summary>
    /// Manuel olarak sıcaklık efekti tetikleme
    /// </summary>
    public void TriggerHeatFlash(float intensity = 1f, float duration = 0.5f)
    {
        StartCoroutine(HeatFlashCoroutine(intensity, duration));
    }
    
    private System.Collections.IEnumerator HeatFlashCoroutine(float intensity, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float flash = Mathf.Sin(t * Mathf.PI) * intensity;
            
            currentHeatLevel = Mathf.Max(currentHeatLevel, flash);
            UpdateHeatVisuals();
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    /// <summary>
    /// Lava kaynağı ekle (runtime)
    /// </summary>
    public void RegisterLavaSource(Transform lavaTransform)
    {
        if (lavaSources == null)
        {
            lavaSources = new Transform[] { lavaTransform };
        }
        else
        {
            var newArray = new Transform[lavaSources.Length + 1];
            lavaSources.CopyTo(newArray, 0);
            newArray[lavaSources.Length] = lavaTransform;
            lavaSources = newArray;
        }
    }
}
