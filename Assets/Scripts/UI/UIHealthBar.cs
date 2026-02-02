using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Sol üst köşede şık health bar UI - SoulUI ile aynı estetik
/// Mor tema, rounded panel, glow efektleri
/// </summary>
public class UIHealthBar : MonoBehaviour
{
    [Header("Position Settings")]
    [SerializeField] private Vector2 position = new Vector2(25f, 25f);
    [SerializeField] private float panelWidth = 200f;
    [SerializeField] private float panelHeight = 55f;

    [Header("Colors")]
    [SerializeField] private Color healthColorFull = new Color(0.6f, 0.2f, 1f, 1f);   // Mor
    [SerializeField] private Color healthColorMid = new Color(0.9f, 0.5f, 0.2f, 1f);  // Turuncu
    [SerializeField] private Color healthColorLow = new Color(0.8f, 0.2f, 0.2f, 1f);  // Kırmızı
    [SerializeField] private Color glowColor = new Color(0.8f, 0.4f, 1f, 0.6f);
    [SerializeField] private Color panelColor = new Color(0.05f, 0.02f, 0.1f, 0.85f);

    [Header("Settings")]
    [SerializeField] private float smoothSpeed = 8f;

    private Canvas canvas;
    private RectTransform container;
    private Image backgroundImage;
    private Image healthFillImage;
    private Image heartOrbImage;
    private Image heartOrbGlow;
    private TextMeshProUGUI labelText;

    private Health playerHealth;
    private float displayedHealth;
    private float targetHealth;
    private bool isInitialized = false;

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
            gameObject.SetActive(false);
            yield break;
        }

        if (!FindExistingUI())
        {
            CreateUI();
        }

        FindPlayerHealth();
        
        if (playerHealth != null)
        {
            displayedHealth = playerHealth.currentHealth;
            targetHealth = playerHealth.currentHealth;
        }
        
        UpdateUI();
        isInitialized = true;
    }

    private void FindPlayerHealth()
    {
        if (playerHealth != null) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<Health>();
        }
    }

    [ContextMenu("Generate UI Layout")]
    public void GenerateUIEditor()
    {
        // Eski child'ları temizle
        var children = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        children.ForEach(child => DestroyImmediate(child));

        CreateUI();
        Debug.Log("Health UI Generated!");
    }

    private bool FindExistingUI()
    {
        Transform canvasTrans = transform.Find("HealthUI_Canvas");
        if (canvasTrans == null) return false;

        canvas = canvasTrans.GetComponent<Canvas>();
        Transform tempContainer = canvasTrans.Find("HealthContainer");
        if (tempContainer == null) return false;
        
        container = tempContainer.GetComponent<RectTransform>();

        // Referansları bul
        Transform bgObj = container.Find("Background");
        if (bgObj) backgroundImage = bgObj.GetComponent<Image>();

        Transform orbContainer = container.Find("HeartOrb");
        if (orbContainer)
        {
            Transform glowObj = orbContainer.Find("Glow");
            if (glowObj) heartOrbGlow = glowObj.GetComponent<Image>();

            Transform orbImg = orbContainer.Find("OrbImage");
            if (orbImg) heartOrbImage = orbImg.GetComponent<Image>();
        }

        Transform barContainer = container.Find("BarContainer");
        if (barContainer)
        {
            Transform fill = barContainer.Find("Fill");
            if (fill) healthFillImage = fill.GetComponent<Image>();

            Transform label = barContainer.Find("Label");
            if (label) labelText = label.GetComponent<TextMeshProUGUI>();
        }

        RefreshSprites();
        return true;
    }

    private void RefreshSprites()
    {
        if (backgroundImage) 
        {
            backgroundImage.sprite = CreateRoundedRectSprite(64, 16);
            backgroundImage.type = Image.Type.Sliced;
        }
        if (heartOrbImage) heartOrbImage.sprite = CreateCircleSprite(64);
        if (heartOrbGlow) heartOrbGlow.sprite = CreateCircleSprite(64);
        
        // Bar background ve fill sprite'larını da yenile
        if (container != null)
        {
            Transform barContainer = container.Find("BarContainer");
            if (barContainer != null)
            {
                Transform barBgT = barContainer.Find("BarBackground");
                if (barBgT != null)
                {
                    Image barBg = barBgT.GetComponent<Image>();
                    if (barBg != null && barBg.sprite == null)
                    {
                        barBg.sprite = CreateRoundedRectSprite(128, 32);
                        barBg.type = Image.Type.Sliced;
                    }
                }
                
                if (healthFillImage != null && healthFillImage.sprite == null)
                {
                    healthFillImage.sprite = CreateRoundedRectSprite(128, 28);
                    healthFillImage.type = Image.Type.Filled;
                    healthFillImage.fillMethod = Image.FillMethod.Horizontal;
                    healthFillImage.fillOrigin = 0;
                }
            }
        }
    }

    private void CreateUI()
    {
        // Mevcut canvas kontrolü
        Transform existingCanvas = transform.Find("HealthUI_Canvas");
        if (existingCanvas != null) return;

        // Canvas
        GameObject canvasObj = new GameObject("HealthUI_Canvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 95;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Container - sol üst köşe
        GameObject containerObj = new GameObject("HealthContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        container = containerObj.AddComponent<RectTransform>();
        container.anchorMin = new Vector2(0f, 1f);  // Sol üst
        container.anchorMax = new Vector2(0f, 1f);
        container.pivot = new Vector2(0f, 1f);
        container.anchoredPosition = new Vector2(position.x, -position.y);
        container.sizeDelta = new Vector2(panelWidth, panelHeight);

        // Background panel - SoulUI ile aynı stil
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(container, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = panelColor;
        backgroundImage.sprite = CreateRoundedRectSprite(64, 16);
        backgroundImage.type = Image.Type.Sliced;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Heart Orb (sol taraf) - SoulUI orb gibi
        float orbSize = panelHeight - 10f;
        GameObject orbContainer = new GameObject("HeartOrb");
        orbContainer.transform.SetParent(container, false);
        RectTransform orbRect = orbContainer.AddComponent<RectTransform>();
        orbRect.anchorMin = new Vector2(0f, 0.5f);
        orbRect.anchorMax = new Vector2(0f, 0.5f);
        orbRect.pivot = new Vector2(0f, 0.5f);
        orbRect.anchoredPosition = new Vector2(8f, 0f);
        orbRect.sizeDelta = new Vector2(orbSize, orbSize);

        // Orb Glow
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(orbContainer.transform, false);
        heartOrbGlow = glowObj.AddComponent<Image>();
        heartOrbGlow.sprite = CreateCircleSprite(64);
        heartOrbGlow.color = glowColor;
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = Vector2.zero;
        glowRect.anchorMax = Vector2.one;
        glowRect.sizeDelta = new Vector2(2f, 2f);  // Daha küçük glow
        glowRect.anchoredPosition = Vector2.zero;

        // Heart Orb Image
        GameObject orbImgObj = new GameObject("OrbImage");
        orbImgObj.transform.SetParent(orbContainer.transform, false);
        heartOrbImage = orbImgObj.AddComponent<Image>();
        heartOrbImage.sprite = CreateCircleSprite(64);
        heartOrbImage.color = healthColorFull;
        RectTransform orbImgRect = orbImgObj.GetComponent<RectTransform>();
        orbImgRect.anchorMin = Vector2.zero;
        orbImgRect.anchorMax = Vector2.one;
        orbImgRect.sizeDelta = new Vector2(-6f, -6f);
        orbImgRect.anchoredPosition = Vector2.zero;

        // Heart symbol
        GameObject heartSymbol = new GameObject("HeartSymbol");
        heartSymbol.transform.SetParent(orbContainer.transform, false);
        TextMeshProUGUI heartText = heartSymbol.AddComponent<TextMeshProUGUI>();
        heartText.text = "♥";
        heartText.fontSize = 22;
        heartText.fontStyle = FontStyles.Bold;
        heartText.alignment = TextAlignmentOptions.Center;
        heartText.color = Color.white;
        RectTransform heartRect = heartSymbol.GetComponent<RectTransform>();
        heartRect.anchorMin = Vector2.zero;
        heartRect.anchorMax = Vector2.one;
        heartRect.sizeDelta = Vector2.zero;

        // Divider
        GameObject dividerObj = new GameObject("Divider");
        dividerObj.transform.SetParent(container, false);
        Image divider = dividerObj.AddComponent<Image>();
        divider.color = new Color(1f, 1f, 1f, 0.1f);
        RectTransform divRect = dividerObj.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0f, 0.15f);
        divRect.anchorMax = new Vector2(0f, 0.85f);
        divRect.pivot = new Vector2(0f, 0.5f);
        divRect.anchoredPosition = new Vector2(orbSize + 12f, 0f);
        divRect.sizeDelta = new Vector2(1.5f, 0f);

        // Bar Container (sağ taraf)
        GameObject barContainer = new GameObject("BarContainer");
        barContainer.transform.SetParent(container, false);
        RectTransform barRect = barContainer.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.offsetMin = new Vector2(orbSize + 20f, 8f);
        barRect.offsetMax = new Vector2(-10f, -8f);

        // Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(barContainer.transform, false);
        labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "HEALTH";
        labelText.fontSize = 10;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.TopLeft;
        labelText.color = new Color(0.8f, 0.7f, 1f, 0.6f);
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0.5f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        // Health bar background - panel arka planı ile aynı renk
        GameObject barBgObj = new GameObject("BarBackground");
        barBgObj.transform.SetParent(barContainer.transform, false);
        Image barBg = barBgObj.AddComponent<Image>();
        barBg.color = panelColor;  // Arka planla aynı renk
        barBg.sprite = CreateRoundedRectSprite(128, 32);
        barBg.type = Image.Type.Sliced;
        RectTransform barBgRect = barBgObj.GetComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0f, 0f);
        barBgRect.anchorMax = new Vector2(1f, 0.55f);
        barBgRect.offsetMin = Vector2.zero;
        barBgRect.offsetMax = Vector2.zero;

        // Health Fill - Filled type ile sol-sağ dolum
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(barContainer.transform, false);
        healthFillImage = fillObj.AddComponent<Image>();
        healthFillImage.color = healthColorFull;
        healthFillImage.sprite = CreateRoundedRectSprite(128, 28);
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        healthFillImage.fillOrigin = 0;  // Soldan sağa
        healthFillImage.fillAmount = 1f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 0.55f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);
    }

    private void Update()
    {
        if (!isInitialized) return;

        if (playerHealth == null)
        {
            FindPlayerHealth();
            return;
        }

        targetHealth = playerHealth.currentHealth;

        if (Mathf.Abs(displayedHealth - targetHealth) > 0.01f)
        {
            displayedHealth = Mathf.Lerp(displayedHealth, targetHealth, Time.deltaTime * smoothSpeed);
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (healthFillImage == null) return;
        if (playerHealth == null)
        {
            FindPlayerHealth();
            if (playerHealth == null) return;
        }

        float maxHealth = playerHealth.maxHealth;
        if (maxHealth <= 0) maxHealth = 1f;

        float healthPercent = Mathf.Clamp01(displayedHealth / maxHealth);

        // Fill amount ile dolum (Filled type kullanıyoruz)
        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = healthPercent;
        }

        // Renk geçişi
        Color currentColor;
        if (healthPercent > 0.6f)
        {
            currentColor = healthColorFull;
        }
        else if (healthPercent > 0.3f)
        {
            float t = (healthPercent - 0.3f) / 0.3f;
            currentColor = Color.Lerp(healthColorMid, healthColorFull, t);
        }
        else
        {
            float t = healthPercent / 0.3f;
            currentColor = Color.Lerp(healthColorLow, healthColorMid, t);
        }

        healthFillImage.color = currentColor;

        // Orb rengi
        if (heartOrbImage != null)
        {
            heartOrbImage.color = currentColor;
        }

        // Orb glow rengi
        if (heartOrbGlow != null)
        {
            Color glow = currentColor;
            glow.a = 0.5f;
            heartOrbGlow.color = glow;

            // Düşük canda pulse
            if (healthPercent <= 0.3f)
            {
                float pulse = Mathf.Sin(Time.time * 5f) * 0.3f + 0.5f;
                glow.a = pulse;
                heartOrbGlow.color = glow;
            }
        }
    }

    #region Sprite Creation - SoulUI ile aynı
    private Sprite CreateRoundedRectSprite(int size, int cornerRadius)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = IsInsideRoundedRect(x, y, size, size, cornerRadius);
                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;

        float border = cornerRadius;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
    {
        if (x < radius && y < radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) <= radius;
        if (x >= width - radius && y < radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, radius)) <= radius;
        if (x < radius && y >= height - radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius - 1)) <= radius;
        if (x >= width - radius && y >= height - radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, height - radius - 1)) <= radius;
        return true;
    }

    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - dist) / 2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    #endregion
}
