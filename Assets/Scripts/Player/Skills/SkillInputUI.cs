using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Skill input UI - Karakterin üzerinde ok yönlerini gösterir
/// Tüm UI elemanları ve ok sprite'ları kod ile oluşturulur.
/// </summary>
public class SkillInputUI : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float arrowSpacing = 80f;
    [SerializeField] private float arrowSize = 60f;
    [SerializeField] private float uiHeight = 2.5f; // Karakterin üstünde yükseklik

    [Header("Colors")]
    [SerializeField] private Color pendingColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color currentColor = Color.yellow;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);

    [Header("Skill Colors")]
    [SerializeField] private Color healColor = new Color(0.3f, 0.6f, 1f, 1f); // Mavi
    [SerializeField] private Color fireballColor = new Color(1f, 0.5f, 0.1f, 1f);
    [SerializeField] private Color doubleJumpColor = new Color(0.4f, 1f, 0.4f, 1f); // Yeşil
    [SerializeField] private Color shockwaveColor = new Color(1f, 0.9f, 0.2f, 1f); // Sarı

    [Header("Animation")]
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmount = 0.15f;
    [SerializeField] private float fadeInDuration = 0.15f;
    [SerializeField] private float fadeOutDuration = 0.25f;

    private Canvas skillCanvas;
    private GameObject arrowContainer;
    private TextMeshProUGUI skillNameText;
    private Image timerFillImage;
    private Image backgroundImage;
    private CanvasGroup canvasGroup;

    private List<Image> arrowImages = new List<Image>();
    private List<Image> arrowBackgrounds = new List<Image>();
    private GuitarSkillSystem skillSystem;
    private Coroutine pulseCoroutine;
    private Coroutine fadeCoroutine;
    private bool isShowing = false;
    private float inputStartTime;

    // Prosedürel oluşturulan ok sprite'ları (cache)
    private Sprite arrowUpSprite;
    private Sprite arrowDownSprite;
    private Sprite arrowLeftSprite;
    private Sprite arrowRightSprite;
    private Sprite circleSprite;
    
    private float initialCanvasScale = 0.015f;
    
    [SerializeField, HideInInspector] private GameObject uiRootObject; // Bulunan veya oluşturulan UI root objesi

    private void Awake()
    {
        // MainMenu kontrolü
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu")
        {
            this.enabled = false;
            return;
        }

        CreateArrowSprites();
        
        // Varsa mevcut UI'yi bul, yoksa yeni oluştur
        if (!FindExistingUI())
        {
            CreateUICanvas();
        }
        else
        {
            // Eğer UI zaten varsa (Preview'dan kalmışsa), çalışma zamanı için sıfırla
            ResetUIState();
        }
    }

    private void Start()
    {
        // Biraz gecikme ile skill system'i bul (initialization sırası için)
        StartCoroutine(FindSkillSystemDelayed());
    }
    

    private void ResetUIState()
    {
        // Preview modunda açık bırakılan canvas'ı gizle ve temizle
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        if (arrowContainer != null) arrowContainer.SetActive(false);
        if (backgroundImage != null) backgroundImage.gameObject.SetActive(false);
        if (skillNameText != null) skillNameText.gameObject.SetActive(false);
        if (timerFillImage != null) timerFillImage.gameObject.SetActive(false);
        
        ClearArrows(); // Tüm okları sil
    }
    
    [ContextMenu("Generate Preview UI")]
    public void GeneratePreviewUI()
    {
        // Temizle (Referanslı obje varsa onu, yoksa isime göre)
        if (uiRootObject != null)
        {
            DestroyImmediate(uiRootObject);
        }
        else
        {
            var existing = transform.Find("SkillInputCanvas");
            if (existing != null) DestroyImmediate(existing.gameObject);
        }
        
        CreateArrowSprites(); // Editor mode için sprite'ları oluştur
        CreateUICanvas();
        
        // Oluşturulan canvas'ı kaydet
        if (skillCanvas != null) uiRootObject = skillCanvas.gameObject;
        
        // Preview için görünür yap
        if (canvasGroup != null) canvasGroup.alpha = 1f;
        if (backgroundImage != null) backgroundImage.gameObject.SetActive(true);
        if (skillNameText != null)
        {
            skillNameText.gameObject.SetActive(true);
            skillNameText.text = "PREVIEW SKILL";
        }
        if (arrowContainer != null) arrowContainer.SetActive(true);
        if (timerFillImage != null)
        {
            timerFillImage.gameObject.SetActive(true);
            timerFillImage.fillAmount = 0.5f;
        }
        
        // Örnek oklar
        ShowArrowSequence(GuitarSkillSystem.SkillType.Fireball, new List<GuitarSkillSystem.ArrowDirection> {
            GuitarSkillSystem.ArrowDirection.Up,
            GuitarSkillSystem.ArrowDirection.Down,
            GuitarSkillSystem.ArrowDirection.Left,
            GuitarSkillSystem.ArrowDirection.Right
        });
        
        Debug.Log("SkillInputUI: Preview UI oluşturuldu. Play modunda otomatik kullanılacak.");
    }
    
    private bool FindExistingUI()
    {
        Transform canvasTrans = null;

        // 1. Önce kayıtlı referansa bak
        if (uiRootObject != null)
        {
            canvasTrans = uiRootObject.transform;
        }
        
        // 2. Referans yoksa veya kırılmışsa isime göre bak
        if (canvasTrans == null)
        {
            canvasTrans = transform.Find("SkillInputCanvas");
        }

        // 3. İsimle de bulunamadıysa çocukları tara (İsim değişmiş olabilir)
        if (canvasTrans == null)
        {
            foreach (Transform child in transform)
            {
                // İçinde "ArrowContainer" olan ve Canvas componenti olan bir obje mi?
                if (child.GetComponent<Canvas>() != null && child.Find("ArrowContainer") != null)
                {
                    canvasTrans = child;
                    break;
                }
            }
        }

        if (canvasTrans == null) return false;
        
        // Referansı güncelle
        uiRootObject = canvasTrans.gameObject;
        
        skillCanvas = canvasTrans.GetComponent<Canvas>();
        canvasGroup = canvasTrans.GetComponent<CanvasGroup>();
        
        // Mevcut scale'i kaydet (Kullanıcı değiştirdiyse korunsun)
        initialCanvasScale = Mathf.Abs(canvasTrans.localScale.x);
        
        Transform bgPanel = canvasTrans.Find("BackgroundPanel");
        if (bgPanel) backgroundImage = bgPanel.GetComponent<Image>();
        
        Transform textObj = canvasTrans.Find("SkillName");
        if (textObj) skillNameText = textObj.GetComponent<TextMeshProUGUI>();
        
        Transform arrowCont = canvasTrans.Find("ArrowContainer");
        if (arrowCont) arrowContainer = arrowCont.gameObject;
        
        Transform timerBg = canvasTrans.Find("TimerBackground");
        if (timerBg)
        {
            Transform timerFill = timerBg.Find("TimerFill");
            if (timerFill) timerFillImage = timerFill.GetComponent<Image>();
        }
        
        return skillCanvas != null && arrowContainer != null;
    }

    private IEnumerator FindSkillSystemDelayed()
    {
        yield return null; // Bir frame bekle
        
        skillSystem = GuitarSkillSystem.Instance;
        
        if (skillSystem == null)
            skillSystem = FindFirstObjectByType<GuitarSkillSystem>();

        if (skillSystem != null)
        {
            skillSystem.OnSkillActivated += OnSkillActivated;
            skillSystem.OnInputReceived += OnInputReceived;
            skillSystem.OnSkillComplete += OnSkillComplete;
            skillSystem.OnSkillCancelled += OnSkillCancelled;
            Debug.Log("SkillInputUI: GuitarSkillSystem bağlandı!");
        }
        else
        {
            Debug.LogWarning("SkillInputUI: GuitarSkillSystem bulunamadı!");
        }
    }

    private void OnDestroy()
    {
        if (skillSystem != null)
        {
            skillSystem.OnSkillActivated -= OnSkillActivated;
            skillSystem.OnInputReceived -= OnInputReceived;
            skillSystem.OnSkillComplete -= OnSkillComplete;
            skillSystem.OnSkillCancelled -= OnSkillCancelled;
        }
    }

    private void LateUpdate()
    {
        // Canvas'ın her zaman kameraya bakmasını sağla ve karakter döndüğünde ters dönmesini engelle
        if (skillCanvas != null && Camera.main != null)
        {
            // Kameraya bak
            skillCanvas.transform.rotation = Camera.main.transform.rotation;
            
            // Karakterin scale.x'i negatifse (sola bakıyorsa), canvas'ın scale.x'ini de negatif yap
            // Bu sayede negatif * negatif = pozitif olur ve canvas düz görünür
            float parentScaleX = transform.lossyScale.x;
            
            // Scale'in işaretini parent'a göre ayarla (her zaman pozitif görünsün)
            Vector3 newScale = new Vector3(initialCanvasScale, initialCanvasScale, initialCanvasScale);
            newScale.x = initialCanvasScale * Mathf.Sign(parentScaleX);
            skillCanvas.transform.localScale = newScale;
        }

        // Timer güncelle - unscaledTime kullan çünkü oyun duraklatılmış olabilir
        if (isShowing && skillSystem != null && skillSystem.IsInSkillInput && timerFillImage != null)
        {
            float elapsed = Time.unscaledTime - inputStartTime;
            float remaining = 5f - elapsed; // 5 saniyelik timeout
            timerFillImage.fillAmount = Mathf.Clamp01(remaining / 5f);
            
            // Timer rengi - azaldıkça kırmızıya dön
            timerFillImage.color = Color.Lerp(failColor, currentColor, timerFillImage.fillAmount);
        }
    }

    private void OnSkillActivated(GuitarSkillSystem.SkillType skillType, List<GuitarSkillSystem.ArrowDirection> sequence)
    {
        Debug.Log($"SkillInputUI: OnSkillActivated - {skillType}, {sequence.Count} arrows");
        inputStartTime = Time.unscaledTime;
        ShowArrowSequence(skillType, sequence);
    }

    private void OnInputReceived(int index, bool isCorrect)
    {
        Debug.Log($"SkillInputUI: OnInputReceived - index:{index}, correct:{isCorrect}");
        
        if (index < arrowImages.Count && index < arrowBackgrounds.Count)
        {
            Color resultColor = isCorrect ? successColor : failColor;
            arrowImages[index].color = resultColor;
            arrowBackgrounds[index].color = new Color(resultColor.r * 0.3f, resultColor.g * 0.3f, resultColor.b * 0.3f, 0.9f);
            
            // Scale animasyonu
            StartCoroutine(ScalePopEffect(arrowImages[index].GetComponent<RectTransform>(), isCorrect));
            
            // Sonraki ok'u highlight yap
            if (isCorrect && index + 1 < arrowImages.Count)
            {
                HighlightArrow(index + 1);
            }
        }
    }

    private IEnumerator ScalePopEffect(RectTransform rect, bool success)
    {
        Vector3 originalScale = rect.localScale;
        float targetScale = success ? 1.3f : 0.8f;
        
        // Pop out - unscaledDeltaTime kullan (oyun duraklamış olabilir)
        float elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / 0.1f;
            rect.localScale = Vector3.Lerp(originalScale, originalScale * targetScale, t);
            yield return null;
        }
        
        // Pop back
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / 0.1f;
            rect.localScale = Vector3.Lerp(originalScale * targetScale, originalScale, t);
            yield return null;
        }
        
        rect.localScale = originalScale;
    }

    private void OnSkillComplete(bool success)
    {
        Debug.Log($"SkillInputUI: OnSkillComplete - success:{success}");
        StartCoroutine(HideWithDelay(success ? 0.3f : 0.5f, success));
    }

    private void OnSkillCancelled()
    {
        Debug.Log("SkillInputUI: OnSkillCancelled");
        HideArrowSequence();
    }

    private void ShowArrowSequence(GuitarSkillSystem.SkillType skillType, List<GuitarSkillSystem.ArrowDirection> sequence)
    {
        // DEBUG: Sequence'ı logla
        Debug.Log($"=== ShowArrowSequence ===");
        Debug.Log($"Skill: {skillType}");
        Debug.Log($"Sequence Count: {sequence.Count}");
        for (int i = 0; i < sequence.Count; i++)
        {
            Debug.Log($"  [{i}] = {sequence[i]}");
        }
        Debug.Log($"========================");

        // Önceki okları temizle
        ClearArrows();

        isShowing = true;

        if (arrowContainer != null)
            arrowContainer.SetActive(true);

        // Skill rengi belirle
        Color skillColor = GetSkillColor(skillType);

        // Skill adını göster
        if (skillNameText != null)
        {
            skillNameText.text = GetSkillName(skillType);
            skillNameText.color = skillColor;
            skillNameText.gameObject.SetActive(true);
        }

        // Background'u göster
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
            // Background boyutunu ayarla
            float totalWidth = sequence.Count * arrowSpacing + 40f;
            backgroundImage.rectTransform.sizeDelta = new Vector2(totalWidth, arrowSize + 60f);
        }

        // Timer'ı resetle
        if (timerFillImage != null)
        {
            timerFillImage.fillAmount = 1f;
            timerFillImage.color = currentColor;
            timerFillImage.gameObject.SetActive(true);
        }

        // Okları oluştur
        float totalArrowWidth = (sequence.Count - 1) * arrowSpacing;
        float startX = -totalArrowWidth / 2f;

        for (int i = 0; i < sequence.Count; i++)
        {
            GameObject arrowObj = CreateArrowObject(sequence[i], startX + i * arrowSpacing);
            if (arrowObj != null)
            {
                Image img = arrowObj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = pendingColor;
                    arrowImages.Add(img);
                }
            }
        }

        // İlk ok'u highlight yap
        if (arrowImages.Count > 0)
        {
            HighlightArrow(0);
        }

        // Fade in animasyonu
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    private Color GetSkillColor(GuitarSkillSystem.SkillType skillType)
    {
        switch (skillType)
        {
            case GuitarSkillSystem.SkillType.Heal: return healColor;
            case GuitarSkillSystem.SkillType.Fireball: return fireballColor;
            case GuitarSkillSystem.SkillType.DoubleJump: return doubleJumpColor;
            case GuitarSkillSystem.SkillType.Shockwave: return shockwaveColor;
            default: return Color.white;
        }
    }

    private void HideArrowSequence()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    private IEnumerator HideWithDelay(float delay, bool success)
    {
        // Tüm okları başarı/başarısız rengine boya
        Color resultColor = success ? successColor : failColor;
        
        for (int i = 0; i < arrowImages.Count; i++)
        {
            if (arrowImages[i] != null)
                arrowImages[i].color = resultColor;
            if (i < arrowBackgrounds.Count && arrowBackgrounds[i] != null)
                arrowBackgrounds[i].color = new Color(resultColor.r * 0.3f, resultColor.g * 0.3f, resultColor.b * 0.3f, 0.9f);
        }

        // WaitForSecondsRealtime - oyun duraklamış olsa bile çalışır
        yield return new WaitForSecondsRealtime(delay);
        HideArrowSequence();
    }

    private void ClearArrows()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        foreach (var img in arrowImages)
        {
            if (img != null)
                Destroy(img.gameObject);
        }
        arrowImages.Clear();
        
        foreach (var bg in arrowBackgrounds)
        {
            if (bg != null)
                Destroy(bg.gameObject);
        }
        arrowBackgrounds.Clear();
    }

    private void HighlightArrow(int index)
    {
        if (index < arrowImages.Count)
        {
            arrowImages[index].color = currentColor;
            
            if (index < arrowBackgrounds.Count)
                arrowBackgrounds[index].color = new Color(currentColor.r * 0.3f, currentColor.g * 0.3f, currentColor.b * 0.3f, 0.9f);
            
            // Pulse animasyonu başlat
            if (pulseCoroutine != null)
                StopCoroutine(pulseCoroutine);
            
            pulseCoroutine = StartCoroutine(PulseArrow(arrowImages[index]));
        }
    }

    private IEnumerator PulseArrow(Image arrow)
    {
        if (arrow == null) yield break;
        
        RectTransform rect = arrow.GetComponent<RectTransform>();
        Vector3 originalScale = rect.localScale;
        
        // unscaledTime kullan - oyun duraklamış olsa bile animasyon çalışsın
        while (arrow != null && arrow.gameObject.activeInHierarchy && isShowing)
        {
            float scale = 1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
            rect.localScale = originalScale * scale;
            yield return null;
        }
        
        if (rect != null)
            rect.localScale = originalScale;
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        // Scale animasyonu için
        Vector3 startScale = skillCanvas.transform.localScale * 0.5f;
        Vector3 endScale = skillCanvas.transform.localScale;
        skillCanvas.transform.localScale = startScale;

        // unscaledDeltaTime - oyun duraklamış olsa bile animasyon çalışsın
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeInDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            canvasGroup.alpha = smoothT;
            skillCanvas.transform.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        skillCanvas.transform.localScale = endScale;
    }

    private IEnumerator FadeOut()
    {
        if (canvasGroup == null)
        {
            CleanupAfterHide();
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = skillCanvas.transform.localScale;
        Vector3 endScale = startScale * 0.8f;

        // unscaledDeltaTime - oyun duraklamış olsa bile animasyon çalışsın
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / fadeOutDuration;
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, smoothT);
            skillCanvas.transform.localScale = Vector3.Lerp(startScale, endScale, smoothT);
            yield return null;
        }

        CleanupAfterHide();
        skillCanvas.transform.localScale = startScale; // Reset scale
    }

    private void CleanupAfterHide()
    {
        if (arrowContainer != null)
            arrowContainer.SetActive(false);
        
        isShowing = false;
        ClearArrows();

        if (skillNameText != null)
            skillNameText.gameObject.SetActive(false);
        if (timerFillImage != null)
            timerFillImage.gameObject.SetActive(false);
        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(false);
    }

    private GameObject CreateArrowObject(GuitarSkillSystem.ArrowDirection direction, float xPos)
    {
        // Background circle
        GameObject bgObj = new GameObject($"ArrowBg_{direction}");
        bgObj.transform.SetParent(arrowContainer.transform, false);
        
        RectTransform bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchoredPosition = new Vector2(xPos, 0);
        bgRect.sizeDelta = new Vector2(arrowSize + 10f, arrowSize + 10f);
        bgRect.localScale = Vector3.one;
        
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = circleSprite;
        bgImg.color = backgroundColor;
        arrowBackgrounds.Add(bgImg);
        
        // Arrow
        GameObject arrowObj = new GameObject($"Arrow_{direction}");
        arrowObj.transform.SetParent(bgObj.transform, false);
        
        RectTransform rect = arrowObj.AddComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(arrowSize, arrowSize);
        rect.localScale = Vector3.one;
        
        Image img = arrowObj.AddComponent<Image>();
        img.sprite = GetArrowSprite(direction);
        img.color = pendingColor;
        
        return arrowObj;
    }

    private Sprite GetArrowSprite(GuitarSkillSystem.ArrowDirection direction)
    {
        switch (direction)
        {
            case GuitarSkillSystem.ArrowDirection.Up: return arrowUpSprite;
            case GuitarSkillSystem.ArrowDirection.Down: return arrowDownSprite;
            case GuitarSkillSystem.ArrowDirection.Left: return arrowLeftSprite;
            case GuitarSkillSystem.ArrowDirection.Right: return arrowRightSprite;
            default: return arrowUpSprite;
        }
    }

    private string GetSkillName(GuitarSkillSystem.SkillType skillType)
    {
        switch (skillType)
        {
            case GuitarSkillSystem.SkillType.Heal: return "💙 HEAL";
            case GuitarSkillSystem.SkillType.Fireball: return "🔥 FIREBALL";
            case GuitarSkillSystem.SkillType.DoubleJump: return "🦘 DOUBLE JUMP";
            case GuitarSkillSystem.SkillType.Shockwave: return "💥 SHOCKWAVE";
            default: return "";
        }
    }

    #region Procedural Sprite Creation

    private void CreateArrowSprites()
    {
        int size = 64;
        arrowUpSprite = CreateArrowTexture(size, Vector2.up);
        arrowDownSprite = CreateArrowTexture(size, Vector2.down);
        arrowLeftSprite = CreateArrowTexture(size, Vector2.left);
        arrowRightSprite = CreateArrowTexture(size, Vector2.right);
        circleSprite = CreateCircleTexture(size);
    }

    private Sprite CreateArrowTexture(int size, Vector2 direction)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        
        // Clear
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.clear;
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float arrowLength = size * 0.35f;
        float arrowWidth = size * 0.25f;
        float stemWidth = size * 0.12f;
        
        // Ok çiz
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y) - center;
                
                // Rotate based on direction
                // Up = 0° (yukarı ok), Down = 180° (aşağı ok)
                // Left = -90° (sol ok), Right = 90° (sağ ok)
                float angle = 0f;
                if (direction == Vector2.up) angle = 0f;
                else if (direction == Vector2.down) angle = 180f;
                else if (direction == Vector2.left) angle = -90f;  // Düzeltildi
                else if (direction == Vector2.right) angle = 90f;   // Düzeltildi
                
                float rad = angle * Mathf.Deg2Rad;
                float rotX = pos.x * Mathf.Cos(rad) - pos.y * Mathf.Sin(rad);
                float rotY = pos.x * Mathf.Sin(rad) + pos.y * Mathf.Cos(rad);
                
                // Arrow head (üçgen)
                float headY = arrowLength * 0.3f;
                if (rotY > headY)
                {
                    float headProgress = (rotY - headY) / (arrowLength - headY);
                    float maxWidth = arrowWidth * (1f - headProgress);
                    if (Mathf.Abs(rotX) <= maxWidth)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
                // Stem (dikdörtgen gövde)
                else if (rotY > -arrowLength && rotY <= headY)
                {
                    if (Mathf.Abs(rotX) <= stemWidth)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }
        }
        
        // Anti-alias edge
        ApplyAntiAlias(pixels, size);
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateCircleTexture(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 2f;
        
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

    private void ApplyAntiAlias(Color[] pixels, int size)
    {
        // Basit kenar yumuşatma
        Color[] result = new Color[pixels.Length];
        System.Array.Copy(pixels, result, pixels.Length);
        
        for (int y = 1; y < size - 1; y++)
        {
            for (int x = 1; x < size - 1; x++)
            {
                int idx = y * size + x;
                if (pixels[idx].a > 0.5f)
                {
                    // Kenar pikseli mi kontrol et
                    bool isEdge = false;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int nIdx = (y + dy) * size + (x + dx);
                            if (pixels[nIdx].a < 0.5f)
                            {
                                isEdge = true;
                                break;
                            }
                        }
                        if (isEdge) break;
                    }
                    
                    if (isEdge)
                    {
                        result[idx] = new Color(1f, 1f, 1f, 0.7f);
                    }
                }
            }
        }
        
        System.Array.Copy(result, pixels, pixels.Length);
    }

    #endregion

    #region UI Creation

    private void CreateUICanvas()
    {
        // World space canvas oluştur (karakter üzerinde takip için)
        GameObject canvasObj = new GameObject("SkillInputCanvas");
        canvasObj.transform.SetParent(transform, false);
        canvasObj.transform.localPosition = new Vector3(0, uiHeight, 0);

        skillCanvas = canvasObj.AddComponent<Canvas>();
        skillCanvas.renderMode = RenderMode.WorldSpace;
        skillCanvas.sortingOrder = 200;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(500, 150);
        canvasRect.localScale = Vector3.one * 0.015f;

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Background panel
        GameObject bgPanel = new GameObject("BackgroundPanel");
        bgPanel.transform.SetParent(canvasObj.transform, false);
        
        RectTransform bgPanelRect = bgPanel.AddComponent<RectTransform>();
        bgPanelRect.anchoredPosition = Vector2.zero;
        bgPanelRect.sizeDelta = new Vector2(350, 120);
        
        backgroundImage = bgPanel.AddComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.85f);
        backgroundImage.sprite = CreateRoundedRectSprite(64, 16);
        backgroundImage.type = Image.Type.Sliced;
        backgroundImage.gameObject.SetActive(false);

        // Skill name text
        GameObject textObj = new GameObject("SkillName");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchoredPosition = new Vector2(0, 50);
        textRect.sizeDelta = new Vector2(300, 45);
        
        skillNameText = textObj.AddComponent<TextMeshProUGUI>();
        skillNameText.alignment = TextAlignmentOptions.Center;
        skillNameText.fontSize = 32;
        skillNameText.fontStyle = FontStyles.Bold;
        skillNameText.color = Color.white;
        skillNameText.enableAutoSizing = false;
        skillNameText.gameObject.SetActive(false);

        // Arrow container
        GameObject containerObj = new GameObject("ArrowContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform containerRect = containerObj.AddComponent<RectTransform>();
        containerRect.anchoredPosition = new Vector2(0, -5);
        containerRect.sizeDelta = new Vector2(400, 80);
        containerRect.localScale = Vector3.one;

        arrowContainer = containerObj;
        arrowContainer.SetActive(false);

        // Timer bar (background)
        GameObject timerBgObj = new GameObject("TimerBackground");
        timerBgObj.transform.SetParent(canvasObj.transform, false);
        
        RectTransform timerBgRect = timerBgObj.AddComponent<RectTransform>();
        timerBgRect.anchoredPosition = new Vector2(0, -50);
        timerBgRect.sizeDelta = new Vector2(200, 8);
        
        Image timerBgImg = timerBgObj.AddComponent<Image>();
        timerBgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        timerBgImg.sprite = CreateRoundedRectSprite(32, 8);
        timerBgImg.type = Image.Type.Sliced;

        // Timer fill
        GameObject timerObj = new GameObject("TimerFill");
        timerObj.transform.SetParent(timerBgObj.transform, false);
        
        RectTransform timerRect = timerObj.AddComponent<RectTransform>();
        timerRect.anchorMin = Vector2.zero;
        timerRect.anchorMax = Vector2.one;
        timerRect.sizeDelta = new Vector2(-4, -2);
        timerRect.anchoredPosition = Vector2.zero;
        
        timerFillImage = timerObj.AddComponent<Image>();
        timerFillImage.color = currentColor;
        timerFillImage.sprite = CreateRoundedRectSprite(32, 6);
        timerFillImage.type = Image.Type.Filled;
        timerFillImage.fillMethod = Image.FillMethod.Horizontal;
        timerFillImage.fillOrigin = 0;
        timerFillImage.gameObject.SetActive(false);

        Debug.Log("SkillInputUI: UI oluşturuldu!");
    }

    private Sprite CreateRoundedRectSprite(int size, int cornerRadius)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        
        Color[] pixels = new Color[size * size];
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = true;
                
                // Corner checks
                if (x < cornerRadius && y < cornerRadius)
                {
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius)) <= cornerRadius;
                }
                else if (x >= size - cornerRadius && y < cornerRadius)
                {
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius - 1, cornerRadius)) <= cornerRadius;
                }
                else if (x < cornerRadius && y >= size - cornerRadius)
                {
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, size - cornerRadius - 1)) <= cornerRadius;
                }
                else if (x >= size - cornerRadius && y >= size - cornerRadius)
                {
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius - 1, size - cornerRadius - 1)) <= cornerRadius;
                }
                
                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        // 9-slice border ayarla
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
    }

    #endregion
}
