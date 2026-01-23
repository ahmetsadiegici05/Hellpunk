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
    [SerializeField] private Color doubleJumpColor = new Color(0.4f, 1f, 0.4f, 1f); // Yeşil
    [SerializeField] private Color shockwaveColor = new Color(1f, 0.9f, 0.2f, 1f); // Sarı

    private Canvas canvas;
    private GuitarSkillSystem skillSystem;

    // Skill UI elements
    // E tuşu artık TimeSlowAbility için (ayrı UI)
    private SkillIcon healSkill;
    private SkillIcon fireballSkill;
    private SkillIcon doubleJumpSkill;

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
        // E tuşu artık TimeSlowAbility tarafından kullanılıyor (ayrı UI'da)
        healSkill = CreateSkillIcon(canvasObj.transform, "Heal", "F", 0, healColor);
        fireballSkill = CreateSkillIcon(canvasObj.transform, "Fireball", "Q", 1, fireballColor);
        doubleJumpSkill = CreateSkillIcon(canvasObj.transform, "DoubleJump", "R", 2, doubleJumpColor);
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

    private void Update()
    {
        if (skillSystem == null)
        {
            skillSystem = FindFirstObjectByType<GuitarSkillSystem>();
            if (skillSystem == null) return;
        }

        UpdateSkillIcon(healSkill, skillSystem.HealCooldownProgress, skillSystem.IsHealReady,
            skillSystem.CurrentSkill == GuitarSkillSystem.SkillType.Heal);
        
        UpdateSkillIcon(fireballSkill, skillSystem.FireballCooldownProgress, skillSystem.IsFireballReady,
            skillSystem.CurrentSkill == GuitarSkillSystem.SkillType.Fireball);
        
        UpdateSkillIcon(doubleJumpSkill, skillSystem.DoubleJumpCooldownProgress, skillSystem.IsDoubleJumpReady,
            skillSystem.CurrentSkill == GuitarSkillSystem.SkillType.DoubleJump);
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
        doubleJumpSkill = FindSkillIcon(canvasTrans, "DoubleJump", doubleJumpColor);
        
        // Spriteları yenile (Editor'de yaratılanlar Play modunda kaybolabilir)
        RefreshSkillSprites(healSkill, "Heal");
        RefreshSkillSprites(fireballSkill, "Fireball");
        RefreshSkillSprites(doubleJumpSkill, "DoubleJump");

        return healSkill != null && fireballSkill != null && doubleJumpSkill != null;
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
        
        if (skill == healSkill)
            return (1f - skillSystem.HealCooldownProgress) * 10f; // healCooldown
        else if (skill == fireballSkill)
            return (1f - skillSystem.FireballCooldownProgress) * 5f; // fireballCooldown
        else if (skill == doubleJumpSkill)
            return (1f - skillSystem.DoubleJumpCooldownProgress) * 20f; // doubleJumpCooldown
        
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
            case "DoubleJump":
                // Kanatlı bot veya çift ok
                DrawDoubleArrow(pixels, size, center, size * 0.38f);
                break;
            case "Shockwave":
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
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                float dist = pos.magnitude;
                float angle = Mathf.Atan2(pos.y, pos.x);
                
                // 8 kollu dalgalı güneş/ateş topu
                float spikes = 8f;
                float wave = Mathf.Cos(angle * spikes);
                // radius değişimi: 0.7 ile 1.0 arası
                float currentRadius = radius * (0.75f + 0.25f * wave);
                
                // İç kısım (çekirdek)
                float coreRadius = radius * 0.5f;

                if (dist <= currentRadius)
                {
                    // Kenarları yumuşat
                    float alpha = 1f;
                    if (dist > currentRadius - 1f) alpha = currentRadius - dist;
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
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

    private void DrawDoubleArrow(Color[] pixels, int size, Vector2 center, float arrowSize)
    {
        // İki yukarı ok çiz (double jump için) - Biraz daha dinamik
        float arrowWidth = arrowSize * 0.7f;
        float arrowHeight = arrowSize * 0.45f;
        float stemWidth = arrowSize * 0.25f; // Sapı kalınlaştır
        float spacing = arrowSize * 0.5f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                
                // Üst ok
                Vector2 topArrowPos = pos - new Vector2(0, spacing * 0.6f);
                if (IsInArrow(topArrowPos, arrowWidth, arrowHeight, stemWidth))
                {
                    pixels[y * size + x] = Color.white;
                    continue; // Overwite
                }
                
                // Alt ok
                Vector2 bottomArrowPos = pos + new Vector2(0, spacing * 0.5f);
                if (IsInArrow(bottomArrowPos, arrowWidth, arrowHeight, stemWidth))
                {
                    pixels[y * size + x] = new Color(1f, 1f, 1f, 0.7f); // Alt ok biraz saydam
                }
            }
        }
    }
    
    private bool IsInArrow(Vector2 pos, float width, float height, float stemWidth)
    {
        // Ok başı (üçgen)
        float headHeight = height * 0.6f;
        if (pos.y > 0 && pos.y < headHeight)
        {
            float progress = pos.y / headHeight;
            float maxWidth = width * (1f - progress) * 0.5f;
            if (Mathf.Abs(pos.x) <= maxWidth)
                return true;
        }
        
        // Ok gövdesi (dikdörtgen)
        if (pos.y >= -height * 0.5f && pos.y <= 0)
        {
            if (Mathf.Abs(pos.x) <= stemWidth * 0.5f)
                return true;
        }
        
        return false;
    }
}
