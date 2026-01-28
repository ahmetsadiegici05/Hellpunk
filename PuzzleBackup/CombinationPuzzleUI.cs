using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Mastermind tarzı şifre çözme puzzle'ı - TextMeshPro ile
/// Oyuncu gizli bir şifreyi tahmin etmeye çalışır
/// </summary>
public class CombinationPuzzleUI : MonoBehaviour
{
    [Header("Puzzle Settings")]
    [SerializeField] private int digitCount = 4;
    [SerializeField] private int maxAttempts = 6;
    [SerializeField] private int maxDigitValue = 9;
    
    [Header("UI References (Assign in Inspector)")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI[] digitTexts;
    [SerializeField] private Image[] digitBackgrounds;
    [SerializeField] private TextMeshProUGUI[] attemptTexts;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private TextMeshProUGUI instructionText;
    
    private int[] secretCode;
    private int[] currentGuess;
    private int currentDigitIndex = 0;
    private int attemptCount = 0;
    
    private System.Action onSolved;
    private System.Action onCancelled;
    
    private Color normalColor = new Color(0.2f, 0.2f, 0.3f);
    private Color selectedColor = new Color(0.3f, 0.5f, 0.8f);
    private Color correctColor = new Color(0.2f, 0.7f, 0.3f);
    private Color wrongPlaceColor = new Color(0.8f, 0.6f, 0.2f);

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        
        HandleInput();
    }
    
    /// <summary>
    /// Puzzle'ı başlat
    /// </summary>
    public void InitializeFromPrefab(int difficulty, System.Action onSolvedCallback, System.Action onCancelledCallback)
    {
        onSolved = onSolvedCallback;
        onCancelled = onCancelledCallback;
        
        // Difficulty ayarları
        digitCount = Mathf.Min(3 + difficulty, digitTexts != null ? digitTexts.Length : 4);
        maxAttempts = 7 - difficulty;
        
        // State reset
        currentDigitIndex = 0;
        attemptCount = 0;
        
        // UI Reset
        ResetUI();
        GenerateSecretCode();
        UpdateDigitDisplay();
        
        Time.timeScale = 0f;
        gameObject.SetActive(true);
    }
    
    private void ResetUI()
    {
        currentGuess = new int[digitCount];
        
        if (digitTexts != null)
        {
            for (int i = 0; i < digitTexts.Length; i++)
            {
                if (digitTexts[i] != null)
                {
                    digitTexts[i].text = "0";
                    digitTexts[i].gameObject.SetActive(i < digitCount);
                }
            }
        }
        
        if (digitBackgrounds != null)
        {
            for (int i = 0; i < digitBackgrounds.Length; i++)
            {
                if (digitBackgrounds[i] != null)
                {
                    digitBackgrounds[i].color = normalColor;
                    digitBackgrounds[i].gameObject.SetActive(i < digitCount);
                }
            }
        }
        
        if (attemptTexts != null)
        {
            for (int i = 0; i < attemptTexts.Length; i++)
            {
                if (attemptTexts[i] != null)
                    attemptTexts[i].text = "";
            }
        }
        
        if (hintText != null)
            hintText.text = "Crack the code!";
    }

    private void GenerateSecretCode()
    {
        secretCode = new int[digitCount];
        for (int i = 0; i < digitCount; i++)
        {
            secretCode[i] = Random.Range(0, maxDigitValue + 1);
        }
        Debug.Log($"Secret Code: {string.Join("", secretCode)}");
    }

    private void HandleInput()
    {
        // ESC ile çıkış
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPuzzle();
            return;
        }
        
        // Sol/Sağ ile digit seçimi
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            currentDigitIndex = (currentDigitIndex - 1 + digitCount) % digitCount;
            UpdateDigitDisplay();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            currentDigitIndex = (currentDigitIndex + 1) % digitCount;
            UpdateDigitDisplay();
        }
        
        // Yukarı/Aşağı ile değer değiştirme
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            currentGuess[currentDigitIndex] = (currentGuess[currentDigitIndex] + 1) % (maxDigitValue + 1);
            UpdateDigitDisplay();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            currentGuess[currentDigitIndex] = (currentGuess[currentDigitIndex] - 1 + maxDigitValue + 1) % (maxDigitValue + 1);
            UpdateDigitDisplay();
        }
        
        // Enter ile tahmin
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            MakeGuess();
        }
    }

    private void UpdateDigitDisplay()
    {
        if (digitTexts == null) return;
        
        for (int i = 0; i < digitCount && i < digitTexts.Length; i++)
        {
            if (digitTexts[i] != null)
                digitTexts[i].text = currentGuess[i].ToString();
            if (digitBackgrounds != null && i < digitBackgrounds.Length && digitBackgrounds[i] != null)
                digitBackgrounds[i].color = (i == currentDigitIndex) ? selectedColor : normalColor;
        }
    }

    private void MakeGuess()
    {
        int correctPosition = 0;
        int wrongPosition = 0;
        
        bool[] secretUsed = new bool[digitCount];
        bool[] guessUsed = new bool[digitCount];
        
        // Doğru pozisyonları bul
        for (int i = 0; i < digitCount; i++)
        {
            if (currentGuess[i] == secretCode[i])
            {
                correctPosition++;
                secretUsed[i] = true;
                guessUsed[i] = true;
            }
        }
        
        // Yanlış pozisyondakileri bul
        for (int i = 0; i < digitCount; i++)
        {
            if (guessUsed[i]) continue;
            for (int j = 0; j < digitCount; j++)
            {
                if (secretUsed[j]) continue;
                if (currentGuess[i] == secretCode[j])
                {
                    wrongPosition++;
                    secretUsed[j] = true;
                    break;
                }
            }
        }
        
        // Sonucu göster
        string guessStr = string.Join("", currentGuess);
        string result = $"{guessStr} - {correctPosition} correct, {wrongPosition} wrong place";
        
        if (attemptTexts != null && attemptCount < attemptTexts.Length && attemptTexts[attemptCount] != null)
        {
            attemptTexts[attemptCount].text = result;
            attemptTexts[attemptCount].color = correctPosition == digitCount ? correctColor : 
                                               correctPosition > 0 ? wrongPlaceColor : Color.white;
        }
        
        attemptCount++;
        
        // Kazandı mı?
        if (correctPosition == digitCount)
        {
            if (hintText != null)
                hintText.text = "CODE CRACKED!";
            Invoke(nameof(SolvePuzzle), 1f);
        }
        else if (attemptCount >= maxAttempts)
        {
            if (hintText != null)
                hintText.text = $"FAILED! Code was: {string.Join("", secretCode)}";
            Invoke(nameof(CancelPuzzle), 2f);
        }
        else
        {
            if (hintText != null)
                hintText.text = $"Attempts left: {maxAttempts - attemptCount}";
        }
    }

    private void SolvePuzzle()
    {
        Time.timeScale = 1f;
        onSolved?.Invoke();
        gameObject.SetActive(false);
    }

    private void CancelPuzzle()
    {
        Time.timeScale = 1f;
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
        
        // Tema renkleri - oyunun mor/turuncu teması
        Color bgColor = new Color(0.08f, 0.05f, 0.12f, 0.98f);
        Color accentColor = new Color(0.6f, 0.3f, 0.8f);
        Color highlightColor = new Color(1f, 0.5f, 0.2f);
        Color digitBgColor = new Color(0.2f, 0.15f, 0.28f);
        Color digitSelectedColor = new Color(0.5f, 0.3f, 0.7f);
        
        // RectTransform ayarla
        RectTransform myRect = GetComponent<RectTransform>();
        if (myRect == null) myRect = gameObject.AddComponent<RectTransform>();
        myRect.anchorMin = new Vector2(0.5f, 0.5f);
        myRect.anchorMax = new Vector2(0.5f, 0.5f);
        myRect.pivot = new Vector2(0.5f, 0.5f);
        myRect.sizeDelta = new Vector2(480, 500);
        myRect.anchoredPosition = Vector2.zero;
        myRect.localScale = Vector3.one;
        
        // Background
        Image bg = GetComponent<Image>();
        if (bg == null) bg = gameObject.AddComponent<Image>();
        bg.color = bgColor;
        
        // İç panel
        GameObject innerPanel = new GameObject("InnerPanel");
        innerPanel.transform.SetParent(transform, false);
        RectTransform innerRect = innerPanel.AddComponent<RectTransform>();
        SetupRect(innerRect, Vector2.zero, new Vector2(470, 490));
        Image innerImg = innerPanel.AddComponent<Image>();
        innerImg.color = new Color(0.12f, 0.08f, 0.18f, 0.9f);
        
        // Üst dekoratif çizgi
        GameObject topLine = new GameObject("TopLine");
        topLine.transform.SetParent(transform, false);
        RectTransform topLineRect = topLine.AddComponent<RectTransform>();
        SetupRect(topLineRect, new Vector2(0, 230), new Vector2(400, 3));
        Image topLineImg = topLine.AddComponent<Image>();
        topLineImg.color = accentColor;
        
        // Kilit ikonu
        TextMeshProUGUI lockIcon = CreateTMPText(transform, "🔐", new Vector2(0, 205), 28);
        lockIcon.color = highlightColor;
        
        // Başlık
        titleText = CreateTMPText(transform, "CRACK THE CODE", new Vector2(0, 170), 32);
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = highlightColor;
        
        // Digit göstergeleri (4 adet)
        digitTexts = new TextMeshProUGUI[4];
        digitBackgrounds = new Image[4];
        
        float startX = -130f;
        for (int i = 0; i < 4; i++)
        {
            // Digit container
            GameObject digitContainer = new GameObject($"DigitContainer_{i}");
            digitContainer.transform.SetParent(transform, false);
            RectTransform containerRect = digitContainer.AddComponent<RectTransform>();
            SetupRect(containerRect, new Vector2(startX + i * 85, 100), new Vector2(72, 95));
            Image containerImg = digitContainer.AddComponent<Image>();
            containerImg.color = new Color(0.15f, 0.1f, 0.2f);
            
            // Digit background
            GameObject digitObj = new GameObject($"Digit_{i}");
            digitObj.transform.SetParent(digitContainer.transform, false);
            RectTransform digitRect = digitObj.AddComponent<RectTransform>();
            SetupRect(digitRect, Vector2.zero, new Vector2(65, 88));
            digitBackgrounds[i] = digitObj.AddComponent<Image>();
            digitBackgrounds[i].color = digitBgColor;
            
            digitTexts[i] = CreateTMPText(digitObj.transform, "0", Vector2.zero, 48);
            digitTexts[i].color = Color.white;
            
            // Ok işaretleri
            TextMeshProUGUI upArrow = CreateTMPText(digitContainer.transform, "▲", new Vector2(0, 38), 14);
            upArrow.color = new Color(0.5f, 0.4f, 0.6f);
            TextMeshProUGUI downArrow = CreateTMPText(digitContainer.transform, "▼", new Vector2(0, -38), 14);
            downArrow.color = new Color(0.5f, 0.4f, 0.6f);
        }
        
        // Hint text
        hintText = CreateTMPText(transform, "[ ↑↓ ] Change  [ ←→ ] Move  [ ENTER ] Guess", new Vector2(0, 20), 15);
        hintText.color = new Color(0.7f, 0.6f, 0.8f);
        
        // Ayırıcı çizgi
        GameObject separatorLine = new GameObject("SeparatorLine");
        separatorLine.transform.SetParent(transform, false);
        RectTransform sepRect = separatorLine.AddComponent<RectTransform>();
        SetupRect(sepRect, new Vector2(0, -5), new Vector2(350, 2));
        Image sepImg = separatorLine.AddComponent<Image>();
        sepImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f);
        
        // Deneme geçmişi başlığı
        TextMeshProUGUI historyTitle = CreateTMPText(transform, "— ATTEMPTS —", new Vector2(0, -25), 14);
        historyTitle.color = new Color(0.5f, 0.4f, 0.6f);
        
        // Deneme geçmişi (6 adet)
        attemptTexts = new TextMeshProUGUI[6];
        for (int i = 0; i < 6; i++)
        {
            attemptTexts[i] = CreateTMPText(transform, "", new Vector2(0, -55 - i * 28), 16);
            attemptTexts[i].color = new Color(0.8f, 0.8f, 0.8f);
        }
        
        // Alt çizgi
        GameObject bottomLine = new GameObject("BottomLine");
        bottomLine.transform.SetParent(transform, false);
        RectTransform bottomRect = bottomLine.AddComponent<RectTransform>();
        SetupRect(bottomRect, new Vector2(0, -215), new Vector2(400, 2));
        Image bottomImg = bottomLine.AddComponent<Image>();
        bottomImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f);
        
        // ESC instruction
        instructionText = CreateTMPText(transform, "[ ESC ] Cancel", new Vector2(0, -235), 13);
        instructionText.color = new Color(0.5f, 0.4f, 0.55f);
        
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("CombinationPuzzleUI Preview Generated!");
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
        rect.sizeDelta = new Vector2(420, 50);
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
