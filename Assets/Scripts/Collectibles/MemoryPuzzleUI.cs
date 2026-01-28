using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hafıza kartı eşleştirme puzzle'ı - TextMeshPro ile
/// Oyuncu aynı sembolleri eşleştirmeli
/// </summary>
public class MemoryPuzzleUI : MonoBehaviour
{
    [Header("Puzzle Settings")]
    [SerializeField] private int cardPairCount = 4;
    [SerializeField] private float showTime = 2f;
    [SerializeField] private int maxMistakes = 5;
    
    [Header("UI References (Assign in Inspector)")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Image[] cardImages;
    [SerializeField] private TextMeshProUGUI[] cardTexts;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI instructionText;
    
    private int[] cardValues;
    private bool[] cardMatched;
    private bool[] cardRevealed;
    private int? firstSelectedIndex = null;
    private int currentMistakes = 0;
    private int matchedPairs = 0;
    private bool isShowingCards = true;
    private bool isProcessing = false;
    private int currentHoverIndex = 0;
    
    private System.Action onSolved;
    private System.Action onCancelled;
    
    public static bool IsPuzzleActive { get; private set; } = false;
    
    // Rock/Punk temalı - sadece çalışan nota sembolleri (farklı kombinasyonlar)
    private string[] cardSymbols = { "♪", "♫", "♪♪", "♫♫", "♪♫", "♫♪", "♪♪♪", "♫♫♫" };
    
    private Color cardBackColor = new Color(0.25f, 0.2f, 0.3f);       // Koyu mor - rock teması
    private Color cardFrontColor = new Color(0.6f, 0.3f, 0.5f);       // Mor-pembe - punk vibes
    private Color cardMatchedColor = new Color(0.8f, 0.5f, 0.1f);     // Turuncu-altın - başarı
    private Color cardHoverColor = new Color(0.8f, 0.3f, 0.4f);       // Kırmızımsı - seçim

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        if (isShowingCards || isProcessing) return;
        
        HandleInput();
    }
    
    /// <summary>
    /// Puzzle'ı başlat
    /// </summary>
    public void InitializeFromPrefab(int difficulty, System.Action onSolvedCallback, System.Action onCancelledCallback)
    {
        // ÖNCE aktif et - coroutine inaktif objede çalışmaz!
        gameObject.SetActive(true);
        
        // Müziği kıs ve oyuncuyu durdur
        if (GameManager.Instance != null)
            GameManager.Instance.OnPuzzleStart();
        
        IsPuzzleActive = true;
        onSolved = onSolvedCallback;
        onCancelled = onCancelledCallback;
        
        // State reset
        currentMistakes = 0;
        matchedPairs = 0;
        isShowingCards = true;
        isProcessing = false;
        firstSelectedIndex = null;
        currentHoverIndex = 0;
        
        // Difficulty settings
        int totalCards = cardImages != null ? cardImages.Length : 8;
        cardPairCount = totalCards / 2;
        showTime = 3f - difficulty * 0.5f;
        maxMistakes = 6 - difficulty;
        
        // Dizileri başlat
        cardValues = new int[totalCards];
        cardMatched = new bool[totalCards];
        cardRevealed = new bool[totalCards];
        
        // UI Reset
        ResetUI();
        
        ShuffleCards();
        
        Time.timeScale = 1f;
        
        // Coroutine'i aktif olduktan sonra başlat
        StartCoroutine(ShowCardsRoutine());
        
        Debug.Log("[MemoryPuzzleUI] Puzzle başlatıldı!");
    }
    
    private void ResetUI()
    {
        // Tüm kartları arka yüze çevir
        if (cardImages != null)
        {
            for (int i = 0; i < cardImages.Length; i++)
            {
                if (cardImages[i] != null)
                    cardImages[i].color = cardBackColor;
            }
        }
        
        if (cardTexts != null)
        {
            for (int i = 0; i < cardTexts.Length; i++)
            {
                if (cardTexts[i] != null)
                {
                    cardTexts[i].text = "?";
                    cardTexts[i].color = Color.white;
                }
            }
        }
        
        // Status text sıfırla
        if (statusText != null)
        {
            statusText.text = "Memorize the cards...";
            statusText.color = Color.yellow;
        }
    }

    private void ShuffleCards()
    {
        int totalCards = cardPairCount * 2;
        List<int> values = new List<int>();
        
        for (int i = 0; i < cardPairCount; i++)
        {
            values.Add(i);
            values.Add(i);
        }
        
        // Fisher-Yates shuffle
        for (int i = values.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = values[i];
            values[i] = values[j];
            values[j] = temp;
        }
        
        for (int i = 0; i < totalCards && i < cardValues.Length; i++)
        {
            cardValues[i] = values[i];
        }
    }

    private IEnumerator ShowCardsRoutine()
    {
        isShowingCards = true;
        
        // Null kontrol
        if (cardImages == null || cardImages.Length == 0)
        {
            Debug.LogError("[MemoryPuzzleUI] cardImages null veya boş! Inspector'da ata.");
            isShowingCards = false;
            yield break;
        }
        
        // Tüm kartları göster
        for (int i = 0; i < cardImages.Length; i++)
        {
            RevealCard(i);
        }
        
        // WaitForSecondsRealtime kullan (Time.timeScale'den bağımsız)
        yield return new WaitForSecondsRealtime(showTime);
        
        // Kartları gizle
        for (int i = 0; i < cardImages.Length; i++)
        {
            HideCard(i);
        }
        
        isShowingCards = false;
        UpdateHoverVisual();
        UpdateStatus();
        
        Debug.Log("[MemoryPuzzleUI] Kartlar gizlendi, input aktif!");
    }

    private void HandleInput()
    {
        // ESC ile çıkış
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPuzzle();
            return;
        }
        
        int cols = 4;
        int totalCards = cardImages.Length;
        
        // Navigation
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            do {
                currentHoverIndex = (currentHoverIndex - 1 + totalCards) % totalCards;
            } while (cardMatched[currentHoverIndex] && !AllMatched());
            UpdateHoverVisual();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            do {
                currentHoverIndex = (currentHoverIndex + 1) % totalCards;
            } while (cardMatched[currentHoverIndex] && !AllMatched());
            UpdateHoverVisual();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            do {
                currentHoverIndex = (currentHoverIndex - cols + totalCards) % totalCards;
            } while (cardMatched[currentHoverIndex] && !AllMatched());
            UpdateHoverVisual();
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            do {
                currentHoverIndex = (currentHoverIndex + cols) % totalCards;
            } while (cardMatched[currentHoverIndex] && !AllMatched());
            UpdateHoverVisual();
        }
        
        // Select
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SelectCard(currentHoverIndex);
        }
    }
    
    private bool AllMatched()
    {
        for (int i = 0; i < cardMatched.Length; i++)
        {
            if (!cardMatched[i]) return false;
        }
        return true;
    }

    private void UpdateHoverVisual()
    {
        for (int i = 0; i < cardImages.Length; i++)
        {
            if (cardMatched[i])
            {
                cardImages[i].color = cardMatchedColor;
            }
            else if (cardRevealed[i])
            {
                cardImages[i].color = cardFrontColor;
            }
            else if (i == currentHoverIndex)
            {
                cardImages[i].color = cardHoverColor;
            }
            else
            {
                cardImages[i].color = cardBackColor;
            }
        }
    }

    private void SelectCard(int index)
    {
        if (cardMatched[index] || cardRevealed[index]) return;
        
        RevealCard(index);
        cardRevealed[index] = true;
        
        if (firstSelectedIndex == null)
        {
            firstSelectedIndex = index;
        }
        else
        {
            StartCoroutine(CheckMatch(firstSelectedIndex.Value, index));
        }
    }

    private IEnumerator CheckMatch(int first, int second)
    {
        isProcessing = true;
        yield return new WaitForSecondsRealtime(0.8f);
        
        if (cardValues[first] == cardValues[second])
        {
            // Match!
            cardMatched[first] = true;
            cardMatched[second] = true;
            cardImages[first].color = cardMatchedColor;
            cardImages[second].color = cardMatchedColor;
            matchedPairs++;
            
            if (matchedPairs >= cardPairCount)
            {
                if (statusText != null)
                    statusText.text = "MEMORY MASTER!";
                yield return new WaitForSecondsRealtime(1f);
                SolvePuzzle();
                yield break;
            }
        }
        else
        {
            // No match
            HideCard(first);
            HideCard(second);
            currentMistakes++;
            
            if (currentMistakes >= maxMistakes)
            {
                if (statusText != null)
                    statusText.text = "TOO MANY MISTAKES!";
                yield return new WaitForSecondsRealtime(1.5f);
                CancelPuzzle();
                yield break;
            }
        }
        
        cardRevealed[first] = false;
        cardRevealed[second] = false;
        firstSelectedIndex = null;
        isProcessing = false;
        
        UpdateStatus();
        UpdateHoverVisual();
    }

    private void RevealCard(int index)
    {
        if (cardImages != null && index < cardImages.Length && cardImages[index] != null)
            cardImages[index].color = cardFrontColor;
        if (cardTexts != null && index < cardTexts.Length && cardTexts[index] != null && cardValues != null && index < cardValues.Length)
            cardTexts[index].text = cardSymbols[cardValues[index] % cardSymbols.Length];
    }

    private void HideCard(int index)
    {
        if (cardImages != null && index < cardImages.Length && cardImages[index] != null)
            cardImages[index].color = cardBackColor;
        if (cardTexts != null && index < cardTexts.Length && cardTexts[index] != null)
            cardTexts[index].text = "?";
    }

    private void UpdateStatus()
    {
        if (statusText != null)
        {
            statusText.text = $"Pairs: {matchedPairs}/{cardPairCount}  |  Mistakes: {currentMistakes}/{maxMistakes}";
        }
    }

    private void SolvePuzzle()
    {
        IsPuzzleActive = false;
        Time.timeScale = 1f;
        
        // Müziği normale döndür ve oyuncuyu serbest bırak
        if (GameManager.Instance != null)
            GameManager.Instance.OnPuzzleEnd();
        
        onSolved?.Invoke();
        gameObject.SetActive(false);
    }

    private void CancelPuzzle()
    {
        IsPuzzleActive = false;
        Time.timeScale = 1f;
        
        // Müziği normale döndür ve oyuncuyu serbest bırak
        if (GameManager.Instance != null)
            GameManager.Instance.OnPuzzleEnd();
        
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
        
        int totalCards = 8; // 4 pairs
        
        // Tema renkleri - oyunun mor/turuncu teması
        Color bgColor = new Color(0.08f, 0.05f, 0.12f, 0.98f);
        Color accentColor = new Color(0.6f, 0.3f, 0.8f);
        Color highlightColor = new Color(1f, 0.5f, 0.2f);
        Color themedCardBack = new Color(0.25f, 0.15f, 0.35f);
        Color themedCardHover = new Color(0.45f, 0.25f, 0.55f);
        
        // RectTransform ayarla
        RectTransform myRect = GetComponent<RectTransform>();
        if (myRect == null) myRect = gameObject.AddComponent<RectTransform>();
        myRect.anchorMin = new Vector2(0.5f, 0.5f);
        myRect.anchorMax = new Vector2(0.5f, 0.5f);
        myRect.pivot = new Vector2(0.5f, 0.5f);
        myRect.sizeDelta = new Vector2(550, 480);
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
        SetupRect(innerRect, Vector2.zero, new Vector2(540, 470));
        Image innerImg = innerPanel.AddComponent<Image>();
        innerImg.color = new Color(0.12f, 0.08f, 0.18f, 0.9f);
        
        // Üst dekoratif çizgi
        GameObject topLine = new GameObject("TopLine");
        topLine.transform.SetParent(transform, false);
        RectTransform topLineRect = topLine.AddComponent<RectTransform>();
        SetupRect(topLineRect, new Vector2(0, 220), new Vector2(450, 3));
        Image topLineImg = topLine.AddComponent<Image>();
        topLineImg.color = accentColor;
        
        // Beyin ikonu
        TextMeshProUGUI brainIcon = CreateTMPText(transform, "🧠", new Vector2(0, 195), 26);
        brainIcon.color = highlightColor;
        
        // Başlık
        titleText = CreateTMPText(transform, "MEMORY MATCH", new Vector2(0, 165), 34);
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = highlightColor;
        
        // Kartlar (8 adet, 4x2 grid)
        cardImages = new Image[totalCards];
        cardTexts = new TextMeshProUGUI[totalCards];
        
        int cols = 4;
        float cardWidth = 95;
        float cardHeight = 110;
        float spacing = 18;
        float startX = -(cols - 1) * (cardWidth + spacing) / 2;
        float startY = 65;
        
        for (int i = 0; i < totalCards; i++)
        {
            int row = i / cols;
            int col = i % cols;
            
            // Kart container (gölge efekti için)
            GameObject cardContainer = new GameObject($"CardContainer_{i}");
            cardContainer.transform.SetParent(transform, false);
            RectTransform containerRect = cardContainer.AddComponent<RectTransform>();
            SetupRect(containerRect, new Vector2(startX + col * (cardWidth + spacing), startY - row * (cardHeight + spacing)), new Vector2(cardWidth + 6, cardHeight + 6));
            Image containerImg = cardContainer.AddComponent<Image>();
            containerImg.color = new Color(0.15f, 0.1f, 0.2f);
            
            // Kart
            GameObject cardObj = new GameObject($"Card_{i}");
            cardObj.transform.SetParent(cardContainer.transform, false);
            RectTransform cardRect = cardObj.AddComponent<RectTransform>();
            SetupRect(cardRect, Vector2.zero, new Vector2(cardWidth, cardHeight));
            cardImages[i] = cardObj.AddComponent<Image>();
            cardImages[i].color = themedCardBack;
            
            cardTexts[i] = CreateTMPText(cardObj.transform, "?", Vector2.zero, 38);
            cardTexts[i].color = new Color(0.7f, 0.5f, 0.85f);
        }
        
        // Ayırıcı çizgi
        GameObject separatorLine = new GameObject("SeparatorLine");
        separatorLine.transform.SetParent(transform, false);
        RectTransform sepRect = separatorLine.AddComponent<RectTransform>();
        SetupRect(sepRect, new Vector2(0, -100), new Vector2(400, 2));
        Image sepImg = separatorLine.AddComponent<Image>();
        sepImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.4f);
        
        // Status text
        statusText = CreateTMPText(transform, "Pairs: 0/4  ★  Mistakes: 0/5", new Vector2(0, -130), 20);
        statusText.color = new Color(0.85f, 0.75f, 0.95f);
        
        // Alt çizgi
        GameObject bottomLine = new GameObject("BottomLine");
        bottomLine.transform.SetParent(transform, false);
        RectTransform bottomRect = bottomLine.AddComponent<RectTransform>();
        SetupRect(bottomRect, new Vector2(0, -175), new Vector2(400, 2));
        Image bottomImg = bottomLine.AddComponent<Image>();
        bottomImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.3f);
        
        // Hint text
        TextMeshProUGUI hintText = CreateTMPText(transform, "[ ←→↑↓ ] Move  [ SPACE ] Select", new Vector2(0, -195), 14);
        hintText.color = new Color(0.6f, 0.5f, 0.7f);
        
        // Instruction
        instructionText = CreateTMPText(transform, "[ ESC ] Cancel", new Vector2(0, -218), 13);
        instructionText.color = new Color(0.5f, 0.4f, 0.55f);
        
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("MemoryPuzzleUI Preview Generated!");
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
        rect.sizeDelta = new Vector2(450, 50);
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
