using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public enum PuzzleType { None, GuitarRiff, Rhythm, Memory }
    
    [Header("Settings")]
    [SerializeField] private bool isBoss = false;
    public bool IsBoss => isBoss; // Public erişim için property
    [SerializeField] private float maxHealth = 9f; // 3 yumrukta ölsün (hasar 3 x 3 = 9)
    [SerializeField] private GameObject deathEffect;
    public bool isDamagableObject = false;
    
    [Header("Checkpoint System")]
    [Tooltip("Benzersiz ID - boş bırakılırsa otomatik oluşturulur")]
    [SerializeField] private string uniqueId = "";
    [SerializeField] private bool rememberDeath = true; // Checkpoint sonrası hatırlansın mı?

    [Header("Puzzle Settings (Devre Dışı - Kaldırıldı)")]
    [SerializeField] public bool hasPuzzle = false; // Puzzle kaldırıldı - her zaman false
    [SerializeField] public PuzzleType puzzleType = PuzzleType.None;
    [SerializeField] public int puzzleDifficulty = 1; // 1-3
    [SerializeField] public int puzzleRewardCoins = 40; // Dengeli ekonomi - puzzle ödülü artırıldı

    [Header("Health Bar")]
    [SerializeField] private bool showHealthBar = true;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    public ParticleSystem particleSystem;

    private float currentHealth;
    private bool isDead = false;
    public bool IsDead => isDead; // Public property for external access
    public float CurrentHealthPercent => maxHealth > 0 ? currentHealth / maxHealth : 0f; // Boss rage mode için
    private bool puzzleStarted = false;
    private Color defaultColor;
    private SimpleEnemyHealthBar healthBar; // Yeni basit can barı
    
    // Boss Effects referansı
    private BossEffects bossEffects;

    [Header("Damage Text")]
    [SerializeField] private Vector3 damageTextOffset = new Vector3(0f, 1.2f, 0f);

    [Header("World Space Canvas")]
    [SerializeField] private Canvas enemyCanvas;
    
    /// <summary>
    /// Düşmanın benzersiz ID'si
    /// </summary>
    public string UniqueId => uniqueId;

    private void Awake()
    {
        // Benzersiz ID oluştur (boşsa)
        if (string.IsNullOrEmpty(uniqueId))
        {
            // Pozisyon ve isim bazlı benzersiz ID
            uniqueId = $"{gameObject.name}_{transform.position.x:F1}_{transform.position.y:F1}";
        }
        
        // Checkpoint'te öldürülmüş mü kontrol et
        if (rememberDeath && !isBoss && !isDamagableObject && CheckpointData.IsEnemyKilled(uniqueId))
        {
            Debug.Log($"[EnemyHealth] {uniqueId} daha önce öldürülmüş - devre dışı bırakılıyor");
            gameObject.SetActive(false);
            return;
        }
    }

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null) defaultColor = spriteRenderer.color;
        
        // Boss değilse canı mutlaka 9 yap (3 yumrukta ölsün)
        if (!isBoss)
        {
            maxHealth = 9f; // Her zaman 9 yap (Inspector override'ı önle)
        }
        else
        {
            maxHealth = 65f; // Boss final savaşı - 10 fireball (50) + 1 ultimate (16) = 66
            
            // Boss Effects bileşenini bul veya ekle
            bossEffects = GetComponent<BossEffects>();
            if (bossEffects == null)
            {
                bossEffects = gameObject.AddComponent<BossEffects>();
                Debug.Log("[EnemyHealth] BossEffects otomatik eklendi");
            }
        }
        
        currentHealth = maxHealth;
        
        Debug.Log($"[EnemyHealth] {gameObject.name} başlatıldı - MaxHealth: {maxHealth}, CurrentHealth: {currentHealth}, ID: {uniqueId}");
        
        // Can barı oluştur
        if (showHealthBar)
        {
            CreateHealthBar();
        }
    }
    
    private void CreateHealthBar()
    {
        GameObject healthBarObj = new GameObject($"{gameObject.name}_HealthBar");
        healthBar = healthBarObj.AddComponent<SimpleEnemyHealthBar>(); // Yeni basit versiyon
        healthBar.Initialize(transform, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        // Kritik vuruş kontrolü
        bool isCritical = false;
        float finalDamage = amount;
        
        if (CriticalHitSystem.Instance != null)
        {
            finalDamage = CriticalHitSystem.Instance.CalculateDamage(amount, out isCritical);
        }
        
        currentHealth -= finalDamage;
        ShowDamageText(finalDamage, isCritical);
        
        // Combo sistemi
        if (ComboSystem.Instance != null)
        {
            ComboSystem.Instance.RegisterHit();
        }
        
        // Vuruş efekti - kritikse daha güçlü
        if (isCritical)
        {
            PlayCriticalHitEffect();
        }
        else
        {
            PlayHitEffect();
        }
        
        // Boss hasar efekti
        if (isBoss && bossEffects != null)
        {
            bossEffects.OnBossDamaged(finalDamage);
        }
        
        Debug.Log($"{gameObject.name} hasar aldı: {finalDamage}{(isCritical ? " (CRITICAL!)" : "")}, Kalan can: {currentHealth}");
        
        // Can barını güncelle
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
            Debug.Log($"{gameObject.name} can barı güncellendi");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} can barı bulunamadı!");
        }

        if (animator != null) animator.SetTrigger("Hurt");

        if (spriteRenderer != null)
        {
            StopCoroutine(nameof(FlashEffect));
            StartCoroutine(nameof(FlashEffect));
        }

        if (!isBoss && GameManager.Instance != null && !isDamagableObject)
        {
            GameManager.Instance.PlayEnemyHitSound();
        }

        // Can 0'a düştüğünde direkt öl/kırıl (puzzle kaldırıldı)
        if (currentHealth <= 0)
        {
            // Puzzle kaldırıldı - sandıklar direkt kırılsın
            // if (isDamagableObject && hasPuzzle && puzzleType != PuzzleType.None)
            // {
            //     StartPuzzleBeforeBreak();
            // }
            // else
            // {
            //     Die();
            // }
            Die(); // Direkt kır/öldür
        }
    }

    /// <summary>
    /// Puzzle'ı başlat, kırılma puzzle sonucuna bağlı
    /// </summary>
    private void StartPuzzleBeforeBreak()
    {
        if (puzzleStarted) return;
        puzzleStarted = true;
        
        switch (puzzleType)
        {
            case PuzzleType.GuitarRiff:
                var guitarRiffUI = FindPuzzleUI<GuitarRiffPuzzleUI>();
                if (guitarRiffUI != null)
                    guitarRiffUI.InitializeFromPrefab(puzzleDifficulty, OnPuzzleSolvedBreak, OnPuzzleFailedNoBreak);
                else
                {
                    Debug.LogWarning("[EnemyHealth] GuitarRiffPuzzleUI bulunamadı! Direkt kırılıyor.");
                    GiveDirectRewardAndDie();
                }
                break;
                
            case PuzzleType.Rhythm:
                var rhythmUI = FindPuzzleUI<RhythmPuzzleUI>();
                if (rhythmUI != null)
                    rhythmUI.InitializeFromPrefab(puzzleDifficulty, OnPuzzleSolvedBreak, OnPuzzleFailedNoBreak);
                else
                {
                    Debug.LogWarning("[EnemyHealth] RhythmPuzzleUI bulunamadı! Direkt kırılıyor.");
                    GiveDirectRewardAndDie();
                }
                break;
                
            case PuzzleType.Memory:
                var memoryUI = FindPuzzleUI<MemoryPuzzleUI>();
                if (memoryUI != null)
                    memoryUI.InitializeFromPrefab(puzzleDifficulty, OnPuzzleSolvedBreak, OnPuzzleFailedNoBreak);
                else
                {
                    Debug.LogWarning("[EnemyHealth] MemoryPuzzleUI bulunamadı! Direkt kırılıyor.");
                    GiveDirectRewardAndDie();
                }
                break;
                
            default:
                GiveDirectRewardAndDie();
                break;
        }
    }

    /// <summary>
    /// Puzzle UI yoksa direkt ödül ver ve kır
    /// </summary>
    private void GiveDirectRewardAndDie()
    {
        GameManager.Instance.coin += 15; // Dengeli ekonomi
        puzzleStarted = false;
        Die();
    }

    private T FindPuzzleUI<T>() where T : MonoBehaviour
    {
        // Önce aktif olanı ara
        T ui = FindFirstObjectByType<T>();
        if (ui != null) return ui;

        // İnaktif olanları da ara
        T[] allUIs = Resources.FindObjectsOfTypeAll<T>();
        foreach (var foundUI in allUIs)
        {
            if (foundUI != null && foundUI.gameObject.scene.isLoaded)
            {
                return foundUI;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Puzzle başarılı - chest kırılsın ve ödül verilsin
    /// </summary>
    private void OnPuzzleSolvedBreak()
    {
        GameManager.Instance.coin += puzzleRewardCoins;
        Debug.Log($"[EnemyHealth] Puzzle çözüldü! Chest kırılıyor, Ödül: {puzzleRewardCoins} coin");
        puzzleStarted = false;
        Die();
    }

    /// <summary>
    /// Puzzle başarısız - chest kırılmasın, tekrar denenebilir
    /// </summary>
    private void OnPuzzleFailedNoBreak()
    {
        Debug.Log("[EnemyHealth] Puzzle başarısız! Chest kırılmadı, tekrar dene.");
        puzzleStarted = false;
        // Canı geri yükle (tekrar vurulabilsin)
        currentHealth = 1f;
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        
        // Checkpoint sistemine ölümü kaydet (boss ve damagable object hariç)
        if (rememberDeath && !isBoss && !isDamagableObject)
        {
            CheckpointData.MarkEnemyAsKilled(uniqueId);
        }

        if (isDamagableObject) 
        {
            if (particleSystem != null) particleSystem.Play();
            if (healthBar != null) Destroy(healthBar.gameObject);
            
            // Sandık kırıldığında coin ver
            GiveDirectReward();
            animator.SetTrigger("Open");
            
            StartCoroutine(FadeAndDestroy());
            return;
        }

        GameManager.Instance.coin += 15; // Dengeli ekonomi - düşman başına 15 coin
        
        // Coin toplama efekti
        if (CoinCollectEffect.Instance != null)
        {
            CoinCollectEffect.Instance.PlayCoinEffect(transform.position);
        }
        
        // Ruh parçacığı efekti
        if (SoulParticleEffect.Instance != null)
        {
            SoulParticleEffect.Instance.SpawnSoulEffect(transform.position);
        }
        
        // Can barını yok et
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }

        if (animator != null) animator.SetTrigger("Die");

        if (isBoss)
        {
            // Boss Effects'i tetikle (varsa)
            if (bossEffects != null)
            {
                bossEffects.OnBossDeath();
            }
            
            // Boss ruh efekti (daha büyük)
            if (SoulParticleEffect.Instance != null)
            {
                SoulParticleEffect.Instance.SpawnBossSoulEffect(transform.position);
            }
            
            // Boss için epik ölüm sekansı başlat
            StartCoroutine(BossDeathSequence());
            return; // Normal ölüm akışını durdur
        }

        var rushingTrap = GetComponent<RushingTrap>();
        if (rushingTrap != null)
        {
            rushingTrap.StopAndDisable();
        }

        // Tüm collider'ları devre dışı bırak - oyuncu takılmasın
        foreach (var col in GetComponents<Collider2D>())
        {
            col.enabled = false;
        }
        foreach (var col in GetComponentsInChildren<Collider2D>())
        {
            col.enabled = false;
        }
        
        if (GetComponent<Rigidbody2D>() != null)
        {
#if UNITY_6000_0_OR_NEWER
            GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
#else
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
#endif
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        }

        if (ScoreManager.Instance != null) ScoreManager.Instance.AddEnemyKill();
        
        // Ruh sistemi - düşman öldürüldüğünde ruh ver
        if (SoulSystem.Instance != null)
        {
            int soulAmount = isBoss ? 5 : 1; // Boss 5 ruh verir
            SoulSystem.Instance.CollectSoul(soulAmount);
        }
        
        // Mini-map'e düşman ölümünü bildir
        if (MiniMap2D.Instance != null)
        {
            MiniMap2D.Instance.OnEnemyDeath(transform);
        }
        
        if (deathEffect != null) Instantiate(deathEffect, transform.position, Quaternion.identity);

        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FlashEffect()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        if (!isDead) spriteRenderer.color = defaultColor;
    }
    
    private void PlayHitEffect()
    {
        if (HitEffect.Instance == null) return;
        
        // Oyuncuyu bul ve vuruş yönünü hesapla
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector2 hitDirection = Vector2.right;
        
        if (player != null)
        {
            hitDirection = (transform.position - player.transform.position).normalized;
        }
        
        // Efekti düşmanın merkezinde oynat
        HitEffect.Instance.PlayHitEffect(transform.position, hitDirection);
    }
    
    private void PlayCriticalHitEffect()
    {
        if (HitEffect.Instance == null) return;
        
        // Oyuncuyu bul ve vuruş yönünü hesapla
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector2 hitDirection = Vector2.right;
        
        if (player != null)
        {
            hitDirection = (transform.position - player.transform.position).normalized;
        }
        
        // Ağır vuruş efekti - kritik için daha güçlü
        HitEffect.Instance.PlayHeavyHitEffect(transform.position, hitDirection);
    }

    private IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(0.15f); // Çok kısa bekleme - oyuncuyu engellemez
        if (spriteRenderer == null)
        {
            yield return new WaitForSeconds(0.1f);
            gameObject.SetActive(false);
            yield break;
        }

        float fadeDuration = 0.25f; // Hızlı kaybolma
        float scaleDuration = 0.5f;
        float elapsed = 0f;
        
        Color startColor = spriteRenderer.color;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * 0.3f;
        
        // Hafif yukarı zıplama efekti
        Vector3 startPos = transform.position;
        float jumpHeight = 0.3f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            float smoothT = t * t * (3f - 2f * t); // Smoothstep
            
            // Fade out
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, smoothT);
            spriteRenderer.color = newColor;
            
            // Scale down (sadece ilk yarısında)
            if (t < 0.6f)
            {
                float scaleT = t / 0.6f;
                float scaleSmooth = scaleT * scaleT;
                transform.localScale = Vector3.Lerp(startScale, targetScale, scaleSmooth);
            }
            
            // Hafif yukarı hareket (bounce efekti)
            float bounce = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = startPos + Vector3.up * bounce;
            
            yield return null;
        }

        if (isDamagableObject) GameManager.Instance.ReturnPlayerToSavedPosition();
        
        // Son temizlik
        spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        gameObject.SetActive(false);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Ulti")
            TakeDamage(100);
    }

    private void ShowDamageText(float damage, bool isCritical = false)
    {
        // Kritik vuruş efekti
        if (isCritical && CriticalHitSystem.Instance != null)
        {
            CriticalHitSystem.Instance.PlayCritEffect(transform.position + damageTextOffset, damage);
            return; // Kritik sistem kendi yazısını gösterir
        }
        
        if (enemyCanvas == null)
        {
            Debug.LogWarning("Enemy canvas atanmadı!");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("FloatingDamageText");
        if (prefab == null) return;

        // Canvas içinde spawn
        GameObject dmgTextObj = Instantiate(prefab, enemyCanvas.transform);

        RectTransform rect = dmgTextObj.GetComponent<RectTransform>();

        // Düşmana göre pozisyon (LOCAL)
        Vector3 localPos = enemyCanvas.transform.InverseTransformPoint(
            transform.position + damageTextOffset
        );

        rect.localPosition = localPos;
        rect.localRotation = Quaternion.identity;

        FloatingDamageText dmgText = dmgTextObj.GetComponent<FloatingDamageText>();
        if (dmgText != null)
            dmgText.Initialize(damage);
    }

    IEnumerator ReturnToSavedPosition()
    {
        yield return new WaitForSeconds(1f);
        GameManager.Instance.ReturnPlayerToSavedPosition();
    }

    #region Puzzle System
    
    private void GiveDirectReward()
    {
        GameManager.Instance.coin += 15; // Dengeli ekonomi
        Debug.Log($"[EnemyHealth] Direkt ödül verildi: 15 coin");
    }
    
    #endregion
    
    #region Boss Death Sequence
    
    /// <summary>
    /// Boss için epik ölüm animasyonu - titreme, patlama ve zafer ekranı
    /// </summary>
    private IEnumerator BossDeathSequence()
    {
        Debug.Log("[EnemyHealth] Boss ölüm sekansı başladı!");
        
        // Oyuncuyu durdur
        if (PlayerMovement.Instance != null)
            PlayerMovement.Instance.lockMovement = true;
        
        // Can barını gizle
        if (healthBar != null)
            Destroy(healthBar.gameObject);
        
        // Tüm collider'ları devre dışı bırak
        foreach (var col in GetComponents<Collider2D>())
            col.enabled = false;
        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;
        
        // FAZE 1: Titreme başlasın (2 saniye, giderek artan)
        float shakePhase = 2f;
        float elapsed = 0f;
        Vector3 originalPos = transform.position;
        
        while (elapsed < shakePhase)
        {
            elapsed += Time.deltaTime;
            float intensity = Mathf.Lerp(0.05f, 0.3f, elapsed / shakePhase);
            transform.position = originalPos + (Vector3)Random.insideUnitCircle * intensity;
            
            // Renk titremesi - kırmızı-beyaz arası
            if (spriteRenderer != null)
            {
                float flash = Mathf.PingPong(Time.time * 15f, 1f);
                spriteRenderer.color = Color.Lerp(Color.red, Color.white, flash);
            }
            
            yield return null;
        }
        
        transform.position = originalPos;
        
        // FAZE 2: Büyük ekran sarsıntısı - manuel titreme
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            Vector3 camOriginalPos = mainCam.transform.position;
            float shakeDuration = 0.5f;
            float shakeElapsed = 0f;
            
            while (shakeElapsed < shakeDuration)
            {
                shakeElapsed += Time.deltaTime;
                float intensity = Mathf.Lerp(0.4f, 0f, shakeElapsed / shakeDuration);
                mainCam.transform.position = camOriginalPos + (Vector3)Random.insideUnitCircle * intensity;
                yield return null;
            }
            mainCam.transform.position = camOriginalPos;
        }
        
        // FAZE 3: Beyaz flaş
        yield return StartCoroutine(ScreenFlash());
        
        // Boss ses efekti
        if (GameManager.Instance != null)
            GameManager.Instance.PlayBossDeathSound();
        
        // Patlama efekti
        if (deathEffect != null)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 randomOffset = (Vector3)Random.insideUnitCircle * 2f;
                Instantiate(deathEffect, transform.position + randomOffset, Quaternion.identity);
                yield return new WaitForSeconds(0.1f);
            }
        }
        
        // Boss'u yok et
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        
        yield return new WaitForSeconds(0.5f);
        
        // FAZE 4: Victory ekranı göster
        ShowVictoryScreen();
        
        // Biraz bekle ve objeyi yok et
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
    
    private IEnumerator ScreenFlash()
    {
        // Beyaz flaş için geçici canvas oluştur
        GameObject flashCanvas = new GameObject("FlashCanvas");
        Canvas canvas = flashCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        
        GameObject flashPanel = new GameObject("FlashPanel");
        flashPanel.transform.SetParent(flashCanvas.transform, false);
        UnityEngine.UI.Image flashImg = flashPanel.AddComponent<UnityEngine.UI.Image>();
        flashImg.color = Color.white;
        
        RectTransform rect = flashPanel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Fade out
        float duration = 0.5f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            flashImg.color = new Color(1f, 1f, 1f, 1f - (elapsed / duration));
            yield return null;
        }
        
        Destroy(flashCanvas);
    }
    
    private void ShowVictoryScreen()
    {
        // Victory UI oluştur
        GameObject victoryCanvas = new GameObject("VictoryCanvas");
        Canvas canvas = victoryCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        
        UnityEngine.UI.CanvasScaler scaler = victoryCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Arka plan - yarı saydam siyah
        GameObject bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(victoryCanvas.transform, false);
        UnityEngine.UI.Image bgImg = bgPanel.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.7f);
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Ana container
        GameObject container = new GameObject("Container");
        container.transform.SetParent(victoryCanvas.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.sizeDelta = new Vector2(1200, 500); // Daha geniş container
        
        // "VICTORY" yazısı - DAHA KALIN VE ETKİLEYİCİ
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(container.transform, false);
        TMPro.TextMeshProUGUI titleText = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
        titleText.text = "VICTORY";
        titleText.fontSize = 130; // Biraz küçülttük - tek satıra sığsın
        titleText.fontStyle = TMPro.FontStyles.Bold;
        titleText.color = new Color(1f, 0.85f, 0.2f); // Altın sarısı
        titleText.alignment = TMPro.TextAlignmentOptions.Center;
        titleText.enableAutoSizing = false;
        titleText.characterSpacing = 10f; // Harfler arası boşluk azaltıldı
        titleText.enableWordWrapping = false; // Kelime kaydırma KAPALI - tek satırda kalsın
        titleText.overflowMode = TMPro.TextOverflowModes.Overflow; // Taşsın ama bölünmesin
        
        // Outline efekti - daha kalın görünüm
        titleText.outlineWidth = 0.25f;
        titleText.outlineColor = new Color(0.4f, 0.2f, 0f, 1f); // Koyu altın outline
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.5f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
        
        // Alt yazı
        GameObject subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(container.transform, false);
        TMPro.TextMeshProUGUI subText = subObj.AddComponent<TMPro.TextMeshProUGUI>();
        subText.text = "You defeated the Boss!";
        subText.fontSize = 36;
        subText.color = Color.white;
        subText.alignment = TMPro.TextAlignmentOptions.Center;
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0, 0.25f);
        subRect.anchorMax = new Vector2(1, 0.5f);
        subRect.offsetMin = Vector2.zero;
        subRect.offsetMax = Vector2.zero;
        
        // "Press any key" yazısı
        GameObject pressObj = new GameObject("PressKey");
        pressObj.transform.SetParent(container.transform, false);
        TMPro.TextMeshProUGUI pressText = pressObj.AddComponent<TMPro.TextMeshProUGUI>();
        pressText.text = "Press any key to continue...";
        pressText.fontSize = 24;
        pressText.color = new Color(1f, 1f, 1f, 0.6f);
        pressText.alignment = TMPro.TextAlignmentOptions.Center;
        RectTransform pressRect = pressObj.GetComponent<RectTransform>();
        pressRect.anchorMin = new Vector2(0, 0f);
        pressRect.anchorMax = new Vector2(1, 0.25f);
        pressRect.offsetMin = Vector2.zero;
        pressRect.offsetMax = Vector2.zero;
        
        // Victory controller ekle
        victoryCanvas.AddComponent<VictoryScreenController>();
        
        // Animasyon başlat
        StartCoroutine(AnimateVictoryScreen(container, titleText));
    }
    
    private IEnumerator AnimateVictoryScreen(GameObject container, TMPro.TextMeshProUGUI titleText)
    {
        // Başlangıç - küçük ve saydam
        container.transform.localScale = Vector3.zero;
        
        // Büyüme animasyonu
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float easeOut = 1f - Mathf.Pow(1f - t, 3f); // Cubic ease out
            container.transform.localScale = Vector3.one * easeOut * 1.1f;
            yield return null;
        }
        
        // Hafif bounce geri
        elapsed = 0f;
        duration = 0.2f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            container.transform.localScale = Vector3.Lerp(Vector3.one * 1.1f, Vector3.one, t);
            yield return null;
        }
        
        container.transform.localScale = Vector3.one;
        
        // Title glow pulse
        while (container != null)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * 2f) * 0.2f + 0.8f;
            if (titleText != null)
            {
                titleText.color = new Color(1f, 0.85f * pulse + 0.15f, 0.2f * pulse);
            }
            yield return null;
        }
    }
    
    #endregion
}

/// <summary>
/// Victory ekranı controller - tuşa basınca devam et
/// </summary>
public class VictoryScreenController : MonoBehaviour
{
    private bool canContinue = false;
    
    private void Start()
    {
        Time.timeScale = 0f; // Oyunu durdur
        StartCoroutine(EnableContinue());
    }
    
    private IEnumerator EnableContinue()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        canContinue = true;
    }
    
    private void Update()
    {
        if (canContinue && Input.anyKeyDown)
        {
            Time.timeScale = 1f;
            
            // Sonraki sahneye geç veya main menu'ye dön
            if (GameManager.Instance != null)
            {
                // Level tamamlandı - coin kaydet ve devam et
                GameManager.Instance.SaveCoin();
            }
            
            // Main menu'ye dön veya sonraki level
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}