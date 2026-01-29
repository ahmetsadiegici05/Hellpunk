using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Tilemap rengini korur ve değişikliklere karşı koruma sağlar
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class TilemapColorKeeper : MonoBehaviour
{
    [Header("Renk Ayarı")]
    [Tooltip("Tilemap'in olması gereken renk")]
    public Color targetColor = Color.white;
    
    [Header("Koruma")]
    [Tooltip("Rengi sürekli kontrol et ve düzelt")]
    public bool enforceColor = true;

    private Tilemap tilemap;
    private TilemapRenderer tilemapRenderer;

    void Start()
    {
        tilemap = GetComponent<Tilemap>();
        tilemapRenderer = GetComponent<TilemapRenderer>();
        
        // Başlangıçta rengi uygula
        ApplyColor();
    }

    void LateUpdate()
    {
        if (enforceColor)
        {
            // Her frame rengi kontrol et
            if (tilemap.color != targetColor)
            {
                tilemap.color = targetColor;
            }
        }
    }

    /// <summary>
    /// Rengi manuel olarak uygula
    /// </summary>
    public void ApplyColor()
    {
        if (tilemap != null)
        {
            tilemap.color = targetColor;
        }
    }

    /// <summary>
    /// Yeni renk ayarla
    /// </summary>
    public void SetColor(Color newColor)
    {
        targetColor = newColor;
        ApplyColor();
    }

    void OnValidate()
    {
        // Editor'da değişiklik yapılınca uygula
        if (tilemap == null) tilemap = GetComponent<Tilemap>();
        if (tilemap != null)
        {
            tilemap.color = targetColor;
        }
    }
}
