using UnityEngine;
using System.Collections;

/// <summary>
/// Time Slow yeteneği için ses yönetimi.
/// SoundManager olmayan projelerde kullanılmak üzere tasarlandı.
/// 
/// Ses akışı:
/// - Start: slowdown_start ses efekti + loop fade-in + BGM pitch düşür
/// - Stop: slowdown_end ses efekti + loop fade-out + BGM pitch normale
/// 
/// Tüm fade/pitch geçişleri unscaledDeltaTime ile yapılır (slow-mo'dan etkilenmez).
/// </summary>
public class TimeSlowAudioController : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("Slow-mo başlangıç sesi (tek sefer çalar)")]
    [SerializeField] private AudioClip timeSlowStartSound;
    
    [Tooltip("Slow-mo loop sesi (slow-mo boyunca çalar)")]
    [SerializeField] private AudioClip timeSlowLoopSound;
    
    [Tooltip("Slow-mo bitiş sesi (tek sefer çalar)")]
    [SerializeField] private AudioClip timeSlowEndSound;

    [Header("Audio Sources")]
    [Tooltip("Start/End sesleri için (PlayOneShot kullanır)")]
    [SerializeField] private AudioSource sfxSource;
    
    [Tooltip("Loop sesi için (ayrı source, loop=true)")]
    [SerializeField] private AudioSource loopSource;
    
    [Tooltip("Background music source (pitch değiştirmek için, opsiyonel)")]
    [SerializeField] private AudioSource bgmSource;

    [Header("Fade Settings")]
    [SerializeField] private float loopFadeInDuration = 0.5f;
    [SerializeField] private float loopFadeOutDuration = 0.5f;
    [SerializeField] private float loopMaxVolume = 0.7f;

    [Header("BGM Pitch Settings")]
    [SerializeField] private bool enableBGMPitchChange = true;
    [SerializeField] private float bgmSlowPitch = 0.5f;
    [SerializeField] private float bgmNormalPitch = 1f;
    [SerializeField] private float bgmPitchChangeDuration = 0.5f;

    [Header("SFX Volume")]
    [SerializeField] private float startSoundVolume = 1f;
    [SerializeField] private float endSoundVolume = 1f;

    // Singleton for easy access
    public static TimeSlowAudioController Instance { get; private set; }

    private Coroutine loopFadeCoroutine;
    private Coroutine bgmPitchCoroutine;
    private bool isTimeSlowActive = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // AudioSource'ları otomatik oluştur (atanmadıysa)
        EnsureAudioSources();
    }

    private void EnsureAudioSources()
    {
        // SFX Source
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("TimeSlowSFX");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        // Loop Source
        if (loopSource == null)
        {
            GameObject loopObj = new GameObject("TimeSlowLoop");
            loopObj.transform.SetParent(transform);
            loopSource = loopObj.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;
            loopSource.volume = 0f;
        }
        else
        {
            // Var olan loopSource'u ayarla
            loopSource.loop = true;
            loopSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// Time slow başladığında çağrılır.
    /// Start SFX + Loop fade-in + BGM pitch düşür.
    /// </summary>
    public void StartTimeSlowSequence()
    {
        if (isTimeSlowActive) return;
        isTimeSlowActive = true;

        // Start SFX
        if (sfxSource != null && timeSlowStartSound != null)
        {
            sfxSource.PlayOneShot(timeSlowStartSound, startSoundVolume);
        }

        // Loop başlat ve fade-in
        if (loopSource != null && timeSlowLoopSound != null)
        {
            loopSource.clip = timeSlowLoopSound;
            loopSource.volume = 0f;
            loopSource.Play();

            if (loopFadeCoroutine != null)
                StopCoroutine(loopFadeCoroutine);
            loopFadeCoroutine = StartCoroutine(FadeLoopVolume(loopMaxVolume, loopFadeInDuration, false));
        }

        // BGM pitch düşür
        if (enableBGMPitchChange && bgmSource != null)
        {
            if (bgmPitchCoroutine != null)
                StopCoroutine(bgmPitchCoroutine);
            bgmPitchCoroutine = StartCoroutine(LerpBGMPitch(bgmSlowPitch, bgmPitchChangeDuration));
        }
    }

    /// <summary>
    /// Time slow bittiğinde çağrılır.
    /// End SFX + Loop fade-out + BGM pitch normale.
    /// </summary>
    public void StopTimeSlowSequence()
    {
        if (!isTimeSlowActive) return;
        isTimeSlowActive = false;

        // End SFX
        if (sfxSource != null && timeSlowEndSound != null)
        {
            sfxSource.PlayOneShot(timeSlowEndSound, endSoundVolume);
        }

        // Loop fade-out ve durdur
        if (loopSource != null && loopSource.isPlaying)
        {
            if (loopFadeCoroutine != null)
                StopCoroutine(loopFadeCoroutine);
            loopFadeCoroutine = StartCoroutine(FadeLoopVolume(0f, loopFadeOutDuration, true));
        }

        // BGM pitch normale
        if (enableBGMPitchChange && bgmSource != null)
        {
            if (bgmPitchCoroutine != null)
                StopCoroutine(bgmPitchCoroutine);
            bgmPitchCoroutine = StartCoroutine(LerpBGMPitch(bgmNormalPitch, bgmPitchChangeDuration));
        }
    }

    /// <summary>
    /// Loop volume'u fade et (unscaledDeltaTime ile).
    /// </summary>
    private IEnumerator FadeLoopVolume(float targetVolume, float duration, bool stopAfterFade)
    {
        if (loopSource == null) yield break;

        float startVolume = loopSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // unscaledDeltaTime kullan - slow-mo'da fade normal hızda olsun
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            loopSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        loopSource.volume = targetVolume;

        if (stopAfterFade && targetVolume <= 0f)
        {
            loopSource.Stop();
        }
    }

    /// <summary>
    /// BGM pitch'ini lerp et (unscaledDeltaTime ile).
    /// </summary>
    private IEnumerator LerpBGMPitch(float targetPitch, float duration)
    {
        if (bgmSource == null) yield break;

        float startPitch = bgmSource.pitch;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // unscaledDeltaTime kullan - slow-mo'da pitch değişimi normal hızda olsun
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bgmSource.pitch = Mathf.Lerp(startPitch, targetPitch, t);
            yield return null;
        }

        bgmSource.pitch = targetPitch;
    }

    /// <summary>
    /// BGM AudioSource'u dışarıdan ayarla.
    /// BackgroundMusic script'i varsa oradan çağrılabilir.
    /// </summary>
    public void SetBGMSource(AudioSource source)
    {
        bgmSource = source;
    }
    
    // Puzzle için volume kontrol
    private float puzzleSavedVolume = 1f;
    
    /// <summary>
    /// Puzzle başladığında müziği yarıya indir
    /// </summary>
    public void SetPuzzleVolume(bool puzzleActive)
    {
        if (bgmSource == null) 
        {
            Debug.LogWarning("[TimeSlowAudioController] bgmSource null - Inspector'da ata!");
            return;
        }
        
        if (puzzleActive)
        {
            puzzleSavedVolume = bgmSource.volume;
            bgmSource.volume = puzzleSavedVolume * 0.5f;
            Debug.Log($"[TimeSlowAudioController] Puzzle müzik: {puzzleSavedVolume} -> {bgmSource.volume}");
        }
        else
        {
            bgmSource.volume = puzzleSavedVolume;
            Debug.Log($"[TimeSlowAudioController] Müzik normale döndü: {bgmSource.volume}");
        }
    }

    /// <summary>
    /// Time slow aktif mi?
    /// </summary>
    public bool IsTimeSlowActive => isTimeSlowActive;

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        // Coroutine'leri temizle
        if (loopFadeCoroutine != null)
            StopCoroutine(loopFadeCoroutine);
        if (bgmPitchCoroutine != null)
            StopCoroutine(bgmPitchCoroutine);
    }

    private void OnDisable()
    {
        // Disable olduğunda sesleri durdur
        if (loopSource != null && loopSource.isPlaying)
        {
            loopSource.Stop();
            loopSource.volume = 0f;
        }

        // BGM pitch'i normale döndür
        if (bgmSource != null)
        {
            bgmSource.pitch = bgmNormalPitch;
        }
    }
}
