using UnityEngine;
using System;

/// <summary>
/// Ruh toplama sistemi - düşman öldürüldüğünde ruh kazanılır
/// Belirli sayıda ruh toplandığında ability kullanma hakkı verir
/// </summary>
public class SoulSystem : MonoBehaviour
{
    public static SoulSystem Instance { get; private set; }

    [Header("Soul Settings")]
    [SerializeField] private int soulsPerCharge = 3; // Kaç ruh = 1 ability hakkı (Test için 1'e düşürüldü)
    [SerializeField] private int maxCharges = 3; // Maksimum biriktirebileceği hak sayısı

    private int currentSouls = 0;
    private int currentCharges = 0;

    // Events
    public event Action<int, int> OnSoulCollected; // (currentSouls, soulsPerCharge)
    public event Action<int> OnChargeGained; // (totalCharges)
    public event Action<int> OnChargeUsed; // (remainingCharges)

    // Properties
    public int CurrentSouls => currentSouls;
    public int SoulsPerCharge => soulsPerCharge;
    public int CurrentCharges => currentCharges;
    public int MaxCharges => maxCharges;
    public bool CanUseAbility => currentCharges > 0;
    public float SoulProgress => (float)currentSouls / soulsPerCharge;

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
        if (currentCharges >= maxCharges)
        {
            // Maksimum hak sayısına ulaşıldı
            return;
        }

        currentSouls += amount;
        OnSoulCollected?.Invoke(currentSouls, soulsPerCharge);

        // Yeterli ruh toplandı mı?
        while (currentSouls >= soulsPerCharge && currentCharges < maxCharges)
        {
            currentSouls -= soulsPerCharge;
            currentCharges++;
            OnChargeGained?.Invoke(currentCharges);
            Debug.Log($"Ability hakkı kazanıldı! Toplam: {currentCharges}");
        }
    }

    /// <summary>
    /// Ability kullanıldığında çağrılır
    /// </summary>
    public bool UseCharge()
    {
        if (currentCharges <= 0)
        {
            Debug.Log("Ability kullanmak için yeterli ruh yok!");
            return false;
        }

        currentCharges--;
        OnChargeUsed?.Invoke(currentCharges);
        Debug.Log($"Ability kullanıldı! Kalan hak: {currentCharges}");
        return true;
    }

    /// <summary>
    /// Test için - ruh ekle
    /// </summary>
    [ContextMenu("Add Soul")]
    public void DebugAddSoul()
    {
        CollectSoul(1);
    }

    /// <summary>
    /// Test için - hak ekle
    /// </summary>
    [ContextMenu("Add Charge")]
    public void DebugAddCharge()
    {
        if (currentCharges < maxCharges)
        {
            currentCharges++;
            OnChargeGained?.Invoke(currentCharges);
        }
    }
}
