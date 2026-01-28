using UnityEngine;
using System.Collections;

/// <summary>
/// Kırılabilir toplanabilir obje - Hasar alarak kırılır ve puzzle/ödül verir.
/// EnemyHealth'ten isDamagableObject sistemiyle entegre çalışır.
/// 
/// KULLANIM:
/// 1. Bu script'i kırılabilir objeye ekle
/// 2. EnemyHealth component'i de ekle ve isDamagableObject = true yap
/// 3. Puzzle tipini seç veya direkt ödül modunu kullan
/// 4. Canvas'taki puzzle UI referanslarını ata
/// </summary>
public class BreakableCollectible : MonoBehaviour
{
    public enum RewardType
    {
        DirectCoin,         // Direkt coin ver
        PuzzleReward,       // Puzzle çöz sonra ödül
        RandomItem          // Rastgele item
    }

    public enum PuzzleType
    {
        None,
        CombinationLock,
        RhythmTiming,
        MemoryCards
    }

    [Header("Reward Settings")]
    [SerializeField] private RewardType rewardType = RewardType.DirectCoin;
    [SerializeField] private int coinReward = 25;
    [SerializeField] private int puzzleBonusReward = 50; // Puzzle çözülürse ekstra

    [Header("Puzzle Settings")]
    [SerializeField] private PuzzleType puzzleType = PuzzleType.None;
    [SerializeField] private int puzzleDifficulty = 1; // 1-3

    [Header("Scene UI References (Assign from scene - NOT prefabs)")]
    [Tooltip("Canvas altındaki CombinationPuzzleUI objesini sürükle")]
    [SerializeField] private CombinationPuzzleUI sceneCombinationUI;
    [Tooltip("Canvas altındaki RhythmPuzzleUI objesini sürükle")]
    [SerializeField] private RhythmPuzzleUI sceneRhythmUI;
    [Tooltip("Canvas altındaki MemoryPuzzleUI objesini sürükle")]
    [SerializeField] private MemoryPuzzleUI sceneMemoryUI;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem breakParticles;
    [SerializeField] private GameObject rewardEffectPrefab;
    [SerializeField] private Color glowColor = new Color(1f, 0.8f, 0.2f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioClip breakSound;
    [SerializeField] private AudioClip rewardSound;

    // State
    private bool isBroken = false;
    private bool isPuzzleActive = false;

    // Components
    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private EnemyHealth enemyHealth;

    // Events
    public System.Action<int> OnCollectibleBroken; // coin amount
    public System.Action OnPuzzleStarted;
    public System.Action<bool> OnPuzzleCompleted; // success

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        enemyHealth = GetComponent<EnemyHealth>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // EnemyHealth ayarlarını kontrol et
        if (enemyHealth != null)
        {
            enemyHealth.isDamagableObject = true;
            if (breakParticles != null)
                enemyHealth.particleSystem = breakParticles;
        }

        // Sahnedeki puzzle UI'larını başlangıçta gizle
        HideAllPuzzleUIs();
    }

    private void HideAllPuzzleUIs()
    {
        if (sceneCombinationUI != null) sceneCombinationUI.gameObject.SetActive(false);
        if (sceneRhythmUI != null) sceneRhythmUI.gameObject.SetActive(false);
        if (sceneMemoryUI != null) sceneMemoryUI.gameObject.SetActive(false);
    }

    /// <summary>
    /// EnemyHealth'ten çağrılır - obje kırıldığında
    /// </summary>
    public void OnObjectBroken()
    {
        if (isBroken) return;
        isBroken = true;

        // Ses çal
        if (breakSound != null)
            audioSource.PlayOneShot(breakSound);

        // Particle efekti
        if (breakParticles != null)
            breakParticles.Play();

        // Ödül tipine göre işlem
        switch (rewardType)
        {
            case RewardType.DirectCoin:
                GiveDirectReward();
                break;
            case RewardType.PuzzleReward:
                StartPuzzle();
                break;
            case RewardType.RandomItem:
                GiveRandomReward();
                break;
        }

        OnCollectibleBroken?.Invoke(coinReward);
    }

    private void GiveDirectReward()
    {
        // Coin ver
        if (GameManager.Instance != null)
        {
            GameManager.Instance.coin += coinReward;
        }

        // Popup göster
        CoinPopup.Show(coinReward, transform.position);

        // Efekt
        if (rewardEffectPrefab != null)
            Instantiate(rewardEffectPrefab, transform.position, Quaternion.identity);

        // Ses
        if (rewardSound != null)
            audioSource.PlayOneShot(rewardSound);

        Debug.Log($"BreakableCollectible: +{coinReward} coin!");
    }

    private void StartPuzzle()
    {
        if (puzzleType == PuzzleType.None)
        {
            GiveDirectReward();
            return;
        }

        isPuzzleActive = true;
        OnPuzzleStarted?.Invoke();

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
        if (sceneCombinationUI != null)
        {
            sceneCombinationUI.gameObject.SetActive(true);
            sceneCombinationUI.InitializeFromPrefab(puzzleDifficulty, OnPuzzleSolved, OnPuzzleFailed);
        }
        else
        {
            Debug.LogWarning("CombinationPuzzleUI referansı atanmamış!");
            GiveDirectReward();
        }
    }

    private void OpenRhythmPuzzle()
    {
        if (sceneRhythmUI != null)
        {
            sceneRhythmUI.gameObject.SetActive(true);
            sceneRhythmUI.InitializeFromPrefab(puzzleDifficulty, OnPuzzleSolved, OnPuzzleFailed);
        }
        else
        {
            Debug.LogWarning("RhythmPuzzleUI referansı atanmamış!");
            GiveDirectReward();
        }
    }

    private void OpenMemoryPuzzle()
    {
        if (sceneMemoryUI != null)
        {
            sceneMemoryUI.gameObject.SetActive(true);
            sceneMemoryUI.InitializeFromPrefab(puzzleDifficulty, OnPuzzleSolved, OnPuzzleFailed);
        }
        else
        {
            Debug.LogWarning("MemoryPuzzleUI referansı atanmamış!");
            GiveDirectReward();
        }
    }

    private void OnPuzzleSolved()
    {
        isPuzzleActive = false;

        // Toplam ödül = base + bonus
        int totalReward = coinReward + puzzleBonusReward;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.coin += totalReward;
        }

        // Popup göster
        CoinPopup.Show(totalReward, transform.position);

        // Efekt
        if (rewardEffectPrefab != null)
            Instantiate(rewardEffectPrefab, transform.position, Quaternion.identity);

        // Ses
        if (rewardSound != null)
            audioSource.PlayOneShot(rewardSound);

        OnPuzzleCompleted?.Invoke(true);

        Debug.Log($"Puzzle çözüldü! +{totalReward} coin (base: {coinReward}, bonus: {puzzleBonusReward})");
    }

    private void OnPuzzleFailed()
    {
        isPuzzleActive = false;

        // Başarısız olsa bile base ödülü ver
        if (GameManager.Instance != null)
        {
            GameManager.Instance.coin += coinReward;
        }

        CoinPopup.Show(coinReward, transform.position);

        OnPuzzleCompleted?.Invoke(false);

        Debug.Log($"Puzzle başarısız! Sadece base ödül: +{coinReward} coin");
    }

    private void GiveRandomReward()
    {
        // Rastgele ödül - şimdilik coin ver, ileride item eklenebilir
        int randomBonus = Random.Range(0, 50);
        int totalReward = coinReward + randomBonus;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.coin += totalReward;
        }

        CoinPopup.Show(totalReward, transform.position);

        Debug.Log($"Random reward: +{totalReward} coin!");
    }

    /// <summary>
    /// Dışarıdan zorla kırma (test/debug için)
    /// </summary>
    [ContextMenu("Force Break")]
    public void ForceBreak()
    {
        OnObjectBroken();
    }
}
