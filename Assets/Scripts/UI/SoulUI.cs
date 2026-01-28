using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Ruh toplama UI'ı - sol alt köşede şık bir gösterge
/// Ruhlar toplandıkça dolar, ability hakkı kazanılınca animasyon oynar
/// </summary>
public class SoulUI : MonoBehaviour
{
    [Header("Position Settings")]
    [SerializeField] private Vector2 position = new Vector2(20f, 120f); // Sol alt köşe
    [SerializeField] private float containerSize = 70f;

    [Header("Colors")]
    [SerializeField] private Color soulColor = new Color(0.6f, 0.2f, 1f, 1f); // Mor
    [SerializeField] private Color chargeColor = new Color(1f, 0.8f, 0.2f, 1f); // Altın
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color glowColor = new Color(0.8f, 0.4f, 1f, 0.6f);

    private Canvas canvas;
    private RectTransform container;
    private Image backgroundImage;
    private Image soulFillImage;
    private Image glowImage;
    private TextMeshProUGUI soulCountText;
    private TextMeshProUGUI chargeCountText;
    private TextMeshProUGUI abilityReadyText;

    // Charge indicators
    private Image[] chargeIndicators;

    private SoulSystem soulSystem;
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
            Destroy(gameObject);
            yield break;
        }

        soulSystem = SoulSystem.Instance;
        if (soulSystem == null)
        {
            // SoulSystem yoksa oluştur
            GameObject soulSystemObj = new GameObject("SoulSystem");
            soulSystem = soulSystemObj.AddComponent<SoulSystem>();
        }

        if (!FindExistingUI())
        {
            CreateUI();
        }
        
        SubscribeToEvents();
        UpdateUI();
        isInitialized = true;
    }

    private void SubscribeToEvents()
    {
        if (soulSystem != null)
        {
            soulSystem.OnKillCountChanged += OnKillCountChanged;
            soulSystem.OnUltimateReady += OnUltimateReady;
            soulSystem.OnUltimateUsed += OnUltimateUsed;
        }
    }

    private void OnDestroy()
    {
        if (soulSystem != null)
        {
            soulSystem.OnKillCountChanged -= OnKillCountChanged;
            soulSystem.OnUltimateReady -= OnUltimateReady;
            soulSystem.OnUltimateUsed -= OnUltimateUsed;
        }
    }

    [ContextMenu("Generate UI Layout")]
    public void GenerateUIEditor()
    {
        var children = new System.Collections.Generic.List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);
        children.ForEach(child => DestroyImmediate(child));
        
        CreateUI();
        Debug.Log("Soul UI Generated for Editing.");
    }

    private bool FindExistingUI()
    {
        Transform canvasTrans = transform.Find("SoulUI_Canvas");
        if (canvasTrans == null) return false;
        
        canvas = canvasTrans.GetComponent<Canvas>();
        Transform tempContainer = canvasTrans.Find("SoulContainer");
        
        if (tempContainer == null) return false;
        container = tempContainer.GetComponent<RectTransform>();

        Transform bgObj = container.Find("Background");
        if(bgObj) backgroundImage = bgObj.GetComponent<Image>();

        Transform orbContainer = container.Find("OrbContainer");
        if(orbContainer)
        {
            Transform glowOb = orbContainer.Find("Glow");
            if(glowOb) glowImage = glowOb.GetComponent<Image>();

            Transform fillOb = orbContainer.Find("SoulFill");
            if(fillOb) soulFillImage = fillOb.GetComponent<Image>();

            Transform countOb = orbContainer.Find("SoulCount");
            if(countOb) soulCountText = countOb.GetComponent<TextMeshProUGUI>();
        }

        Transform infoContainer = container.Find("InfoContainer");
        if(infoContainer)
        {
            // Label'ı artık abilityReadyText olarak kullanmıyoruz
            // Transform labelObj = infoContainer.Find("Label");
            
            Transform readyObj = infoContainer.Find("ReadyText");
            if (readyObj) abilityReadyText = readyObj.GetComponent<TextMeshProUGUI>();

            Transform chargesRow = infoContainer.Find("ChargesRow");
            if (chargesRow)
            {
                // Re-find charge images
                int childCount = chargesRow.childCount;
                chargeIndicators = new Image[childCount];
                for(int i=0; i<childCount; i++)
                {
                    Transform child = chargesRow.GetChild(i);
                    if(child) chargeIndicators[i] = child.GetComponent<Image>();
                }
            }
        }

        // Re-generate sprites
        RefreshSprites();

        return true;
    }

    private void RefreshSprites()
    {
        if (backgroundImage) backgroundImage.sprite = CreateRoundedRectSprite(64, 12);
        if (glowImage) glowImage.sprite = CreateCircleSprite(64);
        if (soulFillImage) soulFillImage.sprite = CreateCircleSprite(64);
        
        if (chargeIndicators != null)
        {
            foreach(var ind in chargeIndicators)
            {
                if(ind) ind.sprite = CreateDiamondSprite(32);
            }
        }
    }

    private void CreateUI()
    {
        // Check if we already have a canvas in children (from Editor creation)
        Transform existingCanvas = transform.Find("SoulUI_Canvas");
        if (existingCanvas != null)
        {
            canvas = existingCanvas.GetComponent<Canvas>();
            container = existingCanvas.Find("SoulContainer").GetComponent<RectTransform>();
            // ... re-assign other references if needed, but FindExistingUI handles this
            return;
        }

        // Canvas
        GameObject canvasObj = new GameObject("SoulUI_Canvas");
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

        // Container - sol alt köşe
        GameObject containerObj = new GameObject("SoulContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        container = containerObj.AddComponent<RectTransform>();
        container.anchorMin = new Vector2(0f, 0f); // Sol alt
        container.anchorMax = new Vector2(0f, 0f);
        container.pivot = new Vector2(0f, 0f);
        // Genişletilmiş boyut - Daha ferah görünüm
        container.anchoredPosition = new Vector2(25f, 25f);
        container.sizeDelta = new Vector2(containerSize * 3.2f, containerSize * 1.2f); // Increased Width

        // Background panel
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(container, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = new Color(0.05f, 0.02f, 0.1f, 0.85f);
        backgroundImage.sprite = CreateRoundedRectSprite(64, 16); // More rounded
        backgroundImage.type = Image.Type.Sliced;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Soul orb container (sol taraf)
        GameObject orbContainer = new GameObject("OrbContainer");
        orbContainer.transform.SetParent(container, false);
        RectTransform orbRect = orbContainer.AddComponent<RectTransform>();
        orbRect.anchorMin = new Vector2(0f, 0.5f);
        orbRect.anchorMax = new Vector2(0f, 0.5f);
        orbRect.pivot = new Vector2(0f, 0.5f);
        orbRect.anchoredPosition = new Vector2(10f, 0f); // Slight offset padding
        orbRect.sizeDelta = new Vector2(containerSize, containerSize);

        // Glow effect (arkada)
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(orbContainer.transform, false);
        glowImage = glowObj.AddComponent<Image>();
        glowImage.sprite = CreateCircleSprite(64);
        glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0f);
        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0f, 0f);
        glowRect.anchorMax = new Vector2(1f, 1f);
        glowRect.sizeDelta = new Vector2(10f, 10f); // Glow slightly larger
        glowRect.anchoredPosition = Vector2.zero;

        // Soul fill (radial)
        GameObject fillObj = new GameObject("SoulFill");
        fillObj.transform.SetParent(orbContainer.transform, false);
        soulFillImage = fillObj.AddComponent<Image>();
        soulFillImage.sprite = CreateCircleSprite(128); // Higher res
        soulFillImage.type = Image.Type.Filled;
        soulFillImage.fillMethod = Image.FillMethod.Radial360;
        soulFillImage.fillOrigin = (int)Image.Origin360.Top;
        soulFillImage.fillClockwise = true;
        soulFillImage.color = soulColor;
        soulFillImage.fillAmount = 0f;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-10f, -10f); // Padding inside glow
        fillRect.anchoredPosition = Vector2.zero;
        
        // Inner circle background for text readability
        GameObject innerBgObj = new GameObject("InnerBg");
        innerBgObj.transform.SetParent(orbContainer.transform, false);
        Image innerBg = innerBgObj.AddComponent<Image>();
        innerBg.sprite = CreateCircleSprite(64);
        innerBg.color = new Color(0f, 0f, 0f, 0.5f);
        RectTransform innerRect = innerBgObj.GetComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.sizeDelta = new Vector2(-20f, -20f);
        innerRect.anchoredPosition = Vector2.zero;

        // Soul count text (ortada)
        GameObject countObj = new GameObject("SoulCount");
        countObj.transform.SetParent(orbContainer.transform, false);
        soulCountText = countObj.AddComponent<TextMeshProUGUI>();
        soulCountText.text = "0/5";
        soulCountText.fontSize = 20;
        soulCountText.fontStyle = FontStyles.Bold;
        soulCountText.alignment = TextAlignmentOptions.Center;
        soulCountText.color = Color.white;
        soulCountText.enableAutoSizing = false;
        RectTransform countRect = countObj.GetComponent<RectTransform>();
        countRect.anchorMin = Vector2.zero;
        countRect.anchorMax = Vector2.one;
        countRect.sizeDelta = Vector2.zero;

        // Divider Line (Optional Visual Separator)
        GameObject dividerObj = new GameObject("Divider");
        dividerObj.transform.SetParent(container, false);
        Image divider = dividerObj.AddComponent<Image>();
        divider.color = new Color(1f, 1f, 1f, 0.1f);
        RectTransform divRect = dividerObj.GetComponent<RectTransform>();
        divRect.anchorMin = new Vector2(0f, 0.1f);
        divRect.anchorMax = new Vector2(0f, 0.9f);
        divRect.pivot = new Vector2(0f, 0.5f);
        divRect.anchoredPosition = new Vector2(containerSize + 15f, 0f);
        divRect.sizeDelta = new Vector2(2f, 0f);

        // Right side info container
        GameObject infoContainer = new GameObject("InfoContainer");
        infoContainer.transform.SetParent(container, false);
        RectTransform infoRect = infoContainer.AddComponent<RectTransform>();
        // Fill remaining space
        infoRect.anchorMin = new Vector2(0f, 0f);
        infoRect.anchorMax = new Vector2(1f, 1f);
        infoRect.pivot = new Vector2(0f, 0.5f);
        infoRect.offsetMin = new Vector2(containerSize + 25f, 5f); // Start after orb
        infoRect.offsetMax = new Vector2(-10f, -5f); // Padding right

        // Vertical Layout for Text (Top) and Charges (Bottom)
        VerticalLayoutGroup vLayout = infoContainer.AddComponent<VerticalLayoutGroup>();
        vLayout.childAlignment = TextAnchor.MiddleLeft;
        vLayout.childControlHeight = true;
        vLayout.childControlWidth = true;
        vLayout.spacing = 2f;
        vLayout.childForceExpandHeight = false;

        // SOULS Label
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(infoContainer.transform, false);
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = "SOUL CHARGES";
        labelText.fontSize = 12;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.BottomLeft; // Align bottom relative to its slot
        labelText.color = new Color(0.8f, 0.7f, 1f, 0.6f);
        
        LayoutElement labelLe = labelObj.AddComponent<LayoutElement>();
        labelLe.minHeight = 16f;
        labelLe.preferredHeight = 18f;

        // Ability Ready Text (Hidden by default, shows "READY!" when charged)
        GameObject readyObj = new GameObject("ReadyText");
        readyObj.transform.SetParent(infoContainer.transform, false);
        abilityReadyText = readyObj.AddComponent<TextMeshProUGUI>();
        abilityReadyText.text = "";
        abilityReadyText.fontSize = 14;
        abilityReadyText.fontStyle = FontStyles.Bold;
        abilityReadyText.alignment = TextAlignmentOptions.BottomLeft;
        abilityReadyText.color = chargeColor;
        
        // Bu yazı normalde gizli olacak, layout'u bozmaması için ignore layout? 
        // Veya label ile swaplansın. Şimdilik ayrı bir satır yapmayalım, Layout Element ile kontrol edelim.
        // En iyisi: Label sabit kalsın, Ready yazısı charges üzerine overlay olsun veya yanı başında çıksın.
        // Basit çözüm: ReadyText'i ayrı bir LayoutElement yapalım ama height 0 verelim ki yer kaplamasın? 
        // Veya ChargesRow'un altına ekleyelim.
        
        LayoutElement readyLe = readyObj.AddComponent<LayoutElement>();
        readyLe.ignoreLayout = true; // Layout dışı
        // Manuel pozisyonlama ile Label'ın üzerine bindirelim (veya yanına)
        RectTransform readyRect = readyObj.GetComponent<RectTransform>();
        readyRect.anchorMin = Vector2.zero;
        readyRect.anchorMax = Vector2.one;
        readyRect.offsetMin = new Vector2(90f, 0f); // Label'ın sağına kaydır
        readyRect.offsetMax = Vector2.zero;

        // Charge indicators container
        CreateChargeIndicators(infoContainer.transform);
    }

    private void CreateChargeIndicators(Transform parent)
    {
        int maxCharges = soulSystem != null ? soulSystem.MaxCharges : 3;
        chargeIndicators = new Image[maxCharges];

        GameObject chargesObj = new GameObject("ChargesRow");
        chargesObj.transform.SetParent(parent, false);
        
        // This row will sit below the label
        LayoutElement rowLe = chargesObj.AddComponent<LayoutElement>();
        rowLe.minHeight = 24f;
        rowLe.preferredHeight = 30f;
        rowLe.flexibleWidth = 1f;

        HorizontalLayoutGroup layout = chargesObj.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false; // We set fixed size on items
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < maxCharges; i++)
        {
            GameObject indicator = new GameObject($"Charge_{i}");
            indicator.transform.SetParent(chargesObj.transform, false);
            
            Image img = indicator.AddComponent<Image>();
            img.sprite = CreateDiamondSprite(64); // Higher res
            img.color = emptyColor;
            
            // Set size explicitly on RectTransform since Layout control is false
            RectTransform rt = indicator.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(24f, 24f); 

            // Add LayoutElement just in case parent decides to control
            LayoutElement le = indicator.AddComponent<LayoutElement>();
            le.preferredWidth = 24f;
            le.preferredHeight = 24f;

            chargeIndicators[i] = img;
        }
    }

    private void Update()
    {
        if (!isInitialized || soulSystem == null) return;

        // Glow pulse when can use ability
        if (soulSystem.CanUseAbility && glowImage != null)
        {
            float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, pulse * 0.5f);
        }
    }

    private void OnKillCountChanged(int current, int max)
    {
        UpdateUI();
        StartCoroutine(SoulCollectAnimation());
    }

    private void OnUltimateReady()
    {
        UpdateUI();
        StartCoroutine(ChargeGainedAnimation());
    }

    private void OnUltimateUsed()
    {
        UpdateUI();
    }

    private void LateUpdate()
    {
        // Force position removed to allow user editing
    }

    private void UpdateUI()
    {
        if (soulSystem == null) return;

        // Soul fill
        if (soulFillImage != null)
        {
            soulFillImage.fillAmount = soulSystem.SoulProgress;
        }

        // Soul count text
        if (soulCountText != null)
        {
            soulCountText.text = $"{soulSystem.CurrentSouls}/{soulSystem.SoulsPerCharge}";
        }

        // Charge indicators
        if (chargeIndicators != null)
        {
            for (int i = 0; i < chargeIndicators.Length; i++)
            {
                if (chargeIndicators[i] != null)
                {
                    chargeIndicators[i].color = i < soulSystem.CurrentCharges ? chargeColor : emptyColor;
                }
            }
        }

        // Ability ready text
        if (abilityReadyText != null)
        {
            if (soulSystem.CanUseAbility)
            {
                abilityReadyText.text = "✦ READY!";
                abilityReadyText.color = chargeColor;
            }
            else
            {
                abilityReadyText.text = "";
            }
        }
    }

    private IEnumerator SoulCollectAnimation()
    {
        if (soulFillImage == null) yield break;

        // Flash effect
        Color original = soulFillImage.color;
        soulFillImage.color = Color.white;
        yield return new WaitForSecondsRealtime(0.1f);
        soulFillImage.color = original;
    }

    private IEnumerator ChargeGainedAnimation()
    {
        if (abilityReadyText == null) yield break;

        // "ABILITY READY!" flash
        abilityReadyText.text = "✦ ABILITY READY! ✦";
        abilityReadyText.fontSize = 16;

        // Glow burst
        if (glowImage != null)
        {
            glowImage.color = new Color(chargeColor.r, chargeColor.g, chargeColor.b, 0.8f);
        }

        yield return new WaitForSecondsRealtime(0.5f);

        // Scale pulse
        for (int i = 0; i < 3; i++)
        {
            if (container != null)
            {
                container.localScale = Vector3.one * 1.1f;
                yield return new WaitForSecondsRealtime(0.1f);
                container.localScale = Vector3.one;
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        abilityReadyText.fontSize = 14;
        UpdateUI();
    }

    // Procedural sprites
    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist <= radius)
                    pixels[y * size + x] = Color.white;
                else
                    pixels[y * size + x] = Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private Sprite CreateRoundedRectSprite(int size, int cornerRadius)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = true;
                
                // Check corners
                if (x < cornerRadius && y < cornerRadius)
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius)) <= cornerRadius;
                else if (x >= size - cornerRadius && y < cornerRadius)
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius - 1, cornerRadius)) <= cornerRadius;
                else if (x < cornerRadius && y >= size - cornerRadius)
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, size - cornerRadius - 1)) <= cornerRadius;
                else if (x >= size - cornerRadius && y >= size - cornerRadius)
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius - 1, size - cornerRadius - 1)) <= cornerRadius;

                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
    }

    private Sprite CreateDiamondSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float halfSize = size / 2f - 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Diamond shape using Manhattan distance
                float dx = Mathf.Abs(x - center.x);
                float dy = Mathf.Abs(y - center.y);
                
                if (dx + dy <= halfSize)
                    pixels[y * size + x] = Color.white;
                else
                    pixels[y * size + x] = Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
