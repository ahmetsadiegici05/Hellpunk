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
    [SerializeField] private int startingCoin = 500; // Başlangıç coin miktarı (Inspector'dan ayarlanabilir)
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
    public List<Transform> randomPoints;
    public List<Transform> randomPoints2;

    [Header("Ulti")]
    public GameObject ultiObject;
    public Animator ultiAnimator;

    [Header("Portal Transforms")]
    public Transform chestRoomSpawnPoint;
    public Transform enemySpawnPoint;
    public Transform chestSpawnPoint;


    public float lastTransformRotationValue = 0;

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
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureSoulSystem();
        EnsureTimeSlowSystem();
        EnsureGuitarSkillSystem();

        SpawnRandomChests();
        SpawnRandomPortals();
        
        // Level geçişinden sonra oyuncu sağlığını geri yükle
        RestorePlayerHealth();
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

        if (randomPoints == null || randomPoints.Count == 0)
        {
            Debug.LogWarning("[GameManager] randomPoints boş!");
            return;
        }

        // Eski portalları temizle
        GameObject[] oldPortals = GameObject.FindGameObjectsWithTag("Portal");
        foreach (var portal in oldPortals)
            Destroy(portal);

        // Random noktaları kopyala
        List<Transform> shuffledPoints = new List<Transform>(randomPoints);

        // Fisher–Yates shuffle
        for (int i = 0; i < shuffledPoints.Count; i++)
        {
            int rnd = Random.Range(i, shuffledPoints.Count);
            Transform temp = shuffledPoints[i];
            shuffledPoints[i] = shuffledPoints[rnd];
            shuffledPoints[rnd] = temp;
        }

        int spawnCount = Mathf.Min(5, shuffledPoints.Count);
        Debug.Log($"[GameManager] {spawnCount} portal spawn edilecek");

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject portal = Instantiate(
                portalPrefab,
                shuffledPoints[i].position,
                Quaternion.identity,
                shuffledPoints[i] // parent olarak point
            );

            Debug.Log($"[GameManager] Portal {i + 1} spawnlandı: {shuffledPoints[i].position}");
        }
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
        if (ShopManager.Instance != null)
        {
            ShopManager.Instance.UpdateCoinText();
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
