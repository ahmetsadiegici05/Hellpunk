using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Ritim tabanlı timing puzzle'ı - TextMeshPro ile
/// Oyuncu doğru zamanda SPACE'e basmalı
/// </summary>
public class RhythmPuzzleUI : MonoBehaviour
{
    // Static flag - puzzle aktifken karakter zıplamasın
    public static bool IsPuzzleActive { get; private set; } = false;
    
    [Header("Puzzle Settings")]
    [SerializeField] private int requiredHits = 5;
    [SerializeField] private int maxMisses = 3;
    [SerializeField] private float beatSpeed = 200f;
    [SerializeField] private float hitZoneWidth = 40f;
    
    [Header("UI References (Assign in Inspector)")]
    [SerializeField] private RectTransform beatIndicator;
    [SerializeField] private RectTransform hitZone;
    [SerializeField] private RectTransform beatBar;
    [SerializeField] private Image hitZoneImage;
    [SerializeField] private Image beatIndicatorImage;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Image[] hitMarkers;
    
    private int currentHits = 0;
    private int currentMisses = 0;
    private bool isActive = false;
    private bool beatMovingRight = true;
    private float barWidth = 300f;
    private float pulseTimer = 0f;
    
    private System.Action onSolved;
    private System.Action onCancelled;
    
    private Color hitZoneNormal = new Color(0.2f, 0.8f, 0.3f, 0.5f);
    private Color hitZoneHit = new Color(0.3f, 1f, 0.4f, 0.8f);
    private Color hitZoneMiss = new Color(1f, 0.3f, 0.3f, 0.8f);

    void Update()
    {
        if (!isActive || !gameObject.activeInHierarchy) return;
        
        MoveBeat();
        HandleInput();
        UpdatePulse();
    }
    
    /// <summary>
    /// Puzzle'ı başlat
    /// </summary>
    public void InitializeFromPrefab(int difficulty, System.Action onSolvedCallback, System.Action onCancelledCallback)
    {
        onSolved = onSolvedCallback;
        onCancelled = onCancelledCallback;
        
        // Difficulty ayarları
        requiredHits = 4 + difficulty;
        maxMisses = 4 - difficulty;
        beatSpeed = 180f + difficulty * 40f;
        hitZoneWidth = 50f - difficulty * 10f;
        
        // State reset
        currentHits = 0;
        currentMisses = 0;
        isActive = true;
        beatMovingRight = true;
        pulseTimer = 0f;
        IsPuzzleActive = true; // Karakter zıplamasın
        
        if (beatBar != null)
            barWidth = beatBar.sizeDelta.x;
        
        // UI Reset
        ResetUI();
        UpdateStatus();
        
        Time.timeScale = 1f;
        gameObject.SetActive(true);
    }
    
    private void ResetUI()
    {
        // Beat indicator'ı başa al
        if (beatIndicator != null)
        {
            beatIndicator.anchoredPosition = new Vector2(-barWidth / 2, 0);
        }
        
        // Hit zone ayarla
        if (hitZone != null)
        {
            hitZone.sizeDelta = new Vector2(hitZoneWidth, hitZone.sizeDelta.y);
        }
        
        // Hit zone rengini sıfırla
        if (hitZoneImage != null)
        {
            hitZoneImage.color = hitZoneNormal;
        }
        
        // Beat indicator rengini sıfırla
        if (beatIndicatorImage != null)
        {
            beatIndicatorImage.color = Color.white;
        }
        
        // Hit marker'ları sıfırla
        if (hitMarkers != null)
        {
            for (int i = 0; i < hitMarkers.Length; i++)
            {
                if (hitMarkers[i] != null)
                    hitMarkers[i].color = new Color(0.3f, 0.3f, 0.3f);
            }
        }
        
        // Instruction text
        if (instructionText != null)
        {
            instructionText.text = "Press SPACE when the beat is in the green zone!";
        }
    }

    private void MoveBeat()
    {
        if (beatIndicator == null) return;
        
        float movement = beatSpeed * Time.deltaTime * (beatMovingRight ? 1 : -1);
        Vector2 pos = beatIndicator.anchoredPosition;
        pos.x += movement;
        
        // Sınır kontrolü
        float halfBar = barWidth / 2;
        if (pos.x >= halfBar)
        {
            pos.x = halfBar;
            beatMovingRight = false;
        }
        else if (pos.x <= -halfBar)
        {
            pos.x = -halfBar;
            beatMovingRight = true;
        }
        
        beatIndicator.anchoredPosition = pos;
    }

    private void HandleInput()
    {
        // ESC ile çıkış
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isActive = false;
            CancelPuzzle();
            return;
        }
        
        // SPACE ile vuruş
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckHit();
        }
    }

    private void CheckHit()
    {
        if (beatIndicator == null) return;
        
        float beatPos = beatIndicator.anchoredPosition.x;
        float hitZoneHalfWidth = hitZoneWidth / 2;
        
        if (Mathf.Abs(beatPos) <= hitZoneHalfWidth)
        {
            // Hit!
            currentHits++;
            if (hitMarkers != null && currentHits - 1 < hitMarkers.Length && hitMarkers[currentHits - 1] != null)
            {
                hitMarkers[currentHits - 1].color = new Color(0.2f, 0.9f, 0.3f);
            }
            StartCoroutine(FlashHitZone(hitZoneHit));
            
            if (currentHits >= requiredHits)
            {
                isActive = false;
                if (statusText != null)
                    statusText.text = "PERFECT RHYTHM!";
                Invoke(nameof(SolvePuzzle), 1f);
            }
        }
        else
        {
            // Miss!
            currentMisses++;
            StartCoroutine(FlashHitZone(hitZoneMiss));
            
            if (currentMisses >= maxMisses)
            {
                isActive = false;
                if (statusText != null)
                    statusText.text = "OUT OF RHYTHM!";
                Invoke(nameof(CancelPuzzle), 1.5f);
            }
        }
        
        UpdateStatus();
    }

    private IEnumerator FlashHitZone(Color flashColor)
    {
        if (hitZoneImage != null)
        {
            hitZoneImage.color = flashColor;
            yield return new WaitForSeconds(0.15f);
            hitZoneImage.color = hitZoneNormal;
        }
    }

    private void UpdateStatus()
    {
        if (statusText != null)
        {
            statusText.text = $"Hits: {currentHits}/{requiredHits}  |  Misses: {currentMisses}/{maxMisses}";
        }
    }
    
    private void UpdatePulse()
    {
        pulseTimer += Time.deltaTime * 3f;
        if (beatIndicatorImage != null)
        {
            float pulse = 0.8f + Mathf.Sin(pulseTimer) * 0.2f;
            beatIndicatorImage.color = new Color(pulse, pulse, pulse);
        }
    }

    private void SolvePuzzle()
    {
        Time.timeScale = 1f;
        IsPuzzleActive = false;
        onSolved?.Invoke();
        gameObject.SetActive(false);
    }

    private void CancelPuzzle()
    {
        Time.timeScale = 1f;
        IsPuzzleActive = false;
        onCancelled?.Invoke();
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Preview UI")]
    private void GeneratePreviewUI()
    {
        // Eski child'ları temizle
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        barWidth = 320f;
        
        // Tema renkleri - oyunun mor/turuncu teması
        Color bgColor = new Color(0.08f, 0.05f, 0.12f, 0.98f); // Koyu mor
        Color accentColor = new Color(0.6f, 0.3f, 0.8f); // Mor accent
        Color highlightColor = new Color(1f, 0.5f, 0.2f); // Turuncu
        Color panelColor = new Color(0.15f, 0.1f, 0.2f, 0.95f); // Panel mor
        
        // RectTransform ayarla
        RectTransform myRect = GetComponent<RectTransform>();
        if (myRect == null) myRect = gameObject.AddComponent<RectTransform>();
        myRect.anchorMin = new Vector2(0.5f, 0.5f);
        myRect.anchorMax = new Vector2(0.5f, 0.5f);
        myRect.pivot = new Vector2(0.5f, 0.5f);
        myRect.sizeDelta = new Vector2(450, 340);
        myRect.anchoredPosition = Vector2.zero;
        myRect.localScale = Vector3.one;
        
        // Background - gradient efekti için çerçeve
        Image bg = GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = bgColor;
        
        // İç panel (border efekti için)
        GameObject innerPanel = new GameObject("InnerPanel");
        innerPanel.transform.SetParent(transform, false);
        RectTransform innerRect = innerPanel.AddComponent<RectTransform>();
        SetupRect(innerRect, Vector2.zero, new Vector2(440, 330));
        Image innerImg = innerPanel.AddComponent<Image>();
        innerImg.color = new Color(0.12f, 0.08f, 0.18f, 0.9f);
        
        // Üst dekoratif çizgi
        GameObject topLine = new GameObject("TopLine");
        topLine.transform.SetParent(transform, false);
        RectTransform topLineRect = topLine.AddComponent<RectTransform>();
        SetupRect(topLineRect, new Vector2(0, 155), new Vector2(380, 3));
        Image topLineImg = topLine.AddComponent<Image>();
        topLineImg.color = accentColor;
        
        // Başlık
        titleText = CreateTMPText(transform, "♪ HIT THE BEAT! ♪", new Vector2(0, 125), 34);
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = highlightColor;
        
        // Beat bar container
        GameObject barContainer = new GameObject("BeatBarContainer");
        barContainer.transform.SetParent(transform, false);
        RectTransform barContainerRect = barContainer.AddComponent<RectTransform>();
        SetupRect(barContainerRect, new Vector2(0, 45), new Vector2(barWidth + 20, 55));
        Image barContainerImg = barContainer.AddComponent<Image>();
        barContainerImg.color = new Color(0.2f, 0.15f, 0.25f);
        
        // Beat bar
        GameObject barObj = new GameObject("BeatBar");
        barObj.transform.SetParent(barContainer.transform, false);
        beatBar = barObj.AddComponent<RectTransform>();
        SetupRect(beatBar, Vector2.zero, new Vector2(barWidth, 45));
        Image barImage = barObj.AddComponent<Image>();
        barImage.color = new Color(0.1f, 0.08f, 0.15f);
        
        // Hit zone - parlak yeşil
        GameObject hitZoneObj = new GameObject("HitZone");
        hitZoneObj.transform.SetParent(barObj.transform, false);
        hitZone = hitZoneObj.AddComponent<RectTransform>();
        SetupRect(hitZone, Vector2.zero, new Vector2(55, 45));
        hitZoneImage = hitZoneObj.AddComponent<Image>();
        hitZoneImage.color = new Color(0.2f, 0.9f, 0.4f, 0.6f);
        
        // Beat indicator - parlak beyaz
        GameObject beatObj = new GameObject("BeatIndicator");
        beatObj.transform.SetParent(barObj.transform, false);
        beatIndicator = beatObj.AddComponent<RectTransform>();
        SetupRect(beatIndicator, new Vector2(-barWidth / 2, 0), new Vector2(12, 55));
        beatIndicatorImage = beatObj.AddComponent<Image>();
        beatIndicatorImage.color = Color.white;
        
        // Status text
        statusText = CreateTMPText(transform, "Hits: 0/5  |  Misses: 0/3", new Vector2(0, -20), 22);
        statusText.color = Color.white;
        
        // Hit markers - yıldız şeklinde
        hitMarkers = new Image[5];
        float markerStartX = -100f;
        for (int i = 0; i < 5; i++)
        {
            GameObject markerObj = new GameObject($"HitMarker_{i}");
            markerObj.transform.SetParent(transform, false);
            RectTransform markerRect = markerObj.AddComponent<RectTransform>();
            SetupRect(markerRect, new Vector2(markerStartX + i * 50, -65), new Vector2(38, 38));
            hitMarkers[i] = markerObj.AddComponent<Image>();
            hitMarkers[i].color = new Color(0.25f, 0.2f, 0.3f);
            
            // Marker içi text (★)
            TextMeshProUGUI starText = CreateTMPText(markerObj.transform, "★", Vector2.zero, 24);
            starText.color = new Color(0.4f, 0.35f, 0.45f);
        }
        
        // Alt dekoratif çizgi
        GameObject bottomLine = new GameObject("BottomLine");
        bottomLine.transform.SetParent(transform, false);
        RectTransform bottomLineRect = bottomLine.AddComponent<RectTransform>();
        SetupRect(bottomLineRect, new Vector2(0, -110), new Vector2(380, 2));
        Image bottomLineImg = bottomLine.AddComponent<Image>();
        bottomLineImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);
        
        // Instruction
        instructionText = CreateTMPText(transform, "[ SPACE ] Hit the beat in the green zone!", new Vector2(0, -135), 15);
        instructionText.color = new Color(0.7f, 0.6f, 0.8f);
        
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("RhythmPuzzleUI Preview Generated!");
    }
    
    private void SetupRect(RectTransform rect, Vector2 pos, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }
    
    private TextMeshProUGUI CreateTMPText(Transform parent, string content, Vector2 position, int fontSize)
    {
        GameObject obj = new GameObject("Text_TMP");
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(400, 50);
        rect.localScale = Vector3.one;
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }
#endif
}
