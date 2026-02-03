using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Karanlık bölgeye girildiğinde radar hakkında bilgi veren tooltip.
/// İlk kez karanlık bölgeye girildiğinde gösterilir.
/// </summary>
public class DarkZoneTooltip : MonoBehaviour
{
    public static DarkZoneTooltip Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float displayDuration = 4f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private string tooltipText = "Karanlık Bölge!\nRadarı kullanarak düşmanları tespit et";
    [SerializeField] private bool showOnlyOnce = true;
    
    [Header("Visual")]
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
    [SerializeField] private Color textColor = new Color(0.9f, 0.95f, 1f, 1f);
    [SerializeField] private Color iconColor = new Color(0.3f, 1f, 0.5f, 1f);
    
    // UI Components
    private Canvas tooltipCanvas;
    private CanvasGroup canvasGroup;
    private Image backgroundImage;
    private Text tooltipTextUI;
    private Image radarIcon;
    
    // State
    private bool hasShownTooltip = false;
    private bool isShowing = false;
    private Coroutine displayCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        CreateTooltipUI();
    }

    private void Start()
    {
        // DarkForestEffect event'lerine abone ol (eğer event sistemi varsa)
        // Şimdilik Update'te kontrol edeceğiz
    }

    private void Update()
    {
        // Karanlık bölge aktif mi kontrol et
        if (!hasShownTooltip || !showOnlyOnce)
        {
            if (DarkForestEffect.Instance != null && DarkForestEffect.Instance.IsActive && !isShowing)
            {
                ShowTooltip();
            }
        }
    }

    private void CreateTooltipUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("DarkZoneTooltipCanvas");
        canvasObj.transform.SetParent(transform);
        
        tooltipCanvas = canvasObj.AddComponent<Canvas>();
        tooltipCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tooltipCanvas.sortingOrder = 100; // Üstte göster
        
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Container (ekranın üst ortasında)
        GameObject containerObj = new GameObject("TooltipContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 1f);
        containerRect.anchorMax = new Vector2(0.5f, 1f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0, -100);
        containerRect.sizeDelta = new Vector2(400, 80);
        
        // Background
        backgroundImage = containerObj.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        backgroundImage.raycastTarget = false;
        
        // Rounded corners efekti için sprite (yoksa düz kullan)
        
        // Horizontal Layout
        var layout = containerObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 15, 15);
        layout.spacing = 15;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        
        // Radar Icon
        GameObject iconObj = new GameObject("RadarIcon");
        iconObj.transform.SetParent(containerObj.transform, false);
        
        radarIcon = iconObj.AddComponent<Image>();
        radarIcon.color = iconColor;
        radarIcon.raycastTarget = false;
        
        // Basit radar ikonu (daire)
        radarIcon.sprite = CreateRadarIconSprite();
        
        var iconLayout = iconObj.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 40;
        iconLayout.preferredHeight = 40;
        
        // Tooltip Text
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(containerObj.transform, false);
        
        tooltipTextUI = textObj.AddComponent<Text>();
        tooltipTextUI.text = tooltipText;
        tooltipTextUI.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tooltipTextUI.fontSize = 18;
        tooltipTextUI.color = textColor;
        tooltipTextUI.alignment = TextAnchor.MiddleLeft;
        tooltipTextUI.raycastTarget = false;
        
        var textLayout = textObj.AddComponent<LayoutElement>();
        textLayout.preferredWidth = 300;
        textLayout.preferredHeight = 50;
        
        // Content Size Fitter
        var fitter = containerObj.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private Sprite CreateRadarIconSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerRadius = size / 2f - 2f;
        float innerRadius = outerRadius * 0.85f;
        
        // Dış halka
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                
                if (dist <= outerRadius && dist >= innerRadius)
                {
                    pixels[y * size + x] = Color.white;
                }
                // Ortada nokta
                else if (dist < 5f)
                {
                    pixels[y * size + x] = Color.white;
                }
                // Çapraz çizgiler
                else if (dist < outerRadius * 0.9f)
                {
                    float angle = Mathf.Atan2(y - center.y, x - center.x) * Mathf.Rad2Deg;
                    if (Mathf.Abs(angle % 90f) < 3f || Mathf.Abs((angle + 45f) % 90f) < 3f)
                    {
                        pixels[y * size + x] = new Color(1, 1, 1, 0.5f);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public void ShowTooltip()
    {
        if (isShowing) return;
        
        if (displayCoroutine != null)
            StopCoroutine(displayCoroutine);
        
        displayCoroutine = StartCoroutine(DisplayTooltipCoroutine());
    }

    private IEnumerator DisplayTooltipCoroutine()
    {
        isShowing = true;
        hasShownTooltip = true;
        
        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        
        // Bekle
        yield return new WaitForSecondsRealtime(displayDuration);
        
        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        
        isShowing = false;
    }

    /// <summary>
    /// Tooltip'i sıfırla (tekrar gösterilebilir)
    /// </summary>
    public void ResetTooltip()
    {
        hasShownTooltip = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
