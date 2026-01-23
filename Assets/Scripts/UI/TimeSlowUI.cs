using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Time Slow yeteneği için kapsamlı UI göstergesi.
/// Cooldown, ikon, pulse animasyonu ve tüm görsel efektleri yönetir.
/// Referanslar atanmadıysa runtime'da kendi UI elemanlarını oluşturur.
/// </summary>
public class TimeSlowUI : MonoBehaviour
{
    [Header("UI References (Optional - Auto-created if null)")]
    [SerializeField] private Image cooldownFillImage;       // Cooldown dolum göstergesi (Image Type: Filled)
    [SerializeField] private Image abilityIconImage;        // Ability ikonu
    [SerializeField] private Image backgroundImage;         // Arka plan
    [SerializeField] private Image glowImage;               // Glow efekti
    [SerializeField] private TextMeshProUGUI cooldownText;  // Kalan süre yazısı
    [SerializeField] private TextMeshProUGUI keyHintText;   // Tuş ipucu

    [Header("Position Settings")]
    [SerializeField] private Vector2 screenPosition = new Vector2(-100f, -100f); // Sağ üst köşe
    [SerializeField] private float iconSize = 64f;

    [Header("Visual Settings")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color activeColor = new Color(0.2f, 0.8f, 1f, 1f);  // Cyan
    [SerializeField] private Color cooldownColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [SerializeField] private Color fillReadyColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private Color fillCooldownColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private Color glowColor = new Color(0.2f, 0.8f, 1f, 0.5f);

    [Header("Animation Settings")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseMinScale = 0.95f;
    [SerializeField] private float pulseMaxScale = 1.05f;
    [SerializeField] private float glowPulseSpeed = 3f;
    [SerializeField] private float flashDuration = 0.3f;

    // Singleton for easy access
    public static TimeSlowUI Instance { get; private set; }

    private TimeSlowAbility timeSlowAbility;
    private Canvas canvas;
    private RectTransform containerRect;
    private Vector3 originalIconScale;
    private bool wasReady = false;
    private bool uiCreated = false;
    private Coroutine flashCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // MainMenu kontrolü
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
        {
            Destroy(gameObject);
            return;
        }

        FindTimeSlowAbility();
        EnsureCanvas();
        EnsureUIElements();
        
        // Pozisyonu sağ üst köşeye zorla ayarla
        ForcePositionToTopRight();
        
        if (abilityIconImage != null)
            originalIconScale = abilityIconImage.transform.localScale;
    }
    
    private void ForcePositionToTopRight()
    {
        // Container'ı doğrudan sağ üst köşeye ayarla
        if (containerRect != null)
        {
            containerRect.anchorMin = new Vector2(1f, 1f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(1f, 1f);
            containerRect.anchoredPosition = new Vector2(-120f, -120f);
        }
    }

    private void FindTimeSlowAbility()
    {
        // Önce static instance'ı dene
        if (TimeSlowAbility.Instance != null)
        {
            timeSlowAbility = TimeSlowAbility.Instance;
            return;
        }

        // Bulunamazsa sahnede ara
        timeSlowAbility = FindFirstObjectByType<TimeSlowAbility>();

        if (timeSlowAbility == null)
        {
            Debug.LogWarning("TimeSlowUI: TimeSlowAbility bulunamadı!");
        }
    }

    private void EnsureCanvas()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            // Canvas yoksa oluştur
            GameObject canvasObj = new GameObject("TimeSlowUI_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            
            // Canvas Scaler ayarla
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasObj.AddComponent<GraphicRaycaster>();
            
            transform.SetParent(canvasObj.transform, false);
        }
    }

    private void EnsureUIElements()
    {
        if (uiCreated) return;

        // Container oluştur
        if (containerRect == null)
        {
            GameObject container = new GameObject("TimeSlowContainer");
            container.transform.SetParent(transform, false);
            containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1f, 1f); // Sağ üst köşe
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(1f, 1f);
            // Her zaman sağ üst köşede olsun (Inspector değerini yoksay)
            containerRect.anchoredPosition = new Vector2(-100f, -100f);
            containerRect.sizeDelta = new Vector2(iconSize, iconSize);
        }

        // Background oluştur
        if (backgroundImage == null)
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(containerRect, false);
            backgroundImage = bgObj.AddComponent<Image>();
            backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            backgroundImage.sprite = GetBuiltInSprite();
            
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;
        }

        // Cooldown Fill oluştur
        if (cooldownFillImage == null)
        {
            GameObject fillObj = new GameObject("CooldownFill");
            fillObj.transform.SetParent(containerRect, false);
            cooldownFillImage = fillObj.AddComponent<Image>();
            cooldownFillImage.sprite = GetBuiltInSprite();
            cooldownFillImage.type = Image.Type.Filled;
            cooldownFillImage.fillMethod = Image.FillMethod.Radial360;
            cooldownFillImage.fillOrigin = (int)Image.Origin360.Top;
            cooldownFillImage.fillClockwise = true;
            cooldownFillImage.color = fillReadyColor;
            
            RectTransform fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = new Vector2(-4f, -4f);
            fillRect.anchoredPosition = Vector2.zero;
        }

        // Icon oluştur
        if (abilityIconImage == null)
        {
            GameObject iconObj = new GameObject("AbilityIcon");
            iconObj.transform.SetParent(containerRect, false);
            abilityIconImage = iconObj.AddComponent<Image>();
            abilityIconImage.sprite = GetBuiltInSprite();
            abilityIconImage.color = readyColor;
            
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(iconSize * 0.6f, iconSize * 0.6f);
            iconRect.anchoredPosition = Vector2.zero;
            
            originalIconScale = iconRect.localScale;
        }

        // Glow oluştur
        if (glowImage == null)
        {
            GameObject glowObj = new GameObject("Glow");
            glowObj.transform.SetParent(containerRect, false);
            glowObj.transform.SetAsFirstSibling(); // En arkaya
            glowImage = glowObj.AddComponent<Image>();
            glowImage.sprite = GetBuiltInSprite();
            glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            
            RectTransform glowRect = glowObj.GetComponent<RectTransform>();
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.sizeDelta = new Vector2(20f, 20f);
            glowRect.anchoredPosition = Vector2.zero;
        }

        // Cooldown Text oluştur
        if (cooldownText == null)
        {
            GameObject textObj = new GameObject("CooldownText");
            textObj.transform.SetParent(containerRect, false);
            cooldownText = textObj.AddComponent<TextMeshProUGUI>();
            cooldownText.fontSize = 18;
            cooldownText.fontStyle = FontStyles.Bold;
            cooldownText.alignment = TextAlignmentOptions.Center;
            cooldownText.color = Color.white;
            cooldownText.text = "";
            cooldownText.enableAutoSizing = false;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(iconSize, 25f);
            textRect.anchoredPosition = Vector2.zero;
        }

        // Key Hint oluştur
        if (keyHintText == null)
        {
            GameObject hintObj = new GameObject("KeyHint");
            hintObj.transform.SetParent(containerRect, false);
            keyHintText = hintObj.AddComponent<TextMeshProUGUI>();
            keyHintText.fontSize = 16;
            keyHintText.fontStyle = FontStyles.Bold;
            keyHintText.alignment = TextAlignmentOptions.Center;
            keyHintText.color = new Color(1f, 1f, 1f, 0.9f);
            keyHintText.text = "[E]";
            keyHintText.enableAutoSizing = false;
            
            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 0f);
            hintRect.anchorMax = new Vector2(0.5f, 0f);
            hintRect.sizeDelta = new Vector2(iconSize, 25f);
            hintRect.anchoredPosition = new Vector2(0f, -18f);
        }

        uiCreated = true;
    }

    private Sprite GetBuiltInSprite()
    {
        // Procedural daire sprite oluştur (Resource yükleme hatasını önlemek için)
        return CreateCircleSprite(128);
    }
    
    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f; // Kenarlardan biraz boşluk

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    // Yumuşak kenarlar (Antialiasing)
                    float alpha = 1f;
                    if (dist > radius - 1f) alpha = radius - dist;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void Update()
    {
        // Pozisyonu her frame zorla (Inspector override'ı önlemek için)
        if (containerRect != null)
        {
            containerRect.anchorMin = new Vector2(1f, 1f);
            containerRect.anchorMax = new Vector2(1f, 1f);
            containerRect.pivot = new Vector2(1f, 1f);
            containerRect.anchoredPosition = new Vector2(-120f, -120f);
        }
        
        // TimeSlowAbility referansını kontrol et
        if (timeSlowAbility == null)
        {
            if (TimeSlowAbility.Instance != null)
                timeSlowAbility = TimeSlowAbility.Instance;
            else
                return;
        }

        // Pause/GameOver kontrolü - UI gizlenir
        if (ShouldHideUI())
        {
            SetUIVisible(false);
            return;
        }
        else
        {
            SetUIVisible(true);
        }

        // Tüm UI güncellemeleri unscaledTime ile çalışır
        UpdateCooldownFill();
        UpdateIconColor();
        UpdateCooldownText();
        UpdateGlow();
        UpdatePulseAnimation();
        
        // Hazır olduğunda flash efekti
        if (timeSlowAbility.IsReady && !wasReady)
        {
            TriggerReadyFlash();
        }
        
        wasReady = timeSlowAbility.IsReady;
    }

    private bool ShouldHideUI()
    {
        // UIManager.Instance varsa pause/gameover kontrolü yap
        if (UIManager.Instance != null)
        {
            if (UIManager.Instance.IsPaused || UIManager.Instance.IsGameOver || UIManager.Instance.IsVictory)
            {
                return true;
            }
        }
        return false;
    }

    private void SetUIVisible(bool visible)
    {
        if (containerRect != null)
            containerRect.gameObject.SetActive(visible);
    }

    private void UpdateCooldownFill()
    {
        if (cooldownFillImage == null) return;

        cooldownFillImage.fillAmount = timeSlowAbility.CooldownProgress;
        
        if (timeSlowAbility.IsSlowMotionActive)
            cooldownFillImage.color = activeColor;
        else if (timeSlowAbility.IsReady)
            cooldownFillImage.color = fillReadyColor;
        else
            cooldownFillImage.color = fillCooldownColor;
    }

    private void UpdateIconColor()
    {
        if (abilityIconImage == null) return;

        if (timeSlowAbility.IsSlowMotionActive)
        {
            abilityIconImage.color = activeColor;
        }
        else if (timeSlowAbility.IsReady)
        {
            abilityIconImage.color = readyColor;
        }
        else
        {
            abilityIconImage.color = cooldownColor;
        }
    }

    private void UpdateCooldownText()
    {
        if (cooldownText == null) return;

        if (timeSlowAbility.IsSlowMotionActive)
        {
            cooldownText.text = "AKTİF";
            cooldownText.color = activeColor;
        }
        else if (timeSlowAbility.IsReady)
        {
            cooldownText.text = "";
        }
        else
        {
            // Kalan süreyi göster
            float remaining = timeSlowAbility.CooldownRemaining;
            cooldownText.text = remaining.ToString("F1");
            cooldownText.color = Color.white;
        }
    }

    private void UpdateGlow()
    {
        if (glowImage == null) return;

        if (timeSlowAbility.IsSlowMotionActive)
        {
            // Aktifken glow pulse
            float pulse = (Mathf.Sin(Time.unscaledTime * glowPulseSpeed) + 1f) * 0.5f;
            glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, pulse * 0.6f);
        }
        else if (timeSlowAbility.IsReady)
        {
            // Hazırken hafif glow
            float pulse = (Mathf.Sin(Time.unscaledTime * glowPulseSpeed * 0.5f) + 1f) * 0.5f;
            glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, pulse * 0.3f);
        }
        else
        {
            // Cooldown'da glow yok
            glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        }
    }

    private void UpdatePulseAnimation()
    {
        if (!enablePulse || abilityIconImage == null) return;

        if (timeSlowAbility.IsReady && !timeSlowAbility.IsSlowMotionActive)
        {
            // Hazır olduğunda pulse efekti - unscaledTime kullan
            float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, 
                (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) * 0.5f);
            abilityIconImage.transform.localScale = originalIconScale * pulse;
        }
        else if (timeSlowAbility.IsSlowMotionActive)
        {
            // Aktifken daha hızlı pulse
            float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, 
                (Mathf.Sin(Time.unscaledTime * pulseSpeed * 2f) + 1f) * 0.5f);
            abilityIconImage.transform.localScale = originalIconScale * pulse;
        }
        else
        {
            // Normal ölçeğe dön
            abilityIconImage.transform.localScale = originalIconScale;
        }
    }

    private void TriggerReadyFlash()
    {
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private System.Collections.IEnumerator FlashRoutine()
    {
        if (abilityIconImage == null) yield break;

        Color startColor = Color.white;
        float elapsed = 0f;

        while (elapsed < flashDuration)
        {
            // unscaledDeltaTime kullan - slow-mo sırasında bile akıcı animasyon
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / flashDuration;
            
            abilityIconImage.color = Color.Lerp(startColor, readyColor, t);
            
            // Scale efekti
            float scale = Mathf.Lerp(1.3f, 1f, t);
            abilityIconImage.transform.localScale = originalIconScale * scale;
            
            yield return null;
        }

        abilityIconImage.color = readyColor;
        abilityIconImage.transform.localScale = originalIconScale;
    }

    /// <summary>
    /// UI'ı göster
    /// </summary>
    public void Show()
    {
        SetUIVisible(true);
    }

    /// <summary>
    /// UI'ı gizle
    /// </summary>
    public void Hide()
    {
        SetUIVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
