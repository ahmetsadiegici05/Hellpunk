using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private AudioClip bossDeathSound;
    [SerializeField] private AudioClip enemyHitSound;
    [SerializeField] private AudioClip playerHitSound;
    [SerializeField] public AudioClip guitarSound1;
    [SerializeField] public AudioClip guitarSound2;
    [SerializeField] public AudioClip guitarSound3;

    [Header("Data")]
    [SerializeField] private int startingCoin = 100; // Başlangıç coin miktarı - dengeli ekonomi
    public int coin;
    
    private const string COIN_KEY = "PLAYER_COIN";
    
    // Level geçişinde oyuncu verilerini sakla
    [HideInInspector] public float savedPlayerHealth = -1f;
    [HideInInspector] public float savedPlayerMaxHealth = -1f;
    [HideInInspector] public bool isTransitioningLevel = false;

    [Header("World Renderers")]
    public List<SpriteRenderer> sprites;
    public List<Tilemap> tilemaps;

    [Header("Chest System")]
    public GameObject portalPrefab;
    public GameObject chestPrefab;
    [Tooltip("Eski sistem - PortalPoint tag'li objeler otomatik bulunur")]
    public List<Transform> randomPoints;
    public List<Transform> randomPoints2;
    
    [Header("Portal Spawn Ayarları")]
    [Tooltip("Her sahnede spawn edilecek maksimum portal sayısı")]
    [SerializeField] private int maxPortalsPerScene = 5;
    [Tooltip("Her spawn noktasında portal çıkma şansı (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float portalSpawnChance = 0.6f;
    [Tooltip("Minimum portal sayısı")]
    [SerializeField] private int minPortals = 2;
    
    // Public accessor for PortalPointTrigger
    public float PortalSpawnChance => portalSpawnChance;

    [Header("Ulti")]
    public GameObject ultiObject;
    public Animator ultiAnimator;

    [Header("Portal Transforms")]
    public Transform chestRoomSpawnPoint;
    public Transform enemySpawnPoint;
    public Transform chestSpawnPoint;


    public float lastTransformRotationValue = 0;
    public Vector3 lastCheckpointPosition;
    public bool hasCheckpoint;
    public bool isRotation = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Coin yükle
        LoadCoin();

        EnsureSoulSystem();
        EnsureTimeSlowSystem();
        EnsureGuitarSkillSystem();
        EnsureShopManager();
        EnsureGameFeedbackUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSoulSystem();
        EnsureTimeSlowSystem();
        EnsureGuitarSkillSystem();
        EnsureShopManager();
        EnsureGameFeedbackUI();

        SpawnRandomChests();
        SpawnRandomPortals();
        
        // Level geçişinden sonra oyuncu sağlığını geri yükle
        RestorePlayerHealth();
        
        // Level geçişinde karanlık ekranı düzelt - vignette'leri sıfırla
        ResetAllVignettes();
        
        // Kamera rotasyonunu sıfırla (transition'dan kalmış olabilir)
        ResetCameraState();
        
        // Mini-map sistemini başlat
        EnsureMiniMapSystem();
        
        // Karanlık bölge efektini başlat
        EnsureDarkForestEffect();
        
        // Radar sistemini başlat (karanlık bölgeler için)
        EnsureRadarSystem();
    }
    
    /// <summary>
    /// SimpleDarkOverlay sisteminin varlığını kontrol et ve yoksa oluştur
    /// </summary>
    private void EnsureDarkForestEffect()
    {
        // Sadece oyun sahnelerinde
        string sceneName = SceneManager.GetActiveScene().name.ToLower();
        if (sceneName.Contains("menu") || sceneName.Contains("main"))
        {
            return;
        }
        
        // SimpleDarkOverlay yoksa oluştur
        if (SimpleDarkOverlay.Instance == null)
        {
            GameObject darkObj = new GameObject("SimpleDarkOverlay");
            darkObj.AddComponent<SimpleDarkOverlay>();
            Debug.Log("[GameManager] SimpleDarkOverlay oluşturuldu - F10 ile test edebilirsin!");
        }
    }
    
    /// <summary>
    /// Radar sisteminin varlığını kontrol et ve yoksa oluştur
    /// Karanlık bölgelerde düşmanları tespit etmek için kullanılır
    /// </summary>
    private void EnsureRadarSystem()
    {
        // Sadece oyun sahnelerinde radar hazırla
        string sceneName = SceneManager.GetActiveScene().name.ToLower();
        if (sceneName.Contains("menu") || sceneName.Contains("main"))
        {
            return;
        }
        
        // EnemyRadarSystem yoksa oluştur
        if (EnemyRadarSystem.Instance == null)
        {
            GameObject radarObj = new GameObject("EnemyRadarSystem");
            radarObj.AddComponent<EnemyRadarSystem>();
            DontDestroyOnLoad(radarObj);
            Debug.Log("[GameManager] EnemyRadarSystem oluşturuldu");
        }
        
        // DarkZoneTooltip yoksa oluştur
        if (DarkZoneTooltip.Instance == null)
        {
            GameObject tooltipObj = new GameObject("DarkZoneTooltip");
            tooltipObj.AddComponent<DarkZoneTooltip>();
            DontDestroyOnLoad(tooltipObj);
            Debug.Log("[GameManager] DarkZoneTooltip oluşturuldu");
        }
    }
    
    /// <summary>
    /// Mini-map sisteminin varlığını kontrol et ve yoksa oluştur
    /// </summary>
    private void EnsureMiniMapSystem()
    {
        // Sadece oyun sahnelerinde mini-map göster (ana menü hariç)
        string sceneName = SceneManager.GetActiveScene().name.ToLower();
        if (sceneName.Contains("menu") || sceneName.Contains("main"))
        {
            // Menüde mini-map'i gizle
            if (MiniMap2D.Instance != null)
            {
                MiniMap2D.Instance.Hide();
            }
            return;
        }
        
        // MiniMap2D yoksa oluştur (2D side-scroller için optimize)
        if (MiniMap2D.Instance == null)
        {
            GameObject miniMapObj = new GameObject("MiniMap2DSystem");
            miniMapObj.AddComponent<MiniMap2D>();
            DontDestroyOnLoad(miniMapObj);
            Debug.Log("[GameManager] MiniMap2D sistemi oluşturuldu");
        }
        else
        {
            // Varsa göster
            MiniMap2D.Instance.Show();
        }
    }
    
    /// <summary>
    /// Kamera durumunu sıfırla - restart/level geçişi buglarını önler
    /// </summary>
    private void ResetCameraState()
    {
        // Ana kamerayı bul ve sıfırla
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.rotation = Quaternion.identity;
            Debug.Log("[GameManager] Kamera rotasyonu sıfırlandı");
        }
        
        // Duplicate kameraları temizle (sadece 1 ana kamera olmalı)
        Camera[] allCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        if (allCameras.Length > 1)
        {
            Debug.LogWarning($"[GameManager] {allCameras.Length} kamera bulundu! Duplicate temizleniyor...");
            bool foundMain = false;
            foreach (Camera cam in allCameras)
            {
                if (cam.CompareTag("MainCamera"))
                {
                    if (!foundMain)
                    {
                        foundMain = true;
                        cam.transform.rotation = Quaternion.identity;
                    }
                    else
                    {
                        // Duplicate MainCamera - yok et
                        Debug.Log($"[GameManager] Duplicate kamera yok edildi: {cam.name}");
                        Destroy(cam.gameObject);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Tüm vignette efektlerini sıfırla - level geçişlerinde çağrılır
    /// </summary>
    private void ResetAllVignettes()
    {
        // DamageVignette sıfırla
        if (DamageVignette.Instance != null)
        {
            DamageVignette.Instance.ResetVignette();
            Debug.Log("[GameManager] DamageVignette sıfırlandı");
        }
        
        // ScreenEffects sıfırla
        if (ScreenEffects.Instance != null)
        {
            ScreenEffects.Instance.UpdateHealthVignette(1f); // Full health = no vignette
            Debug.Log("[GameManager] ScreenEffects sıfırlandı");
        }
        
        // SceneTransitionController'ın blackout'unu da temizle (güvenlik önlemi)
        ClearSceneTransitionBlackout();
    }
    
    /// <summary>
    /// SceneTransitionController'ın blackout overlay'ini temizle
    /// </summary>
    private void ClearSceneTransitionBlackout()
    {
        if (SceneTransitionController.Instance != null)
        {
            SceneTransitionController.Instance.ClearBlackout();
            Debug.Log("[GameManager] SceneTransitionController blackout temizlendi");
        }
    }
    
    private void RestorePlayerHealth()
    {
        if (!isTransitioningLevel) return;
        
        // Oyuncuyu bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Health playerHealth = playerObj.GetComponent<Health>();
            if (playerHealth != null)
            {
                // Level geçişinde canı koru, ama minimum 1 can olsun (ölü gelmesin)
                float healthToRestore = savedPlayerHealth > 0 ? savedPlayerHealth : playerHealth.maxHealth;
                
                // Eğer kaydedilen can max'tan büyükse (upgrade durumu), max'a ayarla
                if (healthToRestore > playerHealth.maxHealth)
                    healthToRestore = playerHealth.maxHealth;
                
                // Canı ayarla
                playerHealth.currentHealth = healthToRestore;
                
                // Vignette'i oyuncunun canına göre güncelle (karanlık ekran düzeltmesi)
                float healthPercent = healthToRestore / playerHealth.maxHealth;
                if (ScreenEffects.Instance != null)
                    ScreenEffects.Instance.UpdateHealthVignette(healthPercent);
                if (DamageVignette.Instance != null)
                    DamageVignette.Instance.UpdateHealthStatus(healthPercent);
                    
                Debug.Log($"[GameManager] Oyuncu canı geri yüklendi: {healthToRestore}/{playerHealth.maxHealth}");
            }
        }
        
        // Reset transition flag
        isTransitioningLevel = false;
        savedPlayerHealth = -1f;
        savedPlayerMaxHealth = -1f;
    }
    
    public void SavePlayerHealthForTransition()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Health playerHealth = playerObj.GetComponent<Health>();
            if (playerHealth != null)
            {
                savedPlayerHealth = playerHealth.currentHealth;
                savedPlayerMaxHealth = playerHealth.maxHealth;
                isTransitioningLevel = true;
                Debug.Log($"[GameManager] Oyuncu canı kaydedildi: {savedPlayerHealth}/{savedPlayerMaxHealth}");
            }
        }
    }

    // ================= CHEST SPAWN =================

    private void SpawnRandomChests()
    {
        randomPoints2 = new List<Transform>();

        // "ChestPoint" tag'li tüm spawn noktalarını bul
        GameObject[] points = GameObject.FindGameObjectsWithTag("ChestPoint");
        Debug.Log($"[GameManager] ChestPoint sayısı: {points.Length}");
        
        foreach (var p in points)
            randomPoints2.Add(p.transform);

        if (chestPrefab == null)
        {
            Debug.LogWarning("[GameManager] chestPrefab atanmamış! Inspector'dan ata.");
            return;
        }
        
        if (randomPoints2.Count == 0)
        {
            Debug.LogWarning("[GameManager] Sahnede 'ChestPoint' tag'li obje bulunamadı! Rastgele sandık spawn edilemedi.");
            return;
        }

        // Eski chestleri temizle
        GameObject[] oldChests = GameObject.FindGameObjectsWithTag("Chest");
        foreach (var chest in oldChests)
            Destroy(chest);

        // Random noktaları kopyala
        List<Transform> shuffledPoints = new List<Transform>(randomPoints2);

        // Fisher–Yates shuffle
        for (int i = 0; i < shuffledPoints.Count; i++)
        {
            int rnd = Random.Range(i, shuffledPoints.Count);
            Transform temp = shuffledPoints[i];
            shuffledPoints[i] = shuffledPoints[rnd];
            shuffledPoints[rnd] = temp;
        }

        int spawnCount = Mathf.Min(5, shuffledPoints.Count);
        Debug.Log($"[GameManager] {spawnCount} sandık spawn edilecek (Toplam nokta: {shuffledPoints.Count})");

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject chest = Instantiate(
                chestPrefab,
                shuffledPoints[i].position,
                Quaternion.identity,
                shuffledPoints[i]
            );

            // Puzzle devre dışı - sandıklar vurarak kırılacak
            EnemyHealth enemyHealth = chest.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.hasPuzzle = false; // Puzzle kapalı
                enemyHealth.puzzleType = EnemyHealth.PuzzleType.None;
                
                Debug.Log($"[GameManager] Sandık {i+1} spawn edildi: {shuffledPoints[i].position}");
            }
        }
        
        Debug.Log($"[GameManager] ✅ Rastgele {spawnCount} sandık başarıyla spawn edildi!");
    }

    // ================= PORTAL SPAWN =================

    private void SpawnRandomPortals()
    {
        if (portalPrefab == null)
        {
            Debug.LogWarning("[GameManager] portalPrefab atanmamış!");
            return;
        }

        // Eski portalları temizle
        GameObject[] oldPortals = GameObject.FindGameObjectsWithTag("Portal");
        foreach (var portal in oldPortals)
            Destroy(portal);

        // Önce "PortalPoint" tag'li noktaları bul (yeni sistem)
        List<Transform> spawnPoints = new List<Transform>();
        GameObject[] portalPoints = GameObject.FindGameObjectsWithTag("PortalPoint");
        foreach (var point in portalPoints)
        {
            if (point != null)
                spawnPoints.Add(point.transform);
        }

        // Eğer PortalPoint yoksa eski randomPoints listesini kullan
        if (spawnPoints.Count == 0 && randomPoints != null)
        {
            foreach (var point in randomPoints)
            {
                if (point != null)
                    spawnPoints.Add(point);
            }
        }

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[GameManager] Portal spawn noktası bulunamadı! (PortalPoint tag'li veya randomPoints)");
            return;
        }

        Debug.Log($"[GameManager] {spawnPoints.Count} portal spawn noktası bulundu");

        // Her nokta için spawn şansını hesapla
        List<Transform> eligiblePoints = new List<Transform>();
        
        foreach (var point in spawnPoints)
        {
            if (point == null) continue;

            // Spawn şansına göre ekle
            if (Random.value < portalSpawnChance)
            {
                eligiblePoints.Add(point);
            }
        }

        // Minimum portal sayısını garanti et
        if (eligiblePoints.Count < minPortals)
        {
            List<Transform> remaining = new List<Transform>(spawnPoints);
            foreach (var added in eligiblePoints)
                remaining.Remove(added);

            // Shuffle remaining
            for (int i = 0; i < remaining.Count; i++)
            {
                int rnd = Random.Range(i, remaining.Count);
                var temp = remaining[i];
                remaining[i] = remaining[rnd];
                remaining[rnd] = temp;
            }

            int needed = minPortals - eligiblePoints.Count;
            for (int i = 0; i < needed && i < remaining.Count; i++)
            {
                eligiblePoints.Add(remaining[i]);
            }
        }

        // Maksimum sayıyı aşmasın
        if (eligiblePoints.Count > maxPortalsPerScene)
        {
            // Shuffle ve kes
            for (int i = 0; i < eligiblePoints.Count; i++)
            {
                int rnd = Random.Range(i, eligiblePoints.Count);
                var temp = eligiblePoints[i];
                eligiblePoints[i] = eligiblePoints[rnd];
                eligiblePoints[rnd] = temp;
            }
            eligiblePoints = eligiblePoints.GetRange(0, maxPortalsPerScene);
        }

        // Portalları spawn et
        foreach (var point in eligiblePoints)
        {
            if (point == null) continue;
            
            GameObject portal = Instantiate(
                portalPrefab,
                point.position,
                Quaternion.identity,
                point // parent olarak point
            );

            Debug.Log($"[GameManager] Portal spawnlandı: {point.position}");
        }

        Debug.Log($"[GameManager] Toplam {eligiblePoints.Count} portal spawn edildi");
    }

    // ================= SYSTEMS =================

    private void EnsureSoulSystem()
    {
        if (SoulSystem.Instance == null)
        {
            var existing = FindFirstObjectByType<SoulSystem>();
            if (existing != null)
                DontDestroyOnLoad(existing.gameObject);
            else
            {
                GameObject obj = new GameObject("SoulSystem");
                obj.AddComponent<SoulSystem>();
                DontDestroyOnLoad(obj);
            }
        }

        var soulUI = FindFirstObjectByType<SoulUI>();
        if (soulUI != null)
            DontDestroyOnLoad(soulUI.gameObject);
        else
        {
            GameObject uiObj = new GameObject("SoulUI");
            uiObj.AddComponent<SoulUI>();
            DontDestroyOnLoad(uiObj);
        }
    }

    private void EnsureTimeSlowSystem()
    {
        var audioController = FindFirstObjectByType<TimeSlowAudioController>();
        if (audioController != null)
            DontDestroyOnLoad(audioController.gameObject);
        else
        {
            GameObject obj = new GameObject("TimeSlowAudioController");
            obj.AddComponent<TimeSlowAudioController>();
            DontDestroyOnLoad(obj);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<TimeSlowAbility>() == null)
            player.AddComponent<TimeSlowAbility>();
    }

    private void EnsureGuitarSkillSystem()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (player.GetComponent<GuitarSkillSystem>() == null)
                player.AddComponent<GuitarSkillSystem>();

            if (player.GetComponent<SkillInputUI>() == null)
                player.AddComponent<SkillInputUI>();
        }

        var skillUI = FindFirstObjectByType<SkillCooldownUI>();
        if (skillUI != null)
            DontDestroyOnLoad(skillUI.gameObject);
        else
        {
            GameObject uiObj = new GameObject("SkillCooldownUI_Manager");
            uiObj.AddComponent<SkillCooldownUI>();
            DontDestroyOnLoad(uiObj);
        }
    }
    
    private void EnsureShopManager()
    {
        // ShopManager yoksa oluştur
        if (ShopManager.Instance == null)
        {
            var existingShop = FindFirstObjectByType<ShopManager>();
            if (existingShop == null)
            {
                GameObject shopObj = new GameObject("ShopManager");
                shopObj.AddComponent<ShopManager>();
                DontDestroyOnLoad(shopObj);
                Debug.Log("[GameManager] ShopManager oluşturuldu");
            }
        }
        
        // UIManager'a ShopManager referansını bağla
        if (UIManager.Instance != null && UIManager.Instance.shopManager == null)
        {
            UIManager.Instance.shopManager = ShopManager.Instance;
        }
    }
    
    /// <summary>
    /// GameFeedbackUI sistemini kontrol et ve yoksa oluştur
    /// Dash yazısı ve skill input feedback'i için
    /// </summary>
    private void EnsureGameFeedbackUI()
    {
        // MainMenu'de oluşturma
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower();
        if (sceneName.Contains("menu") || sceneName.Contains("main"))
        {
            return;
        }
        
        if (GameFeedbackUI.Instance == null)
        {
            var existing = FindFirstObjectByType<GameFeedbackUI>();
            if (existing == null)
            {
                GameObject feedbackObj = new GameObject("GameFeedbackUI");
                feedbackObj.AddComponent<GameFeedbackUI>();
                DontDestroyOnLoad(feedbackObj);
                Debug.Log("[GameManager] GameFeedbackUI oluşturuldu");
            }
        }
    }

    // ================= AUDIO =================

    public void PlayHealSound() => PlayOneShot(healSound);
    public void PlayButtonSound() => PlayOneShot(buttonClickSound);
    public void PlayAttackSound() => PlayOneShot(attackSound);

    public void PlayFootstepSound()
    {
        if (footstepSound == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        PlayOneShot(footstepSound);
        audioSource.pitch = 1f;
    }

    public void PlayBossDeathSound() => PlayOneShot(bossDeathSound);

    public void PlayEnemyHitSound()
    {
        if (enemyHitSound == null) return;
        audioSource.pitch = Random.Range(0.9f, 1.1f);
        PlayOneShot(enemyHitSound);
        audioSource.pitch = 1f;
    }

    public void PlayPlayerHitSound()
    {
        if (playerHitSound == null) return;
        audioSource.pitch = Random.Range(0.95f, 1.05f);
        PlayOneShot(playerHitSound);
        audioSource.pitch = 1f;
    }

    public void PlayGuitarSound(int index)
    {
        AudioClip clip = index switch
        {
            1 => guitarSound1,
            2 => guitarSound2,
            3 => guitarSound3,
            _ => guitarSound1
        };
        
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip);
            audioSource.pitch = 1f;
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    // ================= PUZZLE SYSTEM =================
    
    private bool isPuzzleActive = false;
    private float savedTimeScale = 1f;
    private Vector2 savedPlayerVelocity;
    private Rigidbody2D playerRigidbody;
    
    /// <summary>
    /// Global static flag - PlayerMovement ve diğer scriptler bunu kontrol eder
    /// </summary>
    public static bool IsPuzzleActive { get; private set; } = false;
    
    /// <summary>
    /// Puzzle açıldığında çağrılır - oyunu durdurur, müziği kısar
    /// </summary>
    public void OnPuzzleStart()
    {
        if (isPuzzleActive) return;
        isPuzzleActive = true;
        IsPuzzleActive = true; // Static flag'i de set et
        
        // Oyunu durdur (puzzle UI'lar WaitForSecondsRealtime kullanıyor)
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        // Müziği yarıya indir
        if (TimeSlowAudioController.Instance != null)
        {
            TimeSlowAudioController.Instance.SetPuzzleVolume(true);
        }
        else
        {
            Debug.LogWarning("[GameManager] TimeSlowAudioController.Instance null!");
        }
        
        // Oyuncuyu durdur ve velocity'yi sıfırla
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            player.lockMovement = true;
            
            // Rigidbody velocity'yi kaydet ve sıfırla (havada donmasın)
            playerRigidbody = player.GetComponent<Rigidbody2D>();
            if (playerRigidbody != null)
            {
                savedPlayerVelocity = playerRigidbody.linearVelocity;
                playerRigidbody.linearVelocity = Vector2.zero;
                playerRigidbody.bodyType = RigidbodyType2D.Kinematic; // Tamamen durdur
            }
        }
        
        Debug.Log("[GameManager] Puzzle başladı - oyun durdu, müzik kısıldı");
    }
    
    /// <summary>
    /// Puzzle kapandığında çağrılır - oyunu devam ettirir, müziği normale döndürür
    /// </summary>
    public void OnPuzzleEnd()
    {
        if (!isPuzzleActive) return;
        isPuzzleActive = false;
        IsPuzzleActive = false; // Static flag'i de sıfırla
        
        // Oyuncuyu serbest bırak (Time.timeScale'den ÖNCE yap)
        PlayerMovement player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            player.lockMovement = false;
            
            // Rigidbody'yi geri aç ve gravity'yi restore et
            if (playerRigidbody != null)
            {
                playerRigidbody.bodyType = RigidbodyType2D.Dynamic;
                playerRigidbody.gravityScale = 7f; // groundGravity default değeri
                // Önceki velocity'yi geri yüklemiyoruz, sıfırdan başlasın
            }
        }
        
        // Oyunu devam ettir
        Time.timeScale = savedTimeScale;
        
        // Müziği normale döndür
        if (TimeSlowAudioController.Instance != null)
        {
            TimeSlowAudioController.Instance.SetPuzzleVolume(false);
        }
        
        Debug.Log("[GameManager] Puzzle bitti - oyun devam ediyor, müzik normale döndü");
    }

    #region PORTAL RETURN SYSTEM

    private Transform cachedPlayer;
    private Vector3 cachedPlayerPosition;
    private bool hasReturnPosition = false;

    public void SavePlayerReturnPoint(Transform player)
    {
        cachedPlayer = player;
        cachedPlayerPosition = player.position;
        hasReturnPosition = true;

        Debug.Log($"[GameManager] Geri dönüş pozisyonu kaydedildi: {cachedPlayerPosition}");
    }

    public void ReturnPlayerToSavedPosition()
    {
        if (!hasReturnPosition || cachedPlayer == null)
        {
            Debug.LogWarning("[GameManager] Geri dönüş bilgisi yok!");
            return;
        }

        cachedPlayer.position = cachedPlayerPosition;

        Rigidbody2D rb = cachedPlayer.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        
        // Damage vignette sıfırla
        if (DamageVignette.Instance != null)
            DamageVignette.Instance.ResetVignette();

        Debug.Log("[GameManager] Player eski pozisyona döndü");

        hasReturnPosition = false;
    }

    public void OnSceneChanged()
    {
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.RefreshAllComponents();
        }
    }
    
    public void UpdateCoinUI()
    {
        // ShopManager'ı güncelle
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.UpdateCoinText();
        }
        
        // CoinUI'ı da güncelle (Time.timeScale = 0 olduğunda Update çalışmaz)
        var coinUI = FindFirstObjectByType<CoinUI>();
        if (coinUI != null)
        {
            coinUI.ForceUpdateDisplay();
        }
    }

    #region Coin Management
    
    /// <summary>
    /// Coin'i PlayerPrefs'ten yükle
    /// </summary>
    public void LoadCoin()
    {
        if (PlayerPrefs.HasKey(COIN_KEY))
        {
            coin = PlayerPrefs.GetInt(COIN_KEY);
            Debug.Log($"[GameManager] Coin yüklendi: {coin}");
        }
        else
        {
            // İlk kez - başlangıç değeri
            coin = startingCoin;
            SaveCoin();
            Debug.Log($"[GameManager] İlk başlangıç - Coin: {coin}");
        }
    }
    
    /// <summary>
    /// Coin'i PlayerPrefs'e kaydet
    /// </summary>
    public void SaveCoin()
    {
        PlayerPrefs.SetInt(COIN_KEY, coin);
        PlayerPrefs.Save();
    }
    
    /// <summary>
    /// Coin ekle ve kaydet
    /// </summary>
    public void AddCoin(int amount)
    {
        coin += amount;
        SaveCoin();
        UpdateCoinUI();
        Debug.Log($"[GameManager] +{amount} coin eklendi. Toplam: {coin}");
    }
    
    /// <summary>
    /// Coin harca ve kaydet
    /// </summary>
    public bool SpendCoin(int amount)
    {
        if (coin >= amount)
        {
            coin -= amount;
            SaveCoin();
            UpdateCoinUI();
            Debug.Log($"[GameManager] -{amount} coin harcandı. Kalan: {coin}");
            return true;
        }
        Debug.Log($"[GameManager] Yetersiz coin! Gereken: {amount}, Mevcut: {coin}");
        return false;
    }
    
    /// <summary>
    /// Coin'i başlangıç değerine sıfırla (yeni oyun için)
    /// </summary>
    public void ResetCoin()
    {
        coin = startingCoin;
        SaveCoin();
        UpdateCoinUI();
        Debug.Log($"[GameManager] Coin sıfırlandı: {coin}");
    }
    
    #endregion
    #endregion

}
