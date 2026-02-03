using UnityEngine;

/// <summary>
/// Yağmur ve Kar hava efektleri sistemi
/// Sahneye dinamik olarak eklenebilir
/// </summary>
public class WeatherEffects : MonoBehaviour
{
    public static WeatherEffects Instance { get; private set; }
    
    public enum WeatherType
    {
        None,
        Rain,
        Snow,
        Ash,      // Kül (cehennem teması için)
        Embers    // Kor parçacıkları
    }
    
    [Header("Hava Durumu Ayarları")]
    [SerializeField] private WeatherType currentWeather = WeatherType.None;
    [SerializeField] private float weatherIntensity = 1f;
    [SerializeField] private bool followPlayer = true;
    
    [Header("Yağmur Ayarları")]
    [SerializeField] private Color rainColor = new Color(0.7f, 0.8f, 1f, 0.5f);
    [SerializeField] private float rainSpeed = 15f;
    [SerializeField] private int rainParticleCount = 500;
    [SerializeField] private Vector2 rainAreaSize = new Vector2(20f, 15f);
    
    [Header("Kar Ayarları")]
    [SerializeField] private Color snowColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private float snowSpeed = 2f;
    [SerializeField] private int snowParticleCount = 200;
    [SerializeField] private float snowSwayAmount = 1f;
    
    [Header("Kül Ayarları")]
    [SerializeField] private Color ashColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    [SerializeField] private float ashSpeed = 1f;
    [SerializeField] private int ashParticleCount = 100;
    
    [Header("Kor Ayarları")]
    [SerializeField] private Color emberColorStart = new Color(1f, 0.6f, 0.1f, 0.9f);
    [SerializeField] private Color emberColorEnd = new Color(1f, 0.2f, 0f, 0f);
    [SerializeField] private float emberSpeed = 2f;
    [SerializeField] private int emberParticleCount = 50;
    
    [Header("Ses Efektleri")]
    [SerializeField] private AudioClip rainAmbientSound;
    [SerializeField] private AudioClip windAmbientSound;
    [SerializeField] private float ambientVolume = 0.3f;
    
    private ParticleSystem weatherParticles;
    private ParticleSystem secondaryParticles; // Splash veya accumulation için
    private AudioSource ambientAudio;
    private Transform playerTransform;
    
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
        CreateWeatherSystems();
        CreateAudioSource();
        FindPlayer();
        
        // Başlangıç hava durumu
        SetWeather(currentWeather);
    }
    
    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }
    
    private void Update()
    {
        if (followPlayer && playerTransform != null && weatherParticles != null)
        {
            Vector3 targetPos = playerTransform.position + Vector3.up * 8f;
            weatherParticles.transform.position = Vector3.Lerp(
                weatherParticles.transform.position, 
                targetPos, 
                Time.deltaTime * 2f
            );
            
            if (secondaryParticles != null)
            {
                secondaryParticles.transform.position = playerTransform.position + Vector3.down * 0.5f;
            }
        }
        
        if (playerTransform == null)
        {
            FindPlayer();
        }
    }
    
    private void CreateWeatherSystems()
    {
        // Ana hava durumu particle system
        GameObject weatherObj = new GameObject("WeatherParticles");
        weatherObj.transform.SetParent(transform);
        weatherObj.transform.position = Vector3.up * 10f;
        
        weatherParticles = weatherObj.AddComponent<ParticleSystem>();
        ConfigureParticleSystem(weatherParticles, WeatherType.None);
        
        var renderer = weatherObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 50;
        
        // İkincil efektler (yağmur splash vb.)
        GameObject secondaryObj = new GameObject("SecondaryWeatherParticles");
        secondaryObj.transform.SetParent(transform);
        
        secondaryParticles = secondaryObj.AddComponent<ParticleSystem>();
        
        var secRenderer = secondaryObj.GetComponent<ParticleSystemRenderer>();
        secRenderer.material = new Material(Shader.Find("Sprites/Default"));
        secRenderer.sortingOrder = 49;
    }
    
    private void CreateAudioSource()
    {
        ambientAudio = gameObject.AddComponent<AudioSource>();
        ambientAudio.loop = true;
        ambientAudio.playOnAwake = false;
        ambientAudio.volume = ambientVolume;
        ambientAudio.spatialBlend = 0f; // 2D ses
    }
    
    private void ConfigureParticleSystem(ParticleSystem ps, WeatherType type)
    {
        var main = ps.main;
        var emission = ps.emission;
        var shape = ps.shape;
        var colorOverLifetime = ps.colorOverLifetime;
        var sizeOverLifetime = ps.sizeOverLifetime;
        var velocityOverLifetime = ps.velocityOverLifetime;
        var noise = ps.noise;
        
        // Varsayılan değerler
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(rainAreaSize.x, 1f, 1f);
        
        switch (type)
        {
            case WeatherType.Rain:
                ConfigureRain(main, emission, shape, colorOverLifetime, noise);
                break;
                
            case WeatherType.Snow:
                ConfigureSnow(main, emission, shape, colorOverLifetime, noise);
                break;
                
            case WeatherType.Ash:
                ConfigureAsh(main, emission, shape, colorOverLifetime, noise);
                break;
                
            case WeatherType.Embers:
                ConfigureEmbers(main, emission, shape, colorOverLifetime, noise);
                break;
                
            default:
                emission.rateOverTime = 0;
                break;
        }
    }
    
    private void ConfigureRain(ParticleSystem.MainModule main, ParticleSystem.EmissionModule emission,
        ParticleSystem.ShapeModule shape, ParticleSystem.ColorOverLifetimeModule color, ParticleSystem.NoiseModule noise)
    {
        main.startLifetime = rainAreaSize.y / rainSpeed;
        main.startSpeed = rainSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.startColor = rainColor;
        main.maxParticles = rainParticleCount;
        main.gravityModifier = 0f;
        
        emission.rateOverTime = rainParticleCount * weatherIntensity;
        
        shape.rotation = new Vector3(0, 0, 0);
        
        // Yağmur çizgileri için stretch
        var renderer = weatherParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 2f;
        renderer.velocityScale = 0.1f;
        
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(rainColor, 0f), new GradientColorKey(rainColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(rainColor.a, 0.1f), new GradientAlphaKey(rainColor.a, 0.9f), new GradientAlphaKey(0f, 1f) }
        );
        color.color = gradient;
        
        noise.enabled = true;
        noise.strength = 0.1f;
        noise.frequency = 0.5f;
        
        // Yağmur splash efekti
        ConfigureRainSplash();
    }
    
    private void ConfigureRainSplash()
    {
        if (secondaryParticles == null) return;
        
        var main = secondaryParticles.main;
        main.startLifetime = 0.2f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startColor = rainColor;
        main.maxParticles = 100;
        main.gravityModifier = 0.5f;
        main.playOnAwake = false;
        main.loop = true;
        
        var emission = secondaryParticles.emission;
        emission.rateOverTime = 20 * weatherIntensity;
        
        var shape = secondaryParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(rainAreaSize.x * 0.8f, 0.1f, 1f);
    }
    
    private void ConfigureSnow(ParticleSystem.MainModule main, ParticleSystem.EmissionModule emission,
        ParticleSystem.ShapeModule shape, ParticleSystem.ColorOverLifetimeModule color, ParticleSystem.NoiseModule noise)
    {
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
        main.startSpeed = snowSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.1f);
        main.startColor = snowColor;
        main.maxParticles = snowParticleCount;
        main.gravityModifier = 0.1f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        
        emission.rateOverTime = snowParticleCount * 0.5f * weatherIntensity;
        
        var renderer = weatherParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(snowColor, 0f), new GradientColorKey(snowColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(snowColor.a, 0.2f), new GradientAlphaKey(snowColor.a, 0.8f), new GradientAlphaKey(0f, 1f) }
        );
        color.color = gradient;
        
        noise.enabled = true;
        noise.strength = snowSwayAmount;
        noise.frequency = 0.3f;
        noise.scrollSpeed = 0.2f;
        
        // Kar birikimi (basit)
        if (secondaryParticles != null)
        {
            var secMain = secondaryParticles.main;
            secMain.startLifetime = 0;
            var secEmission = secondaryParticles.emission;
            secEmission.rateOverTime = 0;
        }
    }
    
    private void ConfigureAsh(ParticleSystem.MainModule main, ParticleSystem.EmissionModule emission,
        ParticleSystem.ShapeModule shape, ParticleSystem.ColorOverLifetimeModule color, ParticleSystem.NoiseModule noise)
    {
        main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSpeed = ashSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
        main.startColor = ashColor;
        main.maxParticles = ashParticleCount;
        main.gravityModifier = 0.05f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0, Mathf.PI * 2);
        
        emission.rateOverTime = ashParticleCount * 0.3f * weatherIntensity;
        
        var renderer = weatherParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(ashColor, 0f), new GradientColorKey(ashColor * 0.5f, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(ashColor.a, 0.2f), new GradientAlphaKey(ashColor.a * 0.5f, 1f) }
        );
        color.color = gradient;
        
        noise.enabled = true;
        noise.strength = 0.8f;
        noise.frequency = 0.2f;
        noise.scrollSpeed = 0.1f;
    }
    
    private void ConfigureEmbers(ParticleSystem.MainModule main, ParticleSystem.EmissionModule emission,
        ParticleSystem.ShapeModule shape, ParticleSystem.ColorOverLifetimeModule color, ParticleSystem.NoiseModule noise)
    {
        main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSpeed = emberSpeed;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.startColor = emberColorStart;
        main.maxParticles = emberParticleCount;
        main.gravityModifier = -0.2f; // Yukarı doğru
        
        emission.rateOverTime = emberParticleCount * 0.5f * weatherIntensity;
        
        shape.position = Vector3.down * 5f; // Aşağıdan başlasın
        
        var renderer = weatherParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(emberColorStart, 0f), 
                new GradientColorKey(emberColorEnd, 0.7f),
                new GradientColorKey(new Color(0.2f, 0.05f, 0f), 1f)
            },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0f, 0f), 
                new GradientAlphaKey(emberColorStart.a, 0.1f), 
                new GradientAlphaKey(emberColorStart.a * 0.5f, 0.7f),
                new GradientAlphaKey(0f, 1f) 
            }
        );
        color.color = gradient;
        
        noise.enabled = true;
        noise.strength = 1f;
        noise.frequency = 0.5f;
        noise.scrollSpeed = 0.3f;
        
        // Işık efekti
        var lights = weatherParticles.lights;
        lights.enabled = true;
        lights.ratio = 0.05f;
        lights.intensity = 0.3f;
        lights.range = 0.5f;
    }
    
    /// <summary>
    /// Hava durumunu değiştir
    /// </summary>
    public void SetWeather(WeatherType type)
    {
        currentWeather = type;
        
        if (weatherParticles == null) return;
        
        weatherParticles.Stop();
        weatherParticles.Clear();
        
        if (secondaryParticles != null)
        {
            secondaryParticles.Stop();
            secondaryParticles.Clear();
        }
        
        if (type == WeatherType.None)
        {
            StopAmbientSound();
            return;
        }
        
        ConfigureParticleSystem(weatherParticles, type);
        weatherParticles.Play();
        
        if (secondaryParticles != null && type == WeatherType.Rain)
        {
            secondaryParticles.Play();
        }
        
        // Ambient ses
        PlayAmbientSound(type);
    }
    
    /// <summary>
    /// Hava durumu yoğunluğunu ayarla (0-1)
    /// </summary>
    public void SetIntensity(float intensity)
    {
        weatherIntensity = Mathf.Clamp01(intensity);
        
        if (weatherParticles != null)
        {
            var emission = weatherParticles.emission;
            float baseRate = GetBaseEmissionRate(currentWeather);
            emission.rateOverTime = baseRate * weatherIntensity;
        }
        
        if (ambientAudio != null)
        {
            ambientAudio.volume = ambientVolume * weatherIntensity;
        }
    }
    
    private float GetBaseEmissionRate(WeatherType type)
    {
        return type switch
        {
            WeatherType.Rain => rainParticleCount,
            WeatherType.Snow => snowParticleCount * 0.5f,
            WeatherType.Ash => ashParticleCount * 0.3f,
            WeatherType.Embers => emberParticleCount * 0.5f,
            _ => 0f
        };
    }
    
    private void PlayAmbientSound(WeatherType type)
    {
        if (ambientAudio == null) return;
        
        AudioClip clip = type switch
        {
            WeatherType.Rain => rainAmbientSound,
            WeatherType.Snow => windAmbientSound,
            _ => null
        };
        
        if (clip != null)
        {
            ambientAudio.clip = clip;
            ambientAudio.Play();
        }
    }
    
    private void StopAmbientSound()
    {
        if (ambientAudio != null && ambientAudio.isPlaying)
        {
            ambientAudio.Stop();
        }
    }
    
    /// <summary>
    /// Geçici hava olayı (örn: kısa süreli kar fırtınası)
    /// </summary>
    public void TriggerWeatherEvent(WeatherType type, float duration, float intensity = 1f)
    {
        StartCoroutine(WeatherEventCoroutine(type, duration, intensity));
    }
    
    private System.Collections.IEnumerator WeatherEventCoroutine(WeatherType type, float duration, float intensity)
    {
        WeatherType previousWeather = currentWeather;
        float previousIntensity = weatherIntensity;
        
        SetWeather(type);
        SetIntensity(intensity);
        
        yield return new WaitForSeconds(duration);
        
        // Yavaş geçiş
        float elapsed = 0f;
        float transitionTime = 2f;
        
        while (elapsed < transitionTime)
        {
            SetIntensity(Mathf.Lerp(intensity, 0f, elapsed / transitionTime));
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        SetWeather(previousWeather);
        SetIntensity(previousIntensity);
    }
}
