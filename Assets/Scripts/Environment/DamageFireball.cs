using UnityEngine;

/// <summary>
/// Oyuncuya zarar veren düşen ateş topu
/// Oyuncunun konumuna doğru hedef alır
/// </summary>
public class DamageFireball : MonoBehaviour
{
    [Header("Hareket")]
    public float fallSpeed = 11f; // Daha hızlı
    public float rotationSpeed = 180f;
    [Tooltip("Düşme yönü (kamera rotasyonuna göre ayarlanır)")]
    public Vector2 fallDirection = Vector2.down;
    
    [Header("Hedefleme")]
    [Tooltip("Oyuncuya doğru hedef alsın mı?")]
    public bool targetPlayer = true;
    [Tooltip("Hedefleme hassasiyeti (0 = tam hedef, 1 = büyük sapma)")]
    [Range(0f, 3f)]
    public float aimRandomness = 0.5f;
    [Tooltip("Oyuncu hareketini tahmin et (prediction)")]
    public bool predictPlayerMovement = true;
    [Tooltip("Tahmin çarpanı - yüksek = daha ilerisine nişan al")]
    [Range(0.5f, 2f)]
    public float predictionMultiplier = 1.2f;
    
    [Header("Hasar")]
    public float damage = 1f;
    public float knockbackForce = 5f;
    
    [Header("Görsel")]
    public Color fireColor = new Color(1f, 0.4f, 0.1f);
    public float glowIntensity = 2f;
    public float size = 0.25f; // Küçük boyut
    
    [Header("Efektler")]
    public bool createExplosionOnImpact = true;
    public float explosionRadius = 1f;
    
    private SpriteRenderer spriteRenderer;
    private CircleCollider2D circleCollider;
    private Rigidbody2D rb;
    private TrailRenderer trail;
    private Vector2 moveDirection;
    
    void Start()
    {
        SetupVisuals();
        SetupPhysics();
        SetupTrail();
        AimAtPlayer();
    }
    
    void AimAtPlayer()
    {
        if (!targetPlayer)
        {
            // Hedef yoksa kamera yönünde düş (fallDirection spawner tarafından ayarlanır)
            moveDirection = fallDirection.normalized;
            if (rb != null)
            {
                rb.linearVelocity = moveDirection * fallSpeed;
            }
            return;
        }
        
        // Oyuncuyu bul
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector2 targetPos = player.transform.position;
            
            // Oyuncunun hareketini tahmin et
            if (predictPlayerMovement)
            {
                Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    // Fireball'un oyuncuya ulaşma süresini hesapla
                    float distance = Vector2.Distance(transform.position, targetPos);
                    float timeToReach = distance / fallSpeed;
                    
                    // Oyuncunun o sürede nerede olacağını tahmin et
                    Vector2 predictedPos = targetPos + playerRb.linearVelocity * timeToReach * predictionMultiplier;
                    targetPos = predictedPos;
                }
            }
            
            // Rastgele sapma ekle (kaçış şansı için) - fallDirection yönünde sapma
            Vector2 perpendicular = new Vector2(-fallDirection.y, fallDirection.x); // Düşme yönüne dik
            targetPos += perpendicular * Random.Range(-aimRandomness, aimRandomness);
            
            Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
            moveDirection = direction;
            
            // Velocity'yi güncelle
            if (rb != null)
            {
                rb.linearVelocity = moveDirection * fallSpeed;
            }
        }
        else
        {
            // Oyuncu bulunamazsa kamera yönünde düş
            moveDirection = fallDirection.normalized;
            if (rb != null)
            {
                rb.linearVelocity = moveDirection * fallSpeed;
            }
        }
    }
    
    void SetupVisuals()
    {
        // Sprite Renderer
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = CreateCircleSprite();
        spriteRenderer.color = fireColor * glowIntensity;
        spriteRenderer.sortingOrder = 10; // Oyuncunun önünde
        
        transform.localScale = Vector3.one * size;
    }
    
    void SetupPhysics()
    {
        // Rigidbody2D
        rb = gameObject.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // Gravity kapalı - kendi yönümüz var
        rb.linearVelocity = Vector2.down * fallSpeed; // Başlangıçta aşağı, sonra AimAtPlayer güncelleyecek
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Circle Collider - büyük boyut ve trigger
        circleCollider = gameObject.AddComponent<CircleCollider2D>();
        circleCollider.radius = 0.5f; // Daha büyük collider
        circleCollider.isTrigger = true; // Trigger olarak kullan
        
        // Layer ayarla
        gameObject.layer = LayerMask.NameToLayer("Default");
        
        Debug.Log($"[DamageFireball] Spawn oldu: {transform.position}");
    }
    
    void SetupTrail()
    {
        // Trail efekti
        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.3f;
        trail.startWidth = size * 0.8f;
        trail.endWidth = 0f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = fireColor * glowIntensity;
        trail.endColor = new Color(fireColor.r, fireColor.g, fireColor.b, 0f);
        trail.sortingOrder = 9;
    }
    
    void Update()
    {
        // Dönsün
        transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        
        // Çok aşağı düştüyse yok et
        if (transform.position.y < -20f)
        {
            Destroy(gameObject);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Trigger için contact point yok, hareket yönüne bak
        HandleCollision(other.gameObject, other.transform, null);
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Contact point'leri HandleCollision'a gönder
        HandleCollision(collision.gameObject, collision.transform, collision);
    }
    
    void HandleCollision(GameObject other, Transform otherTransform, Collision2D collision)
    {
        // Oyuncuya çarptı mı?
        if (other.CompareTag("Player"))
        {
            // Shop açıkken hasar verme
            if (IsShopOpen())
            {
                Debug.Log("[DamageFireball] Shop açık - hasar verilmedi");
                return;
            }
            
            Debug.Log("[DamageFireball] Oyuncuya çarptı!");
            
            // Hasar ver - Health component kullan (EnemyDamage ile aynı sistem)
            Health health = other.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(damage);
                Debug.Log($"[DamageFireball] Hasar verildi: {damage}");
            }
            else
            {
                Debug.LogWarning("[DamageFireball] Health component bulunamadı!");
            }
            
            // Knockback
            Rigidbody2D playerRb = other.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                Vector2 knockbackDir = (otherTransform.position - transform.position).normalized;
                knockbackDir.y = 0.5f; // Biraz yukarı it
                playerRb.AddForce(knockbackDir * knockbackForce, ForceMode2D.Impulse);
            }
            
            // Patlama efekti
            if (createExplosionOnImpact)
            {
                CreateExplosionEffect();
            }
            
            Destroy(gameObject);
        }
        // Zemine çarptı mı?
        else if (other.CompareTag("Ground") || other.layer == LayerMask.NameToLayer("Ground"))
        {
            Debug.Log("[DamageFireball] Zemine çarptı!");
            if (createExplosionOnImpact)
            {
                CreateExplosionEffect();
            }
            Destroy(gameObject);
        }
    }
    
    void CreateExplosionEffect()
    {
        // Basit patlama particle
        GameObject explosion = new GameObject("FireballExplosion");
        explosion.transform.position = transform.position;
        
        ParticleSystem ps = explosion.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
        main.startColor = fireColor * glowIntensity;
        main.maxParticles = 20;
        main.duration = 0.1f;
        
        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, 15)
        });
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.2f;
        
        var renderer = explosion.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateGlowMaterial();
        renderer.sortingOrder = 15;
        
        // 2 saniye sonra yok et
        Destroy(explosion, 2f);
    }
    
    Sprite CreateCircleSprite()
    {
        int size = 128; // Daha yüksek çözünürlük
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f - 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                
                if (distance < radius)
                {
                    float normalizedDist = distance / radius;
                    float alpha = 1f - normalizedDist;
                    alpha = Mathf.Pow(alpha, 0.5f);
                    
                    // Anti-aliasing için kenar yumuşatma
                    if (distance > radius - 3f)
                    {
                        alpha *= 1f - ((distance - (radius - 3f)) / 3f);
                    }
                    
                    Color color = Color.Lerp(Color.white, fireColor, normalizedDist * 0.5f);
                    color.a = Mathf.Clamp01(alpha);
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        texture.filterMode = FilterMode.Trilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    
    Material CreateGlowMaterial()
    {
        // Yuvarlak texture oluştur
        Texture2D circleTexture = CreateCircleTexture(32);
        
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = circleTexture;
        mat.color = fireColor * glowIntensity;
        return mat;
    }
    
    Texture2D CreateCircleTexture(int size)
    {
        int textureSize = Mathf.Max(size, 64); // Minimum 64 piksel
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        float center = textureSize / 2f;
        float radius = textureSize / 2f - 1f;
        
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                
                if (distance < radius)
                {
                    float normalizedDist = distance / radius;
                    float alpha = 1f - normalizedDist;
                    alpha = Mathf.Pow(alpha, 0.6f);
                    
                    // Anti-aliasing
                    if (distance > radius - 2f)
                    {
                        alpha *= 1f - ((distance - (radius - 2f)) / 2f);
                    }
                    
                    Color color = Color.Lerp(Color.white, fireColor, normalizedDist);
                    color.a = Mathf.Clamp01(alpha);
                    texture.SetPixel(x, y, color);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        texture.filterMode = FilterMode.Trilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        return texture;
    }
    
    /// <summary>
    /// Shop paneli açık mı kontrol et
    /// </summary>
    private bool IsShopOpen()
    {
        // UIManager'dan shop durumunu kontrol et
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            UIManager uiManager = player.GetComponent<UIManager>();
            if (uiManager != null && uiManager.shopPanel != null)
            {
                return uiManager.shopPanel.activeInHierarchy;
            }
        }
        
        // Alternatif: ShopPanel tag'i ile bul
        GameObject shopPanel = GameObject.Find("ShopPanel");
        if (shopPanel != null)
        {
            return shopPanel.activeInHierarchy;
        }
        
        return false;
    }
}
