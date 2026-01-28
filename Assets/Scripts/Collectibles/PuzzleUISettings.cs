using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Puzzle UI'ları için temel prefab referanslarını ve ayarları tutar.
/// Bu ScriptableObject'i Assets klasöründe oluşturup Inspector'dan düzenleyebilirsin.
/// </summary>
[CreateAssetMenu(fileName = "PuzzleUISettings", menuName = "Game/Puzzle UI Settings")]
public class PuzzleUISettings : ScriptableObject
{
    [Header("=== PREFABS ===")]
    [Tooltip("Combination Lock puzzle prefab'ı")]
    public GameObject combinationPuzzlePrefab;
    
    [Tooltip("Rhythm Timing puzzle prefab'ı")]
    public GameObject rhythmPuzzlePrefab;
    
    [Tooltip("Memory Cards puzzle prefab'ı")]
    public GameObject memoryPuzzlePrefab;
    
    [Header("=== COLORS ===")]
    public Color panelBackgroundColor = new Color(0.12f, 0.12f, 0.18f, 0.98f);
    public Color accentColor = new Color(1f, 0.6f, 0.1f);
    public Color successColor = new Color(0.2f, 0.9f, 0.3f);
    public Color failColor = new Color(1f, 0.3f, 0.3f);
    public Color textColor = Color.white;
    public Color hintTextColor = new Color(0.6f, 0.6f, 0.7f);
    
    [Header("=== COMBINATION LOCK ===")]
    public Color digitSelectedColor = new Color(0.3f, 0.6f, 1f);
    public Color digitDefaultColor = new Color(0.25f, 0.25f, 0.3f);
    
    [Header("=== RHYTHM PUZZLE ===")]
    public Color hitZoneColor = new Color(0.1f, 0.9f, 0.3f, 0.4f);
    public Color beatColor = new Color(1f, 0.4f, 0.1f);
    [Range(40f, 120f)]
    public float hitZoneSize = 80f;
    
    [Header("=== MEMORY CARDS ===")]
    public Color cardBackColor = new Color(0.2f, 0.3f, 0.5f);
    public Color cardFrontColor = new Color(0.9f, 0.85f, 0.7f);
    public Color cardMatchedColor = new Color(0.3f, 0.7f, 0.3f);
    
    [Header("=== FONTS ===")]
    public Font mainFont;
    [Range(14, 48)]
    public int titleFontSize = 36;
    [Range(12, 32)]
    public int bodyFontSize = 20;
    [Range(10, 24)]
    public int hintFontSize = 16;
    
    // Singleton erişimi
    private static PuzzleUISettings _instance;
    public static PuzzleUISettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<PuzzleUISettings>("PuzzleUISettings");
                if (_instance == null)
                {
                    Debug.LogWarning("PuzzleUISettings not found in Resources folder. Using defaults.");
                }
            }
            return _instance;
        }
    }
}
