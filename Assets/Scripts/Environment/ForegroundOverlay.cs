using UnityEngine;

/// <summary>
/// Ön Plan Katmanı (Foreground Overlay)
/// Kameranın önünde yüzen dekoratif elemanlar oluşturur.
/// - Sis/toz partikülleri
/// - Yüzen yapraklar
/// - Işık hüzmeleri
/// 
/// 2.5D derinlik hissi için kritik bir eleman.
/// </summary>
public class ForegroundOverlay : MonoBehaviour
{
    [System.Serializable]
    public class ForegroundElement
    {
        public Sprite sprite;
        public int count = 5;
        [Range(0.1f, 3f)] public float minScale = 0.5f;
        [Range(0.1f, 3f)] public float maxScale = 1.5f;
        [Range(0f, 1f)] public float alpha = 0.3f;
        public float driftSpeed = 0.5f;
        public float floatAmplitude = 0.2f;
    }
    
    [Header("Ön Plan Türü")]
    [SerializeField] private ForegroundType foregroundType = ForegroundType.DustMotes;
    
    public enum ForegroundType
    {
        DustMotes,      // Toz parçacıkları
        FloatingLeaves, // Yüzen yapraklar
        LightRays,      // Işık hüzmeleri
        Fog,            // Sis katmanı
        Embers,         // 🔥 Uçuşan ateş/kıvılcımlar - hızlı!
        Fireflies,      // Ateş böcekleri
        Custom          // Özel sprite kullan
    }
    
    [Header("Ayarlar")]
    [SerializeField] private int particleCount = 50; // 15 → 50 (çok daha yoğun)
    [SerializeField] private float spawnRadius = 20f; // 12 → 20 (tüm ekranı kapla)
    [SerializeField] private float foregroundDepth = -5f;
    
    [Header("Hareket")]
    [SerializeField] private float driftSpeedX = 0.3f;
    [SerializeField] private float driftSpeedY = 0.1f;
    [SerializeField] private float floatAmplitude = 0.5f;
    [SerializeField] private float floatFrequency = 0.5f;
    
    [Header("Ember/Ateş Özel Ayarları")]
    [SerializeField] private float emberSpeed = 4f; // 8 → 4 (daha yavaş, daha uzun ekranda)
    [SerializeField] private float emberLifetime = 6f; // 2 → 6 (çok daha uzun ömür)
    [SerializeField] private bool emberGlow = true;
    [SerializeField] private bool spawnAcrossScreen = true; // Tüm ekranda spawn
    
    [Header("Görünüm")]
    [SerializeField] private Color tintColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private float minScale = 0.3f;
    [SerializeField] private float maxScale = 1f;
    [SerializeField] private bool randomRotation = true;
    [SerializeField] private float rotationSpeed = 10f;
    
    [Header("Blur Efekti (Sahte)")]
    [Tooltip("Ön plandaki objeler bulanık görünsün")]
    [SerializeField] private bool simulateBlur = true;
    [SerializeField] private int blurCopies = 2;
    [SerializeField] private float blurOffset = 0.02f;
    
    [Header("Özel Sprite (Custom type için)")]
    [SerializeField] private Sprite customSprite;
    
    // Internal
    private class ForegroundParticle
    {
        public GameObject obj;
        public SpriteRenderer renderer;
        public float floatOffset;
        public float scale;
        public float rotSpeed;
        public Vector2 driftDir;
        public GameObject[] blurCopies;
        
        // Ember için ek
        public float lifetime;
        public float maxLifetime;
        public Vector2 velocity;
        public bool isEmber;
    }
    
    private ForegroundParticle[] particles;
    private Transform cameraTransform;
    private Sprite activeSprite;
    private Vector3 lastCameraPos; // Kamera takibi için
    
    private void Start()
    {
        cameraTransform = Camera.main?.transform;
        if (cameraTransform == null)
        {
            enabled = false;
            return;
        }
        
        // Sprite seç
        activeSprite = GetSpriteForType();
        if (activeSprite == null)
        {
            activeSprite = CreateDefaultSprite();
        }
        
        CreateParticles();
    }
    
    private Sprite GetSpriteForType()
    {
        switch (foregroundType)
        {
            case ForegroundType.Custom:
                return customSprite;
            default:
                return null; // Prosedürel oluştur
        }
    }
    
    private Sprite CreateDefaultSprite()
    {
        // Basit beyaz daire texture
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 0f;
                
                switch (foregroundType)
                {
                    case ForegroundType.DustMotes:
                        // Yumuşak daire
                        alpha = Mathf.Clamp01(1f - (dist / radius));
                        alpha = alpha * alpha; // Daha yumuşak kenar
                        break;
                        
                    case ForegroundType.FloatingLeaves:
                        // Elips şekli
                        float nx = (x - center.x) / radius;
                        float ny = (y - center.y) / (radius * 0.5f);
                        alpha = (nx * nx + ny * ny < 1f) ? 0.8f : 0f;
                        break;
                        
                    case ForegroundType.LightRays:
                        // Dikey çizgi
                        float xDist = Mathf.Abs(x - center.x) / radius;
                        alpha = Mathf.Clamp01(1f - xDist * 3f);
                        alpha *= Mathf.Clamp01(1f - Mathf.Abs(y - center.y) / radius);
                        break;
                        
                    case ForegroundType.Fog:
                        // Geniş yumuşak blob
                        alpha = Mathf.Clamp01(1f - (dist / radius));
                        alpha = Mathf.Pow(alpha, 0.5f);
                        break;
                    
                    case ForegroundType.Embers:
                    case ForegroundType.Fireflies:
                        // Parlak küçük nokta - merkezi parlak
                        alpha = Mathf.Clamp01(1f - (dist / radius));
                        alpha = Mathf.Pow(alpha, 3f); // Çok keskin merkez
                        break;
                        
                    default:
                        alpha = Mathf.Clamp01(1f - (dist / radius));
                        break;
                }
                
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    
    private void CreateParticles()
    {
        particles = new ForegroundParticle[particleCount];
        
        bool isEmberType = foregroundType == ForegroundType.Embers || foregroundType == ForegroundType.Fireflies;
        
        for (int i = 0; i < particleCount; i++)
        {
            var particle = new ForegroundParticle();
            particle.isEmber = isEmberType;
            
            // Ana obje
            particle.obj = new GameObject($"FG_Particle_{i}");
            particle.obj.transform.SetParent(transform);
            
            // Rastgele pozisyon - Ember için TÜM EKRANDA spawn
            Vector2 randomPos;
            if (isEmberType && spawnAcrossScreen)
            {
                // Tüm ekranda rastgele spawn - her yerde ember olsun
                randomPos = new Vector2(
                    Random.Range(-spawnRadius, spawnRadius),
                    Random.Range(-spawnRadius, spawnRadius)
                );
            }
            else if (isEmberType)
            {
                // Sadece alttan spawn
                randomPos = new Vector2(
                    Random.Range(-spawnRadius, spawnRadius),
                    Random.Range(-spawnRadius, -spawnRadius * 0.3f)
                );
            }
            else
            {
                randomPos = Random.insideUnitCircle * spawnRadius;
            }
            
            particle.obj.transform.position = new Vector3(
                cameraTransform.position.x + randomPos.x,
                cameraTransform.position.y + randomPos.y,
                foregroundDepth
            );
            
            // Sprite renderer
            particle.renderer = particle.obj.AddComponent<SpriteRenderer>();
            particle.renderer.sprite = activeSprite;
            
            // Ember için turuncu/kırmızı renk
            if (isEmberType)
            {
                Color emberColor = Color.Lerp(
                    new Color(1f, 0.5f, 0f, 0.8f),  // Turuncu
                    new Color(1f, 0.2f, 0f, 0.9f),  // Kırmızı
                    Random.value
                );
                particle.renderer.color = emberColor;
            }
            else
            {
                particle.renderer.color = tintColor;
            }
            
            particle.renderer.sortingOrder = 32767; // Maksimum sorting order - EN ÖNDE!
            particle.renderer.sortingLayerName = "UI"; // UI layer varsa kullan
            
            // Rastgele özellikler
            particle.scale = Random.Range(minScale, maxScale);
            if (isEmberType)
            {
                particle.scale *= 0.5f; // Ember'lar daha küçük
            }
            particle.obj.transform.localScale = Vector3.one * particle.scale;
            particle.floatOffset = Random.value * Mathf.PI * 2f;
            particle.rotSpeed = randomRotation ? Random.Range(-rotationSpeed, rotationSpeed) : 0f;
            
            // Ember için özel velocity ve lifetime
            if (isEmberType)
            {
                // Yukarı ve hafif yana doğru hızlı hareket
                particle.velocity = new Vector2(
                    Random.Range(-2f, 2f),
                    Random.Range(emberSpeed * 0.7f, emberSpeed * 1.3f)
                );
                particle.maxLifetime = Random.Range(emberLifetime * 0.5f, emberLifetime * 1.5f);
                particle.lifetime = Random.value * particle.maxLifetime; // Başlangıçta farklı aşamalarda
                particle.driftDir = Vector2.zero; // Ember'lar velocity kullanır
            }
            else
            {
                particle.driftDir = new Vector2(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ).normalized;
            }
            
            // Blur kopyaları
            if (simulateBlur && blurCopies > 0)
            {
                particle.blurCopies = new GameObject[blurCopies];
                for (int b = 0; b < blurCopies; b++)
                {
                    var blur = new GameObject($"Blur_{b}");
                    blur.transform.SetParent(particle.obj.transform);
                    blur.transform.localPosition = new Vector3(
                        Random.Range(-blurOffset, blurOffset),
                        Random.Range(-blurOffset, blurOffset),
                        0
                    );
                    blur.transform.localScale = Vector3.one * (1f + blurOffset * (b + 1));
                    
                    var blurRenderer = blur.AddComponent<SpriteRenderer>();
                    blurRenderer.sprite = activeSprite;
                    blurRenderer.color = new Color(tintColor.r, tintColor.g, tintColor.b, tintColor.a * 0.3f);
                    blurRenderer.sortingOrder = 999;
                    
                    particle.blurCopies[b] = blur;
                }
            }
            
            if (randomRotation)
            {
                particle.obj.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            }
            
            particles[i] = particle;
        }
        
        // İlk kamera pozisyonunu kaydet
        lastCameraPos = cameraTransform.position;
    }
    
    private void LateUpdate()
    {
        if (cameraTransform == null) return;
        
        float time = Time.time;
        
        foreach (var particle in particles)
        {
            if (particle == null || particle.obj == null) continue;
            
            Vector3 pos = particle.obj.transform.position;
            
            // EMBER HAREKET - hızlı yukarı uçuş
            if (particle.isEmber)
            {
                particle.lifetime += Time.deltaTime;
                
                // Velocity ile hareket
                pos.x += particle.velocity.x * Time.deltaTime;
                pos.y += particle.velocity.y * Time.deltaTime;
                
                // Hafif yatay salınım
                pos.x += Mathf.Sin(time * 3f + particle.floatOffset) * 0.5f * Time.deltaTime;
                
                // ÖNEMLİ: Kamera ile birlikte hareket et (parallax gibi)
                // Böylece ember'lar kamera hareket edince ekrandan çıkmaz
                Vector3 camDelta = cameraTransform.position - lastCameraPos;
                pos.x += camDelta.x * 0.95f; // Kamerayla neredeyse aynı hızda
                pos.y += camDelta.y * 0.95f;
                
                // Fade out - ömür bitince
                float lifeRatio = particle.lifetime / particle.maxLifetime;
                if (particle.renderer != null)
                {
                    Color c = particle.renderer.color;
                    c.a = Mathf.Lerp(0.9f, 0f, lifeRatio);
                    particle.renderer.color = c;
                    
                    // Küçül
                    float scale = particle.scale * Mathf.Lerp(1f, 0.3f, lifeRatio);
                    particle.obj.transform.localScale = Vector3.one * scale;
                }
                
                // Ekran dışına çıktıysa VEYA ömür bittiyse yeniden spawn
                Vector2 toCam = new Vector2(
                    cameraTransform.position.x - pos.x,
                    cameraTransform.position.y - pos.y
                );
                
                if (particle.lifetime >= particle.maxLifetime || toCam.magnitude > spawnRadius * 1.5f)
                {
                    RespawnEmber(particle);
                    pos = particle.obj.transform.position; // Yeni pozisyonu al
                }
            }
            else
            {
                // Normal drift hareketi
                pos.x += particle.driftDir.x * driftSpeedX * Time.deltaTime;
                pos.y += particle.driftDir.y * driftSpeedY * Time.deltaTime;
                
                // Float hareketi
                float floatY = Mathf.Sin(time * floatFrequency + particle.floatOffset) * floatAmplitude * Time.deltaTime;
                pos.y += floatY;
                
                // Kameraya göre yeniden konumlandır (çok uzaklaşırsa)
                Vector2 toCam = new Vector2(
                    cameraTransform.position.x - pos.x,
                    cameraTransform.position.y - pos.y
                );
                
                if (toCam.magnitude > spawnRadius * 1.2f)
                {
                    Vector2 newPos = (Vector2)cameraTransform.position - toCam.normalized * spawnRadius * 0.9f;
                    newPos += Random.insideUnitCircle * 2f;
                    pos.x = newPos.x;
                    pos.y = newPos.y;
                }
            }
            
            // Z'yi koru
            pos.z = foregroundDepth;
            particle.obj.transform.position = pos;
            
            // Rotasyon
            if (randomRotation && !particle.isEmber)
            {
                particle.obj.transform.Rotate(0, 0, particle.rotSpeed * Time.deltaTime);
            }
        }
        
        // Kamera pozisyonunu güncelle (sonraki frame için)
        lastCameraPos = cameraTransform.position;
    }
    
    private void RespawnEmber(ForegroundParticle particle)
    {
        // Yeniden spawn - tüm ekranda veya alttan
        particle.lifetime = 0f;
        particle.maxLifetime = Random.Range(emberLifetime * 0.7f, emberLifetime * 1.3f);
        
        Vector3 newPos;
        if (spawnAcrossScreen)
        {
            // Tüm ekranda rastgele spawn
            newPos = new Vector3(
                cameraTransform.position.x + Random.Range(-spawnRadius, spawnRadius),
                cameraTransform.position.y + Random.Range(-spawnRadius * 0.8f, spawnRadius * 0.8f),
                foregroundDepth
            );
        }
        else
        {
            // Alttan spawn
            newPos = new Vector3(
                cameraTransform.position.x + Random.Range(-spawnRadius, spawnRadius),
                cameraTransform.position.y - spawnRadius * 0.5f + Random.Range(-2f, 2f),
                foregroundDepth
            );
        }
        particle.obj.transform.position = newPos;
        
        // Yeni velocity
        particle.velocity = new Vector2(
            Random.Range(-2f, 2f),
            Random.Range(emberSpeed * 0.7f, emberSpeed * 1.3f)
        );
        
        // Yeni renk
        Color emberColor = Color.Lerp(
            new Color(1f, 0.5f, 0f, 0.8f),
            new Color(1f, 0.2f, 0f, 0.9f),
            Random.value
        );
        particle.renderer.color = emberColor;
        
        // Scale reset
        particle.scale = Random.Range(minScale, maxScale) * 0.5f;
        particle.obj.transform.localScale = Vector3.one * particle.scale;
    }
    
    private void OnDestroy()
    {
        // Texture'ları temizle
        if (particles != null)
        {
            foreach (var p in particles)
            {
                if (p?.renderer?.sprite?.texture != null)
                {
                    // Sadece prosedürel oluşturduklarımızı sil
                    if (p.renderer.sprite.texture.name.StartsWith("FG_"))
                    {
                        Destroy(p.renderer.sprite.texture);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Efekt yoğunluğunu değiştir
    /// </summary>
    public void SetIntensity(float intensity)
    {
        Color newColor = tintColor;
        newColor.a = tintColor.a * intensity;
        
        foreach (var p in particles)
        {
            if (p?.renderer != null)
            {
                p.renderer.color = newColor;
            }
        }
    }
}
