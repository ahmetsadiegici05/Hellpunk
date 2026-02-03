using UnityEngine;
using System.Collections;

/// <summary>
/// Düşman ölümünde ruh parçacığı efekti
/// Ruhlar sol alttaki Soul UI'a doğru uçar
/// </summary>
public class SoulParticleEffect : MonoBehaviour
{
    public static SoulParticleEffect Instance { get; private set; }
    
    [Header("Ruh Parçacığı Ayarları")]
    [SerializeField] private Color soulColorStart = new Color(0.6f, 0.8f, 1f, 0.9f);
    [SerializeField] private Color soulColorEnd = new Color(0.3f, 0.5f, 1f, 0f);
    [SerializeField] private Color soulCoreColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private int soulParticleCount = 8;
    [SerializeField] private float soulSpeed = 6f;
    [SerializeField] private float soulLifetime = 1.5f;
    [SerializeField] private float soulSize = 0.2f;
    [SerializeField] private bool soulsFollowUI = true; // UI'a doğru git
    [SerializeField] private float followDelay = 0.5f;
    
    [Header("Boss Ruh Ayarları")]
    [SerializeField] private Color bossSoulColor = new Color(1f, 0.3f, 0.3f, 0.9f);
    [SerializeField] private int bossSoulCount = 25;
    [SerializeField] private float bossSoulSize = 0.4f;
    
    [Header("UI Hedef Ayarları")]
    [SerializeField] private Vector2 soulUIScreenPosition = new Vector2(90f, 120f); // Sol alt köşe (piksel)
    
    private ParticleSystem soulBurstParticles;
    private ParticleSystem soulTrailParticles;
    private Transform playerTransform;
    private Camera mainCamera;
    private RectTransform soulUITarget; // Soul UI referansı
    
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
        CreateSoulParticles();
        FindPlayer();
        FindSoulUI();
        mainCamera = Camera.main;
    }
    
    private void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }
    
    private void FindSoulUI()
    {
        // SoulUI'ı bul
        SoulUI soulUI = FindFirstObjectByType<SoulUI>();
        if (soulUI != null)
        {
            soulUITarget = soulUI.GetComponent<RectTransform>();
        }
    }
    
    /// <summary>
    /// Soul UI'ın world pozisyonunu al
    /// </summary>
    private Vector3 GetSoulUIWorldPosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera == null) return Vector3.zero;
        
        // Eğer SoulUI RectTransform bulunduysa onu kullan
        if (soulUITarget != null)
        {
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, soulUITarget.position);
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            return worldPos;
        }
        
        // Fallback: Sabit ekran pozisyonu kullan (sol alt köşe)
        Vector3 targetScreenPos = new Vector3(soulUIScreenPosition.x, soulUIScreenPosition.y, 10f);
        return mainCamera.ScreenToWorldPoint(targetScreenPos);
    }
    
    private void CreateSoulParticles()
    {
        // Ana ruh burst efekti
        GameObject soulObj = new GameObject("SoulBurstParticles");
        soulObj.transform.SetParent(transform);
        
        soulBurstParticles = soulObj.AddComponent<ParticleSystem>();
        
        var main = soulBurstParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(soulLifetime * 0.5f, soulLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(soulSpeed * 0.3f, soulSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(soulSize * 0.5f, soulSize);
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.3f; // Yukarı doğru hafif çekim
        main.playOnAwake = false;
        main.startColor = soulColorStart;
        
        // Emisyon
        var emission = soulBurstParticles.emission;
        emission.rateOverTime = 0;
        
        // Şekil - küre şeklinde yayılsın
        var shape = soulBurstParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;
        
        // Renk değişimi
        var colorOverLifetime = soulBurstParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(soulCoreColor, 0f),
                new GradientColorKey(soulColorStart, 0.2f),
                new GradientColorKey(soulColorEnd, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        // Boyut değişimi
        var sizeOverLifetime = soulBurstParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.3f);
        sizeCurve.AddKey(0.2f, 1f);
        sizeCurve.AddKey(0.8f, 1.2f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Noise - organik hareket
        var noise = soulBurstParticles.noise;
        noise.enabled = true;
        noise.strength = 1f;
        noise.frequency = 2f;
        noise.scrollSpeed = 0.5f;
        
        // Trail
        var trails = soulBurstParticles.trails;
        trails.enabled = true;
        trails.lifetime = 0.3f;
        trails.widthOverTrail = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
        
        // Işık efekti
        var lights = soulBurstParticles.lights;
        lights.enabled = true;
        lights.ratio = 0.1f;
        lights.intensity = 0.5f;
        lights.range = 1f;
        
        // Renderer
        var renderer = soulObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateGlowMaterial();
        renderer.trailMaterial = CreateGlowMaterial();
        renderer.sortingOrder = 15;
        
        // Oyuncuya doğru hareket (sub-emitter olarak veya kod ile)
        CreateSoulFollowParticles();
    }
    
    private void CreateSoulFollowParticles()
    {
        GameObject trailObj = new GameObject("SoulTrailParticles");
        trailObj.transform.SetParent(transform);
        
        soulTrailParticles = trailObj.AddComponent<ParticleSystem>();
        
        var main = soulTrailParticles.main;
        main.startLifetime = 2f;
        main.startSpeed = 0.5f;
        main.startSize = new ParticleSystem.MinMaxCurve(soulSize * 0.3f, soulSize * 0.5f);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        main.startColor = soulColorStart;
        
        var emission = soulTrailParticles.emission;
        emission.rateOverTime = 0;
        
        var colorOverLifetime = soulTrailParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(soulColorStart, 0f),
                new GradientColorKey(soulColorEnd, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.6f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var renderer = trailObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateGlowMaterial();
        renderer.sortingOrder = 14;
    }
    
    private Material CreateGlowMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        return mat;
    }
    
    /// <summary>
    /// Normal düşman ölümünde ruh efekti
    /// </summary>
    public void SpawnSoulEffect(Vector3 position)
    {
        if (soulBurstParticles == null) return;
        
        soulBurstParticles.transform.position = position;
        soulBurstParticles.Emit(soulParticleCount);
        
        // Sol alttaki Soul UI'a doğru uçan ruhlar
        if (soulsFollowUI)
        {
            StartCoroutine(SpawnFollowingSouls(position, soulParticleCount / 2, false));
        }
    }
    
    /// <summary>
    /// Boss ölümünde büyük ruh efekti
    /// </summary>
    public void SpawnBossSoulEffect(Vector3 position)
    {
        if (soulBurstParticles == null) return;
        
        // Boss için özel renk
        var main = soulBurstParticles.main;
        main.startColor = bossSoulColor;
        main.startSize = new ParticleSystem.MinMaxCurve(bossSoulSize * 0.5f, bossSoulSize);
        
        soulBurstParticles.transform.position = position;
        soulBurstParticles.Emit(bossSoulCount);
        
        // Birden fazla dalga
        StartCoroutine(BossSoulWaves(position));
        
        // Renkleri geri al
        main.startColor = soulColorStart;
        main.startSize = new ParticleSystem.MinMaxCurve(soulSize * 0.5f, soulSize);
    }
    
    private IEnumerator BossSoulWaves(Vector3 position)
    {
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.3f);
            soulBurstParticles.transform.position = position + Random.insideUnitSphere * 0.5f;
            soulBurstParticles.Emit(bossSoulCount / 3);
            
            if (soulsFollowUI)
            {
                StartCoroutine(SpawnFollowingSouls(position, 5, true));
            }
        }
    }
    
    private IEnumerator SpawnFollowingSouls(Vector3 startPos, int count, bool isBoss)
    {
        yield return new WaitForSeconds(followDelay);
        
        for (int i = 0; i < count; i++)
        {
            StartCoroutine(MoveSoulToUI(startPos + Random.insideUnitSphere * 0.5f, isBoss));
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private IEnumerator MoveSoulToUI(Vector3 startPos, bool isBoss)
    {
        GameObject soul = new GameObject("FollowingSoul");
        SpriteRenderer sr = soul.AddComponent<SpriteRenderer>();
        
        // Basit daire sprite
        Texture2D texture = new Texture2D(16, 16);
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(8, 8)) / 8f;
                if (dist < 1f)
                {
                    float alpha = 1f - dist;
                    texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        texture.Apply();
        
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        sr.color = isBoss ? bossSoulColor : soulColorStart;
        sr.sortingOrder = 100; // UI'ın önünde görünsün
        
        soul.transform.position = startPos;
        float size = isBoss ? bossSoulSize : soulSize * 0.7f;
        soul.transform.localScale = Vector3.one * size;
        
        float elapsed = 0f;
        float duration = 1.2f; // Biraz daha uzun süre (UI'a ulaşması için)
        Vector3 velocity = Vector3.zero;
        
        // SoulUI bulunamadıysa tekrar ara
        if (soulUITarget == null)
        {
            FindSoulUI();
        }
        
        while (elapsed < duration)
        {
            // Hedef pozisyon: Sol alt köşedeki Soul UI
            Vector3 targetPos = GetSoulUIWorldPosition();
            
            // Bezier eğrisi benzeri hareket için yukarı doğru bir yay çiz
            float curveHeight = 2f * (1f - elapsed / duration) * (elapsed / duration) * 4f;
            Vector3 curvedTarget = targetPos + Vector3.up * curveHeight;
            
            soul.transform.position = Vector3.SmoothDamp(soul.transform.position, curvedTarget, ref velocity, 0.15f, soulSpeed * 3f);
            
            // Boyut küçülsün (UI'a yaklaştıkça)
            float scale = Mathf.Lerp(size, size * 0.2f, elapsed / duration);
            soul.transform.localScale = Vector3.one * scale;
            
            // Renk parlasın (UI'a yaklaştıkça)
            float glowIntensity = Mathf.Lerp(1f, 2f, elapsed / duration);
            Color currentColor = isBoss ? bossSoulColor : soulColorStart;
            sr.color = currentColor * glowIntensity;
            
            // UI'a yaklaşınca yok et
            if (Vector3.Distance(soul.transform.position, targetPos) < 1f)
            {
                // UI'da flash efekti tetikle
                TriggerUIFlash();
                break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Toplama efekti (Soul UI pozisyonunda)
        Vector3 uiWorldPos = GetSoulUIWorldPosition();
        if (soulTrailParticles != null)
        {
            soulTrailParticles.transform.position = uiWorldPos;
            soulTrailParticles.Emit(5);
        }
        
        Destroy(soul);
    }
    
    /// <summary>
    /// Soul UI'da toplama flash efekti
    /// </summary>
    private void TriggerUIFlash()
    {
        // SoulUI'da flash efekti tetikle (varsa)
        SoulUI soulUI = FindFirstObjectByType<SoulUI>();
        if (soulUI != null)
        {
            // SoulUI'da OnSoulCollected gibi bir metod varsa çağır
            soulUI.SendMessage("OnSoulCollectedVisual", SendMessageOptions.DontRequireReceiver);
        }
    }
}
