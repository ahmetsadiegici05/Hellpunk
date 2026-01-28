using UnityEngine;
using System.Collections;

/// <summary>
/// Puzzle ile açılan hazine kutusu.
/// Oyuncu yaklaşıp E'ye basınca puzzle açılır.
/// Puzzle çözülünce coin verir.
/// 
/// KULLANIM:
/// 1. Canvas altına puzzle UI oluştur (sağ tık → Generate Preview UI)
/// 2. UI'ı başlangıçta INACTIVE yap
/// 3. PuzzleBox'a bu UI'ın referansını ata (Scene UI References)
/// 4. Artık prefab/instantiate yok, sahnedeki UI kullanılıyor
/// </summary>
public class PuzzleBox : MonoBehaviour
{
    public enum PuzzleType
    {
        CombinationLock,  // Mastermind benzeri şifre kırma
        RhythmTiming,     // Ritim/timing puzzle
        MemoryCards       // Hafıza kartları
    }
    
    [Header("Puzzle Settings")]
    [SerializeField] private PuzzleType puzzleType = PuzzleType.CombinationLock;
    [SerializeField] private int coinReward = 50;
    [SerializeField] private int difficulty = 1; // 1-3 arası zorluk
    
    [Header("Scene UI References (Assign from scene - NOT prefabs)")]
    [Tooltip("Canvas altındaki CombinationPuzzleUI objesini sürükle")]
    [SerializeField] private CombinationPuzzleUI sceneCombinatonUI;
    [Tooltip("Canvas altındaki RhythmPuzzleUI objesini sürükle")]
    [SerializeField] private RhythmPuzzleUI sceneRhythmUI;
    [Tooltip("Canvas altındaki MemoryPuzzleUI objesini sürükle")]
    [SerializeField] private MemoryPuzzleUI sceneMemoryUI;
    
    [Header("Visuals")]
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite openSprite;
    [SerializeField] private Color glowColor = new Color(1f, 0.8f, 0.2f, 1f);
    
    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip interactSound;
    
    // State
    private bool isOpened = false;
    private bool isPlayerNear = false;
    private bool isPuzzleActive = false;
    
    // Components
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    
    // UI Reference (currently active)
    private GameObject currentPuzzleUI;
    
    // Events
    public System.Action<int> OnBoxOpened; // coin amount
    
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        
        // Sahnedeki UI'ları başlangıçta gizle
        HideAllSceneUIs();
    }
    
    private void HideAllSceneUIs()
    {
        if (sceneCombinatonUI != null) sceneCombinatonUI.gameObject.SetActive(false);
        if (sceneRhythmUI != null) sceneRhythmUI.gameObject.SetActive(false);
        if (sceneMemoryUI != null) sceneMemoryUI.gameObject.SetActive(false);
    }
    
    private void Update()
    {
        if (isOpened || isPuzzleActive) return;
        
        // E tuşu ile etkileşim
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            OpenPuzzle();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isOpened)
        {
            isPlayerNear = true;
            ShowInteractPrompt(true);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            ShowInteractPrompt(false);
        }
    }
    
    private void ShowInteractPrompt(bool show)
    {
        // TODO: "E tuşuna bas" UI göster
    }
    
    private void OpenPuzzle()
    {
        if (interactSound != null)
            audioSource.PlayOneShot(interactSound);
        
        isPuzzleActive = true;
        
        // Puzzle tipine göre UI aç
        switch (puzzleType)
        {
            case PuzzleType.CombinationLock:
                OpenCombinationPuzzle();
                break;
            case PuzzleType.RhythmTiming:
                OpenRhythmPuzzle();
                break;
            case PuzzleType.MemoryCards:
                OpenMemoryPuzzle();
                break;
        }
    }
    
    private void OpenCombinationPuzzle()
    {
        // Sahnedeki UI varsa onu kullan
        if (sceneCombinatonUI != null)
        {
            sceneCombinatonUI.gameObject.SetActive(true);
            sceneCombinatonUI.InitializeFromPrefab(difficulty, OnPuzzleSolved, OnPuzzleCancelled);
            currentPuzzleUI = sceneCombinatonUI.gameObject;
        }
        else
        {
            // Sahnede UI yoksa runtime oluştur
            GameObject puzzleObj = new GameObject("CombinationPuzzleUI");
            CombinationPuzzleUI puzzle = puzzleObj.AddComponent<CombinationPuzzleUI>();
            puzzle.InitializeFromPrefab(difficulty, OnPuzzleSolved, OnPuzzleCancelled);
            currentPuzzleUI = puzzleObj;
        }
    }
    
    private void OpenRhythmPuzzle()
    {
        // Sahnedeki UI varsa onu kullan
        if (sceneRhythmUI != null)
        {
            sceneRhythmUI.gameObject.SetActive(true);
            sceneRhythmUI.InitializeFromPrefab(difficulty, OnPuzzleSolved, OnPuzzleCancelled);
            currentPuzzleUI = sceneRhythmUI.gameObject;
        }
        else
        {
            // Sahnede UI yoksa runtime oluştur
            GameObject puzzleObj = new GameObject("RhythmPuzzleUI");
            RhythmPuzzleUI puzzle = puzzleObj.AddComponent<RhythmPuzzleUI>();
            puzzle.InitializeFromPrefab(difficulty, OnPuzzleSolved, OnPuzzleCancelled);
            currentPuzzleUI = puzzleObj;
        }
    }
    
    private void OpenMemoryPuzzle()
    {
        // Sahnedeki UI varsa onu kullan
        if (sceneMemoryUI != null)
        {
            sceneMemoryUI.gameObject.SetActive(true);
            sceneMemoryUI.InitializeFromPrefab(difficulty, OnPuzzleSolved, OnPuzzleCancelled);
            currentPuzzleUI = sceneMemoryUI.gameObject;
        }
        else
        {
            // Sahnede UI yoksa runtime oluştur
            GameObject puzzleObj = new GameObject("MemoryPuzzleUI");
            MemoryPuzzleUI puzzle = puzzleObj.AddComponent<MemoryPuzzleUI>();
            puzzle.InitializeFromPrefab(difficulty, OnPuzzleSolved, OnPuzzleCancelled);
            currentPuzzleUI = puzzleObj;
        }
    }
    
    private void OnPuzzleSolved()
    {
        isPuzzleActive = false;
        isOpened = true;
        
        // UI'ı gizle (destroy etme, sahnede kalacak)
        HidePuzzleUI();
        
        // Kutuyu aç
        if (openSprite != null && spriteRenderer != null)
            spriteRenderer.sprite = openSprite;
        
        // Ses çal
        if (openSound != null)
            audioSource.PlayOneShot(openSound);
        
        // Coin ver
        if (GameManager.Instance != null)
        {
            GameManager.Instance.coin += coinReward;
        }
        
        OnBoxOpened?.Invoke(coinReward);
        
        // Coin popup göster
        CoinPopup.Show(coinReward, transform.position);
        
        // Efekt
        StartCoroutine(OpenEffect());
        
        Debug.Log($"Puzzle çözüldü! +{coinReward} coin");
    }
    
    private void OnPuzzleCancelled()
    {
        isPuzzleActive = false;
        
        // UI'ı gizle (destroy etme, sahnede kalacak)
        HidePuzzleUI();
    }
    
    private void HidePuzzleUI()
    {
        if (currentPuzzleUI != null)
        {
            // Sahnedeki UI ise sadece gizle
            if (currentPuzzleUI == sceneCombinatonUI?.gameObject ||
                currentPuzzleUI == sceneRhythmUI?.gameObject ||
                currentPuzzleUI == sceneMemoryUI?.gameObject)
            {
                currentPuzzleUI.SetActive(false);
            }
            else
            {
                // Runtime oluşturulan UI ise destroy et
                Destroy(currentPuzzleUI);
            }
            currentPuzzleUI = null;
        }
    }
    
    private IEnumerator OpenEffect()
    {
        // Parıltı efekti
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = glowColor;
            yield return new WaitForSeconds(0.3f);
            spriteRenderer.color = original;
        }
    }
}
