using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Combo sistemi - Ardışık vuruşlarda artan hasar ve özel efektler
/// </summary>
public class ComboSystem : MonoBehaviour
{
    public static ComboSystem Instance { get; private set; }
    
    [Header("Combo Ayarları")]
    [SerializeField] private float comboResetTime = 2f; // Combo sıfırlanma süresi
    [SerializeField] private float baseDamageMultiplier = 1f;
    [SerializeField] private float damageIncreasePerHit = 0.1f; // Her vuruşta %10 artış
    [SerializeField] private float maxDamageMultiplier = 2.5f; // Max %250 hasar
    
    [Header("Combo Seviyeleri")]
    [SerializeField] private int[] comboThresholds = { 3, 5, 10, 15, 25 }; // Combo seviyeleri
    [SerializeField] private Color[] comboColors = {
        new Color(1f, 1f, 1f),      // Normal - Beyaz
        new Color(1f, 1f, 0.3f),    // 3+ Sarı
        new Color(1f, 0.6f, 0.2f),  // 5+ Turuncu
        new Color(1f, 0.3f, 0.3f),  // 10+ Kırmızı
        new Color(1f, 0.2f, 1f),    // 15+ Mor
        new Color(0.3f, 1f, 1f)     // 25+ Cyan
    };
    
    private int currentCombo = 0;
    private float comboTimer = 0f;
    private int comboLevel = 0;
    
    // UI
    private Canvas comboCanvas;
    private TMP_Text comboText;
    private CanvasGroup comboCanvasGroup;
    private Coroutine fadeCoroutine;
    
    // Efekt
    private ParticleSystem comboParticles;
    
    public int CurrentCombo => currentCombo;
    public float DamageMultiplier => Mathf.Min(baseDamageMultiplier + (currentCombo * damageIncreasePerHit), maxDamageMultiplier);
    public int ComboLevel => comboLevel;
    
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
    }
    
    private void Start()
    {
        CreateComboUI();
        CreateComboParticles();
    }
    
    private void Update()
    {
        if (currentCombo > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                ResetCombo();
            }
        }
    }
    
    /// <summary>
    /// Vuruş kaydı - her başarılı vuruşta çağır
    /// </summary>
    public void RegisterHit()
    {
        currentCombo++;
        comboTimer = comboResetTime;
        
        // Combo seviyesini güncelle
        int newLevel = 0;
        for (int i = 0; i < comboThresholds.Length; i++)
        {
            if (currentCombo >= comboThresholds[i])
                newLevel = i + 1;
        }
        
        bool levelUp = newLevel > comboLevel;
        comboLevel = newLevel;
        
        // UI güncelle
        UpdateComboUI(levelUp);
        
        // Seviye atladıysa özel efekt
        if (levelUp && comboLevel > 0)
        {
            PlayLevelUpEffect();
        }
    }
    
    /// <summary>
    /// Combo sıfırla (hasar alınca veya süre dolunca)
    /// </summary>
    public void ResetCombo()
    {
        if (currentCombo > 5)
        {
            // Yüksek combo kaybedildi efekti
            PlayComboLostEffect();
        }
        
        currentCombo = 0;
        comboLevel = 0;
        comboTimer = 0f;
        
        // UI gizle
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutComboUI());
    }
    
    private void CreateComboUI()
    {
        // Canvas
        GameObject canvasObj = new GameObject("ComboCanvas");
        canvasObj.transform.SetParent(transform);
        comboCanvas = canvasObj.AddComponent<Canvas>();
        comboCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        comboCanvas.sortingOrder = 100;
        
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Combo Text Container
        GameObject containerObj = new GameObject("ComboContainer");
        containerObj.transform.SetParent(canvasObj.transform, false);
        
        comboCanvasGroup = containerObj.AddComponent<CanvasGroup>();
        comboCanvasGroup.alpha = 0f;
        
        RectTransform containerRect = containerObj.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.7f);
        containerRect.anchorMax = new Vector2(0.5f, 0.7f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(400, 100);
        
        // Combo Text
        GameObject textObj = new GameObject("ComboText");
        textObj.transform.SetParent(containerObj.transform, false);
        
        comboText = textObj.AddComponent<TextMeshProUGUI>();
        comboText.text = "";
        comboText.fontSize = 48;
        comboText.fontStyle = FontStyles.Bold;
        comboText.alignment = TextAlignmentOptions.Center;
        comboText.color = Color.white;
        
        // Outline
        var outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);
        
        RectTransform textRect = comboText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
    }
    
    private void CreateComboParticles()
    {
        GameObject particleObj = new GameObject("ComboParticles");
        particleObj.transform.SetParent(transform);
        
        comboParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = comboParticles.main;
        main.startLifetime = 0.5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f);
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -1f;
        main.playOnAwake = false;
        
        var emission = comboParticles.emission;
        emission.rateOverTime = 0;
        
        var shape = comboParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        
        var colorOverLifetime = comboParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.yellow, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 101;
        
        // ParticleHelper ile material uygula
        ParticleHelper.ApplyMaterial(renderer, true, ParticleHelper.GetGlowTexture());
    }
    
    private void UpdateComboUI(bool levelUp)
    {
        if (comboText == null) return;
        
        // Fade in
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        comboCanvasGroup.alpha = 1f;
        
        // Text güncelle
        Color textColor = comboColors[Mathf.Min(comboLevel, comboColors.Length - 1)];
        comboText.color = textColor;
        
        string comboString = $"{currentCombo} HIT";
        if (comboLevel > 0)
        {
            string[] levelNames = { "", "NICE!", "GREAT!", "AWESOME!", "INCREDIBLE!", "LEGENDARY!" };
            comboString += $"\n<size=60%>{levelNames[Mathf.Min(comboLevel, levelNames.Length - 1)]}</size>";
        }
        comboText.text = comboString;
        
        // Punch animasyonu
        StartCoroutine(PunchScale(comboText.transform, levelUp ? 1.3f : 1.15f));
    }
    
    private IEnumerator PunchScale(Transform target, float scale)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 punchScale = originalScale * scale;
        
        float duration = 0.15f;
        float elapsed = 0f;
        
        // Büyüt
        while (elapsed < duration * 0.4f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (duration * 0.4f);
            target.localScale = Vector3.Lerp(originalScale, punchScale, t);
            yield return null;
        }
        
        // Küçült
        elapsed = 0f;
        while (elapsed < duration * 0.6f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / (duration * 0.6f);
            t = 1f - (1f - t) * (1f - t); // Ease out
            target.localScale = Vector3.Lerp(punchScale, originalScale, t);
            yield return null;
        }
        
        target.localScale = originalScale;
    }
    
    private IEnumerator FadeOutComboUI()
    {
        yield return new WaitForSeconds(0.5f);
        
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            comboCanvasGroup.alpha = 1f - (elapsed / duration);
            yield return null;
        }
        
        comboCanvasGroup.alpha = 0f;
    }
    
    private void PlayLevelUpEffect()
    {
        // Oyuncuyu bul ve efekt çıkar
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && comboParticles != null)
        {
            comboParticles.transform.position = player.transform.position;
            
            var main = comboParticles.main;
            main.startColor = comboColors[Mathf.Min(comboLevel, comboColors.Length - 1)];
            
            comboParticles.Emit(15);
        }
        
        // Ekran shake
        if (HitStop.Instance != null)
        {
            HitStop.Instance.HeavyHit();
        }
    }
    
    private void PlayComboLostEffect()
    {
        // Combo kaybedildi efekti - ekrana kısa bir flash
        // İsteğe bağlı: ses efekti eklenebilir
    }
}
