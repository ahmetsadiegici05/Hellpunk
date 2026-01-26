using UnityEngine;
using System.Collections;

/// <summary>
/// Oyuncu yere düştüğünde, hasar aldığında veya skill kullandığında ekranı sallayan sistem.
/// Kameraya eklenir.
/// </summary>
public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance { get; private set; }
    
    [Header("Default Settings")]
    [SerializeField] private float defaultDuration = 0.1f;
    [SerializeField] private float defaultMagnitude = 0.03f;
    [SerializeField] private float dampingSpeed = 2f;
    
    [Header("Preset Intensities")]
    [SerializeField] private float lightShake = 0.015f;
    [SerializeField] private float mediumShake = 0.04f;
    [SerializeField] private float heavyShake = 0.08f;
    
    private Vector3 originalPosition;
    private float currentShakeDuration;
    private float currentShakeMagnitude;
    private bool isShaking;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
        
        originalPosition = transform.localPosition;
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
    
    /// <summary>
    /// Hafif sarsıntı (yere iniş, hafif hasar)
    /// </summary>
    public void ShakeLight()
    {
        Shake(0.1f, lightShake);
    }
    
    /// <summary>
    /// Orta sarsıntı (hasar alma, düşman öldürme)
    /// </summary>
    public void ShakeMedium()
    {
        Shake(0.15f, mediumShake);
    }
    
    /// <summary>
    /// Ağır sarsıntı (skill kullanımı, büyük patlama)
    /// </summary>
    public void ShakeHeavy()
    {
        Shake(0.25f, heavyShake);
    }
    
    /// <summary>
    /// Özel parametrelerle sarsıntı
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        if (duration > currentShakeDuration)
        {
            currentShakeDuration = duration;
        }
        
        if (magnitude > currentShakeMagnitude)
        {
            currentShakeMagnitude = magnitude;
        }
        
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine());
        }
    }
    
    private IEnumerator ShakeCoroutine()
    {
        isShaking = true;
        
        while (currentShakeDuration > 0)
        {
            // Rastgele offset
            float offsetX = Random.Range(-1f, 1f) * currentShakeMagnitude;
            float offsetY = Random.Range(-1f, 1f) * currentShakeMagnitude;
            
            transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);
            
            currentShakeDuration -= Time.unscaledDeltaTime;
            currentShakeMagnitude = Mathf.Lerp(currentShakeMagnitude, 0, Time.unscaledDeltaTime * dampingSpeed);
            
            yield return null;
        }
        
        transform.localPosition = originalPosition;
        currentShakeMagnitude = 0;
        isShaking = false;
    }
    
    /// <summary>
    /// Yönlü sarsıntı (örn: hasar yönünde)
    /// </summary>
    public void ShakeDirectional(Vector2 direction, float duration, float magnitude)
    {
        StartCoroutine(DirectionalShakeCoroutine(direction.normalized, duration, magnitude));
    }
    
    private IEnumerator DirectionalShakeCoroutine(Vector2 direction, float duration, float magnitude)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            
            // Yönde ileri-geri hareket
            float wave = Mathf.Sin(t * Mathf.PI * 4) * (1f - t);
            Vector3 offset = (Vector3)(direction * wave * magnitude);
            
            // Rastgele titreme ekle
            offset += new Vector3(
                Random.Range(-0.02f, 0.02f),
                Random.Range(-0.02f, 0.02f),
                0
            ) * (1f - t);
            
            transform.localPosition = originalPosition + offset;
            
            yield return null;
        }
        
        transform.localPosition = originalPosition;
    }
}
