using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Guitar Riff Sequence Puzzle - Simon Says tarzı
/// Gösterilen nota/tel sırasını ezberle ve tekrarla
/// Oyunun rock/punk temasına uygun
/// </summary>
public class GuitarRiffPuzzleUI : MonoBehaviour
{
    public static bool IsPuzzleActive { get; private set; } = false;
    
    [Header("Puzzle Settings")]
    [SerializeField] private int startingSequenceLength = 3;
    [SerializeField] private int targetSequenceLength = 5;
    [SerializeField] private float noteShowDuration = 0.5f;
    [SerializeField] private float noteGapDuration = 0.2f;
    [SerializeField] private int maxMistakes = 2;
    
    [Header("UI References (Assign in Inspector)")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Image[] noteButtons; // 4 nota butonu
    [SerializeField] private TextMeshProUGUI[] noteLabels; // Q, W, E, R
    
    // Nota renkleri - rock teması
    private Color[] noteColors = new Color[]
    {
        new Color(1f, 0.3f, 0.3f),    // Kırmızı - Q
        new Color(0.3f, 1f, 0.3f),    // Yeşil - W
        new Color(0.3f, 0.5f, 1f),    // Mavi - E
        new Color(1f, 0.8f, 0.2f)     // Sarı - R
    };
    
    private Color noteInactiveColor = new Color(0.2f, 0.15f, 0.25f);
    private Color noteFlashColor = Color.white;
    
    private List<int> sequence = new List<int>();
    private int currentInputIndex = 0;
    private int currentRound = 0;
    private int mistakes = 0;
    private bool isShowingSequence = false;
    private bool isInputPhase = false;
    
    private System.Action onSolved;
    private System.Action onCancelled;
    
    private KeyCode[] inputKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };

    void Update()
    {
        if (!gameObject.activeInHierarchy) return;
        
        // ESC ile çıkış
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPuzzle();
            return;
        }
        
        if (!isInputPhase || isShowingSequence) return;
        
        HandleInput();
    }

    public void InitializeFromPrefab(int difficulty, System.Action onSolvedCallback, System.Action onCancelledCallback)
    {
        // ÖNCE aktif et
        gameObject.SetActive(true);
        
        // Müziği kıs ve oyuncuyu durdur
        if (GameManager.Instance != null)
            GameManager.Instance.OnPuzzleStart();
        
        IsPuzzleActive = true;
        onSolved = onSolvedCallback;
        onCancelled = onCancelledCallback;
        
        // Difficulty ayarları
        startingSequenceLength = 2 + difficulty;
        targetSequenceLength = 4 + difficulty;
        noteShowDuration = 0.6f - difficulty * 0.1f;
        maxMistakes = 3 - difficulty;
        
        // State reset
        sequence.Clear();
        currentInputIndex = 0;
        currentRound = 0;
        mistakes = 0;
        isShowingSequence = false;
        isInputPhase = false;
        
        // UI Reset
        ResetUI();
        
        Time.timeScale = 1f;
        
        // İlk sekansı oluştur ve göster
        GenerateInitialSequence();
        StartCoroutine(ShowSequenceRoutine());
        
        Debug.Log("[GuitarRiffPuzzleUI] Puzzle başlatıldı!");
    }
    
    private void ResetUI()
    {
        if (noteButtons != null)
        {
            for (int i = 0; i < noteButtons.Length; i++)
            {
                if (noteButtons[i] != null)
                    noteButtons[i].color = noteInactiveColor;
            }
        }
        
        if (statusText != null)
            statusText.text = "Watch the sequence...";
            
        if (instructionText != null)
            instructionText.text = "Memorize the notes!";
    }
    
    private void GenerateInitialSequence()
    {
        sequence.Clear();
        for (int i = 0; i < startingSequenceLength; i++)
        {
            sequence.Add(Random.Range(0, 4));
        }
    }
    
    private void AddNoteToSequence()
    {
        sequence.Add(Random.Range(0, 4));
    }
    
    private IEnumerator ShowSequenceRoutine()
    {
        isShowingSequence = true;
        isInputPhase = false;
        
        if (statusText != null)
            statusText.text = "Watch carefully...";
        if (instructionText != null)
            instructionText.text = $"Round {currentRound + 1} - {sequence.Count} notes";
        
        yield return new WaitForSecondsRealtime(0.8f);
        
        // Sekansı göster
        for (int i = 0; i < sequence.Count; i++)
        {
            int noteIndex = sequence[i];
            
            // Notayı yak
            if (noteButtons != null && noteIndex < noteButtons.Length && noteButtons[noteIndex] != null)
            {
                noteButtons[noteIndex].color = noteColors[noteIndex];
                
                // TODO: Ses çal (GameManager'dan gitar sesi)
                PlayNoteSound(noteIndex);
            }
            
            yield return new WaitForSecondsRealtime(noteShowDuration);
            
            // Notayı söndür
            if (noteButtons != null && noteIndex < noteButtons.Length && noteButtons[noteIndex] != null)
            {
                noteButtons[noteIndex].color = noteInactiveColor;
            }
            
            yield return new WaitForSecondsRealtime(noteGapDuration);
        }
        
        // Input fazına geç
        isShowingSequence = false;
        isInputPhase = true;
        currentInputIndex = 0;
        
        if (statusText != null)
            statusText.text = "Your turn!";
        if (instructionText != null)
            instructionText.text = $"Press: Q W E R | Mistakes: {mistakes}/{maxMistakes}";
    }
    
    private void HandleInput()
    {
        for (int i = 0; i < inputKeys.Length; i++)
        {
            if (Input.GetKeyDown(inputKeys[i]))
            {
                ProcessInput(i);
                break;
            }
        }
    }
    
    private void ProcessInput(int noteIndex)
    {
        // Butonu flash yap
        StartCoroutine(FlashNote(noteIndex));
        PlayNoteSound(noteIndex);
        
        if (noteIndex == sequence[currentInputIndex])
        {
            // Doğru!
            currentInputIndex++;
            
            if (currentInputIndex >= sequence.Count)
            {
                // Sekans tamamlandı!
                currentRound++;
                
                if (sequence.Count >= targetSequenceLength)
                {
                    // Puzzle tamamlandı!
                    StartCoroutine(WinRoutine());
                }
                else
                {
                    // Sonraki round
                    StartCoroutine(NextRoundRoutine());
                }
            }
        }
        else
        {
            // Yanlış!
            mistakes++;
            StartCoroutine(MistakeFlash());
            
            if (mistakes >= maxMistakes)
            {
                // Puzzle başarısız
                StartCoroutine(FailRoutine());
            }
            else
            {
                // Sekansı tekrar göster
                if (statusText != null)
                    statusText.text = $"Wrong! Watch again... ({mistakes}/{maxMistakes})";
                StartCoroutine(RetryRoutine());
            }
        }
    }
    
    private IEnumerator FlashNote(int noteIndex)
    {
        if (noteButtons != null && noteIndex < noteButtons.Length && noteButtons[noteIndex] != null)
        {
            noteButtons[noteIndex].color = noteColors[noteIndex];
            yield return new WaitForSecondsRealtime(0.15f);
            noteButtons[noteIndex].color = noteInactiveColor;
        }
    }
    
    private IEnumerator MistakeFlash()
    {
        // Tüm notaları kırmızı yap
        if (noteButtons != null)
        {
            foreach (var btn in noteButtons)
            {
                if (btn != null) btn.color = new Color(1f, 0.2f, 0.2f);
            }
        }
        yield return new WaitForSecondsRealtime(0.3f);
        if (noteButtons != null)
        {
            foreach (var btn in noteButtons)
            {
                if (btn != null) btn.color = noteInactiveColor;
            }
        }
    }
    
    private IEnumerator NextRoundRoutine()
    {
        isInputPhase = false;
        
        if (statusText != null)
            statusText.text = "Correct! Next round...";
        
        yield return new WaitForSecondsRealtime(1f);
        
        AddNoteToSequence();
        StartCoroutine(ShowSequenceRoutine());
    }
    
    private IEnumerator RetryRoutine()
    {
        isInputPhase = false;
        yield return new WaitForSecondsRealtime(1f);
        currentInputIndex = 0;
        StartCoroutine(ShowSequenceRoutine());
    }
    
    private IEnumerator WinRoutine()
    {
        isInputPhase = false;
        
        if (statusText != null)
            statusText.text = "🎸 ROCK ON! 🎸";
        if (instructionText != null)
            instructionText.text = "Perfect performance!";
        
        // Kutlama animasyonu
        for (int flash = 0; flash < 3; flash++)
        {
            if (noteButtons != null)
            {
                for (int i = 0; i < noteButtons.Length; i++)
                {
                    if (noteButtons[i] != null)
                        noteButtons[i].color = noteColors[i];
                }
            }
            yield return new WaitForSecondsRealtime(0.2f);
            if (noteButtons != null)
            {
                foreach (var btn in noteButtons)
                {
                    if (btn != null) btn.color = noteInactiveColor;
                }
            }
            yield return new WaitForSecondsRealtime(0.1f);
        }
        
        yield return new WaitForSecondsRealtime(0.5f);
        SolvePuzzle();
    }
    
    private IEnumerator FailRoutine()
    {
        isInputPhase = false;
        
        if (statusText != null)
            statusText.text = "Out of tune...";
        if (instructionText != null)
            instructionText.text = "Try again next time!";
        
        yield return new WaitForSecondsRealtime(1.5f);
        CancelPuzzle();
    }
    
    private void PlayNoteSound(int noteIndex)
    {
        // GameManager'dan gitar sesi çal
        if (GameManager.Instance != null)
        {
            switch (noteIndex)
            {
                case 0:
                    if (GameManager.Instance.guitarSound1 != null)
                        GameManager.Instance.PlayGuitarSound(1);
                    break;
                case 1:
                    if (GameManager.Instance.guitarSound2 != null)
                        GameManager.Instance.PlayGuitarSound(2);
                    break;
                case 2:
                    if (GameManager.Instance.guitarSound3 != null)
                        GameManager.Instance.PlayGuitarSound(3);
                    break;
                case 3:
                    // 4. ses yoksa 1. sesi çal
                    if (GameManager.Instance.guitarSound1 != null)
                        GameManager.Instance.PlayGuitarSound(1);
                    break;
            }
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
        
        // Tema renkleri
        Color bgColor = new Color(0.08f, 0.05f, 0.12f, 0.98f);
        Color panelColor = new Color(0.12f, 0.08f, 0.18f, 0.95f);
        Color accentColor = new Color(0.8f, 0.4f, 0.2f); // Turuncu
        
        // RectTransform ayarla
        RectTransform myRect = GetComponent<RectTransform>();
        if (myRect == null) myRect = gameObject.AddComponent<RectTransform>();
        myRect.anchorMin = new Vector2(0.5f, 0.5f);
        myRect.anchorMax = new Vector2(0.5f, 0.5f);
        myRect.pivot = new Vector2(0.5f, 0.5f);
        myRect.sizeDelta = new Vector2(500, 350);
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
        SetupRect(innerRect, Vector2.zero, new Vector2(490, 340));
        Image innerImg = innerPanel.AddComponent<Image>();
        innerImg.color = panelColor;
        
        // Üst çizgi
        GameObject topLine = new GameObject("TopLine");
        topLine.transform.SetParent(transform, false);
        RectTransform topLineRect = topLine.AddComponent<RectTransform>();
        SetupRect(topLineRect, new Vector2(0, 160), new Vector2(420, 3));
        Image topLineImg = topLine.AddComponent<Image>();
        topLineImg.color = accentColor;
        
        // Başlık
        titleText = CreateTMPText(transform, "🎸 GUITAR RIFF 🎸", new Vector2(0, 130), 32);
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = accentColor;
        
        // Nota butonları
        noteButtons = new Image[4];
        noteLabels = new TextMeshProUGUI[4];
        string[] labels = { "Q", "W", "E", "R" };
        float buttonStartX = -150f;
        float buttonSpacing = 100f;
        
        for (int i = 0; i < 4; i++)
        {
            // Buton container
            GameObject btnContainer = new GameObject($"NoteButton_{i}");
            btnContainer.transform.SetParent(transform, false);
            RectTransform btnRect = btnContainer.AddComponent<RectTransform>();
            SetupRect(btnRect, new Vector2(buttonStartX + i * buttonSpacing, 20), new Vector2(80, 80));
            
            // Buton arka planı
            noteButtons[i] = btnContainer.AddComponent<Image>();
            noteButtons[i].color = noteInactiveColor;
            
            // Buton içi glow
            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(btnContainer.transform, false);
            RectTransform glowRect = glow.AddComponent<RectTransform>();
            SetupRect(glowRect, Vector2.zero, new Vector2(70, 70));
            Image glowImg = glow.AddComponent<Image>();
            glowImg.color = new Color(1f, 1f, 1f, 0.1f);
            
            // Tuş etiketi
            noteLabels[i] = CreateTMPText(btnContainer.transform, labels[i], Vector2.zero, 36);
            noteLabels[i].fontStyle = FontStyles.Bold;
            noteLabels[i].color = Color.white;
        }
        
        // Status text
        statusText = CreateTMPText(transform, "Watch the sequence...", new Vector2(0, -60), 22);
        statusText.color = Color.white;
        
        // Alt çizgi
        GameObject bottomLine = new GameObject("BottomLine");
        bottomLine.transform.SetParent(transform, false);
        RectTransform bottomLineRect = bottomLine.AddComponent<RectTransform>();
        SetupRect(bottomLineRect, new Vector2(0, -100), new Vector2(420, 2));
        Image bottomLineImg = bottomLine.AddComponent<Image>();
        bottomLineImg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.5f);
        
        // Instruction text
        instructionText = CreateTMPText(transform, "[ Q ] [ W ] [ E ] [ R ] - Repeat the pattern!", new Vector2(0, -130), 14);
        instructionText.color = new Color(0.7f, 0.6f, 0.8f);
        
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("GuitarRiffPuzzleUI Generated!");
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
