using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Button))]
public class MenuButtonAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Header("Scale Animation")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float animationSpeed = 8f;

    [Header("Position Animation")]
    [SerializeField] private float hoverOffsetX = 20f;
    [SerializeField] private bool usePositionAnimation = true;

    [Header("Glow Effect")]
    [SerializeField] private bool useGlowEffect = true;
    [SerializeField] private Color glowColor = new Color(0.8f, 0.4f, 1f, 1f);  // Mor glow
    [SerializeField] private float glowIntensity = 1.5f;

    [Header("Text Color Animation")]
    [SerializeField] private bool useTextColorAnimation = true;
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color hoverTextColor = new Color(1f, 0.6f, 0.9f, 1f);  // Pembe-mor hover

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float soundVolume = 0.5f;

    private Button button;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Vector3 targetScale;
    private Vector3 targetPosition;
    private bool isHovered = false;
    private bool isSelected = false;

    // Text components
    private Text uiText;
    private TMPro.TextMeshProUGUI tmpText;
    private Color originalTextColor;

    // Glow
    private Outline outline;
    private Shadow shadow;

    // Audio
    private AudioSource audioSource;

    private void Awake()
    {
        button = GetComponent<Button>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
        originalPosition = rectTransform.anchoredPosition;
        targetScale = originalScale;
        targetPosition = originalPosition;

        // Text bileşenlerini bul
        uiText = GetComponentInChildren<Text>();
        tmpText = GetComponentInChildren<TMPro.TextMeshProUGUI>();

        if (uiText != null)
            originalTextColor = uiText.color;
        else if (tmpText != null)
            originalTextColor = tmpText.color;

        // Glow için Outline ekle
        if (useGlowEffect)
        {
            SetupGlowEffect();
        }

        // Audio source
        SetupAudio();

        // Click listener
        button.onClick.AddListener(OnClick);
    }

    private void SetupGlowEffect()
    {
        // Outline ekle (yoksa)
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        outline.effectDistance = new Vector2(3, 3);

        // Shadow ekle
        shadow = GetComponent<Shadow>();
        if (shadow == null && GetComponents<Shadow>().Length < 2)
        {
            shadow = gameObject.AddComponent<Shadow>();
        }
        if (shadow != null)
        {
            shadow.effectColor = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
            shadow.effectDistance = new Vector2(5, -5);
        }
    }

    private void SetupAudio()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    private void Update()
    {
        // Smooth scale animation
        rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, targetScale, Time.unscaledDeltaTime * animationSpeed);

        // Smooth position animation
        if (usePositionAnimation)
        {
            rectTransform.anchoredPosition = Vector3.Lerp(rectTransform.anchoredPosition, targetPosition, Time.unscaledDeltaTime * animationSpeed);
        }

        // Glow animation
        if (useGlowEffect && outline != null)
        {
            float targetAlpha = (isHovered || isSelected) ? 0.8f : 0f;
            Color currentColor = outline.effectColor;
            currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, Time.unscaledDeltaTime * animationSpeed);
            outline.effectColor = currentColor;

            if (shadow != null)
            {
                Color shadowColor = shadow.effectColor;
                shadowColor.a = Mathf.Lerp(shadowColor.a, targetAlpha * 0.5f, Time.unscaledDeltaTime * animationSpeed);
                shadow.effectColor = shadowColor;
            }
        }

        // Text color animation
        if (useTextColorAnimation)
        {
            Color targetColor = (isHovered || isSelected) ? hoverTextColor : originalTextColor;

            if (tmpText != null)
            {
                tmpText.color = Color.Lerp(tmpText.color, targetColor, Time.unscaledDeltaTime * animationSpeed);
            }
            else if (uiText != null)
            {
                uiText.color = Color.Lerp(uiText.color, targetColor, Time.unscaledDeltaTime * animationSpeed);
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;

        isHovered = true;
        SetHoverState(true);
        PlaySound(hoverSound);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        if (!isSelected)
        {
            SetHoverState(false);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!button.interactable) return;

        isSelected = true;
        SetHoverState(true);
        PlaySound(hoverSound);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        isSelected = false;
        if (!isHovered)
        {
            SetHoverState(false);
        }
    }

    private void SetHoverState(bool hover)
    {
        if (hover)
        {
            targetScale = originalScale * hoverScale;
            if (usePositionAnimation)
            {
                targetPosition = originalPosition + new Vector3(hoverOffsetX, 0f, 0f);
            }
        }
        else
        {
            targetScale = originalScale;
            targetPosition = originalPosition;
        }
    }

    private void OnClick()
    {
        PlaySound(clickSound);
        StartCoroutine(ClickAnimation());
    }

    private IEnumerator ClickAnimation()
    {
        // Küçült
        targetScale = originalScale * clickScale;
        yield return new WaitForSecondsRealtime(0.1f);

        // Geri büyüt
        if (isHovered || isSelected)
        {
            targetScale = originalScale * hoverScale;
        }
        else
        {
            targetScale = originalScale;
        }
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
        // Reset states
        rectTransform.localScale = originalScale;
        rectTransform.anchoredPosition = originalPosition;
        isHovered = false;
        isSelected = false;
    }

    // Inspector'dan ayarları değiştirince güncelle
    private void OnValidate()
    {
        if (outline != null)
        {
            outline.effectColor = glowColor;
        }
    }
}
