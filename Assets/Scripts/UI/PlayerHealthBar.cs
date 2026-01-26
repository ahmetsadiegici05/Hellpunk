using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Oyuncunun üzerinde görünen can barı.
/// Bu script oyuncunun GameObject'ine eklenir.
/// </summary>
public class PlayerHealthBar : MonoBehaviour
{
    [Header("Ayarlar")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(1.2f, 0.15f);
    [SerializeField] private Color healthColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color damagedColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color borderColor = new Color(0.1f, 0.1f, 0.1f, 1f);
    
    [Header("Referanslar")]
    [SerializeField] private Health playerHealth;
    
    private Transform healthBarTransform;
    private SpriteRenderer backgroundRenderer;
    private SpriteRenderer fillRenderer;
    private SpriteRenderer borderRenderer;
    
    private float displayedHealth;
    private float smoothSpeed = 8f;
    
    private void Awake()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<Health>();
    }
    
    private void Start()
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("PlayerHealthBar: Health component bulunamadı!");
            enabled = false;
            return;
        }
        
        // Offset'i oyuncunun boyutuna göre ayarla
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            offset = new Vector3(0f, sr.bounds.extents.y + 0.3f, 0f);
        }
        
        displayedHealth = playerHealth.currentHealth;
        CreateHealthBar();
    }
    
    private void CreateHealthBar()
    {
        // Ana obje
        GameObject healthBarObj = new GameObject("PlayerHealthBar");
        healthBarObj.transform.SetParent(transform);
        healthBarObj.transform.localPosition = offset;
        healthBarObj.transform.localScale = Vector3.one;
        healthBarTransform = healthBarObj.transform;
        
        // Border (en arkada)
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(healthBarTransform);
        borderObj.transform.localPosition = Vector3.zero;
        borderRenderer = borderObj.AddComponent<SpriteRenderer>();
        borderRenderer.sprite = CreateSquareSprite();
        borderRenderer.color = borderColor;
        borderRenderer.sortingOrder = 998;
        borderObj.transform.localScale = new Vector3(barSize.x + 0.06f, barSize.y + 0.06f, 1f);
        
        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(healthBarTransform);
        bgObj.transform.localPosition = Vector3.zero;
        backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = CreateSquareSprite();
        backgroundRenderer.color = backgroundColor;
        backgroundRenderer.sortingOrder = 999;
        bgObj.transform.localScale = new Vector3(barSize.x, barSize.y, 1f);
        
        // Fill (can göstergesi)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(healthBarTransform);
        fillObj.transform.localPosition = Vector3.zero;
        fillRenderer = fillObj.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = CreateSquareSprite();
        fillRenderer.color = healthColor;
        fillRenderer.sortingOrder = 1000;
        fillObj.transform.localScale = new Vector3(barSize.x, barSize.y * 0.85f, 1f);
        
        UpdateHealthBar();
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
    
    private void Update()
    {
        if (playerHealth == null) return;
        
        // Smooth health transition
        if (Mathf.Abs(displayedHealth - playerHealth.currentHealth) > 0.01f)
        {
            displayedHealth = Mathf.Lerp(displayedHealth, playerHealth.currentHealth, Time.deltaTime * smoothSpeed);
            UpdateHealthBar();
        }
    }
    
    private void LateUpdate()
    {
        if (healthBarTransform == null) return;
        
        // Can barı her zaman düz kalsın (karakter dönse bile)
        healthBarTransform.rotation = Quaternion.identity;
    }
    
    private void UpdateHealthBar()
    {
        if (fillRenderer == null || playerHealth == null) return;
        
        float maxHealth = playerHealth.maxHealth;
        if (maxHealth <= 0) maxHealth = 1f;
        
        float healthPercent = Mathf.Clamp01(displayedHealth / maxHealth);
        
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
    
    private void OnDestroy()
    {
        if (healthBarTransform != null)
            Destroy(healthBarTransform.gameObject);
    }
}
