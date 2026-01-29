using UnityEngine;

/// <summary>
/// 2.5D Kamera Efektleri
/// - Hareket yönüne göre hafif tilt (eğim)
/// - Hız bazlı dinamik zoom
/// - Landing/jump sarsıntısı (mevcut ScreenShake ile uyumlu)
/// - Parallax his için kamera offset
/// 
/// Mevcut CameraController'a ek olarak kullan.
/// </summary>
[RequireComponent(typeof(Camera))]
public class Camera2_5DEffects : MonoBehaviour
{
    [Header("Dinamik Tilt (Eğim)")]
    [Tooltip("Hareket yönüne göre kamerayı hafifçe eğ")]
    [SerializeField] private bool enableTilt = false;
    [SerializeField] private float maxTiltAngle = 0.5f;
    [SerializeField] private float tiltSpeed = 1.5f;
    
    [Header("Hareket Bazlı Zoom")]
    [Tooltip("Hızlı hareket = biraz zoom out")]
    [SerializeField] private bool enableDynamicZoom = false; // KAPALI - CameraController ile çakışıyor
    [SerializeField] private float baseOrthographicSize = 7f;
    [SerializeField] private float maxZoomOutAmount = 0.5f;
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float velocityThreshold = 8f;
    
    [Header("Düşüş Zoom")]
    [Tooltip("Aşağı düşerken zoom out")]
    [SerializeField] private bool enableFallZoom = false; // KAPALI
    [SerializeField] private float fallZoomMultiplier = 0.15f;
    
    [Header("Look Ahead (İleri Bakış)")]
    [Tooltip("Hareket yönünde biraz öne bak - CameraController zaten yapıyor, bu kapalı")]
    [SerializeField] private bool enableLookAhead = false;
    [SerializeField] private float lookAheadDistanceX = 0.5f;
    [SerializeField] private float lookAheadDistanceY = 0.2f;
    [SerializeField] private float lookAheadSpeed = 1f;
    
    [Header("Parallax Kamera Offset")]
    [Tooltip("Mouse pozisyonuna göre hafif offset (derinlik hissi)")]
    [SerializeField] private bool enableMouseParallax = false;
    [SerializeField] private float mouseParallaxAmount = 0.15f;
    
    [Header("⚠️ UYARI")]
    [Tooltip("Bu script CameraController ile çakışabilir. Sorun olursa componenti kaldır.")]
    [SerializeField] private bool disableAllEffects = true; // YENİ: Tüm efektleri kapat
    
    [Header("Referanslar")]
    [SerializeField] private Transform player;
    
    // Internal
    private Camera cam;
    private float currentTilt = 0f;
    private float currentZoomOffset = 0f;
    private Vector2 currentLookAhead = Vector2.zero;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    private Vector3 smoothedVelocity; // Yeni: yumuşatılmış velocity
    private Vector2 mouseOffset = Vector2.zero;
    
    // Original values
    private float originalOrthoSize;
    private Quaternion originalRotation;
    private Vector3 basePosition;
    
    private const float VELOCITY_SMOOTHING = 0.1f; // Velocity smoothing faktörü
    
    private void Start()
    {
        cam = GetComponent<Camera>();
        originalOrthoSize = cam.orthographicSize;
        originalRotation = transform.rotation;
        
        if (baseOrthographicSize <= 0)
            baseOrthographicSize = originalOrthoSize;
        
        // Player bul
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        if (player != null)
            lastPlayerPosition = player.position;
    }
    
    private void LateUpdate()
    {
        // Tüm efektler kapalıysa hiçbir şey yapma
        if (disableAllEffects) return;
        
        if (player == null) return;
        
        // Player velocity hesapla - SMOOTHED
        Vector3 rawVelocity = (player.position - lastPlayerPosition) / Time.deltaTime;
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, rawVelocity, VELOCITY_SMOOTHING);
        playerVelocity = smoothedVelocity; // Smoothed değeri kullan
        lastPlayerPosition = player.position;
        
        // Efektleri uygula
        ApplyTilt();
        ApplyDynamicZoom();
        ApplyLookAhead();
        ApplyMouseParallax();
    }
    
    private void ApplyTilt()
    {
        if (!enableTilt) return;
        
        // Yatay hareket yönüne göre tilt
        float targetTilt = -playerVelocity.x / 10f * maxTiltAngle;
        targetTilt = Mathf.Clamp(targetTilt, -maxTiltAngle, maxTiltAngle);
        
        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSpeed);
        
        // Sadece Z ekseninde döndür (2D için)
        transform.rotation = originalRotation * Quaternion.Euler(0, 0, currentTilt);
    }
    
    private void ApplyDynamicZoom()
    {
        if (!enableDynamicZoom || !cam.orthographic) return;
        
        float targetZoomOffset = 0f;
        
        // Hız bazlı zoom
        float speed = playerVelocity.magnitude;
        if (speed > velocityThreshold)
        {
            float speedFactor = (speed - velocityThreshold) / 10f;
            targetZoomOffset = Mathf.Min(speedFactor * maxZoomOutAmount, maxZoomOutAmount);
        }
        
        // Düşüş zoom
        if (enableFallZoom && playerVelocity.y < -velocityThreshold)
        {
            float fallSpeed = Mathf.Abs(playerVelocity.y);
            float fallZoom = (fallSpeed - velocityThreshold) / 10f * fallZoomMultiplier;
            targetZoomOffset = Mathf.Max(targetZoomOffset, Mathf.Min(fallZoom, maxZoomOutAmount));
        }
        
        currentZoomOffset = Mathf.Lerp(currentZoomOffset, targetZoomOffset, Time.deltaTime * zoomSpeed);
        
        // Not: CameraController da zoom yapıyorsa, bu offset olarak eklenebilir
        // Şimdilik direkt ayarlıyoruz
        cam.orthographicSize = baseOrthographicSize + currentZoomOffset;
    }
    
    private void ApplyLookAhead()
    {
        if (!enableLookAhead) return;
        
        // Hareket yönüne göre look ahead
        Vector2 targetLookAhead = Vector2.zero;
        
        if (Mathf.Abs(playerVelocity.x) > 0.5f)
        {
            targetLookAhead.x = Mathf.Sign(playerVelocity.x) * lookAheadDistanceX;
        }
        
        if (playerVelocity.y < -velocityThreshold)
        {
            targetLookAhead.y = -lookAheadDistanceY;
        }
        else if (playerVelocity.y > velocityThreshold)
        {
            targetLookAhead.y = lookAheadDistanceY * 0.5f;
        }
        
        currentLookAhead = Vector2.Lerp(currentLookAhead, targetLookAhead, Time.deltaTime * lookAheadSpeed);
        
        // Bu offset'i CameraController'ın hedefine eklemek için public property
        // Şimdilik transform'a direkt eklemiyoruz çünkü CameraController zaten takip yapıyor
    }
    
    private void ApplyMouseParallax()
    {
        if (!enableMouseParallax) return;
        
        // Mouse ekranın neresinde?
        Vector2 mouseViewport = cam.ScreenToViewportPoint(Input.mousePosition);
        mouseViewport = (mouseViewport - new Vector2(0.5f, 0.5f)) * 2f; // -1 to 1
        
        Vector2 targetOffset = mouseViewport * mouseParallaxAmount;
        mouseOffset = Vector2.Lerp(mouseOffset, targetOffset, Time.deltaTime * 5f);
        
        // Bu offset'i kamera pozisyonuna ekle
        // Not: CameraController ile çakışmamak için dikkatli kullan
    }
    
    /// <summary>
    /// Mevcut look ahead offset'i al (CameraController entegrasyonu için)
    /// </summary>
    public Vector2 GetLookAheadOffset()
    {
        return currentLookAhead;
    }
    
    /// <summary>
    /// Mevcut mouse parallax offset'i al
    /// </summary>
    public Vector2 GetMouseParallaxOffset()
    {
        return mouseOffset;
    }
    
    /// <summary>
    /// Zoom'u resetle
    /// </summary>
    public void ResetZoom()
    {
        currentZoomOffset = 0f;
        cam.orthographicSize = baseOrthographicSize;
    }
    
    /// <summary>
    /// Efektleri geçici olarak devre dışı bırak
    /// </summary>
    public void SetEffectsEnabled(bool enabled)
    {
        enableTilt = enabled;
        enableDynamicZoom = enabled;
        enableLookAhead = enabled;
        enableMouseParallax = enabled;
        
        if (!enabled)
        {
            transform.rotation = originalRotation;
            cam.orthographicSize = baseOrthographicSize;
        }
    }
}
