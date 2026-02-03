using UnityEngine;
using System.Collections;

/// <summary>
/// Boss savaşı için basit efektler - SLOW MOTION YOK
/// - Her adımda ekran sarsıntısı
/// - Rage mode (can azaldığında renk değişimi)
/// - Hasar efektleri
/// </summary>
public class BossEffects : MonoBehaviour
{
    public static BossEffects Instance { get; private set; }
    
    [Header("References")]
    [SerializeField] private EnemyHealth bossHealth;
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private SpriteRenderer bossSprite;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Footstep Shake")]
    [SerializeField] private bool enableFootstepShake = true;
    [SerializeField] private float footstepShakeMagnitude = 0.05f;
    [SerializeField] private float footstepShakeDuration = 0.1f;
    [SerializeField] private AudioClip footstepSound;
    [SerializeField] private float footstepVolume = 0.5f;
    
    [Header("Rage Mode (HP < 30%)")]
    [SerializeField] private bool enableRageMode = true;
    [SerializeField] private float rageThreshold = 0.3f;
    [SerializeField] private Color rageColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float ragePulseSpeed = 3f;
    [SerializeField] private AudioClip rageActivationSound;
    
    [Header("Damage Effects")]
    [SerializeField] private float damageShakeMagnitude = 0.06f;
    [SerializeField] private AudioClip damageSound;
    
    [Header("Attack Effects")]
    [SerializeField] private float attackShakeMagnitude = 0.08f;
    [SerializeField] private float attackShakeDuration = 0.15f;
    [SerializeField] private AudioClip attackSound;
    
    [Header("Boss Intro Cinematic")]
    [SerializeField] private bool enableIntroZoom = true;
    [SerializeField] private float introZoomAmount = 5.5f; // Daha yakın zoom
    [SerializeField] private float introZoomDuration = 1.2f;
    [SerializeField] private float introZoomHoldTime = 0.8f; // Zoom'da kalma süresi
    
    [Header("Footstep Visual Effect")]
    [SerializeField] private bool enableFootstepVisualEffect = true;
    [SerializeField] private Color footstepFlashColor = new Color(0.3f, 0.25f, 0.2f, 0.4f); // Kahverengi toz efekti
    [SerializeField] private float footstepFlashDuration = 0.15f;
    
    // State
    private bool isRageMode = false;
    private bool isDead = false;
    private Color originalColor;
    private bool isMoving = false;
    private bool hasPlayedIntro = false;
    private Camera mainCamera;
    private float originalCameraSize;
    
    private void Awake()
    {
        Instance = this;
        
        if (bossHealth == null) bossHealth = GetComponent<EnemyHealth>();
        if (bossAnimator == null) bossAnimator = GetComponent<Animator>();
        if (bossSprite == null) bossSprite = GetComponent<SpriteRenderer>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }
    
    private void Start()
    {
        if (bossSprite != null)
            originalColor = bossSprite.color;
        
        mainCamera = Camera.main;
        if (mainCamera != null)
            originalCameraSize = mainCamera.orthographicSize;
        
        // Boss sahneye girdiğinde intro oyna
        if (enableIntroZoom && !hasPlayedIntro)
        {
            StartCoroutine(PlayBossIntro());
        }
    }
    
    private void Update()
    {
        if (isDead) return;
        
        // Rage mode kontrolü
        if (enableRageMode && !isRageMode && bossHealth != null)
        {
            float healthPercent = bossHealth.CurrentHealthPercent;
            if (healthPercent <= rageThreshold)
            {
                ActivateRageMode();
            }
        }
        
        // Rage mode pulse efekti
        if (isRageMode && bossSprite != null)
        {
            float pulse = Mathf.Sin(Time.time * ragePulseSpeed) * 0.5f + 0.5f;
            bossSprite.color = Color.Lerp(originalColor, rageColor, pulse);
        }
    }
    
    #region Public Methods - Animation Events
    
    /// <summary>
    /// Animator'dan çağrılır - Boss hareket ederken
    /// </summary>
    public void OnFootstep()
    {
        if (!enableFootstepShake || isDead) return;
        TriggerFootstep();
    }
    
    /// <summary>
    /// Animator'dan çağrılır - Boss saldırı anında
    /// </summary>
    public void OnAttackHit()
    {
        if (isDead) return;
        TriggerAttackEffect();
    }
    
    /// <summary>
    /// Animator'dan çağrılır - Boss hareket başladığında
    /// </summary>
    public void OnMoveStart()
    {
        isMoving = true;
    }
    
    /// <summary>
    /// Animator'dan çağrılır - Boss hareket bittiğinde
    /// </summary>
    public void OnMoveStop()
    {
        isMoving = false;
    }
    
    /// <summary>
    /// Boss hasar aldığında çağrılır
    /// </summary>
    public void OnBossDamaged(float damageAmount)
    {
        if (isDead) return;
        StartCoroutine(DamageEffect());
    }
    
    /// <summary>
    /// Boss öldüğünde çağrılır
    /// </summary>
    public void OnBossDeath()
    {
        if (isDead) return;
        isDead = true;
        StartCoroutine(DeathEffect());
    }
    
    #endregion
    
    #region Effect Methods
    
    private void TriggerFootstep()
    {
        // Ekran sarsıntısı
        if (ScreenShake.Instance != null)
        {
            ScreenShake.Instance.Shake(footstepShakeDuration, footstepShakeMagnitude);
        }
        
        // Görsel adım efekti - ekranda kısa flash
        if (enableFootstepVisualEffect)
        {
            StartCoroutine(FootstepVisualEffect());
        }
        
        // Ses
        if (footstepSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(footstepSound, footstepVolume);
        }
    }
    
    /// <summary>
    /// Boss intro sinematik - zoom in efekti
    /// </summary>
    private IEnumerator PlayBossIntro()
    {
        hasPlayedIntro = true;
        
        if (mainCamera == null || !mainCamera.orthographic) yield break;
        
        float startSize = mainCamera.orthographicSize;
        
        // Zoom in - Boss'a yaklaş
        float elapsed = 0f;
        while (elapsed < introZoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / introZoomDuration;
            // Smooth ease out
            float easeT = 1f - Mathf.Pow(1f - t, 3f);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, introZoomAmount, easeT);
            yield return null;
        }
        
        mainCamera.orthographicSize = introZoomAmount;
        
        // Dramatik bekleme
        yield return new WaitForSeconds(introZoomHoldTime);
        
        // Zoom out - normale dön
        elapsed = 0f;
        float zoomOutDuration = introZoomDuration * 0.7f; // Biraz daha hızlı geri dön
        while (elapsed < zoomOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / zoomOutDuration;
            float easeT = t * t; // Ease in
            mainCamera.orthographicSize = Mathf.Lerp(introZoomAmount, startSize, easeT);
            yield return null;
        }
        
        mainCamera.orthographicSize = startSize;
    }
    
    /// <summary>
    /// Adım sırasında ekranda görsel efekt - pat pat hissi
    /// </summary>
    private IEnumerator FootstepVisualEffect()
    {
        // Ekran kenarlarında kısa bir flash overlay oluştur
        GameObject flashOverlay = new GameObject("FootstepFlash");
        Canvas canvas = flashOverlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        UnityEngine.UI.Image flashImage = flashOverlay.AddComponent<UnityEngine.UI.Image>();
        flashImage.color = footstepFlashColor;
        
        // Vignette efekti için gradient texture simülasyonu
        // Kenarlardan merkeze doğru fade
        RectTransform rect = flashOverlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Hızlı fade out
        float elapsed = 0f;
        Color startColor = footstepFlashColor;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);
        
        while (elapsed < footstepFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / footstepFlashDuration;
            // Quick ease out
            float easeT = 1f - Mathf.Pow(1f - t, 2f);
            flashImage.color = Color.Lerp(startColor, endColor, easeT);
            yield return null;
        }
        
        Destroy(flashOverlay);
    }
    
    /// <summary>
    /// Dışarıdan boss intro'yu tetiklemek için
    /// </summary>
    public void TriggerBossIntro()
    {
        if (!hasPlayedIntro && enableIntroZoom)
        {
            StartCoroutine(PlayBossIntro());
        }
    }
    
    private void TriggerAttackEffect()
    {
        // Ekran sarsıntısı
        if (ScreenShake.Instance != null)
        {
            ScreenShake.Instance.Shake(attackShakeDuration, attackShakeMagnitude);
        }
        
        // Saldırı sesi
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
    }
    
    private IEnumerator DamageEffect()
    {
        // Ekran sarsıntısı
        if (ScreenShake.Instance != null)
        {
            ScreenShake.Instance.Shake(0.12f, damageShakeMagnitude);
        }
        
        // Hasar sesi
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        // Flash efekti
        if (bossSprite != null)
        {
            Color flashColor = Color.white;
            bossSprite.color = flashColor;
            yield return new WaitForSeconds(0.05f);
            bossSprite.color = isRageMode ? rageColor : originalColor;
        }
    }
    
    private void ActivateRageMode()
    {
        if (isRageMode) return;
        isRageMode = true;
        
        Debug.Log("[BossEffects] RAGE MODE ACTIVATED!");
        
        // Rage sesi
        if (rageActivationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(rageActivationSound);
        }
        
        // Sarsıntı
        if (ScreenShake.Instance != null)
        {
            ScreenShake.Instance.Shake(0.3f, 0.1f);
        }
    }
    
    private IEnumerator DeathEffect()
    {
        Debug.Log("[BossEffects] Boss death!");
        
        // Sarsıntı
        if (ScreenShake.Instance != null)
        {
            ScreenShake.Instance.Shake(0.4f, 0.12f);
        }
        
        // Flash efekti
        if (bossSprite != null)
        {
            for (int i = 0; i < 3; i++)
            {
                bossSprite.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                bossSprite.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    
    #endregion
    
    private void OnDestroy()
    {
        // Her zaman Time.timeScale'i düzelt
        Time.timeScale = 1f;
        if (Instance == this)
            Instance = null;
    }
}
