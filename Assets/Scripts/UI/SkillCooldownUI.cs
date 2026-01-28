using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Skill cooldown göstergeleri için HUD UI
/// Tüm UI elemanları kod ile oluşturulur.
/// </summary>
public class SkillCooldownUI : MonoBehaviour
{
    [Header("Position Settings")]
    [SerializeField] private Vector2 startPosition = new Vector2(-150f, 120f); // Sağ alt köşe
    [SerializeField] private float skillSpacing = 70f;
    [SerializeField] private float iconSize = 56f;

    [Header("Colors")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color cooldownColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
    [SerializeField] private Color activeColor = new Color(0.3f, 0.9f, 1f, 1f);
    [SerializeField] private Color healColor = new Color(0.3f, 0.6f, 1f, 1f); // Mavi
    [SerializeField] private Color fireballColor = new Color(1f, 0.5f, 0.1f, 1f);
    [SerializeField] private Color timeSlowColor = new Color(0.6f, 0.4f, 1f, 1f); // Mor
    [SerializeField] private Color shockwaveColor = new Color(1f, 0.9f, 0.2f, 1f); // Sarı
    [SerializeField] private Color ultimateColor = Color.red;

    private Canvas canvas;
    private GuitarSkillSystem skillSystem;

    // Skill UI elements
    private SkillIcon healSkill;
    private SkillIcon fireballSkill;
    private SkillIcon timeSlowSkill;
    private SkillIcon ultimateSkill;

    private class SkillIcon
    {
        public RectTransform container;
        public Image background;
        public Image cooldownFill;
        public Image icon;
        public TextMeshProUGUI keyText;
        public TextMeshProUGUI cooldownText;
        public Color skillColor;
        public bool wasReadyLastFrame; // For tracking state changes
    }

    private void Start()
    {
        StartCoroutine(InitializeDelayed());
    }

    private IEnumerator InitializeDelayed()
    {
        yield return null;
        
        // MainMenu kontrolü
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
        {
            Destroy(gameObject);
            yield break;
        }
        
        skillSystem = GuitarSkillSystem.Instance;
        if (skillSystem == null)
            skillSystem = FindFirstObjectByType<GuitarSkillSystem>();

        if (skillSystem != null)
        {
            if (!FindExistingUI())
            {
                CreateUI();
            }
            
            // Event'e abone ol
            skillSystem.OnAbilityFailedNoSoul += OnAbilityFailedNoSoul;
            
            Debug.Log("SkillCooldownUI: UI Hazır!");
        }
        else
        {
            Debug.LogWarning("SkillCooldownUI: GuitarSkillSystem bulunamadı!");
        }
    }

    private void CreateUI()
    {
        // Canvas oluştur
        GameObject canvasObj = new GameObject("SkillCooldownUI_Canvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f; // Hem genişlik hem yüksekliğe göre

        canvasObj.AddComponent<GraphicRaycaster>();

        // Skill ikonlarını oluştur (sağdan sola: F, Q, R)
        healSkill = CreateSkillIcon(canvasObj.transform, "Heal", "F", 0, healColor);
        fireballSkill = CreateSkillIcon(canvasObj.transform, "Fireball", "Q", 1, fireballColor);
        timeSlowSkill = CreateSkillIcon(canvasObj.transform, "TimeSlow", "R", 2, timeSlowColor);
        ultimateSkill = CreateSkillIcon(canvasObj.transform, "Ultimate", "U", 3, ultimateColor);
    }

    private SkillIcon CreateSkillIcon(Transform parent, string name, string key, int index, Color skillColor)
    {
        SkillIcon skill = new SkillIcon();
        skill.skillColor = skillColor;

        // Container
        GameObject containerObj = new GameObject($"Skill_{name}");
        containerObj.transform.SetParent(parent, false);
        skill.container = containerObj.AddComponent<RectTransform>();
        skill.container.anchorMin = new Vector2(1f, 0f); // Sağ alt köşe
        skill.container.anchorMax = new Vector2(1f, 0f);
        skill.container.pivot = new Vector2(1f, 0f);
        // Hardcoded pozisyon - sağ alt köşe, index'e göre sola kay
        skill.container.anchoredPosition = new Vector2(-20f - (index * skillSpacing), 60f); // Biraz daha yukarı kaldır
        skill.container.sizeDelta = new Vector2(iconSize, iconSize);

        // Background (daire)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(containerObj.transform, false);
        skill.background = bgObj.AddComponent<Image>();
        skill.background.color = new Color(0.1f, 0.1f, 0.12f, 0.95f); // Daha koyu ve opak
        skill.background.sprite = CreateCircleSprite(128); // Yuksek cozunurluk
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Border (Halka ekle)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(containerObj.transform, false);
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.sprite = CreateRingSprite(128, 0.08f);
        borderImg.color = new Color(1f, 1f, 1f, 0.3f);
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.sizeDelta = Vector2.zero;

        // Cooldown fill
        GameObject fillObj = new GameObject("CooldownFill");
        fillObj.transform.SetParent(containerObj.transform, false);
        skill.cooldownFill = fillObj.AddComponent<Image>();
        skill.cooldownFill.sprite = CreateCircleSprite(128);
        skill.cooldownFill.type = Image.Type.Filled;
        skill.cooldownFill.fillMethod = Image.FillMethod.Radial360;
        skill.cooldownFill.fillOrigin = (int)Image.Origin360.Top;
        skill.cooldownFill.fillClockwise = true;
        skill.cooldownFill.color = skillColor;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-6f, -6f); // Hafif içeride
        fillRect.anchoredPosition = Vector2.zero;

        // Icon
        GameObject iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(containerObj.transform, false);
        skill.icon = iconObj.AddComponent<Image>();
        skill.icon.sprite = CreateSkillIconSprite(name, 128);
        skill.icon.color = readyColor;
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(iconSize * 0.65f, iconSize * 0.65f);

        // Key Text Badge (Tuş yazısının arkasına ufak yuvarlak)
        GameObject keyBgObj = new GameObject("KeyBg");
        keyBgObj.transform.SetParent(containerObj.transform, false);
        Image keyBg = keyBgObj.AddComponent<Image>();
        keyBg.sprite = CreateCircleSprite(64);
        keyBg.color = new Color(0f, 0f, 0f, 0.9f);
        RectTransform keyBgRect = keyBgObj.GetComponent<RectTransform>();
        keyBgRect.anchorMin = new Vector2(0.5f, 0f);
        keyBgRect.anchorMax = new Vector2(0.5f, 0f);
        keyBgRect.sizeDelta = new Vector2(28, 28);
        keyBgRect.anchoredPosition = new Vector2(0, -14f); // Biraz daha dışarı

        // Key text
        GameObject keyObj = new GameObject("KeyText");
        keyObj.transform.SetParent(keyBgObj.transform, false);
        skill.keyText = keyObj.AddComponent<TextMeshProUGUI>();
        skill.keyText.text = key;
        skill.keyText.fontSize = 16;
        skill.keyText.fontStyle = FontStyles.Bold;
        skill.keyText.alignment = TextAlignmentOptions.Center;
        skill.keyText.color = new Color(1f, 0.85f, 0.3f, 1f); // Altın sarısı
        skill.keyText.enableAutoSizing = false;
        RectTransform keyRect = keyObj.GetComponent<RectTransform>();
        keyRect.anchorMin = Vector2.zero;
        keyRect.anchorMax = Vector2.one;
        keyRect.sizeDelta = Vector2.zero;
        keyRect.anchoredPosition = new Vector2(0, 1f); // Ortala

        // Cooldown text (merkez)
        GameObject cdObj = new GameObject("CooldownText");
        cdObj.transform.SetParent(containerObj.transform, false);
        skill.cooldownText = cdObj.AddComponent<TextMeshProUGUI>();
        skill.cooldownText.text = "";
        skill.cooldownText.fontSize = 24;
        skill.cooldownText.fontStyle = FontStyles.Bold;
        skill.cooldownText.alignment = TextAlignmentOptions.Center;
        skill.cooldownText.color = Color.white;
        skill.cooldownText.enableAutoSizing = false;
        // Gölge efekti (Kodla materyal oluşturmak zor, basitçe outline verelim)
        skill.cooldownText.outlineColor = new Color(0,0,0,1);
        skill.cooldownText.outlineWidth = 0.2f;

        RectTransform cdRect = cdObj.GetComponent<RectTransform>();
        cdRect.anchorMin = new Vector2(0.5f, 0.5f);
        cdRect.anchorMax = new Vector2(0.5f, 0.5f);
        cdRect.sizeDelta = new Vector2(iconSize, 30);
        cdRect.anchoredPosition = Vector2.zero;

        return skill;
    }

    private PlayerAttack playerAttack;
    
    // No soul feedback için
    private Coroutine healShakeCoroutine;
    private Coroutine fireballShakeCoroutine;
    private Coroutine timeSlowShakeCoroutine;
    private Coroutine ultimateShakeCoroutine;
    
    private void OnDestroy()
    {
        if (skillSystem != null)
        {
            skillSystem.OnAbilityFailedNoSoul -= OnAbilityFailedNoSoul;
        }
    }
    
    private void OnAbilityFailedNoSoul(GuitarSkillSystem.SkillType skillType)
    {
        // İlgili skill ikonunu kırmızı shake efekti ile göster
        SkillIcon targetSkill = null;
        
        switch (skillType)
        {
            case GuitarSkillSystem.SkillType.Heal:
                targetSkill = healSkill;
                if (healShakeCoroutine != null) StopCoroutine(healShakeCoroutine);
                if (targetSkill != null && targetSkill.container != null)
                    healShakeCoroutine = StartCoroutine(NoSoulShakeEffect(targetSkill));
                break;
            case GuitarSkillSystem.SkillType.Fireball:
                targetSkill = fireballSkill;
                if (fireballShakeCoroutine != null) StopCoroutine(fireballShakeCoroutine);
                if (targetSkill != null && targetSkill.container != null)
                    fireballShakeCoroutine = StartCoroutine(NoSoulShakeEffect(targetSkill));
                break;
            case GuitarSkillSystem.SkillType.TimeSlow:
                targetSkill = timeSlowSkill;
                if (timeSlowShakeCoroutine != null) StopCoroutine(timeSlowShakeCoroutine);
                if (targetSkill != null && targetSkill.container != null)
                    timeSlowShakeCoroutine = StartCoroutine(NoSoulShakeEffect(targetSkill));
                break;
            case GuitarSkillSystem.SkillType.Ultimate:
                targetSkill = ultimateSkill;
                if (ultimateShakeCoroutine != null) StopCoroutine(ultimateShakeCoroutine);
                if (targetSkill != null && targetSkill.container != null)
                    ultimateShakeCoroutine = StartCoroutine(NoSoulShakeEffect(targetSkill));
                break;
        }
    }
    
    private IEnumerator NoSoulShakeEffect(SkillIcon skill)
    {
        if (skill == null || skill.container == null) yield break;
        
        Vector2 originalPos = skill.container.anchoredPosition;
        Color originalIconColor = skill.icon != null ? skill.icon.color : Color.white;
        Color originalBgColor = skill.background != null ? skill.background.color : Color.black;
        
        float duration = 0.5f;
        float shakeIntensity = 8f;
        float flashFrequency = 20f;
        float time = 0f;
        
        // Kırmızı renk
        Color noSoulRed = new Color(1f, 0.2f, 0.2f, 1f);
        Color darkRed = new Color(0.4f, 0.1f, 0.1f, 0.95f);
        
        while (time < duration)
        {
            // Shake
            float shakeX = Mathf.Sin(time * 50f) * shakeIntensity * (1f - time / duration);
            skill.container.anchoredPosition = originalPos + new Vector2(shakeX, 0);
            
            // Flash (kırmızı yanıp sönme)
            float flash = (Mathf.Sin(time * flashFrequency) + 1f) / 2f;
            
            if (skill.icon != null)
                skill.icon.color = Color.Lerp(originalIconColor, noSoulRed, flash * 0.8f);
            
            if (skill.background != null)
                skill.background.color = Color.Lerp(originalBgColor, darkRed, flash * 0.6f);
            
            if (skill.cooldownFill != null)
                skill.cooldownFill.color = Color.Lerp(skill.skillColor, noSoulRed, flash * 0.7f);
            
            time += Time.unscaledDeltaTime;
            yield return null;
        }
        
        // Reset
        skill.container.anchoredPosition = originalPos;
        if (skill.icon != null) skill.icon.color = originalIconColor;
        if (skill.background != null) skill.background.color = originalBgColor;
        if (skill.cooldownFill != null) skill.cooldownFill.color = skill.skillColor;
    }

    private void Update()
    {
        if (skillSystem == null)
        {
            skillSystem = FindFirstObjectByType<GuitarSkillSystem>();
            if (skillSystem == null) return;
        }
        
        // PlayerAttack referansını al (fireball modu için)
        if (playerAttack == null)
        {
            playerAttack = FindFirstObjectByType<PlayerAttack>();
        }

        // Heal için sayı bazlı güncelleme
        UpdateHealSkillIcon();
        
        // Fireball için sayı bazlı güncelleme
        UpdateFireballSkillIcon();
        
        // TimeSlow için özel güncelleme (aktifken kalan süreyi göster)
        UpdateTimeSlowSkillIcon();

        UpdateUltimateSkillIcon();
    }
    
    private void UpdateHealSkillIcon()
    {
        if (healSkill == null || healSkill.container == null) return;
        if (skillSystem == null) return;
        
        int charges = skillSystem.HealCharges;
        bool isReady = skillSystem.IsHealReady;
        bool isActive = skillSystem.CurrentSkill == GuitarSkillSystem.SkillType.Heal;
        
        // Fill amount - sayı bazlı (her zaman dolu veya boş)
        if (healSkill.cooldownFill != null)
        {
            healSkill.cooldownFill.fillAmount = isReady ? 1f : 0f;
            
            if (isActive)
                healSkill.cooldownFill.color = activeColor;
            else if (isReady)
                healSkill.cooldownFill.color = healSkill.skillColor;
            else
                healSkill.cooldownFill.color = cooldownColor;
        }
        
        // Kalan sayıyı göster - ikonun üstünde (üst üste binmez)
        if (healSkill.cooldownText != null)
        {
            healSkill.cooldownText.text = charges.ToString();
            healSkill.cooldownText.color = isReady ? readyColor : cooldownColor;
            healSkill.cooldownText.fontSize = 18;
            healSkill.cooldownText.rectTransform.anchoredPosition = new Vector2(0, 45f); // İkonun üstünde
        }
        
        // Icon - ortalanmış
        if (healSkill.icon != null)
        {
            healSkill.icon.rectTransform.anchoredPosition = Vector2.zero; // Ortada
            
            if (isActive)
            {
                healSkill.icon.color = activeColor;
                float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.15f;
                healSkill.icon.transform.localScale = Vector3.one * pulse;
            }
            else if (isReady)
            {
                healSkill.icon.color = readyColor;
                float breathe = 1f + Mathf.Sin(Time.time * 2f) * 0.05f;
                healSkill.icon.transform.localScale = Vector3.one * breathe;
            }
            else
            {
                healSkill.icon.color = cooldownColor;
                healSkill.icon.transform.localScale = Vector3.one;
            }
        }
    }
    
    private void UpdateTimeSlowSkillIcon()
    {
        if (timeSlowSkill == null || timeSlowSkill.container == null) return;
        
        bool isTimeSlowActive = TimeSlowAbility.Instance != null && TimeSlowAbility.Instance.IsSlowMotionActive;
        
        if (isTimeSlowActive)
        {
            // Time Slow aktif - özel görünüm
            float timeRemaining = TimeSlowAbility.Instance.ActiveTimeRemaining;
            float duration = TimeSlowAbility.Instance.ActiveDuration;
            float progress = timeRemaining / duration;
            
            // Fill amount - kalan süreyi göster
            if (timeSlowSkill.cooldownFill != null)
            {
                timeSlowSkill.cooldownFill.fillAmount = progress;
                timeSlowSkill.cooldownFill.color = new Color(0.5f, 0.3f, 1f, 1f); // Parlak mor
            }
            
            // Icon - küçült ve yukarı kaydır
            if (timeSlowSkill.icon != null)
            {
                timeSlowSkill.icon.color = new Color(0.8f, 0.6f, 1f, 1f); // Açık mor
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.15f;
                timeSlowSkill.icon.transform.localScale = Vector3.one * pulse * 0.7f;
                timeSlowSkill.icon.rectTransform.anchoredPosition = new Vector2(0, 8f);
            }
            
            // Kalan süreyi göster
            if (timeSlowSkill.cooldownText != null)
            {
                timeSlowSkill.cooldownText.text = timeRemaining.ToString("F1");
                timeSlowSkill.cooldownText.color = new Color(0.8f, 0.6f, 1f, 1f);
                timeSlowSkill.cooldownText.fontSize = 18;
                timeSlowSkill.cooldownText.rectTransform.anchoredPosition = new Vector2(0, -8f);
            }
        }
        else
        {
            // Normal mod - pozisyonları sıfırla
            if (timeSlowSkill.icon != null)
            {
                timeSlowSkill.icon.rectTransform.anchoredPosition = Vector2.zero;
            }
            if (timeSlowSkill.cooldownText != null)
            {
                timeSlowSkill.cooldownText.rectTransform.anchoredPosition = Vector2.zero;
                timeSlowSkill.cooldownText.fontSize = 24;
            }
            
            // Standart skill icon güncelleme
            UpdateSkillIcon(timeSlowSkill, skillSystem.TimeSlowCooldownProgress, skillSystem.IsTimeSlowReady,
                skillSystem.CurrentSkill == GuitarSkillSystem.SkillType.TimeSlow);
        }
    }

    private void UpdateFireballSkillIcon()
    {
        if (fireballSkill == null || fireballSkill.container == null) return;
        
        bool isFireballModeActive = playerAttack != null && playerAttack.IsFireballModeActive;
        
        if (isFireballModeActive)
        {
            // Fireball modu aktif - özel görünüm
            float timeRemaining = playerAttack.FireballModeTimeRemaining;
            float progress = timeRemaining / 10f; // 10 saniye max
            
            // Fill amount - kalan süreyi göster
            if (fireballSkill.cooldownFill != null)
            {
                fireballSkill.cooldownFill.fillAmount = progress;
                fireballSkill.cooldownFill.color = new Color(1f, 0.3f, 0.1f, 1f); // Parlak turuncu
            }
            
            // Icon - küçült ve yukarı kaydır (süre yazısına yer aç)
            if (fireballSkill.icon != null)
            {
                fireballSkill.icon.color = new Color(1f, 0.8f, 0.3f, 1f); // Altın sarısı
                float pulse = 1f + Mathf.Sin(Time.time * 8f) * 0.15f;
                fireballSkill.icon.transform.localScale = Vector3.one * pulse * 0.7f; // Küçült
                fireballSkill.icon.rectTransform.anchoredPosition = new Vector2(0, 8f); // Yukarı kaydır
            }
            
            // Kalan süreyi ve sayıyı göster (altta)
            if (fireballSkill.cooldownText != null)
            {
                int charges = skillSystem.FireballCharges;
                fireballSkill.cooldownText.text = timeRemaining.ToString("F1"); // Sadece süre
                fireballSkill.cooldownText.color = new Color(1f, 0.9f, 0.3f, 1f); // Sarı
                fireballSkill.cooldownText.fontSize = 18;
                fireballSkill.cooldownText.rectTransform.anchoredPosition = new Vector2(0, 45f); // İkonun üstünde
            }
        }
        else
        {
            // Normal mod - sayıyı ikonun üstünde göster
            if (fireballSkill.icon != null)
            {
                fireballSkill.icon.rectTransform.anchoredPosition = Vector2.zero; // Ortada
            }
            if (fireballSkill.cooldownText != null)
            {
                fireballSkill.cooldownText.rectTransform.anchoredPosition = new Vector2(0, 45f); // İkonun üstünde
                fireballSkill.cooldownText.fontSize = 18;
            }
            
            // Standart skill icon güncelleme - sayı bazlı
            int charges = skillSystem.FireballCharges;
            bool isReady = skillSystem.IsFireballReady;
            bool isActive = skillSystem.CurrentSkill == GuitarSkillSystem.SkillType.Fireball;
            
            if (fireballSkill.cooldownFill != null)
            {
                fireballSkill.cooldownFill.fillAmount = isReady ? 1f : 0f;
                
                if (isActive)
                    fireballSkill.cooldownFill.color = activeColor;
                else if (isReady)
                    fireballSkill.cooldownFill.color = fireballSkill.skillColor;
                else
                    fireballSkill.cooldownFill.color = cooldownColor;
            }
            
            if (fireballSkill.cooldownText != null)
            {
                fireballSkill.cooldownText.text = charges.ToString();
                fireballSkill.cooldownText.color = isReady ? readyColor : cooldownColor;
            }
            
            if (fireballSkill.icon != null)
            {
                if (isActive)
                {
                    fireballSkill.icon.color = activeColor;
                    float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.15f;
                    fireballSkill.icon.transform.localScale = Vector3.one * pulse;
                }
                else if (isReady)
                {
                    fireballSkill.icon.color = readyColor;
                    float breathe = 1f + Mathf.Sin(Time.time * 2f) * 0.05f;
                    fireballSkill.icon.transform.localScale = Vector3.one * breathe;
                }
                else
                {
                    fireballSkill.icon.color = cooldownColor;
                    fireballSkill.icon.transform.localScale = Vector3.one;
                }
            }
        }
    }

    private void UpdateSkillIcon(SkillIcon skill, float progress, bool isReady, bool isActive)
    {
        if (skill == null || skill.container == null) return;

        // Fill amount
        if (skill.cooldownFill != null)
        {
            skill.cooldownFill.fillAmount = progress;
            
            if (isActive)
                skill.cooldownFill.color = activeColor;
            else if (isReady)
                skill.cooldownFill.color = skill.skillColor;
            else
                skill.cooldownFill.color = cooldownColor;
        }

        // Icon Animations & Visuals
        if (skill.icon != null)
        {
            if (isActive)
            {
                skill.icon.color = activeColor;
                // Active Pulse Animation
                float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.15f;
                skill.icon.transform.localScale = Vector3.one * pulse;
            }
            else if (isReady)
            {
                skill.icon.color = readyColor;
                
                // Ready "Breathing" Animation
                float breathe = 1f + Mathf.Sin(Time.time * 2f) * 0.05f;
                skill.icon.transform.localScale = Vector3.one * breathe;
            }
            else
            {
                skill.icon.color = cooldownColor;
                skill.icon.transform.localScale = Vector3.one;
            }
        }

        // Pop effect when becoming ready
        if (isReady && !skill.wasReadyLastFrame)
        {
            StartCoroutine(PopEffect(skill.container));
        }
        skill.wasReadyLastFrame = isReady;

        // Cooldown text
        if (skill.cooldownText != null)
        {
            if (!isReady && !isActive)
            {
                float remaining = GetCooldownRemaining(skill);
                skill.cooldownText.text = remaining.ToString("F1");
            }
            else
            {
                skill.cooldownText.text = "";
            }
        }
    }

    private void UpdateUltimateSkillIcon()
    {
        if (ultimateSkill == null || ultimateSkill.container == null) return;

        // UltimateAbility.Instance kontrolü yapılıyor [1]
        bool isUltimateActive = UltimateAbility.Instance != null && UltimateAbility.Instance.IsUltimateActive;

        if (isUltimateActive)
        {
            float timeRemaining = UltimateAbility.Instance.ActiveTimeRemaining; // [3]
            float duration = UltimateAbility.Instance.ActiveDuration;
            float progress = timeRemaining / duration;

            if (ultimateSkill.cooldownFill != null)
            {
                ultimateSkill.cooldownFill.fillAmount = progress;
                ultimateSkill.cooldownFill.color = new Color(1f, 0.2f, 0.2f, 1f); // Ulti için parlak kırmızı
            }

            if (ultimateSkill.icon != null)
            {
                ultimateSkill.icon.color = new Color(1f, 0.5f, 0.5f, 1f);
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.2f;
                ultimateSkill.icon.transform.localScale = Vector3.one * pulse * 0.7f;
                ultimateSkill.icon.rectTransform.anchoredPosition = new Vector2(0, 8f);
            }

            if (ultimateSkill.cooldownText != null)
            {
                ultimateSkill.cooldownText.text = timeRemaining.ToString("F1");
                ultimateSkill.cooldownText.fontSize = 18;
                ultimateSkill.cooldownText.rectTransform.anchoredPosition = new Vector2(0, -8f);
            }
        }
        else
        {
            // Normal moda dönme ve cooldown gösterme [5]
            if (ultimateSkill.icon != null)
            {
                ultimateSkill.icon.rectTransform.anchoredPosition = Vector2.zero;
                ultimateSkill.icon.transform.localScale = Vector3.one;
            }

            if (ultimateSkill.cooldownText != null)
            {
                ultimateSkill.cooldownText.rectTransform.anchoredPosition = Vector2.zero;
                ultimateSkill.cooldownText.fontSize = 24;
            }

            // DÜZELTME: Ultimate artık 7 kill bazlı - SoulSystem'den al
            bool isReady = skillSystem.IsUltimateReady;
            bool isActive = skillSystem.CurrentSkill == GuitarSkillSystem.SkillType.Ultimate;
            
            // Kill sayısını göster
            int currentKills = SoulSystem.Instance != null ? SoulSystem.Instance.CurrentKills : 0;
            int requiredKills = SoulSystem.Instance != null ? SoulSystem.Instance.KillsRequired : 7;
            float progress = (float)currentKills / requiredKills;
            
            if (ultimateSkill.cooldownFill != null)
            {
                ultimateSkill.cooldownFill.fillAmount = progress;
                
                if (isActive)
                    ultimateSkill.cooldownFill.color = activeColor;
                else if (isReady)
                    ultimateSkill.cooldownFill.color = ultimateSkill.skillColor;
                else
                    ultimateSkill.cooldownFill.color = cooldownColor;
            }
            
            if (ultimateSkill.cooldownText != null)
            {
                ultimateSkill.cooldownText.text = isReady ? "!" : $"{currentKills}/{requiredKills}";
                ultimateSkill.cooldownText.color = isReady ? readyColor : cooldownColor;
            }
            
            if (ultimateSkill.icon != null)
            {
                if (isActive)
                {
                    ultimateSkill.icon.color = activeColor;
                    float pulse = 1f + Mathf.Sin(Time.time * 10f) * 0.15f;
                    ultimateSkill.icon.transform.localScale = Vector3.one * pulse;
                }
                else if (isReady)
                {
                    ultimateSkill.icon.color = readyColor;
                    // Ultimate hazır - parlama efekti
                    float breathe = 1f + Mathf.Sin(Time.time * 4f) * 0.1f;
                    ultimateSkill.icon.transform.localScale = Vector3.one * breathe;
                }
                else
                {
                    ultimateSkill.icon.color = cooldownColor;
                    ultimateSkill.icon.transform.localScale = Vector3.one;
                }
            }
        }
    }

    private IEnumerator PopEffect(RectTransform target)
    {
        float duration = 0.2f;
        float time = 0;
        
        Vector3 originalScale = Vector3.one; 
        // Note: Assuming original scale is roughly 1. 
        // If the generate UI sets sizeDelta, scale is usually 1.

        while (time < duration)
        {
            float t = time / duration;
            // Scale up to 1.3 and back to 1
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.3f;
            target.localScale = originalScale * scale;
            time += Time.deltaTime;
            yield return null;
        }
        target.localScale = originalScale;
    }

    
    [ContextMenu("Generate UI Layout")]
    public void GenerateUIEditor()
    {
        // Temizle
        var children = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        children.ForEach(child => DestroyImmediate(child));
        
        CreateUI();
        Debug.Log("UI Generated for Editing. You can now move objects in Hierarchy.");
    }
    
    private bool FindExistingUI()
    {
        Transform canvasTrans = transform.Find("SkillCooldownUI_Canvas");
        if (canvasTrans == null) return false;
        
        canvas = canvasTrans.GetComponent<Canvas>();
        
        healSkill = FindSkillIcon(canvasTrans, "Heal", healColor);
        fireballSkill = FindSkillIcon(canvasTrans, "Fireball", fireballColor);
        timeSlowSkill = FindSkillIcon(canvasTrans, "TimeSlow", timeSlowColor);
        ultimateSkill = FindSkillIcon(canvasTrans, "Ultimate", ultimateColor);        
        // Spriteları yenile (Editor'de yaratılanlar Play modunda kaybolabilir)
        RefreshSkillSprites(healSkill, "Heal");
        RefreshSkillSprites(fireballSkill, "Fireball");
        RefreshSkillSprites(timeSlowSkill, "TimeSlow");
        RefreshSkillSprites(ultimateSkill, "Ultimate");

        return healSkill != null && fireballSkill != null && timeSlowSkill != null && ultimateSkill != null;
    }

    private SkillIcon FindSkillIcon(Transform parent, string name, Color color)
    {
        Transform container = parent.Find($"Skill_{name}");
        if (container == null) return null;

        SkillIcon skill = new SkillIcon();
        skill.skillColor = color;
        skill.container = container.GetComponent<RectTransform>();
        
        Transform bg = container.Find("Background");
        if (bg) skill.background = bg.GetComponent<Image>();
        
        Transform fill = container.Find("CooldownFill");
        if (fill) skill.cooldownFill = fill.GetComponent<Image>();
        
        Transform icon = container.Find("Icon");
        if (icon) skill.icon = icon.GetComponent<Image>();
        
        Transform keyObj = container.Find("KeyText");
        if (keyObj) skill.keyText = keyObj.GetComponent<TextMeshProUGUI>();
        
        Transform cdObj = container.Find("CooldownText");
        if (cdObj) skill.cooldownText = cdObj.GetComponent<TextMeshProUGUI>();
        
        return skill;
    }

    private void RefreshSkillSprites(SkillIcon skill, string name)
    {
        if (skill == null) return;
        if (skill.background) skill.background.sprite = CreateCircleSprite(64);
        if (skill.cooldownFill) skill.cooldownFill.sprite = CreateCircleSprite(64);
        if (skill.icon) skill.icon.sprite = CreateSkillIconSprite(name, 48);
    }

    private float GetCooldownRemaining(SkillIcon skill)
    {
        if (skillSystem == null) return 0f;
        
        // Heal ve Fireball artık sayı bazlı - cooldown yok
        if (skill == healSkill)
            return skillSystem.HealCharges; // Kalan sayı
        else if (skill == fireballSkill)
            return skillSystem.FireballCharges; // Kalan sayı
        else if (skill == timeSlowSkill)
            return (1f - skillSystem.TimeSlowCooldownProgress) * 15f; // timeSlowCooldown
        else if (skill == ultimateSkill)
        {
            // Ultimate için kalan kill sayısı
            int current = SoulSystem.Instance != null ? SoulSystem.Instance.CurrentKills : 0;
            int required = SoulSystem.Instance != null ? SoulSystem.Instance.KillsRequired : 7;
            return required - current;
        }
        
        return 0f;
    }

    private Sprite CreateRingSprite(int size, float thicknessPercent)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;
        float innerRadius = radius * (1f - thicknessPercent);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius && dist >= innerRadius)
                {
                    // Antialiasing
                    float alpha = 1f;
                    if (dist > radius - 1f) alpha = radius - dist;
                    else if (dist < innerRadius + 1f) alpha = dist - innerRadius;
                    
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
    
    private Sprite CreateCircleSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                {
                    float alpha = Mathf.Clamp01((radius - dist) * 2f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateSkillIconSprite(string skillName, int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        
        // Clear
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;

        switch (skillName)
        {
            case "Heal":
                // Artı işareti çiz (daha kalın)
                DrawPlus(pixels, size, center, size * 0.35f, size * 0.15f);
                break;
            case "Fireball":
                // Güneş/Patlama efekti (gelişmiş)
                DrawFireball(pixels, size, center, size * 0.42f);
                break;
            case "TimeSlow":
                // Saat ikonu (zaman yavaşlatma)
                DrawClock(pixels, size, center, size * 0.4f);
                break;
            case "Shockwave":
                // Dalga/patlama çiz
                DrawShockwave(pixels, size, center, size * 0.4f);
                break;
            case "Ultimate":
                // Dalga/patlama çiz
                DrawShockwave(pixels, size, center, size * 0.4f);
                break;
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private void DrawPlus(Color[] pixels, int size, Vector2 center, float length, float thickness)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                
                // Yatay çizgi (rounded edges için dist check eklenebilir ama basit tutalım)
                if (Mathf.Abs(pos.y) <= thickness && Mathf.Abs(pos.x) <= length)
                {
                    pixels[y * size + x] = Color.white;
                }
                // Dikey çizgi
                else if (Mathf.Abs(pos.x) <= thickness && Mathf.Abs(pos.y) <= length)
                {
                    pixels[y * size + x] = Color.white;
                }
            }
        }
    }

    private void DrawFireball(Color[] pixels, int size, Vector2 center, float radius)
    {
        // Meteor/Ateş topu ikonu - daire + kuyruk
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                float dist = pos.magnitude;
                
                // Ana ateş topu (daire) - biraz sağa kaydır
                Vector2 fireballCenter = new Vector2(radius * 0.2f, 0);
                float fireballDist = (pos - fireballCenter).magnitude;
                float fireballRadius = radius * 0.55f;
                
                if (fireballDist <= fireballRadius)
                {
                    // Kenarları yumuşat
                    float alpha = 1f;
                    if (fireballDist > fireballRadius - 2f) 
                        alpha = (fireballRadius - fireballDist) / 2f;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
                    continue;
                }
                
                // Kuyruk (sol tarafa doğru uzayan üçgen)
                // Kuyruk başlangıcı: ateş topunun sol kenarı
                float tailStartX = -radius * 0.3f;
                float tailEndX = -radius * 0.9f;
                float tailWidth = radius * 0.5f;
                
                if (pos.x >= tailEndX && pos.x <= tailStartX)
                {
                    // Kuyruk genişliği sola doğru daralır
                    float progress = (pos.x - tailEndX) / (tailStartX - tailEndX); // 0 = sol, 1 = sağ
                    float maxY = tailWidth * progress * 0.5f; // Sola doğru daralır
                    
                    if (Mathf.Abs(pos.y) <= maxY)
                    {
                        // Kuyruk için gradient alpha
                        float alpha = progress * 0.9f + 0.1f;
                        pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                }
            }
        }
    }

    private void DrawShockwave(Color[] pixels, int size, Vector2 center, float radius)
    {
        // Shockwave - genişleyen dalgalar (3 halka)
        float[] ringRadii = { radius * 0.4f, radius * 0.7f, radius * 1.0f };
        float ringThickness = radius * 0.12f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                
                foreach (float ringRadius in ringRadii)
                {
                    if (Mathf.Abs(dist - ringRadius) <= ringThickness)
                    {
                        pixels[y * size + x] = Color.white;
                        break;
                    }
                }
            }
        }
    }

    private void DrawClock(Color[] pixels, int size, Vector2 center, float radius)
    {
        // Saat ikonu çiz (zaman yavaşlatma için)
        float ringThickness = radius * 0.15f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                float dist = pos.magnitude;
                
                // Dış çember (saat kadranı)
                if (Mathf.Abs(dist - radius) <= ringThickness)
                {
                    pixels[y * size + x] = Color.white;
                    continue;
                }
                
                // Saat akrep (dikey - 12'yi gösterir)
                if (Mathf.Abs(pos.x) <= radius * 0.08f && pos.y > 0 && pos.y < radius * 0.7f)
                {
                    pixels[y * size + x] = Color.white;
                    continue;
                }
                
                // Yelkovan (yatay-çapraz - 3'ü gösterir)
                if (pos.x > 0 && pos.x < radius * 0.5f && Mathf.Abs(pos.y) <= radius * 0.08f)
                {
                    pixels[y * size + x] = Color.white;
                    continue;
                }
                
                // Merkez nokta
                if (dist <= radius * 0.12f)
                {
                    pixels[y * size + x] = Color.white;
                }
            }
        }
    }
}
