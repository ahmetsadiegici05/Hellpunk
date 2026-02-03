using UnityEngine;

/// <summary>
/// Duvara çarpınca kıvılcım efekti
/// PlayerMovement'a otomatik entegre olur
/// </summary>
public class WallSparkEffect : MonoBehaviour
{
    public static WallSparkEffect Instance { get; private set; }
    
    [Header("Kıvılcım Ayarları")]
    [SerializeField] private Color sparkColor = new Color(1f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color sparkColorSecondary = new Color(1f, 0.5f, 0.1f, 1f);
    [SerializeField] private int sparkCount = 12;
    [SerializeField] private float sparkSpeed = 5f;
    [SerializeField] private float sparkLifetime = 0.3f;
    
    private ParticleSystem sparkParticles;
    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private float lastWallHitTime;
    private float wallHitCooldown = 0.1f;
    
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
        CreateSparkParticles();
        FindPlayer();
    }
    
    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerRb = player.GetComponent<Rigidbody2D>();
        }
    }
    
    private void Update()
    {
        if (playerTransform == null || playerRb == null)
        {
            FindPlayer();
            return;
        }
        
        CheckWallCollision();
    }
    
    private void CheckWallCollision()
    {
        if (Time.time - lastWallHitTime < wallHitCooldown) return;
        
        // Yatay hız kontrolü - duvara hızlı çarpma
        float horizontalSpeed = Mathf.Abs(playerRb.linearVelocity.x);
        if (horizontalSpeed < 3f) return;
        
        // Duvar kontrolü
        float direction = Mathf.Sign(playerRb.linearVelocity.x);
        Vector2 origin = playerTransform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * direction, 0.6f, LayerMask.GetMask("Ground", "Wall"));
        
        if (hit.collider != null)
        {
            SpawnSparks(hit.point, hit.normal, horizontalSpeed);
            lastWallHitTime = Time.time;
        }
    }
    
    private void CreateSparkParticles()
    {
        GameObject sparkObj = new GameObject("WallSparkParticles");
        sparkObj.transform.SetParent(transform);
        
        sparkParticles = sparkObj.AddComponent<ParticleSystem>();
        
        var main = sparkParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.1f, sparkLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(sparkSpeed * 0.5f, sparkSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 1f;
        main.playOnAwake = false;
        
        // İki renkli gradient
        var colorOverLifetime = sparkParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(sparkColor, 0f),
                new GradientColorKey(sparkColorSecondary, 0.5f),
                new GradientColorKey(new Color(0.3f, 0.1f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Emisyon
        var emission = sparkParticles.emission;
        emission.rateOverTime = 0;
        
        // Şekil - cone şeklinde yayılsın
        var shape = sparkParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 45f;
        shape.radius = 0.05f;
        
        // Boyut azalması
        var sizeOverLifetime = sparkParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Trail
        var trails = sparkParticles.trails;
        trails.enabled = true;
        trails.lifetime = 0.1f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        trails.colorOverLifetime = gradient;
        
        // Renderer
        var renderer = sparkObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.trailMaterial = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 10;
    }
    
    /// <summary>
    /// Belirtilen pozisyonda kıvılcım efekti oluştur
    /// </summary>
    public void SpawnSparks(Vector3 position, Vector3 normal, float intensity = 1f)
    {
        if (sparkParticles == null) return;
        
        sparkParticles.transform.position = position;
        
        // Normal yönüne doğru bak
        float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;
        sparkParticles.transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // Yoğunluğa göre parçacık sayısı
        int count = Mathf.RoundToInt(sparkCount * Mathf.Clamp01(intensity / 10f));
        sparkParticles.Emit(count);
        
        // Kamera sarsıntısı (hafif)
        if (ScreenShake.Instance != null && intensity > 5f)
        {
            ScreenShake.Instance.ShakeLight();
        }
    }
    
    /// <summary>
    /// Herhangi bir pozisyonda kıvılcım efekti (dışarıdan çağrılabilir)
    /// </summary>
    public void SpawnSparksAt(Vector3 position, Vector3 direction)
    {
        SpawnSparks(position, direction, 8f);
    }
}
