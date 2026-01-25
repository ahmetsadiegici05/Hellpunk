using UnityEngine;

/// <summary>
/// Basit SpriteRenderer tabanlı düşman can barı
/// World Space Canvas sorunlarını çözmek için
/// </summary>
public class SimpleEnemyHealthBar : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(0.8f, 0.1f);

    [Header("Colors")]
    [SerializeField] private Color healthColor = new Color(0.2f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color damagedColor = new Color(0.9f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    [SerializeField] private Color borderColor = new Color(0f, 0f, 0f, 1f);

    private Transform target;
    private float maxHealth;
    private float currentHealth;

    private SpriteRenderer backgroundRenderer;
    private SpriteRenderer fillRenderer;
    private SpriteRenderer borderRenderer;

    public void Initialize(Transform enemyTransform, float maxHp)
    {
        target = enemyTransform;
        maxHealth = maxHp;
        currentHealth = maxHp;

        // Offset'i düşmanın boyutuna göre ayarla
        SpriteRenderer sr = enemyTransform.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            offset = new Vector3(0f, sr.bounds.extents.y + 0.25f, 0f);
        }

        CreateHealthBar();
        
        // Başlangıçta görünür yap (test için)
        SetVisible(true);
        UpdateHealthBar();
        
        Debug.Log($"SimpleHealthBar oluşturuldu: {enemyTransform.name}, MaxHP: {maxHp}");
    }

    private void CreateHealthBar()
    {
        // Border (en arkada)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(transform);
        borderObj.transform.localPosition = Vector3.zero;
        borderRenderer = borderObj.AddComponent<SpriteRenderer>();
        borderRenderer.sprite = CreateSquareSprite();
        borderRenderer.color = borderColor;
        borderRenderer.sortingOrder = 100;
        borderObj.transform.localScale = new Vector3(barSize.x + 0.04f, barSize.y + 0.04f, 1f);

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(transform);
        bgObj.transform.localPosition = Vector3.zero;
        backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = CreateSquareSprite();
        backgroundRenderer.color = backgroundColor;
        backgroundRenderer.sortingOrder = 101;
        bgObj.transform.localScale = new Vector3(barSize.x, barSize.y, 1f);

        // Health Fill
        GameObject fillObj = new GameObject("HealthFill");
        fillObj.transform.SetParent(transform);
        fillObj.transform.localPosition = new Vector3(-barSize.x / 2f, 0f, 0f);
        fillRenderer = fillObj.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = CreateSquareSprite();
        fillRenderer.color = healthColor;
        fillRenderer.sortingOrder = 102;
        
        // Pivot'u sola ayarlamak için child obje kullan
        GameObject fillPivot = new GameObject("FillPivot");
        fillPivot.transform.SetParent(fillObj.transform);
        fillPivot.transform.localPosition = new Vector3(barSize.x / 2f, 0f, 0f);
        fillPivot.transform.localScale = Vector3.one;
        
        // Fill'i pivot'a taşı
        fillRenderer.transform.localScale = new Vector3(barSize.x, barSize.y, 1f);
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Pozisyon güncelle
        transform.position = target.position + offset;
    }

    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        
        // Görünür yap
        SetVisible(true);
        
        // Fill'i güncelle
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (fillRenderer == null) return;

        float healthPercent = currentHealth / maxHealth;
        
        // Scale X ile doluluk ayarla
        Vector3 scale = fillRenderer.transform.localScale;
        scale.x = barSize.x * healthPercent;
        fillRenderer.transform.localScale = scale;
        
        // Pozisyonu ayarla (sol pivot için)
        Vector3 pos = fillRenderer.transform.localPosition;
        pos.x = -barSize.x / 2f + (barSize.x * healthPercent) / 2f;
        fillRenderer.transform.localPosition = pos;

        // Renk (yeşil -> kırmızı)
        fillRenderer.color = Color.Lerp(damagedColor, healthColor, healthPercent);
    }

    private void SetVisible(bool visible)
    {
        if (backgroundRenderer != null) backgroundRenderer.enabled = visible;
        if (fillRenderer != null) fillRenderer.enabled = visible;
        if (borderRenderer != null) borderRenderer.enabled = visible;
    }

    private void OnDestroy()
    {
        // Cleanup
    }
}
