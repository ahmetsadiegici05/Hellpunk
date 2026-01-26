using UnityEngine;

/// <summary>
/// Ortamda yüzen ambient partiküller oluşturur (toz, yaprak, kıvılcım vb.)
/// Kameraya bağlı kalır ve sürekli partiküller üretir.
/// </summary>
public class AmbientParticles : MonoBehaviour
{
    [Header("Particle Settings")]
    [SerializeField] private int maxParticles = 30;
    [SerializeField] private float spawnRadius = 15f;
    [SerializeField] private float particleLifetime = 8f;
    [SerializeField] private float spawnInterval = 0.3f;
    
    [Header("Movement")]
    [SerializeField] private float driftSpeed = 0.5f;
    [SerializeField] private float floatAmplitude = 0.3f;
    [SerializeField] private float floatFrequency = 1f;
    
    [Header("Appearance")]
    [SerializeField] private Color particleColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private float minSize = 0.02f;
    [SerializeField] private float maxSize = 0.08f;
    [SerializeField] private int sortingOrder = 50;
    
    [Header("Type")]
    [SerializeField] private ParticleType particleType = ParticleType.Dust;
    
    public enum ParticleType { Dust, Leaves, Sparkles, Fireflies }
    
    private class Particle
    {
        public GameObject obj;
        public SpriteRenderer renderer;
        public float lifetime;
        public float maxLifetime;
        public Vector3 startPos;
        public float floatOffset;
        public float driftDirection;
        public float size;
    }
    
    private Particle[] particles;
    private Transform cameraTransform;
    private float spawnTimer;
    private Sprite particleSprite;
    
    private void Start()
    {
        cameraTransform = Camera.main?.transform;
        if (cameraTransform == null)
        {
            enabled = false;
            return;
        }
        
        particles = new Particle[maxParticles];
        particleSprite = CreateParticleSprite();
        
        // Başlangıçta bazı partiküller oluştur
        for (int i = 0; i < maxParticles / 2; i++)
        {
            SpawnParticle(i, true);
        }
    }
    
    private void Update()
    {
        spawnTimer += Time.deltaTime;
        
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnParticle();
        }
        
        UpdateParticles();
    }
    
    private void TrySpawnParticle()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            if (particles[i] == null || particles[i].obj == null)
            {
                SpawnParticle(i, false);
                break;
            }
        }
    }
    
    private void SpawnParticle(int index, bool randomLifetime)
    {
        if (particles[index] == null)
        {
            particles[index] = new Particle();
        }
        
        if (particles[index].obj == null)
        {
            GameObject obj = new GameObject($"AmbientParticle_{index}");
            obj.transform.SetParent(transform);
            
            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = particleSprite;
            sr.sortingOrder = sortingOrder;
            
            particles[index].obj = obj;
            particles[index].renderer = sr;
        }
        
        Particle p = particles[index];
        
        // Rastgele pozisyon (kamera etrafında)
        Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
        p.startPos = cameraTransform.position + new Vector3(randomOffset.x, randomOffset.y, 0);
        p.obj.transform.position = p.startPos;
        
        // Rastgele özellikler
        p.maxLifetime = randomLifetime ? Random.Range(0f, particleLifetime) : particleLifetime;
        p.lifetime = p.maxLifetime;
        p.floatOffset = Random.Range(0f, Mathf.PI * 2f);
        p.driftDirection = Random.Range(-1f, 1f);
        p.size = Random.Range(minSize, maxSize);
        
        p.obj.transform.localScale = Vector3.one * p.size;
        
        // Renk (partikül tipine göre)
        Color startColor = GetParticleColor();
        p.renderer.color = startColor;
        
        p.obj.SetActive(true);
    }
    
    private Color GetParticleColor()
    {
        switch (particleType)
        {
            case ParticleType.Dust:
                return new Color(
                    particleColor.r + Random.Range(-0.1f, 0.1f),
                    particleColor.g + Random.Range(-0.1f, 0.1f),
                    particleColor.b + Random.Range(-0.1f, 0.1f),
                    particleColor.a
                );
            case ParticleType.Leaves:
                return new Color(
                    Random.Range(0.4f, 0.6f),
                    Random.Range(0.6f, 0.9f),
                    Random.Range(0.2f, 0.4f),
                    particleColor.a
                );
            case ParticleType.Sparkles:
                return new Color(1f, 1f, Random.Range(0.7f, 1f), particleColor.a);
            case ParticleType.Fireflies:
                return new Color(
                    Random.Range(0.8f, 1f),
                    Random.Range(0.9f, 1f),
                    Random.Range(0.3f, 0.6f),
                    particleColor.a
                );
            default:
                return particleColor;
        }
    }
    
    private void UpdateParticles()
    {
        float time = Time.time;
        
        for (int i = 0; i < particles.Length; i++)
        {
            Particle p = particles[i];
            if (p == null || p.obj == null || !p.obj.activeSelf) continue;
            
            p.lifetime -= Time.deltaTime;
            
            if (p.lifetime <= 0)
            {
                p.obj.SetActive(false);
                continue;
            }
            
            // Hareket
            float floatY = Mathf.Sin((time + p.floatOffset) * floatFrequency) * floatAmplitude;
            float driftX = p.driftDirection * driftSpeed * Time.deltaTime;
            
            Vector3 newPos = p.obj.transform.position;
            newPos.x += driftX;
            newPos.y = p.startPos.y + floatY;
            p.obj.transform.position = newPos;
            
            // Fade in/out
            float lifePercent = p.lifetime / p.maxLifetime;
            float alpha = particleColor.a;
            
            // İlk %20'de fade in, son %20'de fade out
            if (lifePercent > 0.8f)
                alpha *= (1f - lifePercent) / 0.2f;
            else if (lifePercent < 0.2f)
                alpha *= lifePercent / 0.2f;
            
            Color c = p.renderer.color;
            c.a = alpha;
            p.renderer.color = c;
            
            // Fireflies için parıldama
            if (particleType == ParticleType.Fireflies)
            {
                float glow = 0.5f + 0.5f * Mathf.Sin((time + p.floatOffset) * 3f);
                c.a = alpha * glow;
                p.renderer.color = c;
            }
        }
    }
    
    private Sprite CreateParticleSprite()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(dist / (size / 2f));
                alpha = Mathf.Pow(alpha, 2f); // Soft edge
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    
    private void OnDestroy()
    {
        if (particles != null)
        {
            foreach (var p in particles)
            {
                if (p?.obj != null)
                    Destroy(p.obj);
            }
        }
    }
}
