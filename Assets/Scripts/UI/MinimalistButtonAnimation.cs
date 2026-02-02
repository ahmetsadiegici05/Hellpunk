using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

/// <summary>
/// Minimalist Premium Buton Animasyonu
/// - Hover'da büyüme YOK
/// - Metin önünde parlama efekti
/// - Lav turuncusu renk geçişi
/// </summary>
[RequireComponent(typeof(Button))]
public class MinimalistButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Text Color Animation")]
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color hoverTextColor = new Color(1f, 0.45f, 0.1f, 1f);  // Lav turuncusu
    [SerializeField] private float colorTransitionSpeed = 8f;

    [Header("Glow/Shine Effect")]
    [SerializeField] private bool useGlowEffect = true;
    [SerializeField] private Color glowColor = new Color(1f, 0.5f, 0.15f, 0.8f);  // Lav turuncu glow
    [SerializeField] private float glowIntensity = 2f;
    [SerializeField] private float glowAnimationSpeed = 6f;

    [Header("Underline Effect")]
    [SerializeField] private bool useUnderline = true;
    [SerializeField] private Color underlineColor = new Color(1f, 0.45f, 0.1f, 1f);
    [SerializeField] private float underlineHeight = 2f;

    [Header("Icon/Arrow Animation")]
    [SerializeField] private bool useArrowIndicator = true;
    [SerializeField] private string arrowCharacter = "► ";  // Metnin önüne eklenir
    [SerializeField] private float arrowFadeSpeed = 10f;

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float soundVolume = 0.3f;

    [Header("Click Feedback")]
    [SerializeField] private float clickPunchScale = 0.95f;
    [SerializeField] private float clickPunchDuration = 0.1f;

    private Button button;
    private RectTransform rectTransform;
    private Vector3 originalScale;

    // Text
    private TextMeshProUGUI tmpText;
    private Text uiText;
    private string originalText;
    private Color currentTextColor;

    // Glow
    private GameObject glowObject;
    private Image glowImage;
    private CanvasGroup glowCanvasGroup;

    // Underline
    private GameObject underlineObject;
    private Image underlineImage;
    private RectTransform underlineRect;

    // State
    private bool isHovered = false;
    private bool isSelected = false;
    private float targetGlowAlpha = 0f;
    private float currentUnderlineWidth = 0f;
    private float targetUnderlineWidth = 0f;
    private bool hasArrowPrefix = false;

    // Audio
    private AudioSource audioSource;

    private void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;

        // Text bileşenlerini bul
        tmpText = GetComponentInChildren<TextMeshProUGUI>();
        uiText = GetComponentInChildren<Text>();

        if (tmpText != null)
        {
            originalText = tmpText.text;
            currentTextColor = tmpText.color;
            normalTextColor = currentTextColor;
        }
        else if (uiText != null)
        {
            originalText = uiText.text;
            currentTextColor = uiText.color;
            normalTextColor = currentTextColor;
        }

        // Efektleri kur
        if (useGlowEffect)
        {
            SetupGlowEffect();
        }

        if (useUnderline)
        {
            SetupUnderline();
        }

        SetupAudio();

        button.onClick.AddListener(OnClick);
    }

    private void SetupGlowEffect()
    {
        // Text'in arkasına glow sprite ekle
        glowObject = new GameObject("TextGlow");
        glowObject.transform.SetParent(transform);
        glowObject.transform.SetAsFirstSibling(); // Text'in arkasında

        RectTransform glowRect = glowObject.AddComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0, 0.5f);
        glowRect.anchorMax = new Vector2(0, 0.5f);
        glowRect.pivot = new Vector2(0, 0.5f);
        glowRect.anchoredPosition = new Vector2(-20f, 0f);
        glowRect.sizeDelta = new Vector2(40f, 40f);

        glowImage = glowObject.AddComponent<Image>();
        glowImage.color = glowColor;
        glowImage.raycastTarget = false;

        // Soft circle sprite için gradient texture oluştur
        glowImage.sprite = CreateGlowSprite();

        glowCanvasGroup = glowObject.AddComponent<CanvasGroup>();
        glowCanvasGroup.alpha = 0f;
        glowCanvasGroup.blocksRaycasts = false;
    }

    private Sprite CreateGlowSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxRadius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = 1f - Mathf.Clamp01(distance / maxRadius);
                alpha = alpha * alpha; // Quadratic falloff for softer glow
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void SetupUnderline()
    {
        underlineObject = new GameObject("Underline");
        underlineObject.transform.SetParent(transform);

        underlineRect = underlineObject.AddComponent<RectTransform>();
        underlineRect.anchorMin = new Vector2(0, 0);
        underlineRect.anchorMax = new Vector2(0, 0);
        underlineRect.pivot = new Vector2(0, 0);
        underlineRect.anchoredPosition = new Vector2(0f, -5f);
        underlineRect.sizeDelta = new Vector2(0f, underlineHeight);

        underlineImage = underlineObject.AddComponent<Image>();
        underlineImage.color = underlineColor;
        underlineImage.raycastTarget = false;
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private void Update()
    {
        bool isActive = isHovered || isSelected;

        // Text renk animasyonu
        Color targetColor = isActive ? hoverTextColor : normalTextColor;
        currentTextColor = Color.Lerp(currentTextColor, targetColor, Time.unscaledDeltaTime * colorTransitionSpeed);

        if (tmpText != null)
        {
            tmpText.color = currentTextColor;
        }
        else if (uiText != null)
        {
            uiText.color = currentTextColor;
        }

        // Glow animasyonu
        if (useGlowEffect && glowCanvasGroup != null)
        {
            targetGlowAlpha = isActive ? 1f : 0f;
            glowCanvasGroup.alpha = Mathf.Lerp(glowCanvasGroup.alpha, targetGlowAlpha, Time.unscaledDeltaTime * glowAnimationSpeed);

            // Pulse effect when hovered
            if (isActive && glowImage != null)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.15f;
                glowImage.transform.localScale = Vector3.one * pulse * glowIntensity;
            }
        }

        // Underline animasyonu
        if (useUnderline && underlineRect != null)
        {
            float textWidth = GetTextWidth();
            targetUnderlineWidth = isActive ? textWidth : 0f;
            currentUnderlineWidth = Mathf.Lerp(currentUnderlineWidth, targetUnderlineWidth, Time.unscaledDeltaTime * colorTransitionSpeed);
            underlineRect.sizeDelta = new Vector2(currentUnderlineWidth, underlineHeight);
        }

        // Arrow indicator animasyonu
        if (useArrowIndicator)
        {
            UpdateArrowIndicator(isActive);
        }
    }

    private float GetTextWidth()
    {
        if (tmpText != null)
        {
            return tmpText.preferredWidth;
        }
        else if (uiText != null)
        {
            return uiText.preferredWidth;
        }
        return 100f;
    }

    private void UpdateArrowIndicator(bool show)
    {
        if (tmpText != null)
        {
            if (show && !hasArrowPrefix)
            {
                tmpText.text = arrowCharacter + originalText;
                hasArrowPrefix = true;
            }
            else if (!show && hasArrowPrefix)
            {
                tmpText.text = originalText;
                hasArrowPrefix = false;
            }
        }
        else if (uiText != null)
        {
            if (show && !hasArrowPrefix)
            {
                uiText.text = arrowCharacter + originalText;
                hasArrowPrefix = true;
            }
            else if (!show && hasArrowPrefix)
            {
                uiText.text = originalText;
                hasArrowPrefix = false;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;
        isHovered = true;
        isSelected = true; // Mouse hover = seçili görünsün
        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isSelected = false;
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!button.interactable) return;
        isSelected = true;
        PlaySound(hoverSound);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
    }

    private void OnClick()
    {
        PlaySound(clickSound);
        StartCoroutine(ClickAnimation());
    }

    private IEnumerator ClickAnimation()
    {
        // Küçük bir "punch" efekti - minimalist ama feedback veriyor
        Vector3 punchScale = originalScale * clickPunchScale;
        
        float elapsed = 0f;
        while (elapsed < clickPunchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / clickPunchDuration;
            rectTransform.localScale = Vector3.Lerp(originalScale, punchScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < clickPunchDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / clickPunchDuration;
            rectTransform.localScale = Vector3.Lerp(punchScale, originalScale, t);
            yield return null;
        }

        rectTransform.localScale = originalScale;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    private void OnDisable()
    {
        // Reset
        rectTransform.localScale = originalScale;
        isHovered = false;
        isSelected = false;

        if (tmpText != null && hasArrowPrefix)
        {
            tmpText.text = originalText;
            hasArrowPrefix = false;
        }
        else if (uiText != null && hasArrowPrefix)
        {
            uiText.text = originalText;
            hasArrowPrefix = false;
        }
    }

    private void OnDestroy()
    {
        if (glowObject != null) Destroy(glowObject);
        if (underlineObject != null) Destroy(underlineObject);
    }
}
