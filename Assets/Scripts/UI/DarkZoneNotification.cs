using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Dark Zone'da olduğunuz sürece sürekli görünen bildirim.
/// Otomatik olarak çalışır - hiçbir şey bağlamana gerek yok.
/// Yuvarlak köşeli, modern tasarım.
/// Sahne değişikliğinde otomatik sıfırlanır.
/// </summary>
public class DarkZoneNotification : MonoBehaviour
{
    public static DarkZoneNotification Instance { get; private set; }
    
    [Header("Mesajlar")]
    [SerializeField] private string darkZoneMessage = "YOU ARE IN DARK ZONE";
    [SerializeField] private string subMessage = "Use your radar to detect enemies";
    
    [Header("Görünüm")]
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    [Header("Renkler")]
    [SerializeField] private Color mainTextColor = new Color(1f, 0.85f, 0.85f, 1f); // Açık kırmızımsı beyaz
    [SerializeField] private Color subTextColor = new Color(0.7f, 0.7f, 0.75f, 1f); // Soft gri
    [SerializeField] private Color backgroundColor = new Color(0.1f, 0.05f, 0.15f, 0.92f); // Koyu mor-siyah
    [SerializeField] private Color borderColor = new Color(0.8f, 0.2f, 0.3f, 0.8f); // Kırmızı kenar
    [SerializeField] private Color accentColor = new Color(1f, 0.3f, 0.4f, 1f); // Accent kırmızı
    
    // UI Elements
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Text mainText;
    private Text subText;
    private Image backgroundImage;
    private Image borderImage;
    private Image iconImage;
    private Image leftAccent;
    private Image rightAccent;
    
    // State
    private Coroutine showCoroutine;
    private bool isShowing = false;
    private bool wasInDarkZone = false; // Önceki frame'de karanlık bölgede miydi?
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateUI();
            
            // Sahne değişikliğini dinle
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        if (Instance == this)
            Instance = null;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Bildirimi hemen gizle
        HideNotification();
        wasInDarkZone = false;
    }
    
    private void Update()
    {
        // SimpleDarkOverlay aktif mi kontrol et
        bool isInDarkZone = SimpleDarkOverlay.Instance != null && SimpleDarkOverlay.Instance.IsActive;
        
        // Karanlık bölgeye girdiyse göster
        if (isInDarkZone && !wasInDarkZone)
        {
            ShowDarkZoneNotification();
        }
        // Karanlık bölgeden çıktıysa gizle
        else if (!isInDarkZone && wasInDarkZone)
        {
            HideDarkZoneNotification();
        }
        
        // Pulse efekti (karanlık bölgedeyken)
        if (isShowing && isInDarkZone)
        {
            UpdatePulseEffect();
        }
        
        wasInDarkZone = isInDarkZone;
    }
    
    private void UpdatePulseEffect()
    {
        if (leftAccent != null && rightAccent != null)
        {
            float pulse = 0.7f + Mathf.Sin(Time.unscaledTime * 3f) * 0.3f;
            Color c = accentColor;
            c.a = pulse;
            leftAccent.color = c;
            rightAccent.color = c;
        }
    }
    
    private void CreateUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("DarkZoneNotificationCanvas");
        canvasObj.transform.SetParent(transform);
        
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Ana Container (ekranın üst-orta kısmında)
        GameObject container = new GameObject("Container");
        container.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRT = container.AddComponent<RectTransform>();
        containerRT.anchorMin = new Vector2(0.5f, 0.75f);
        containerRT.anchorMax = new Vector2(0.5f, 0.75f);
        containerRT.sizeDelta = new Vector2(600, 120);
        containerRT.anchoredPosition = Vector2.zero;
        
        // Yuvarlak köşeli arka plan - Sprite kullanarak
        CreateRoundedBackground(container.transform);
        
        // Sol accent çizgi (dikey)
        GameObject leftAccentObj = new GameObject("LeftAccent");
        leftAccentObj.transform.SetParent(container.transform, false);
        leftAccent = leftAccentObj.AddComponent<Image>();
        leftAccent.color = accentColor;
        RectTransform leftRT = leftAccentObj.GetComponent<RectTransform>();
        leftRT.anchorMin = new Vector2(0, 0.15f);
        leftRT.anchorMax = new Vector2(0, 0.85f);
        leftRT.sizeDelta = new Vector2(4, 0);
        leftRT.anchoredPosition = new Vector2(15, 0);
        
        // Sağ accent çizgi (dikey)
        GameObject rightAccentObj = new GameObject("RightAccent");
        rightAccentObj.transform.SetParent(container.transform, false);
        rightAccent = rightAccentObj.AddComponent<Image>();
        rightAccent.color = accentColor;
        RectTransform rightRT = rightAccentObj.GetComponent<RectTransform>();
        rightRT.anchorMin = new Vector2(1, 0.15f);
        rightRT.anchorMax = new Vector2(1, 0.85f);
        rightRT.sizeDelta = new Vector2(4, 0);
        rightRT.anchoredPosition = new Vector2(-15, 0);
        
        // Warning Icon (⚠ benzeri)
        GameObject iconObj = new GameObject("WarningIcon");
        iconObj.transform.SetParent(container.transform, false);
        Text iconText = iconObj.AddComponent<Text>();
        iconText.text = "⚠";
        iconText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconText.fontSize = 28;
        iconText.color = accentColor;
        iconText.alignment = TextAnchor.MiddleCenter;
        RectTransform iconRT = iconObj.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.5f, 0.7f);
        iconRT.anchorMax = new Vector2(0.5f, 0.95f);
        iconRT.sizeDelta = new Vector2(50, 30);
        iconRT.anchoredPosition = Vector2.zero;
        
        // Ana Başlık - "KARANLIK BÖLGEDESINIZ"
        GameObject mainTextObj = new GameObject("MainText");
        mainTextObj.transform.SetParent(container.transform, false);
        mainText = mainTextObj.AddComponent<Text>();
        mainText.text = darkZoneMessage;
        mainText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        mainText.fontSize = 32;
        mainText.fontStyle = FontStyle.Bold;
        mainText.color = mainTextColor;
        mainText.alignment = TextAnchor.MiddleCenter;
        
        // Letter spacing efekti için horizontalOverflow
        mainText.horizontalOverflow = HorizontalWrapMode.Overflow;
        mainText.verticalOverflow = VerticalWrapMode.Overflow;
        
        RectTransform mainTextRT = mainTextObj.GetComponent<RectTransform>();
        mainTextRT.anchorMin = new Vector2(0.05f, 0.35f);
        mainTextRT.anchorMax = new Vector2(0.95f, 0.72f);
        mainTextRT.offsetMin = Vector2.zero;
        mainTextRT.offsetMax = Vector2.zero;
        
        // Glow efekti için shadow
        var mainShadow = mainTextObj.AddComponent<Shadow>();
        mainShadow.effectColor = new Color(1f, 0.2f, 0.3f, 0.5f);
        mainShadow.effectDistance = new Vector2(0, 0);
        
        // Outline
        var mainOutline = mainTextObj.AddComponent<Outline>();
        mainOutline.effectColor = new Color(0, 0, 0, 0.6f);
        mainOutline.effectDistance = new Vector2(1.5f, -1.5f);
        
        // Alt Yazı - "Use your radar to detect enemies"
        GameObject subTextObj = new GameObject("SubText");
        subTextObj.transform.SetParent(container.transform, false);
        subText = subTextObj.AddComponent<Text>();
        subText.text = subMessage;
        subText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subText.fontSize = 18;
        subText.fontStyle = FontStyle.Normal;
        subText.color = subTextColor;
        subText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform subTextRT = subTextObj.GetComponent<RectTransform>();
        subTextRT.anchorMin = new Vector2(0.1f, 0.08f);
        subTextRT.anchorMax = new Vector2(0.9f, 0.35f);
        subTextRT.offsetMin = Vector2.zero;
        subTextRT.offsetMax = Vector2.zero;
        
        // Radar hint küçük ikon
        GameObject radarHint = new GameObject("RadarHint");
        radarHint.transform.SetParent(container.transform, false);
        Text radarText = radarHint.AddComponent<Text>();
        radarText.text = "◉";
        radarText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        radarText.fontSize = 14;
        radarText.color = new Color(0.4f, 1f, 0.5f, 0.9f); // Yeşil
        radarText.alignment = TextAnchor.MiddleRight;
        RectTransform radarRT = radarHint.GetComponent<RectTransform>();
        radarRT.anchorMin = new Vector2(0.85f, 0.08f);
        radarRT.anchorMax = new Vector2(0.95f, 0.35f);
        radarRT.offsetMin = Vector2.zero;
        radarRT.offsetMax = Vector2.zero;
    }
    
    private void CreateRoundedBackground(Transform parent)
    {
        // Border (dış çerçeve) - biraz daha büyük
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(parent, false);
        borderImage = borderObj.AddComponent<Image>();
        borderImage.color = borderColor;
        
        // Yuvarlak köşeler için sprite oluştur
        borderImage.sprite = CreateRoundedSprite(32);
        borderImage.type = Image.Type.Sliced;
        borderImage.pixelsPerUnitMultiplier = 1f;
        
        RectTransform borderRT = borderObj.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero;
        borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-3, -3);
        borderRT.offsetMax = new Vector2(3, 3);
        
        // İç arka plan
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(parent, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        
        // Yuvarlak köşeler
        backgroundImage.sprite = CreateRoundedSprite(28);
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.pixelsPerUnitMultiplier = 1f;
        
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
    }
    
    /// <summary>
    /// Yuvarlak köşeli sprite oluştur
    /// </summary>
    private Sprite CreateRoundedSprite(int radius)
    {
        int size = radius * 3;
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Color white = Color.white;
        Color clear = Color.clear;
        
        int center = size / 2;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Köşelerde yuvarlak, kenarlarda düz
                float dist = 0f;
                
                // Sol alt köşe
                if (x < radius && y < radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius));
                    pixels[y * size + x] = dist <= radius ? white : clear;
                }
                // Sağ alt köşe
                else if (x >= size - radius && y < radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - radius - 1, radius));
                    pixels[y * size + x] = dist <= radius ? white : clear;
                }
                // Sol üst köşe
                else if (x < radius && y >= size - radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, size - radius - 1));
                    pixels[y * size + x] = dist <= radius ? white : clear;
                }
                // Sağ üst köşe
                else if (x >= size - radius && y >= size - radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(size - radius - 1, size - radius - 1));
                    pixels[y * size + x] = dist <= radius ? white : clear;
                }
                // Kenarlar ve orta
                else
                {
                    pixels[y * size + x] = white;
                }
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        // 9-slice için border ayarla
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }
    
    /// <summary>
    /// Karanlık bölge bildirimini göster (sürekli görünür kalır)
    /// </summary>
    public void ShowDarkZoneNotification()
    {
        if (showCoroutine != null)
            StopCoroutine(showCoroutine);
        
        showCoroutine = StartCoroutine(FadeInRoutine());
    }
    
    /// <summary>
    /// Karanlık bölge bildirimini gizle
    /// </summary>
    public void HideDarkZoneNotification()
    {
        if (showCoroutine != null)
            StopCoroutine(showCoroutine);
        
        showCoroutine = StartCoroutine(FadeOutRoutine());
    }
    
    /// <summary>
    /// Dark Zone'a girildiğinde bildirimi göster (eski uyumluluk için)
    /// </summary>
    public void ShowEnterNotification()
    {
        ShowDarkZoneNotification();
    }
    
    /// <summary>
    /// Özel mesajla bildirim göster
    /// </summary>
    public void ShowNotification(string main, string sub)
    {
        if (mainText != null) mainText.text = main;
        if (subText != null) subText.text = sub;
        
        ShowDarkZoneNotification();
    }
    
    private IEnumerator FadeInRoutine()
    {
        isShowing = true;
        
        // Reset text
        if (mainText != null) mainText.text = darkZoneMessage;
        if (subText != null) subText.text = subMessage;
        
        // Slide in + Fade in
        float elapsed = 0f;
        RectTransform containerRT = canvasGroup.transform.GetChild(0).GetComponent<RectTransform>();
        Vector2 startPos = new Vector2(0, 50); // Yukarıdan gelsin
        Vector2 endPos = Vector2.zero;
        
        float startAlpha = canvasGroup.alpha;
        
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeInDuration;
            float smoothT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, smoothT);
            containerRT.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
            
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
        containerRT.anchoredPosition = endPos;
        showCoroutine = null;
        
        // Artık sürekli görünür - otomatik kapanmıyor
    }
    
    private IEnumerator FadeOutRoutine()
    {
        // Fade out + slide up
        float elapsed = 0f;
        RectTransform containerRT = canvasGroup.transform.GetChild(0).GetComponent<RectTransform>();
        Vector2 startPos = containerRT.anchoredPosition;
        Vector2 endPos = new Vector2(0, 30);
        float startAlpha = canvasGroup.alpha;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeOutDuration;
            float smoothT = t * t; // Ease in quad
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, smoothT);
            containerRT.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT);
            
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        isShowing = false;
        showCoroutine = null;
    }
    
    /// <summary>
    /// Bildirimi hemen gizle
    /// </summary>
    public void HideNotification()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }
        
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        
        isShowing = false;
    }
}
