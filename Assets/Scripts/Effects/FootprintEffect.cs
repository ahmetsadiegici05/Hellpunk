using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Koşarken ayak izleri bırakan efekt sistemi
/// PlayerMovement'a otomatik entegre olur
/// </summary>
public class FootprintEffect : MonoBehaviour
{
    public static FootprintEffect Instance { get; private set; }
    
    [Header("Ayak İzi Ayarları")]
    [SerializeField] private Color footprintColor = new Color(0.3f, 0.25f, 0.2f, 0.5f);
    [SerializeField] private float footprintSize = 0.15f;
    [SerializeField] private float footprintLifetime = 2f;
    [SerializeField] private float footprintInterval = 0.25f;
    [SerializeField] private int poolSize = 30;
    
    [Header("Toz Efekti")]
    [SerializeField] private bool enableRunDust = true;
    [SerializeField] private Color dustColor = new Color(0.7f, 0.65f, 0.5f, 0.6f);
    
    private List<SpriteRenderer> footprintPool = new List<SpriteRenderer>();
    private Queue<SpriteRenderer> availableFootprints = new Queue<SpriteRenderer>();
    
    private Transform playerTransform;
    private float lastFootprintTime;
    private bool isLeftFoot = true;
    
    // Running dust particle system
    private ParticleSystem runDustParticles;
    
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
        CreatePool();
        CreateRunDustParticles();
        FindPlayer();
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
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }
        
        // PlayerMovement'tan hareket durumunu kontrol et
        if (PlayerMovement.Instance == null) return;
        
        bool isMoving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f;
        bool isGrounded = IsPlayerGrounded();
        
        if (isMoving && isGrounded && !PlayerMovement.Instance.IsDashing)
        {
            // Ayak izi bırak
            if (Time.time - lastFootprintTime >= footprintInterval)
            {
                SpawnFootprint();
                lastFootprintTime = Time.time;
            }
            
            // Toz efekti
            if (enableRunDust && runDustParticles != null)
            {
                if (!runDustParticles.isPlaying)
                {
                    runDustParticles.Play();
                }
                runDustParticles.transform.position = playerTransform.position + Vector3.down * 0.3f;
            }
        }
        else
        {
            if (runDustParticles != null && runDustParticles.isPlaying)
            {
                runDustParticles.Stop();
            }
        }
    }
    
    private bool IsPlayerGrounded()
    {
        // Basit ground check
        RaycastHit2D hit = Physics2D.Raycast(playerTransform.position, Vector2.down, 0.6f, LayerMask.GetMask("Ground"));
        return hit.collider != null;
    }
    
    private void CreatePool()
    {
        // Basit ayak izi sprite'ı oluştur
        Texture2D footprintTexture = CreateFootprintTexture();
        Sprite footprintSprite = Sprite.Create(footprintTexture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16);
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject footprintObj = new GameObject($"Footprint_{i}");
            footprintObj.transform.SetParent(transform);
            
            SpriteRenderer sr = footprintObj.AddComponent<SpriteRenderer>();
            sr.sprite = footprintSprite;
            sr.color = footprintColor;
            sr.sortingOrder = -1; // Oyuncunun altında
            footprintObj.SetActive(false);
            
            footprintPool.Add(sr);
            availableFootprints.Enqueue(sr);
        }
    }
    
    private Texture2D CreateFootprintTexture()
    {
        Texture2D texture = new Texture2D(16, 16);
        texture.filterMode = FilterMode.Point;
        
        // Basit ayak izi şekli (oval)
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                float dx = (x - 8f) / 4f;
                float dy = (y - 8f) / 6f;
                float dist = dx * dx + dy * dy;
                
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
        return texture;
    }
    
    private void CreateRunDustParticles()
    {
        if (!enableRunDust) return;
        
        GameObject dustObj = new GameObject("RunDustParticles");
        dustObj.transform.SetParent(transform);
        
        runDustParticles = dustObj.AddComponent<ParticleSystem>();
        
        var main = runDustParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.1f;
        main.startColor = dustColor;
        main.playOnAwake = false;
        main.loop = true;
        
        var emission = runDustParticles.emission;
        emission.rateOverTime = 15;
        
        var shape = runDustParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.1f;
        
        var colorOverLifetime = runDustParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(dustColor, 0f),
                new GradientColorKey(dustColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.6f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var sizeOverLifetime = runDustParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.5f));
        
        // Renderer
        var renderer = dustObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = -1;
    }
    
    private void SpawnFootprint()
    {
        if (availableFootprints.Count == 0) return;
        
        SpriteRenderer footprint = availableFootprints.Dequeue();
        
        // Pozisyon - sağ/sol ayak değişimi
        float xOffset = isLeftFoot ? -0.1f : 0.1f;
        Vector3 pos = playerTransform.position + new Vector3(xOffset, -0.4f, 0);
        
        footprint.transform.position = pos;
        footprint.transform.localScale = Vector3.one * footprintSize;
        
        // Yön
        float facing = Mathf.Sign(playerTransform.localScale.x);
        footprint.transform.localScale = new Vector3(footprintSize * facing, footprintSize, 1);
        
        footprint.color = footprintColor;
        footprint.gameObject.SetActive(true);
        
        isLeftFoot = !isLeftFoot;
        
        StartCoroutine(FadeFootprint(footprint));
    }
    
    private System.Collections.IEnumerator FadeFootprint(SpriteRenderer footprint)
    {
        float elapsed = 0f;
        Color startColor = footprintColor;
        
        while (elapsed < footprintLifetime)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / footprintLifetime);
            footprint.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
        
        footprint.gameObject.SetActive(false);
        availableFootprints.Enqueue(footprint);
    }
}
