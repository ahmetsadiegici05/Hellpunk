using UnityEngine;
using System.Collections;

/// <summary>
/// Vuruş anında kısa süreli oyun durması efekti (Hit Stop / Freeze Frame)
/// Vuruşlara ağırlık ve etki hissi katar
/// </summary>
public class HitStop : MonoBehaviour
{
    public static HitStop Instance { get; private set; }
    
    [Header("Hit Stop Ayarları")]
    [SerializeField] private float normalHitDuration = 0.04f;
    [SerializeField] private float heavyHitDuration = 0.08f;
    [SerializeField] private float criticalHitDuration = 0.12f;
    
    [Header("Opsiyonel")]
    [SerializeField] private bool affectTimeScale = true;
    
    private bool isHitStopping = false;
    private float originalTimeScale = 1f;
    private float originalFixedDeltaTime;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            originalFixedDeltaTime = Time.fixedDeltaTime;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// Normal vuruş durması
    /// </summary>
    public void NormalHit()
    {
        if (!isHitStopping)
            StartCoroutine(HitStopRoutine(normalHitDuration));
    }
    
    /// <summary>
    /// Ağır vuruş durması
    /// </summary>
    public void HeavyHit()
    {
        if (!isHitStopping)
            StartCoroutine(HitStopRoutine(heavyHitDuration));
    }
    
    /// <summary>
    /// Kritik vuruş durması
    /// </summary>
    public void CriticalHit()
    {
        if (!isHitStopping)
            StartCoroutine(HitStopRoutine(criticalHitDuration));
    }
    
    /// <summary>
    /// Özel süreli vuruş durması
    /// </summary>
    public void CustomHitStop(float duration)
    {
        if (!isHitStopping)
            StartCoroutine(HitStopRoutine(duration));
    }
    
    private IEnumerator HitStopRoutine(float duration)
    {
        if (!affectTimeScale) yield break;
        
        // Time slow aktifken hit stop uygulanmasın (çakışma önleme)
        if (TimeSlowAbility.Instance != null && TimeSlowAbility.Instance.IsSlowMotionActive)
            yield break;
        
        isHitStopping = true;
        originalTimeScale = Time.timeScale;
        
        // Zamanı durdur
        Time.timeScale = 0.02f; // Neredeyse tamamen durdur ama 0 değil
        Time.fixedDeltaTime = originalFixedDeltaTime * 0.02f;
        
        // Gerçek zamanda bekle
        yield return new WaitForSecondsRealtime(duration);
        
        // Zamanı geri getir
        Time.timeScale = originalTimeScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * originalTimeScale;
        
        isHitStopping = false;
    }
    
    private void OnDestroy()
    {
        // Temizlik
        if (isHitStopping)
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }
    }
}
