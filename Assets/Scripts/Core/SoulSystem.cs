using UnityEngine;
using System;

/// <summary>
/// Ultimate için Kill sayacı sistemi
/// 5 düşman öldürüldüğünde Ultimate kullanılabilir
/// </summary>
public class SoulSystem : MonoBehaviour
{
    public static SoulSystem Instance { get; private set; }

    [Header("Ultimate Settings")]
    [SerializeField] private int killsForUltimate = 5; // 5 kill = 1 Ultimate hakkı

    private int currentKills = 0;
    private bool ultimateReady = false;

    // Events
    public event Action<int, int> OnKillCountChanged; // (currentKills, killsForUltimate)
    public event Action OnUltimateReady; // Ultimate hazır olduğunda
    public event Action OnUltimateUsed; // Ultimate kullanıldığında

    // Properties
    public int CurrentKills => currentKills;
    public int KillsRequired => killsForUltimate;
    public bool IsUltimateReady => ultimateReady;
    public float UltimateProgress => (float)currentKills / killsForUltimate;
    
    // Eski API uyumluluğu için
    public int CurrentSouls => currentKills;
    public int SoulsPerCharge => killsForUltimate;
    public int CurrentCharges => ultimateReady ? 1 : 0;
    public int MaxCharges => 1;
    public bool CanUseAbility => ultimateReady; // Sadece Ultimate için kullanılacak
    public float SoulProgress => UltimateProgress;

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
        }
    }

    /// <summary>
    /// Düşman öldürüldüğünde çağrılır
    /// </summary>
    public void CollectSoul(int amount = 1)
    {
        if (ultimateReady)
        {
            // Ultimate zaten hazır, kill sayma
            return;
        }

        currentKills += amount;
        OnKillCountChanged?.Invoke(currentKills, killsForUltimate);

        // 7 kill'e ulaştı mı?
        if (currentKills >= killsForUltimate)
        {
            ultimateReady = true;
            OnUltimateReady?.Invoke();
            Debug.Log("🔥 ULTIMATE HAZIR! 7 düşman öldürüldü!");
        }
        else
        {
            Debug.Log($"Kill: {currentKills}/{killsForUltimate}");
        }
    }

    /// <summary>
    /// Ultimate kullanıldığında çağrılır
    /// </summary>
    public bool UseCharge()
    {
        if (!ultimateReady)
        {
            Debug.Log($"Ultimate için {killsForUltimate - currentKills} kill daha gerekli!");
            return false;
        }

        ultimateReady = false;
        currentKills = 0;
        OnUltimateUsed?.Invoke();
        OnKillCountChanged?.Invoke(currentKills, killsForUltimate);
        Debug.Log("⚡ ULTIMATE KULLANILDI! Sayaç sıfırlandı.");
        return true;
    }

    /// <summary>
    /// Test için - kill ekle
    /// </summary>
    [ContextMenu("Add Kill")]
    public void DebugAddKill()
    {
        CollectSoul(1);
    }

    /// <summary>
    /// Test için - Ultimate hazır yap
    /// </summary>
    [ContextMenu("Make Ultimate Ready")]
    public void DebugMakeUltimateReady()
    {
        currentKills = killsForUltimate;
        ultimateReady = true;
        OnUltimateReady?.Invoke();
        OnKillCountChanged?.Invoke(currentKills, killsForUltimate);
    }

    /// <summary>
    /// Kill sayacını sıfırlar (Restart için)
    /// </summary>
    public void ResetKills()
    {
        currentKills = 0;
        ultimateReady = false;
        OnKillCountChanged?.Invoke(currentKills, killsForUltimate);
        Debug.Log("[SoulSystem] Kill sayacı sıfırlandı.");
    }
}
