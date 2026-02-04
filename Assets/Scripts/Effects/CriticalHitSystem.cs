using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Kritik vuruş sistemi - Rastgele ekstra hasarlı vuruşlar
/// </summary>
public class CriticalHitSystem : MonoBehaviour
{
    public static CriticalHitSystem Instance { get; private set; }
    
    [Header("Kritik Vuruş Ayarları")]
    [SerializeField] private float baseCritChance = 0.15f; // %15 kritik şansı
    [SerializeField] private float critDamageMultiplier = 2f; // 2x hasar
    [SerializeField] private float critChancePerCombo = 0.02f; // Combo başına +%2
    
    [Header("Görsel Efektler")]
    [SerializeField] private Color critColor = new Color(1f, 0.5f, 0f, 1f); // Turuncu
    [SerializeField] private Color critTextColor = new Color(1f, 0.6f, 0.1f, 1f); // Turuncu
    
    private ParticleSystem critParticles;
    private ParticleSystem critFlashParticles;
    
    public float CritChance => baseCritChance + (ComboSystem.Instance != null ? ComboSystem.Instance.CurrentCombo * critChancePerCombo : 0f);
    public float CritMultiplier => critDamageMultiplier;
    
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
        CreateCritParticles();
        CreateCritFlashParticles();
    }
    
    /// <summary>
    /// Dash sırasında saldırı yapılıyor mu kontrolü
    /// </summary>
    private bool IsPlayerDashing()
    {
        return PlayerMovement.Instance != null && PlayerMovement.Instance.IsDashing;
    }
    
    /// <summary>
    /// Kritik vuruş kontrolü yap ve hasarı döndür
    /// </summary>
    public float CalculateDamage(float baseDamage, out bool isCritical)
    {
        float critChance = CritChance;
        
        // Dash sırasında %100 kritik şansı
        if (IsPlayerDashing())
        {
            isCritical = true;
        }
        else
        {
            isCritical = Random.value < critChance;
        }
        
        float finalDamage = baseDamage;
        
        // Combo çarpanı
        if (ComboSystem.Instance != null)
        {
            finalDamage *= ComboSystem.Instance.DamageMultiplier;
        }
        
        // Kritik çarpanı
        if (isCritical)
        {
            finalDamage *= critDamageMultiplier;
        }
        
        return finalDamage;
    }
    
    /// <summary>
    /// Kritik vuruş efektini oynat
    /// </summary>
    public void PlayCritEffect(Vector3 position, float damage)
    {
        // Parçacık efekti
        if (critParticles != null)
        {
            critParticles.transform.position = position;
            critParticles.Emit(20);
        }
        
        // Flash efekti
        if (critFlashParticles != null)
        {
            critFlashParticles.transform.position = position;
            critFlashParticles.Emit(1);
        }
        
        // Büyük hasar yazısı
        ShowCritText(position, damage);
        
        // Güçlü hit stop
        if (HitStop.Instance != null)
        {
            HitStop.Instance.CriticalHit();
        }
        
        // Ekran shake
        if (ScreenShake.Instance != null)
        {
            ScreenShake.Instance.ShakeHeavy();
        }
    }
    
    private void CreateCritParticles()
    {
        GameObject particleObj = new GameObject("CritParticles");
        particleObj.transform.SetParent(transform);
        
        critParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = critParticles.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.5f;
        main.startColor = critColor;
        main.playOnAwake = false;
        
        var emission = critParticles.emission;
        emission.rateOverTime = 0;
        
        // Patlama şekli
        var shape = critParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;
        
        // Boyut
        var sizeOverLifetime = critParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 1f);
        sizeCurve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk
        var colorOverLifetime = critParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(critColor, 0.3f),
                new GradientColorKey(new Color(1f, 0.5f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 25;
        
        // ParticleHelper ile material uygula
        ParticleHelper.ApplyMaterial(renderer, true, ParticleHelper.GetSparkTexture());
    }
    
    private void CreateCritFlashParticles()
    {
        GameObject flashObj = new GameObject("CritFlash");
        flashObj.transform.SetParent(transform);
        
        critFlashParticles = flashObj.AddComponent<ParticleSystem>();
        
        var main = critFlashParticles.main;
        main.startLifetime = 0.15f;
        main.startSpeed = 0f;
        main.startSize = 3f;
        main.maxParticles = 5;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new Color(1f, 1f, 0.8f, 0.8f);
        main.playOnAwake = false;
        
        var emission = critFlashParticles.emission;
        emission.rateOverTime = 0;
        
        // Boyut - hızlı büyüme
        var sizeOverLifetime = critFlashParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.5f);
        sizeCurve.AddKey(0.3f, 1f);
        sizeCurve.AddKey(1f, 1.5f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
        
        // Renk - hızlı solma
        var colorOverLifetime = critFlashParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(critTextColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;
        
        var renderer = flashObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 24;
        
        // ParticleHelper ile material uygula
        ParticleHelper.ApplyMaterial(renderer, true, ParticleHelper.GetGlowTexture());
    }
    
    private void ShowCritText(Vector3 worldPosition, float damage)
    {
        StartCoroutine(CritTextRoutine(worldPosition, damage));
    }
    
    private IEnumerator CritTextRoutine(Vector3 worldPosition, float damage)
    {
        // Canvas oluştur
        GameObject canvasObj = new GameObject("CritTextCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 200;
        
        // CanvasScaler ekle
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200f, 100f);
        canvasObj.transform.position = worldPosition + Vector3.up * 0.3f;
        canvasObj.transform.localScale = Vector3.one * 0.008f; // Daha küçük scale
        
        // Text
        GameObject textObj = new GameObject("CritText");
        textObj.transform.SetParent(canvasObj.transform, false);
        
        TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = $"CRITICAL!\n{Mathf.RoundToInt(damage)}";
        text.fontSize = 28; // Daha kompakt font
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.characterSpacing = 4f; // Harfler arası şık boşluk
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        
        // Gradient renk efekti - turuncu'dan sarıya
        text.enableVertexGradient = true;
        text.colorGradient = new VertexGradient(
            new Color(1f, 0.9f, 0.3f),   // Sol üst - parlak sarı
            new Color(1f, 0.7f, 0.2f),   // Sağ üst - turuncu-sarı
            new Color(1f, 0.5f, 0.1f),   // Sol alt - turuncu
            new Color(1f, 0.6f, 0.15f)   // Sağ alt
        );
        
        // Outline ekle - daha belirgin
        text.outlineWidth = 0.15f;
        text.outlineColor = new Color(0.3f, 0.1f, 0f, 1f); // Koyu turuncu/kahverengi
        
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(200f, 100f);
        
        // Animasyon
        float duration = 0.8f;
        float elapsed = 0f;
        Vector3 startPos = canvasObj.transform.position;
        Vector3 startScale = canvasObj.transform.localScale;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Yukarı hareket
            canvasObj.transform.position = startPos + Vector3.up * t * 0.8f;
            
            // Punch scale efekti
            float scaleMultiplier = 1f;
            if (t < 0.15f)
            {
                // Hızlı büyüme
                scaleMultiplier = 1f + (t / 0.15f) * 0.4f;
            }
            else if (t < 0.3f)
            {
                // Geri küçülme
                scaleMultiplier = 1.4f - ((t - 0.15f) / 0.15f) * 0.3f;
            }
            else
            {
                scaleMultiplier = 1.1f;
            }
            canvasObj.transform.localScale = startScale * scaleMultiplier;
            
            // Solma - gradient alpha ile
            float alpha = t < 0.5f ? 1f : 1f - ((t - 0.5f) / 0.5f);
            text.alpha = alpha;
            
            yield return null;
        }
        
        Destroy(canvasObj);
    }
}
