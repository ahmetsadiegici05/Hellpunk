using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Oyun içi geri bildirim UI'ı
/// - Dash yapıldığında şık "DASH" yazısı gösterir
/// </summary>
public class GameFeedbackUI : MonoBehaviour
{
    public static GameFeedbackUI Instance { get; private set; }

    [Header("Dash Text Settings")]
    [SerializeField] private Color dashTextColor = new Color(0.3f, 0.8f, 1f, 1f); // Cyan
    [SerializeField] private Color dashGlowColor = new Color(0.2f, 0.6f, 1f, 0.5f);
    [SerializeField] private float dashTextDuration = 0.4f;
    [SerializeField] private float dashTextScale = 1.15f; // Daha minimal scale

    // UI Elements
    private Canvas feedbackCanvas;
    private GameObject dashTextContainer;
    private TextMeshProUGUI dashText;
    private TextMeshProUGUI dashGlowText;
    private CanvasGroup dashCanvasGroup;

    // State
    private Coroutine dashAnimCoroutine;
    private PlayerMovement playerMovement;
    private bool wasDashing = false;

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

        CreateUI();
    }

    private void Start()
    {
        FindPlayer();
    }

    private void FindPlayer()
    {
        if (playerMovement == null)
        {
            playerMovement = PlayerMovement.Instance;
            if (playerMovement == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerMovement = player.GetComponent<PlayerMovement>();
            }
        }
    }

    private void Update()
    {
        // Player referansını kontrol et
        if (playerMovement == null)
        {
            FindPlayer();
            return;
        }

        // Dash başladığında tetikle
        bool isDashing = playerMovement.IsDashing;
        if (isDashing && !wasDashing)
        {
            ShowDashText();
        }
        wasDashing = isDashing;
    }

    private void CreateUI()
    {
        // Ana Canvas
        GameObject canvasObj = new GameObject("FeedbackUI_Canvas");
        canvasObj.transform.SetParent(transform);
        feedbackCanvas = canvasObj.AddComponent<Canvas>();
        feedbackCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        feedbackCanvas.sortingOrder = 150; // Çoğu UI'ın üstünde

        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Dash Text Container
        CreateDashText();
    }

    private void CreateDashText()
    {
        // Container
        dashTextContainer = new GameObject("DashTextContainer");
        dashTextContainer.transform.SetParent(feedbackCanvas.transform, false);
        
        RectTransform containerRect = dashTextContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0f, 80f); // Ekranın ortasından biraz yukarı
        containerRect.sizeDelta = new Vector2(200f, 50f);

        dashCanvasGroup = dashTextContainer.AddComponent<CanvasGroup>();
        dashCanvasGroup.alpha = 0f;

        // Glow Text (arka plan blur efekti için)
        GameObject glowObj = new GameObject("DashGlow");
        glowObj.transform.SetParent(dashTextContainer.transform, false);
        
        dashGlowText = glowObj.AddComponent<TextMeshProUGUI>();
        dashGlowText.text = "DASH";
        dashGlowText.fontSize = 32;
        dashGlowText.fontStyle = FontStyles.Bold;
        dashGlowText.alignment = TextAlignmentOptions.Center;
        dashGlowText.color = dashGlowColor;
        dashGlowText.characterSpacing = 8f; // Harfler arası boşluk
        
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.offsetMin = new Vector2(2f, 2f);
        glowRect.offsetMax = new Vector2(2f, 2f);

        // Ana Text
        GameObject textObj = new GameObject("DashText");
        textObj.transform.SetParent(dashTextContainer.transform, false);
        
        dashText = textObj.AddComponent<TextMeshProUGUI>();
        dashText.text = "DASH";
        dashText.fontSize = 32;
        dashText.fontStyle = FontStyles.Bold;
        dashText.alignment = TextAlignmentOptions.Center;
        dashText.color = dashTextColor;
        dashText.characterSpacing = 8f; // Harfler arası boşluk - şık görünüm
        dashText.enableVertexGradient = true;
        dashText.colorGradient = new VertexGradient(
            new Color(0.6f, 0.95f, 1f),  // Sol üst - parlak cyan
            new Color(0.4f, 0.8f, 1f),   // Sağ üst
            new Color(0.3f, 0.7f, 0.95f),// Sol alt
            new Color(0.5f, 0.85f, 1f)   // Sağ alt
        );

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        dashTextContainer.SetActive(false);
    }

    /// <summary>
    /// Dash yazısını göster
    /// </summary>
    public void ShowDashText()
    {
        if (dashAnimCoroutine != null)
            StopCoroutine(dashAnimCoroutine);

        dashAnimCoroutine = StartCoroutine(DashTextAnimation());
    }

    private IEnumerator DashTextAnimation()
    {
        dashTextContainer.SetActive(true);
        
        float elapsed = 0f;
        float fadeInDuration = 0.1f;
        float holdDuration = dashTextDuration - 0.2f;
        float fadeOutDuration = 0.1f;

        // Scale ve position başlangıç değerleri
        RectTransform containerRect = dashTextContainer.GetComponent<RectTransform>();
        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 targetScale = Vector3.one * dashTextScale;
        Vector3 endScale = Vector3.one * (dashTextScale + 0.3f);

        // Fade In + Scale Up
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeInDuration;
            float easeT = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

            dashCanvasGroup.alpha = easeT;
            containerRect.localScale = Vector3.Lerp(startScale, targetScale, easeT);

            yield return null;
        }

        dashCanvasGroup.alpha = 1f;
        containerRect.localScale = targetScale;

        // Hold
        yield return new WaitForSecondsRealtime(holdDuration);

        // Fade Out + Scale Up
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeOutDuration;
            float easeT = t * t; // Ease in quad

            dashCanvasGroup.alpha = 1f - easeT;
            containerRect.localScale = Vector3.Lerp(targetScale, endScale, easeT);

            yield return null;
        }

        dashCanvasGroup.alpha = 0f;
        dashTextContainer.SetActive(false);
    }

    /// <summary>
    /// Dışarıdan doğrudan çağrılabilir (test için)
    /// </summary>
    public void TestDashText()
    {
        ShowDashText();
    }
}
