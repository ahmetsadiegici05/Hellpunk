using UnityEngine;

/// <summary>
/// Scene'deki puzzle UI'ları yöneten manager.
/// Bu script UICanvas veya benzeri bir objeye eklenir.
/// Puzzle UI prefab'larını runtime'da oluşturur veya mevcut UI'ları yönetir.
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    [Header("Puzzle UI References (Assign in Inspector)")]
    [Tooltip("Scene'deki GuitarRiffPuzzleUI objesi")]
    public GuitarRiffPuzzleUI guitarRiffPuzzleUI;
    
    [Tooltip("Scene'deki RhythmPuzzleUI objesi")]
    public RhythmPuzzleUI rhythmPuzzleUI;
    
    [Tooltip("Scene'deki MemoryPuzzleUI objesi")]
    public MemoryPuzzleUI memoryPuzzleUI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Puzzle UI'ları başlangıçta deaktif et
        if (guitarRiffPuzzleUI != null) guitarRiffPuzzleUI.gameObject.SetActive(false);
        if (rhythmPuzzleUI != null) rhythmPuzzleUI.gameObject.SetActive(false);
        if (memoryPuzzleUI != null) memoryPuzzleUI.gameObject.SetActive(false);
        
        Debug.Log("[PuzzleManager] Puzzle UI'lar hazır.");
    }

    /// <summary>
    /// GuitarRiff puzzle'ı başlat
    /// </summary>
    public void StartGuitarRiffPuzzle(int difficulty, System.Action onSolved, System.Action onFailed)
    {
        if (guitarRiffPuzzleUI == null)
        {
            Debug.LogError("[PuzzleManager] GuitarRiffPuzzleUI referansı yok!");
            onFailed?.Invoke();
            return;
        }
        
        guitarRiffPuzzleUI.InitializeFromPrefab(difficulty, onSolved, onFailed);
    }

    /// <summary>
    /// Rhythm puzzle'ı başlat
    /// </summary>
    public void StartRhythmPuzzle(int difficulty, System.Action onSolved, System.Action onFailed)
    {
        if (rhythmPuzzleUI == null)
        {
            Debug.LogError("[PuzzleManager] RhythmPuzzleUI referansı yok!");
            onFailed?.Invoke();
            return;
        }
        
        rhythmPuzzleUI.InitializeFromPrefab(difficulty, onSolved, onFailed);
    }

    /// <summary>
    /// Memory puzzle'ı başlat
    /// </summary>
    public void StartMemoryPuzzle(int difficulty, System.Action onSolved, System.Action onFailed)
    {
        if (memoryPuzzleUI == null)
        {
            Debug.LogError("[PuzzleManager] MemoryPuzzleUI referansı yok!");
            onFailed?.Invoke();
            return;
        }
        
        memoryPuzzleUI.InitializeFromPrefab(difficulty, onSolved, onFailed);
    }
}
