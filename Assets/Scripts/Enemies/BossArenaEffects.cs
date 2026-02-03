using UnityEngine;
using System.Collections;

/// <summary>
/// Boss arenası için atmosferik efektler
/// - Arena sınır efektleri
/// - Dramatik aydınlatma
/// - Müzik yoğunluğu
/// - Ortam parçacıkları
/// </summary>
public class BossArenaEffects : MonoBehaviour
{
    public static BossArenaEffects Instance { get; private set; }
    
    [Header("Arena Bounds")]
    [SerializeField] private bool enableArenaBounds = true;
    [SerializeField] private float arenaWidth = 20f;
    [SerializeField] private float arenaHeight = 10f;
    [SerializeField] private Color boundaryColor = new Color(1f, 0.2f, 0.1f, 0.5f);
    
    [Header("Lighting")]
    [SerializeField] private bool enableDramaticLighting = true;
    [SerializeField] private Color ambientBattleColor = new Color(0.3f, 0.15f, 0.1f, 1f);
    [SerializeField] private float lightingTransitionTime = 2f;
    [SerializeField] private bool flickerLights = true;
    [SerializeField] private float flickerIntensity = 0.1f;
    
    [Header("Environment Particles")]
    [SerializeField] private bool enableEmbers = true;
    [SerializeField] private int emberCount = 30;
    [SerializeField] private Color emberColor = new Color(1f, 0.5f, 0.1f, 0.8f);
    
    [Header("Dust/Debris")]
    [SerializeField] private bool enableDust = true;
    [SerializeField] private int dustCount = 20;
    
    [Header("Screen Effects")]
    [SerializeField] private bool enableVignette = true;
    [SerializeField] private float vignetteIntensity = 0.3f;
    [SerializeField] private Color vignetteColor = new Color(0.1f, 0f, 0f, 1f);
    
    [Header("Music")]
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private float musicFadeTime = 1f;
    [SerializeField] private float bossTrackVolume = 0.8f;
    
    // Private
    private ParticleSystem emberParticles;
    private ParticleSystem dustParticles;
    private LineRenderer[] boundaryLines;
    private Color originalAmbientColor;
    private AudioSource musicSource;
    private bool isActive = false;
    private GameObject vignetteOverlay;
    
    private void Awake()
    {
        Instance = this;
    }
    
    /// <summary>
    /// Boss savaşı başladığında çağrılır
    /// </summary>
    public void ActivateBossArena()
    {
        if (isActive) return;
        isActive = true;
        
        Debug.Log("[BossArenaEffects] Arena aktive edildi!");
        
        // Aydınlatma değişimi
        if (enableDramaticLighting)
            StartCoroutine(TransitionLighting());
        
        // Arena sınırları
        if (enableArenaBounds)
            CreateArenaBoundaries();
        
        // Ortam parçacıkları
        if (enableEmbers)
            CreateEmberParticles();
        
        if (enableDust)
            CreateDustParticles();
        
        // Vignette
        if (enableVignette)
            CreateVignetteOverlay();
        
        // Müzik
        if (bossMusic != null)
            StartCoroutine(FadeInBossMusic());
    }
    
    /// <summary>
    /// Boss yenildiğinde çağrılır
    /// </summary>
    public void DeactivateBossArena()
    {
        if (!isActive) return;
        
        Debug.Log("[BossArenaEffects] Arena deaktive edildi!");
        
        StartCoroutine(DeactivateSequence());
    }
    
    private IEnumerator DeactivateSequence()
    {
        // Parçacıkları durdur
        if (emberParticles != null) emberParticles.Stop();
        if (dustParticles != null) dustParticles.Stop();
        
        // Aydınlatmayı normale döndür
        if (enableDramaticLighting)
        {
            float elapsed = 0f;
            Color currentAmbient = RenderSettings.ambientLight;
            
            while (elapsed < lightingTransitionTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lightingTransitionTime;
                RenderSettings.ambientLight = Color.Lerp(currentAmbient, originalAmbientColor, t);
                yield return null;
            }
        }
        
        // Vignette'i kaldır
        if (vignetteOverlay != null)
        {
            UnityEngine.UI.Image img = vignetteOverlay.GetComponentInChildren<UnityEngine.UI.Image>();
            if (img != null)
            {
                float elapsed = 0f;
                Color startColor = img.color;
                
                while (elapsed < 1f)
                {
                    elapsed += Time.deltaTime;
                    img.color = Color.Lerp(startColor, Color.clear, elapsed);
                    yield return null;
                }
            }
            Destroy(vignetteOverlay);
        }
        
        // Sınırları kaldır
        if (boundaryLines != null)
        {
            foreach (var line in boundaryLines)
            {
                if (line != null) Destroy(line.gameObject);
            }
        }
        
        // Müziği fade out
        if (musicSource != null)
        {
            float elapsed = 0f;
            float startVol = musicSource.volume;
            
            while (elapsed < musicFadeTime)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / musicFadeTime);
                yield return null;
            }
            
            Destroy(musicSource.gameObject);
        }
        
        // Cleanup
        yield return new WaitForSeconds(2f);
        
        if (emberParticles != null) Destroy(emberParticles.gameObject);
        if (dustParticles != null) Destroy(dustParticles.gameObject);
        
        isActive = false;
    }
    
    #region Lighting
    
    private IEnumerator TransitionLighting()
    {
        originalAmbientColor = RenderSettings.ambientLight;
        
        float elapsed = 0f;
        while (elapsed < lightingTransitionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lightingTransitionTime;
            RenderSettings.ambientLight = Color.Lerp(originalAmbientColor, ambientBattleColor, t);
            yield return null;
        }
        
        // Titreşim efekti
        if (flickerLights)
        {
            StartCoroutine(LightFlicker());
        }
    }
    
    private IEnumerator LightFlicker()
    {
        while (isActive)
        {
            float flicker = 1f + Random.Range(-flickerIntensity, flickerIntensity);
            RenderSettings.ambientLight = ambientBattleColor * flicker;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }
    }
    
    #endregion
    
    #region Particles
    
    private void CreateEmberParticles()
    {
        GameObject emberObj = new GameObject("BossEmbers");
        emberObj.transform.position = transform.position;
        
        emberParticles = emberObj.AddComponent<ParticleSystem>();
        
        var main = emberParticles.main;
        main.duration = 0f;
        main.loop = true;
        main.startLifetime = 3f;
        main.startSpeed = 1f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = emberColor;
        main.maxParticles = emberCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = emberParticles.emission;
        emission.rateOverTime = emberCount / 3f;
        
        var shape = emberParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(arenaWidth, 2f, 1f);
        shape.position = new Vector3(0f, -2f, 0f);
        
        var velocityOverLifetime = emberParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        
        var colorOverLifetime = emberParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(emberColor, 0f), 
                new GradientColorKey(new Color(1f, 0.2f, 0f), 1f) 
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f), 
                new GradientAlphaKey(0.8f, 0.2f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        colorOverLifetime.color = gradient;
        
        // Işık efekti
        var lights = emberParticles.lights;
        lights.enabled = true;
        lights.ratio = 0.3f;
        lights.intensity = 0.5f;
        lights.range = 0.5f;
        
        emberParticles.Play();
    }
    
    private void CreateDustParticles()
    {
        GameObject dustObj = new GameObject("BossDust");
        dustObj.transform.position = transform.position;
        
        dustParticles = dustObj.AddComponent<ParticleSystem>();
        
        var main = dustParticles.main;
        main.duration = 0f;
        main.loop = true;
        main.startLifetime = 4f;
        main.startSpeed = 0.3f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = new Color(0.5f, 0.4f, 0.3f, 0.3f);
        main.maxParticles = dustCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = dustParticles.emission;
        emission.rateOverTime = dustCount / 4f;
        
        var shape = dustParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(arenaWidth, arenaHeight, 1f);
        
        var noise = dustParticles.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.5f;
        
        dustParticles.Play();
    }
    
    #endregion
    
    #region Arena Boundaries
    
    private void CreateArenaBoundaries()
    {
        boundaryLines = new LineRenderer[4];
        Color glowColor = new Color(boundaryColor.r, boundaryColor.g, boundaryColor.b, 0.3f);
        
        // Sol sınır
        boundaryLines[0] = CreateBoundaryLine("LeftBoundary", 
            new Vector3(-arenaWidth/2, -arenaHeight/2, 0f),
            new Vector3(-arenaWidth/2, arenaHeight/2, 0f));
        
        // Sağ sınır
        boundaryLines[1] = CreateBoundaryLine("RightBoundary",
            new Vector3(arenaWidth/2, -arenaHeight/2, 0f),
            new Vector3(arenaWidth/2, arenaHeight/2, 0f));
        
        // Alt sınır
        boundaryLines[2] = CreateBoundaryLine("BottomBoundary",
            new Vector3(-arenaWidth/2, -arenaHeight/2, 0f),
            new Vector3(arenaWidth/2, -arenaHeight/2, 0f));
        
        // Üst sınır
        boundaryLines[3] = CreateBoundaryLine("TopBoundary",
            new Vector3(-arenaWidth/2, arenaHeight/2, 0f),
            new Vector3(arenaWidth/2, arenaHeight/2, 0f));
        
        // Animasyonlu pulse efekti
        StartCoroutine(PulseBoundaries());
    }
    
    private LineRenderer CreateBoundaryLine(string name, Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject(name);
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;
        
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPosition(0, start + transform.position);
        lr.SetPosition(1, end + transform.position);
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = boundaryColor;
        lr.endColor = boundaryColor;
        lr.sortingOrder = 100;
        
        return lr;
    }
    
    private IEnumerator PulseBoundaries()
    {
        while (isActive && boundaryLines != null)
        {
            float pulse = Mathf.Sin(Time.time * 3f) * 0.5f + 0.5f;
            Color pulsedColor = new Color(
                boundaryColor.r,
                boundaryColor.g,
                boundaryColor.b,
                Mathf.Lerp(0.2f, 0.6f, pulse)
            );
            
            foreach (var line in boundaryLines)
            {
                if (line != null)
                {
                    line.startColor = pulsedColor;
                    line.endColor = pulsedColor;
                    line.startWidth = Mathf.Lerp(0.08f, 0.15f, pulse);
                    line.endWidth = line.startWidth;
                }
            }
            
            yield return null;
        }
    }
    
    #endregion
    
    #region Vignette
    
    private void CreateVignetteOverlay()
    {
        vignetteOverlay = new GameObject("BossVignette");
        Canvas canvas = vignetteOverlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        
        GameObject imgObj = new GameObject("VignetteImage");
        imgObj.transform.SetParent(vignetteOverlay.transform, false);
        
        UnityEngine.UI.Image img = imgObj.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, vignetteIntensity);
        img.raycastTarget = false;
        
        // Vignette sprite oluştur (radial gradient simülasyonu)
        Texture2D tex = new Texture2D(256, 256);
        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                float dx = (x - 128) / 128f;
                float dy = (y - 128) / 128f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01((dist - 0.5f) * 2f);
                tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
            }
        }
        tex.Apply();
        
        img.sprite = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
        
        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        // Fade in
        StartCoroutine(FadeInVignette(img));
    }
    
    private IEnumerator FadeInVignette(UnityEngine.UI.Image img)
    {
        float elapsed = 0f;
        Color targetColor = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, vignetteIntensity);
        
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            img.color = Color.Lerp(Color.clear, targetColor, elapsed);
            yield return null;
        }
    }
    
    #endregion
    
    #region Music
    
    private IEnumerator FadeInBossMusic()
    {
        GameObject musicObj = new GameObject("BossMusic");
        musicSource = musicObj.AddComponent<AudioSource>();
        musicSource.clip = bossMusic;
        musicSource.loop = true;
        musicSource.volume = 0f;
        musicSource.Play();
        
        float elapsed = 0f;
        while (elapsed < musicFadeTime)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, bossTrackVolume, elapsed / musicFadeTime);
            yield return null;
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
