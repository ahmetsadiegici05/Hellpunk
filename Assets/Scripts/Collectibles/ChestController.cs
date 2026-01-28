using UnityEngine;

/// <summary>
/// Chest'lere puzzle mekaniğini otomatik ekleyen controller.
/// Bu script Chest prefab'ına eklenir ve spawn edildiğinde puzzle ayarlarını yapar.
/// </summary>
public class ChestController : MonoBehaviour
{
    [Header("Puzzle Settings")]
    [SerializeField] private bool enablePuzzle = true;
    [SerializeField] private bool randomPuzzleType = true;
    [SerializeField] private EnemyHealth.PuzzleType specificPuzzleType = EnemyHealth.PuzzleType.GuitarRiff;
    [SerializeField] private int minDifficulty = 1;
    [SerializeField] private int maxDifficulty = 2;
    [SerializeField] private int rewardCoins = 25;

    private EnemyHealth enemyHealth;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
        {
            Debug.LogError("[ChestController] EnemyHealth komponenti bulunamadı!");
            return;
        }

        SetupPuzzle();
    }

    private void SetupPuzzle()
    {
        if (!enablePuzzle)
        {
            enemyHealth.hasPuzzle = false;
            return;
        }

        // Puzzle tipini belirle
        EnemyHealth.PuzzleType puzzleType;
        if (randomPuzzleType)
        {
            int random = Random.Range(0, 3);
            puzzleType = random switch
            {
                0 => EnemyHealth.PuzzleType.GuitarRiff,
                1 => EnemyHealth.PuzzleType.Rhythm,
                2 => EnemyHealth.PuzzleType.Memory,
                _ => EnemyHealth.PuzzleType.GuitarRiff
            };
        }
        else
        {
            puzzleType = specificPuzzleType;
        }

        // Difficulty belirle
        int difficulty = Random.Range(minDifficulty, maxDifficulty + 1);

        // EnemyHealth'e ayarları yaz (artık public field'lar)
        enemyHealth.hasPuzzle = true;
        enemyHealth.puzzleType = puzzleType;
        enemyHealth.puzzleDifficulty = difficulty;
        enemyHealth.puzzleRewardCoins = rewardCoins;
        
        Debug.Log($"[ChestController] {gameObject.name} puzzle ayarlandı: {puzzleType}, Difficulty: {difficulty}");
    }
}