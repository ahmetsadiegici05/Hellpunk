using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using FirstGearGames.SmoothCameraShaker;

public class RotateOnTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private string triggerTag = "Player";

    [Header("Rotate Edilecek Ana Obje")]
    [SerializeField] private Transform rotationRoot;

    [Header("Rotation Settings")]
    [SerializeField] private float rotateZAmount = 90f;
    [SerializeField] private float rotateDuration = 1.5f;

    [Header("QTE Settings")]
    [SerializeField] private int requiredPressCount = 7;
    [SerializeField] private GameObject qtePanel; // ekranda açılacak panel

    [Header("QTE UI Style")]
    [SerializeField] private Color accentColor = new Color(1f, 0.45f, 0.1f, 1f);
    [SerializeField] private Color glowColor = new Color(1f, 0.6f, 0.2f, 0.6f);

    private bool hasRotated = false;
    private Transform player;
    private Rigidbody2D playerRb;
    private Health playerHealth;

    private int currentPressCount = 0;
    private bool qteActive = false;
    
    // UI elemanları
    private GameObject qteUIRoot;
    private Image mainProgressBar;
    private Image progressGlow;
    private CanvasGroup keyPromptGroup;
    private RectTransform keyPromptRect;
    private float targetProgress = 0f;
    private float currentProgress = 0f;

    public ShakeData rotationShakeData;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasRotated) return;

        if (other.CompareTag(triggerTag))
        {
            hasRotated = true;

            player = other.transform;
            playerRb = player.GetComponent<Rigidbody2D>();
            playerHealth = player.GetComponent<Health>();

            StartCoroutine(RotateSequence());
        }
    }

    private void Update()
    {
        if (!qteActive) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            currentPressCount++;
            targetProgress = Mathf.Clamp01((float)currentPressCount / requiredPressCount);
            
            // Tuşa basma feedback
            StartCoroutine(KeyPressFeedback());
        }
        
        // Smooth progress bar animasyonu
        currentProgress = Mathf.Lerp(currentProgress, targetProgress, Time.deltaTime * 12f);
        if (mainProgressBar != null)
        {
            mainProgressBar.fillAmount = currentProgress;
            
            // Tamamlandığında renk değişimi
            if (currentProgress >= 0.99f)
            {
                mainProgressBar.color = new Color(0.4f, 1f, 0.5f, 1f); // Yeşil
                if (progressGlow != null)
                    progressGlow.color = new Color(0.3f, 1f, 0.4f, 0.4f);
            }
        }
    }
    
    private IEnumerator KeyPressFeedback()
    {
        if (keyPromptRect == null) yield break;
        
        // Hızlı punch scale
        Vector3 original = Vector3.one;
        keyPromptRect.localScale = original * 1.2f;
        
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            keyPromptRect.localScale = Vector3.Lerp(original * 1.2f, original, elapsed / 0.1f);
            yield return null;
        }
        keyPromptRect.localScale = original;
    }

    private IEnumerator RotateSequence()
    {
        PlayerMovement.Instance.lockMovement = true;
        CameraShakerHandler.Shake(rotationShakeData);

        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        currentPressCount = 0;
        currentProgress = 0f;
        targetProgress = 0f;
        requiredPressCount = 7; // Force 7 presses
        qteActive = true;
        GameManager.Instance.isRotation = true;

        CreateMinimalQTEUI();

        if (qtePanel != null)
            qtePanel.SetActive(true);

        float elapsed = 0f;

        Quaternion startRot = rotationRoot.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, rotateZAmount);

        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / rotateDuration;

            rotationRoot.rotation = Quaternion.Lerp(startRot, endRot, t);
            player.position = transform.position;

            yield return null;
        }

        rotationRoot.rotation = endRot;
        qteActive = false;

        if (qtePanel != null)
            qtePanel.SetActive(false);
            
        // Sonuç animasyonu
        yield return StartCoroutine(ShowResult(currentPressCount >= requiredPressCount));
            
        DestroyQTEUI();

        if (currentPressCount < requiredPressCount)
        {
            if (playerHealth != null)
                playerHealth.TakeDamage(playerHealth.currentHealth + 999f);
            yield break;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.lastTransformRotationValue = rotationRoot.eulerAngles.z;
            
        PlayerMovement.Instance.lockMovement = false;
        GameManager.Instance.isRotation = false;
    }
    
    private IEnumerator ShowResult(bool success)
    {
        yield return new WaitForSeconds(0.2f);
    }
    
    private void CreateMinimalQTEUI()
    {
        qteUIRoot = new GameObject("QTE_UI");
        Canvas canvas = qteUIRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = qteUIRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        // Ana container - ekranın ortasında, biraz aşağıda
        GameObject container = new GameObject("Container");
        container.transform.SetParent(qteUIRoot.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0, -100);
        containerRect.sizeDelta = new Vector2(140, 180);
        
        // Daire E tuşu container - diğer skill ikonlarına benzer
        float circleSize = 80f;
        GameObject keyContainer = new GameObject("KeyCircle");
        keyContainer.transform.SetParent(container.transform, false);
        keyPromptRect = keyContainer.AddComponent<RectTransform>();
        keyPromptRect.anchorMin = new Vector2(0.5f, 1f);
        keyPromptRect.anchorMax = new Vector2(0.5f, 1f);
        keyPromptRect.anchoredPosition = new Vector2(0, -circleSize/2 - 10);
        keyPromptRect.sizeDelta = new Vector2(circleSize, circleSize);
        keyPromptGroup = keyContainer.AddComponent<CanvasGroup>();
        
        // Arka plan daire (koyu)
        GameObject keyBg = new GameObject("KeyBg");
        keyBg.transform.SetParent(keyContainer.transform, false);
        Image keyBgImg = keyBg.AddComponent<Image>();
        keyBgImg.sprite = CreateCircleSprite(128);
        keyBgImg.color = new Color(0.12f, 0.1f, 0.08f, 0.95f);
        RectTransform keyBgRect = keyBg.GetComponent<RectTransform>();
        keyBgRect.anchorMin = Vector2.zero;
        keyBgRect.anchorMax = Vector2.one;
        keyBgRect.offsetMin = Vector2.zero;
        keyBgRect.offsetMax = Vector2.zero;
        
        // Radial progress (ring around circle)
        GameObject progressRing = new GameObject("ProgressRing");
        progressRing.transform.SetParent(keyContainer.transform, false);
        mainProgressBar = progressRing.AddComponent<Image>();
        mainProgressBar.sprite = CreateRingSprite(128, 0.12f);
        mainProgressBar.color = accentColor;
        mainProgressBar.type = Image.Type.Filled;
        mainProgressBar.fillMethod = Image.FillMethod.Radial360;
        mainProgressBar.fillOrigin = (int)Image.Origin360.Top;
        mainProgressBar.fillClockwise = true;
        mainProgressBar.fillAmount = 0f;
        RectTransform progressRingRect = progressRing.GetComponent<RectTransform>();
        progressRingRect.anchorMin = Vector2.zero;
        progressRingRect.anchorMax = Vector2.one;
        progressRingRect.offsetMin = new Vector2(-6, -6);
        progressRingRect.offsetMax = new Vector2(6, 6);
        
        // Glow ring (arkada)
        GameObject glowRing = new GameObject("GlowRing");
        glowRing.transform.SetParent(keyContainer.transform, false);
        glowRing.transform.SetAsFirstSibling();
        progressGlow = glowRing.AddComponent<Image>();
        progressGlow.sprite = CreateRingSprite(128, 0.18f);
        progressGlow.color = glowColor;
        progressGlow.type = Image.Type.Filled;
        progressGlow.fillMethod = Image.FillMethod.Radial360;
        progressGlow.fillOrigin = (int)Image.Origin360.Top;
        progressGlow.fillClockwise = true;
        progressGlow.fillAmount = 0f;
        RectTransform glowRingRect = glowRing.GetComponent<RectTransform>();
        glowRingRect.anchorMin = Vector2.zero;
        glowRingRect.anchorMax = Vector2.one;
        glowRingRect.offsetMin = new Vector2(-12, -12);
        glowRingRect.offsetMax = new Vector2(12, 12);
        
        // Border ring (ince çizgi)
        GameObject borderRing = new GameObject("BorderRing");
        borderRing.transform.SetParent(keyContainer.transform, false);
        Image borderImg = borderRing.AddComponent<Image>();
        borderImg.sprite = CreateRingSprite(128, 0.06f);
        borderImg.color = new Color(1f, 1f, 1f, 0.25f);
        RectTransform borderRect = borderRing.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-6, -6);
        borderRect.offsetMax = new Vector2(6, 6);
        
        // E harfi
        GameObject keyText = new GameObject("KeyText");
        keyText.transform.SetParent(keyContainer.transform, false);
        TextMeshProUGUI keyTMP = keyText.AddComponent<TextMeshProUGUI>();
        keyTMP.text = "E";
        keyTMP.fontSize = 38;
        keyTMP.fontStyle = FontStyles.Bold;
        keyTMP.color = Color.white;
        keyTMP.alignment = TextAlignmentOptions.Center;
        RectTransform keyTextRect = keyText.GetComponent<RectTransform>();
        keyTextRect.anchorMin = Vector2.zero;
        keyTextRect.anchorMax = Vector2.one;
        keyTextRect.offsetMin = Vector2.zero;
        keyTextRect.offsetMax = Vector2.zero;
        
        // "MASH!" yazısı - altında
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(container.transform, false);
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "MASH!";
        titleText.fontSize = 22;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = accentColor;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.outlineWidth = 0.2f;
        titleText.outlineColor = new Color(0, 0, 0, 0.8f);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0);
        titleRect.anchorMax = new Vector2(0.5f, 0);
        titleRect.anchoredPosition = new Vector2(0, 50);
        titleRect.sizeDelta = new Vector2(120, 30);
        
        // Glow'u progress ile senkronize et
        StartCoroutine(SyncGlow());
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
    
    private Sprite CreateRingSprite(int size, float thickness)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float outerRadius = size / 2f - 1f;
        float innerRadius = outerRadius * (1f - thickness);
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= outerRadius && dist >= innerRadius)
                {
                    // Anti-aliased edges
                    float outerAlpha = Mathf.Clamp01((outerRadius - dist) * 2f);
                    float innerAlpha = Mathf.Clamp01((dist - innerRadius) * 2f);
                    float alpha = Mathf.Min(outerAlpha, innerAlpha);
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
    
    private IEnumerator SyncGlow()
    {
        while (qteUIRoot != null && progressGlow != null && mainProgressBar != null)
        {
            progressGlow.fillAmount = mainProgressBar.fillAmount;
            yield return null;
        }
    }
    
    private void DestroyQTEUI()
    {
        if (qteUIRoot != null)
        {
            Destroy(qteUIRoot);
            qteUIRoot = null;
            mainProgressBar = null;
            progressGlow = null;
            keyPromptGroup = null;
            keyPromptRect = null;
        }
    }
}
