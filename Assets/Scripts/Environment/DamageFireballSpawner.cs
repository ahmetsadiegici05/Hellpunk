using UnityEngine;

/// <summary>
/// Oyuncuya zarar veren ateş toplarını spawn eden sistem
/// Görsel HellFireEffects'in yanı sıra gerçek tehlike oluşturur
/// </summary>
public class DamageFireballSpawner : MonoBehaviour
{
    [Header("Spawn Ayarları")]
    [Tooltip("Ateş topu spawn'ını aktif et")]
    public bool enableSpawning = true;
    
    [Tooltip("İki ateş topu arası minimum süre")]
    [Range(1f, 10f)]
    public float minSpawnInterval = 2f;
    
    [Tooltip("İki ateş topu arası maksimum süre")]
    [Range(2f, 15f)]
    public float maxSpawnInterval = 5f;
    
    [Tooltip("Spawn alanı genişliği (kamera etrafında)")]
    public float spawnWidth = 20f;
    
    [Tooltip("Kameranın ne kadar üstünde spawn olsun")]
    public float spawnHeightAboveCamera = 10f;
    
    [Header("Ateş Topu Özellikleri")]
    [Range(1f, 15f)]
    public float fireballSpeed = 6f;
    
    [Range(0.1f, 1f)]
    public float fireballSize = 0.3f; // Küçük boyut
    
    [Range(1f, 5f)]
    public float fireballDamage = 1f;
    
    [Range(1f, 20f)]
    public float knockbackForce = 8f;
    
    [Header("Görsel")]
    public Color fireColor = new Color(1f, 0.4f, 0.1f);
    [Range(1f, 5f)]
    public float glowIntensity = 2f;
    
    [Header("Hedefleme")]
    [Tooltip("Ateş topları oyuncuya doğru hedef alsın")]
    public bool targetPlayer = true;
    [Tooltip("Hedefleme sapması (0 = kusursuz nişan, 2 = büyük sapma)")]
    [Range(0f, 3f)]
    public float aimRandomness = 1f;
    
    [Header("Uyarı Sistemi")]
    [Tooltip("Ateş topu düşmeden önce uyarı göster")]
    public bool showWarning = true;
    [Tooltip("Uyarı süresi (saniye)")]
    [Range(0.5f, 3f)]
    public float warningDuration = 1f;
    public Color warningColor = new Color(1f, 0f, 0f, 0.5f);
    
    [Header("Kamera Takibi")]
    public bool followCamera = true;
    
    private Transform cameraTransform;
    private float nextSpawnTime;
    
    void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        ScheduleNextSpawn();
    }
    
    void Update()
    {
        if (!enableSpawning) return;
        
        // Kamerayı takip et
        if (followCamera && cameraTransform != null)
        {
            transform.position = new Vector3(cameraTransform.position.x, cameraTransform.position.y, 0);
        }
        
        // Spawn zamanı geldi mi?
        if (Time.time >= nextSpawnTime)
        {
            SpawnFireball();
            ScheduleNextSpawn();
        }
    }
    
    void ScheduleNextSpawn()
    {
        float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
        nextSpawnTime = Time.time + interval;
    }
    
    void SpawnFireball()
    {
        // Rastgele X pozisyonu
        float randomX = Random.Range(-spawnWidth / 2f, spawnWidth / 2f);
        Vector3 spawnPos = new Vector3(
            transform.position.x + randomX,
            transform.position.y + spawnHeightAboveCamera,
            0
        );
        
        if (showWarning)
        {
            StartCoroutine(SpawnWithWarning(spawnPos));
        }
        else
        {
            CreateFireball(spawnPos);
        }
    }
    
    System.Collections.IEnumerator SpawnWithWarning(Vector3 spawnPos)
    {
        // Uyarı işareti oluştur
        GameObject warning = CreateWarningIndicator(spawnPos);
        
        // Uyarı süresi bekle
        yield return new WaitForSeconds(warningDuration);
        
        // Uyarıyı kaldır
        if (warning != null) Destroy(warning);
        
        // Ateş topunu spawn et
        CreateFireball(spawnPos);
    }
    
    GameObject CreateWarningIndicator(Vector3 spawnPos)
    {
        // Oyuncuyu bul
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 targetPos;
        
        if (player != null && targetPlayer)
        {
            // Oyuncuya doğru uyarı çizgisi
            targetPos = player.transform.position;
        }
        else
        {
            // Direk aşağı
            targetPos = new Vector3(spawnPos.x, spawnPos.y - 30f, 0);
        }
        
        // Uyarı çizgisi - spawn noktasından hedefe (ince çizgi)
        GameObject warning = new GameObject("FireballWarning");
        warning.transform.position = spawnPos;
        
        LineRenderer line = warning.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.SetPosition(0, new Vector3(spawnPos.x, spawnPos.y, 0));
        line.SetPosition(1, new Vector3(targetPos.x, targetPos.y, 0));
        
        // Çok ince çizgi - kare görünmez
        line.startWidth = 0.05f;
        line.endWidth = 0.08f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1f, 0.3f, 0f, 0.6f);
        line.endColor = new Color(1f, 0f, 0f, 0.4f);
        line.sortingOrder = 5;
        
        // Hedefte küçük yuvarlak işaret ekle
        AddTargetMarker(warning, targetPos);
        
        // Yanıp sönsün
        StartCoroutine(BlinkWarning(line));
        
        return warning;
    }
    
    void AddTargetMarker(GameObject parent, Vector3 targetPos)
    {
        // Hedef noktasında küçük yuvarlak işaret
        GameObject marker = new GameObject("TargetMarker");
        marker.transform.SetParent(parent.transform);
        marker.transform.position = new Vector3(targetPos.x, targetPos.y, 0);
        
        SpriteRenderer sr = marker.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite(32);
        sr.color = new Color(1f, 0.2f, 0f, 0.5f);
        sr.sortingOrder = 6;
        marker.transform.localScale = Vector3.one * 0.3f; // Küçük işaret
    }
    
    Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = size / 2f;
        float radius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (distance < radius)
                {
                    float alpha = 1f - (distance / radius);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * 0.8f));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        texture.Apply();
        texture.filterMode = FilterMode.Bilinear;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
    
    System.Collections.IEnumerator BlinkWarning(LineRenderer line)
    {
        float elapsed = 0f;
        while (elapsed < warningDuration && line != null)
        {
            // Yanıp sönme efekti
            float alpha = Mathf.PingPong(elapsed * 4f, 1f) * 0.5f + 0.2f;
            Color c = warningColor;
            c.a = alpha;
            line.startColor = c;
            line.endColor = new Color(c.r, c.g, c.b, 0f);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    void CreateFireball(Vector3 position)
    {
        // Z pozisyonunu 0 yap - oyuncu seviyesinde olmalı!
        Vector3 spawnPos = new Vector3(position.x, position.y, 0f);
        
        GameObject fireball = new GameObject("DamageFireball");
        fireball.transform.position = spawnPos;
        fireball.tag = "Hazard"; // Opsiyonel tag
        
        DamageFireball fb = fireball.AddComponent<DamageFireball>();
        fb.fallSpeed = fireballSpeed;
        fb.size = fireballSize;
        fb.damage = fireballDamage;
        fb.knockbackForce = knockbackForce;
        fb.fireColor = fireColor;
        fb.glowIntensity = glowIntensity;
        fb.targetPlayer = targetPlayer;
        fb.aimRandomness = aimRandomness;
    }
    
    void OnDrawGizmosSelected()
    {
        // Spawn alanını göster
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.3f);
        Vector3 center = transform.position + Vector3.up * spawnHeightAboveCamera;
        Gizmos.DrawWireCube(center, new Vector3(spawnWidth, 1f, 0.1f));
        
        // Aşağı ok
        Gizmos.color = Color.red;
        Gizmos.DrawLine(center, center + Vector3.down * 5f);
    }
}
