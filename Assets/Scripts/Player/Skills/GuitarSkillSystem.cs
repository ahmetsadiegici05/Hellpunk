using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Ana gitar skill sistemi. Q, E, F, R tuşlarıyla aktifleşir ve ok yönü kombinasyonu ister.
/// E: Shockwave (düşmanları iter)
/// F: Heal (iyileştirme)
/// Q: Fireball (ateş topu)
/// R: Ultimate (alan hasarı)
/// </summary>
public class GuitarSkillSystem : MonoBehaviour
{
    public static GuitarSkillSystem Instance { get; private set; }

    [Header("Skill Settings")]
    [SerializeField] private float healAmount = 1f;
    [SerializeField] private float fireballDamage = 25f;
    [SerializeField] private float ultimateDamage = 50f;
    [SerializeField] private float ultimateRadius = 5f;
    [SerializeField] private float shockwaveRadius = 4f;
    [SerializeField] private float shockwaveForce = 15f;

    [Header("Ability Charges (Sayı Bazlı)")]
    [SerializeField] public int healCharges = 2;      // Başlangıçta 2 heal hakkı
    [SerializeField] public int fireballCharges = 2;  // Başlangıçta 2 fireball hakkı
    
    [Header("Cooldowns (Sadece Time Slow)")]
    [SerializeField] private float timeSlowCooldown = 15f;
    
    [Header("Input Timeout")]
    [SerializeField] private float inputTimeout = 3f;
    [SerializeField] private float inputWindowPerArrow = 1f;

    [Header("Enemy Detection")]
    [SerializeField] private LayerMask enemyLayer;

    [Header("Skill Prefabs")]
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject timeSlowEffectPrefab;
    [SerializeField] private GameObject healEffectPrefab;
    [SerializeField] private GameObject shockwaveEffectPrefab;

    [Header("Audio")]
    [SerializeField] private AudioClip skillActivateSound;
    [SerializeField] private AudioClip skillSuccessSound;
    [SerializeField] private AudioClip skillFailSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip fireballSound;
    [SerializeField] private AudioClip timeSlowSound;
    [SerializeField] private AudioClip shockwaveSound;

    [Header("Visual Effects")]
    [SerializeField] private Color healTintColor = new Color(0.3f, 0.6f, 1f, 1f); // Mavi
    [SerializeField] private Color shockwaveTintColor = new Color(1f, 0.8f, 0.2f, 1f); // Sarı
    [SerializeField] private float effectDuration = 0.8f;
    [SerializeField] private float shakeIntensity = 0.15f;
    [SerializeField] private float shakeDuration = 0.4f;

    [Header("Skill Input Mode")]
    [SerializeField] private Color skillInputOverlayColor = new Color(0.1f, 0.2f, 0.5f, 0.6f); // Koyu mavi
    [SerializeField] private float skillInputDuration = 3f; // Input için verilen süre (3 saniye)

    // Cooldown timers (sadece Time Slow için)
    private float timeSlowCooldownTimer = 0f;
    
    // TimeSlow state
    private bool isTimeSlowActive = false;

    // Skill state
    private bool isInSkillInput = false;
    private SkillType currentSkillType = SkillType.None;
    private List<ArrowDirection> requiredSequence = new List<ArrowDirection>();
    private List<ArrowDirection> playerInputSequence = new List<ArrowDirection>();
    private int currentInputIndex = 0;
    private float inputTimer = 0f;

    // Components
    private Health playerHealth;
    private Animator anim;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    // Skill Input Mode - Oyun duraksatma
    private GameObject skillInputOverlay;
    private float savedTimeScale = 1f;
    
    // Skill aktivasyon koruması - çakismayı önle
    private float skillActivationCooldown = 0f;
    private const float SKILL_ACTIVATION_DELAY = 0.5f; // Skill bittikten sonra bekleme süresi

    // Events for UI
    public System.Action<SkillType, List<ArrowDirection>> OnSkillActivated;
    public System.Action<int, bool> OnInputReceived;
    public System.Action<bool> OnSkillComplete;
    public System.Action OnSkillCancelled;
    public System.Action<SkillType> OnAbilityFailedNoSoul; // Yeterli ruh olmadığında tetiklenir

    // Properties
    public bool IsInSkillInput => isInSkillInput;
    public SkillType CurrentSkill => currentSkillType;
    public List<ArrowDirection> RequiredSequence => requiredSequence;
    public int CurrentInputIndex => currentInputIndex;

    // Sayı bazlı ability'ler için
    public int HealCharges => healCharges;
    public int FireballCharges => fireballCharges;
    
    // Time Slow cooldown
    public float TimeSlowCooldownProgress => timeSlowCooldownTimer <= 0 ? 1f : 1f - (timeSlowCooldownTimer / timeSlowCooldown);
    public bool IsTimeSlowReady => timeSlowCooldownTimer <= 0f;
    
    // Sayı bazlı kontroller
    public bool IsHealReady => healCharges > 0;
    public bool IsFireballReady => fireballCharges > 0;
    
    // Ultimate için SoulSystem kontrolü
    public bool IsUltimateReady => SoulSystem.Instance != null && SoulSystem.Instance.IsUltimateReady;

    public enum SkillType
    {
        None,
        Heal,       // F tuşu - 3 input - Mavi efekt
        Fireball,   // Q tuşu - 3 input
        TimeSlow,   // R tuşu - 3 input - Zaman yavaşlatma
        Shockwave,  // Kullanılmıyor şu an
        Ultimate    // U tuşu - 3 input - Ultimate skill
    }

    public enum ArrowDirection
    {
        Up,
        Down,
        Left,
        Right
    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);

        playerHealth = GetComponent<Health>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // Skill input overlay oluştur (başlangıçta gizli)
        CreateSkillInputOverlay();
    }

    private void CreateSkillInputOverlay()
    {
        // Full screen overlay için canvas oluştur
        GameObject overlayCanvas = new GameObject("SkillInputOverlayCanvas");
        Canvas canvas = overlayCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // En üstte olsun
        overlayCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();

        // Overlay image
        skillInputOverlay = new GameObject("SkillInputOverlay");
        skillInputOverlay.transform.SetParent(overlayCanvas.transform, false);
        
        UnityEngine.UI.Image overlayImage = skillInputOverlay.AddComponent<UnityEngine.UI.Image>();
        overlayImage.color = Color.clear; // Mavi ekranı kaldır
        
        // Full screen yap
        RectTransform rt = skillInputOverlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Başlangıçta gizle
        skillInputOverlay.SetActive(false);
        
        // Canvas'ı player'a bağlama, sahnede kalsın
        DontDestroyOnLoad(overlayCanvas);
    }

    private void Update()
    {
        // Puzzle aktifken hiçbir skill input kabul etme
        if (GameManager.IsPuzzleActive)
            return;
            
        UpdateCooldowns();

        // Skill aktivasyon cooldown'u güncelle (unscaled çünkü oyun duraklayabilir)
        if (skillActivationCooldown > 0)
            skillActivationCooldown -= Time.unscaledDeltaTime;

        if (isInSkillInput)
        {
            HandleSkillInput();
            return;
        }

        // Skill aktivasyon koruması - çok hızlı ardı ardına skill önleme
        if (skillActivationCooldown > 0)
            return;

        // Skill aktivasyonu
        // F: Heal (sayı bazlı)
        if (Input.GetKeyDown(KeyCode.F) && IsHealReady)
        {
            ActivateSkill(SkillType.Heal, 3);
        }
        // Q: Fireball (sayı bazlı) - Fireball modu aktif değilse
        else if (Input.GetKeyDown(KeyCode.Q) && IsFireballReady && !IsFireballModeActive())
        {
            ActivateSkill(SkillType.Fireball, 3);
        }
        // R: Time Slow (cooldown bazlı)
        else if (Input.GetKeyDown(KeyCode.R) && IsTimeSlowReady)
        {
            ActivateSkill(SkillType.TimeSlow, 3);
        }
        // U: Ultimate (7 kill gerekli)
        else if (Input.GetKeyDown(KeyCode.U) && IsUltimateReady)
        {
            ActivateSkill(SkillType.Ultimate, 3);
        }
        // Yeterli hak/kill yoksa feedback ver
        else if (Input.GetKeyDown(KeyCode.F) && !IsHealReady)
        {
            TriggerNoChargeFeedback(SkillType.Heal, healCharges);
        }
        else if (Input.GetKeyDown(KeyCode.Q) && !IsFireballReady && !IsFireballModeActive())
        {
            TriggerNoChargeFeedback(SkillType.Fireball, fireballCharges);
        }
        else if (Input.GetKeyDown(KeyCode.U) && !IsUltimateReady)
        {
            TriggerNoUltimateFeedback();
        }
    }
    
    /// <summary>
    /// PlayerAttack'tan fireball modunun aktif olup olmadığını kontrol et
    /// </summary>
    private bool IsFireballModeActive()
    {
        PlayerAttack playerAttack = GetComponent<PlayerAttack>();
        return playerAttack != null && playerAttack.IsFireballModeActive;
    }
    
    /// <summary>
    /// Yeterli charge olmadığında feedback (Heal/Fireball için)
    /// </summary>
    private void TriggerNoChargeFeedback(SkillType skillType, int remaining)
    {
        Debug.Log($"{skillType} hakkı kalmadı! Kalan: {remaining}");
        OnAbilityFailedNoSoul?.Invoke(skillType);
        
        if (audioSource != null && skillFailSound != null)
            audioSource.PlayOneShot(skillFailSound, 0.5f);
    }
    
    /// <summary>
    /// Ultimate için yeterli kill olmadığında feedback
    /// </summary>
    private void TriggerNoUltimateFeedback()
    {
        int currentKills = SoulSystem.Instance != null ? SoulSystem.Instance.CurrentKills : 0;
        int required = SoulSystem.Instance != null ? SoulSystem.Instance.KillsRequired : 7;
        Debug.Log($"Ultimate için {required - currentKills} kill daha gerekli! ({currentKills}/{required})");
        OnAbilityFailedNoSoul?.Invoke(SkillType.Ultimate);
        
        if (audioSource != null && skillFailSound != null)
            audioSource.PlayOneShot(skillFailSound, 0.5f);
    }

    private void UpdateCooldowns()
    {
        // TimeSlow cooldown'u sadece TimeSlow aktif DEĞİLKEN say
        bool isTimeSlowActive = TimeSlowAbility.Instance != null && TimeSlowAbility.Instance.IsSlowMotionActive;
        if (timeSlowCooldownTimer > 0 && !isTimeSlowActive)
            timeSlowCooldownTimer -= Time.unscaledDeltaTime;
    }

    private void ActivateSkill(SkillType skillType, int inputCount)
    {
        currentSkillType = skillType;
        isInSkillInput = true;
        currentInputIndex = 0;
        inputTimer = skillInputDuration;

        // Rastgele ok yönleri oluştur
        requiredSequence.Clear();
        playerInputSequence.Clear();

        Debug.Log($"=== SKILL ACTIVATED ===");
        Debug.Log($"Skill: {skillType}");
        Debug.Log($"Input Count: {inputCount}");

        for (int i = 0; i < inputCount; i++)
        {
            ArrowDirection randomDir = (ArrowDirection)Random.Range(0, 4);
            requiredSequence.Add(randomDir);
            Debug.Log($"  Sequence[{i}] = {randomDir}");
        }
        
        Debug.Log($"Full Sequence: {string.Join(" -> ", requiredSequence)}");
        Debug.Log($"========================");

        // OYUNU DURAKLAT - Mavi ekran göster
        EnterSkillInputMode();

        // Ses çal
        if (skillActivateSound != null && audioSource != null)
            audioSource.PlayOneShot(skillActivateSound);

        // UI'a bildir
        OnSkillActivated?.Invoke(skillType, requiredSequence);
    }

    private void EnterSkillInputMode()
    {
        // TimeScale'i kaydet ve duraklat
        savedTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // Mavi overlay'i göster
        if (skillInputOverlay != null)
            skillInputOverlay.SetActive(true);
    }

    private void ExitSkillInputMode()
    {
        // TimeScale'i eski haline getir
        Time.timeScale = savedTimeScale;

        // Overlay'i gizle
        if (skillInputOverlay != null)
            skillInputOverlay.SetActive(false);
    }

    private void HandleSkillInput()
    {
        // Timeout kontrolü - unscaledDeltaTime kullan çünkü oyun duraklamış
        inputTimer -= Time.unscaledDeltaTime;
        if (inputTimer <= 0)
        {
            Debug.Log("TIMEOUT! Süre doldu.");
            FailSkill();
            return;
        }

        // Escape ile iptal - önce kontrol et
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("ESC ile iptal edildi.");
            CancelSkill();
            return;
        }

        // Ok tuşlarını ayrı ayrı kontrol et - her biri için ayrı değişken
        bool upPressed = Input.GetKeyDown(KeyCode.UpArrow);
        bool downPressed = Input.GetKeyDown(KeyCode.DownArrow);
        bool leftPressed = Input.GetKeyDown(KeyCode.LeftArrow);
        bool rightPressed = Input.GetKeyDown(KeyCode.RightArrow);

        // Kaç tuş basıldı say
        int pressCount = (upPressed ? 1 : 0) + (downPressed ? 1 : 0) + (leftPressed ? 1 : 0) + (rightPressed ? 1 : 0);

        // Birden fazla tuş basıldıysa hiçbirini kabul etme (karışıklık önleme)
        if (pressCount > 1)
        {
            Debug.Log("Birden fazla tuşa aynı anda basıldı, girdi yoksayıldı");
            return;
        }

        // Sadece tek bir tuş basıldıysa işle
        if (pressCount == 1)
        {
            ArrowDirection pressedDirection;
            
            if (upPressed)
                pressedDirection = ArrowDirection.Up;
            else if (downPressed)
                pressedDirection = ArrowDirection.Down;
            else if (leftPressed)
                pressedDirection = ArrowDirection.Left;
            else
                pressedDirection = ArrowDirection.Right;

            // Beklenen yön ile karşılaştır
            ArrowDirection expectedDirection = requiredSequence[currentInputIndex];
            bool isCorrect = pressedDirection == expectedDirection;
            
            Debug.Log($"Input: {pressedDirection}, Beklenen: {expectedDirection}, Doğru: {isCorrect}");
            
            playerInputSequence.Add(pressedDirection);

            // UI'a bildir
            OnInputReceived?.Invoke(currentInputIndex, isCorrect);

            if (isCorrect)
            {
                currentInputIndex++;

                if (currentInputIndex == 1) audioSource.PlayOneShot(GameManager.Instance.guitarSound1);
                else if (currentInputIndex == 2) audioSource.PlayOneShot(GameManager.Instance.guitarSound2);
                else if (currentInputIndex == 3) audioSource.PlayOneShot(GameManager.Instance.guitarSound3);

                // Tüm inputlar doğru girildi mi?
                if (currentInputIndex >= requiredSequence.Count)
                {
                    SuccessSkill();
                }
            }
            else
            {
                // Yanlış input - skill başarısız
                Debug.Log($"HATALI INPUT! Basılan: {pressedDirection}, Beklenen: {expectedDirection}");
                FailSkill();
            }
        }
    }

    private void SuccessSkill()
    {
        isInSkillInput = false;

        // Oyunu devam ettir
        ExitSkillInputMode();
        
        // Aktivasyon cooldown'u başlat - hemen yeni skill önleme
        skillActivationCooldown = SKILL_ACTIVATION_DELAY;

        // Skill tipine göre harcama yap
        ConsumeSkillCharge(currentSkillType);

        // Ses çal
        if (skillSuccessSound != null && audioSource != null)
            audioSource.PlayOneShot(skillSuccessSound);

        // Skill'i uygula
        ExecuteSkill(currentSkillType);

        // UI'a bildir
        OnSkillComplete?.Invoke(true);

        currentSkillType = SkillType.None;
    }
    
    /// <summary>
    /// Skill tipine göre doğru harcama yap
    /// </summary>
    private void ConsumeSkillCharge(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Heal:
                healCharges--;
                Debug.Log($"Heal kullanıldı! Kalan: {healCharges}");
                break;
            case SkillType.Fireball:
                fireballCharges--;
                Debug.Log($"Fireball kullanıldı! Kalan: {fireballCharges}");
                break;
            case SkillType.TimeSlow:
                timeSlowCooldownTimer = timeSlowCooldown;
                Debug.Log($"Time Slow kullanıldı! Cooldown: {timeSlowCooldown}sn");
                break;
            case SkillType.Ultimate:
                if (SoulSystem.Instance != null)
                    SoulSystem.Instance.UseCharge();
                break;
        }
    }

    private void FailSkill()
    {
        isInSkillInput = false;

        // Oyunu devam ettir
        ExitSkillInputMode();
        
        // Aktivasyon cooldown'u başlat - hemen yeni skill önleme
        skillActivationCooldown = SKILL_ACTIVATION_DELAY;

        // Ses çal
        if (skillFailSound != null && audioSource != null)
            audioSource.PlayOneShot(skillFailSound);

        // UI'a bildir
        OnSkillComplete?.Invoke(false);

        Debug.Log($"Skill failed: {currentSkillType}");
        currentSkillType = SkillType.None;
    }

    private void CancelSkill()
    {
        isInSkillInput = false;

        // Oyunu devam ettir
        ExitSkillInputMode();
        
        // Aktivasyon cooldown'u başlat - hemen yeni skill önleme
        skillActivationCooldown = SKILL_ACTIVATION_DELAY;

        OnSkillCancelled?.Invoke();
        currentSkillType = SkillType.None;
    }

    private void ExecuteSkill(SkillType skillType)
    {
        switch (skillType)
        {
            case SkillType.Heal:
                ExecuteHeal();
                break;
            case SkillType.Fireball:
                ExecuteFireball();
                break;
            case SkillType.TimeSlow:
                ExecuteTimeSlow();
                break;
            case SkillType.Shockwave:
                ExecuteShockwave();
                break;
            // Ultimate durumunu buraya ekle
            case SkillType.Ultimate:
                UseUlti();
                break;
        }
    }

    private void ExecuteHeal()
    {
        if (playerHealth != null)
        {
            playerHealth.AddHealth(healAmount);
        }

        // Mavi renk efekti
        StartCoroutine(ColorTintEffect(healTintColor));

        // Efekt
        if (healEffectPrefab != null)
        {
            Instantiate(healEffectPrefab, transform.position, Quaternion.identity, transform);
        }

        // Ses
        if (healSound != null && audioSource != null)
            audioSource.PlayOneShot(healSound);
        else if (GameManager.Instance != null)
            GameManager.Instance.PlayHealSound();

        Debug.Log($"Heal executed! Kalan: {healCharges}");
    }

    private void ExecuteShockwave()
    {
        StartCoroutine(ShockwaveSequence());
    }

    private IEnumerator ShockwaveSequence()
    {
        // Titreme efekti başlat
        StartCoroutine(ShakeEffect());
        
        // Sarı renk efekti
        StartCoroutine(ColorTintEffect(shockwaveTintColor));

        // Kısa bekleme (build-up)
        yield return new WaitForSeconds(0.2f);

        // Shockwave efekti oluştur
        if (shockwaveEffectPrefab != null)
        {
            Instantiate(shockwaveEffectPrefab, transform.position, Quaternion.identity);
        }

        // Çevredeki düşmanları bul ve it
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, shockwaveRadius, enemyLayer);
        foreach (var enemy in hitEnemies)
        {
            // Knockback yönünü hesapla
            Vector2 knockbackDir = (enemy.transform.position - transform.position).normalized;
            
            // Rigidbody varsa fiziksel olarak it
            Rigidbody2D enemyRb = enemy.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                enemyRb.AddForce(knockbackDir * shockwaveForce, ForceMode2D.Impulse);
            }
            else
            {
                // Rigidbody yoksa transform ile it
                StartCoroutine(PushEnemy(enemy.transform, knockbackDir, shockwaveForce * 0.3f));
            }

            // Hafif hasar ver (opsiyonel)
            EnemyHealth enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(5f);
            }
        }

        // Ses
        if (shockwaveSound != null && audioSource != null)
            audioSource.PlayOneShot(shockwaveSound);

        Debug.Log($"Shockwave executed! Hit {hitEnemies.Length} enemies");
    }

    private IEnumerator PushEnemy(Transform enemy, Vector2 direction, float distance)
    {
        if (enemy == null) yield break;

        Vector3 startPos = enemy.position;
        Vector3 endPos = startPos + (Vector3)(direction * distance);
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration && enemy != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); // Ease out
            enemy.position = Vector3.Lerp(startPos, endPos, smoothT);
            yield return null;
        }
    }

    private IEnumerator ShakeEffect()
    {
        if (spriteRenderer == null) yield break;

        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            
            float intensity = shakeIntensity * (1f - elapsed / shakeDuration);
            float offsetX = Random.Range(-intensity, intensity);
            float offsetY = Random.Range(-intensity, intensity);
            
            transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0);
            yield return null;
        }

        transform.localPosition = originalPos;
    }

    private IEnumerator ColorTintEffect(Color tintColor)
    {
        if (spriteRenderer == null) yield break;

        float elapsed = 0f;
        float halfDuration = effectDuration / 2f;

        // Fade to tint color
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            spriteRenderer.color = Color.Lerp(originalColor, tintColor, t);
            yield return null;
        }

        spriteRenderer.color = tintColor;
        elapsed = 0f;

        // Fade back to original
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            spriteRenderer.color = Color.Lerp(tintColor, originalColor, t);
            yield return null;
        }

        spriteRenderer.color = originalColor;
    }

    private void ExecuteFireball()
    {
        // PlayerAttack'ın fireball modunu aktif et (15 saniye boyunca fireball atabilir)
        PlayerAttack playerAttack = GetComponent<PlayerAttack>();
        if (playerAttack != null)
        {
            playerAttack.ActivateFireballMode();
            Debug.Log("Fireball modu aktif edildi! 15 saniye boyunca sol tık ile fireball at!");
        }
        else
        {
            Debug.LogError("PlayerAttack komponenti bulunamadı!");
        }

        // Ses
        if (fireballSound != null && audioSource != null)
            audioSource.PlayOneShot(fireballSound);
        else if (GameManager.Instance != null)
            GameManager.Instance.PlayAttackSound();

        Debug.Log($"Fireball executed! Kalan: {fireballCharges}");
    }

    private void ExecuteTimeSlow()
    {
        // TimeSlowAbility'yi tetikle
        if (TimeSlowAbility.Instance != null)
        {
            TimeSlowAbility.Instance.ActivateFromSkillSystem();
            Debug.Log("Time Slow aktif edildi!");
        }
        else
        {
            Debug.LogWarning("TimeSlowAbility bulunamadı!");
        }
        
        // Görsel efekt
        StartCoroutine(TimeSlowActivateEffect());
        
        // Ses
        if (timeSlowSound != null && audioSource != null)
            audioSource.PlayOneShot(timeSlowSound);
        
        // Cooldown başlat
        timeSlowCooldownTimer = timeSlowCooldown;
    }
    
    private IEnumerator TimeSlowActivateEffect()
    {
        // Efekt oluştur
        if (timeSlowEffectPrefab != null)
        {
            Instantiate(timeSlowEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Karakteri kısa süreliğine parlat (mor/mavi renk)
        if (spriteRenderer != null)
        {
            Color timeSlowColor = new Color(0.6f, 0.4f, 1f, 1f); // Mor
            yield return StartCoroutine(ColorTintEffect(timeSlowColor));
        }
    }

    public void UseUlti()
    {
        // Ultimate SoulSystem tarafından yönetiliyor, cooldown yok
        BossSlashUltimate.Instance.ActivateUltimate();
    }
    
    // ================= MARKET SİSTEMİ =================
    
    /// <summary>
    /// Market'ten Heal satın alındığında çağrılır
    /// </summary>
    public void AddHealCharges(int amount)
    {
        healCharges += amount;
        Debug.Log($"Heal satın alındı! +{amount}, Toplam: {healCharges}");
    }
    
    /// <summary>
    /// Market'ten Fireball satın alındığında çağrılır
    /// </summary>
    public void AddFireballCharges(int amount)
    {
        fireballCharges += amount;
        Debug.Log($"Fireball satın alındı! +{amount}, Toplam: {fireballCharges}");
    }

    // Debug görselleştirme
    private void OnDrawGizmosSelected()
    {
        // Ultimate radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, ultimateRadius);
        
        // Shockwave radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shockwaveRadius);
    }
}
