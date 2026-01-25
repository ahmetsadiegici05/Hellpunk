using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Düşman can barı - düşmanın üzerinde görünen sağlık göstergesi
/// EnemyHealth bileşenine otomatik bağlanır
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(1f, 0.15f);
    [SerializeField] private bool hideWhenFull = true;
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Colors")]
    [SerializeField] private Color healthColor = new Color(0.2f, 0.8f, 0.2f, 1f); // Yeşil
    [SerializeField] private Color damagedColor = new Color(0.9f, 0.2f, 0.2f, 1f); // Kırmızı
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    private Transform target;
    private Canvas canvas;
    private Image backgroundImage;
    private Image healthFillImage;
    private CanvasGroup canvasGroup;

    private float maxHealth;
    private float currentHealth;
    private float displayedHealth;
    private bool isVisible = false;
    private float lastDamageTime;

    public void Initialize(Transform enemyTransform, float maxHp)
    {
        target = enemyTransform;
        maxHealth = maxHp;
        currentHealth = maxHp;
        displayedHealth = maxHp;
        lastDamageTime = Time.time; // Başlangıçta görünür olsun
        isVisible = true;
        
        // Offset'i düşmanın boyutuna göre ayarla
        SpriteRenderer sr = enemyTransform.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            offset = new Vector3(0f, sr.bounds.extents.y + 0.3f, 0f);
        }

        CreateHealthBar();
        
        Debug.Log($"EnemyHealthBar oluşturuldu: {enemyTransform.name}, MaxHP: {maxHp}");
    }

    private void CreateHealthBar()
    {
        // Canvas oluştur (World Space)
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        // Default sorting layer kullan, yüksek order ile
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(100, 20);
        canvasRect.localScale = Vector3.one * 0.015f; // Biraz daha büyük

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f; // Başlangıçta görünür

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        backgroundImage.sprite = CreateRoundedRectSprite(32, 8);
        backgroundImage.type = Image.Type.Sliced;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;

        // Health Fill
        GameObject fillObj = new GameObject("HealthFill");
        fillObj.transform.SetParent(canvasObj.transform, false);
        healthFillImage = fillObj.AddComponent<Image>();
        healthFillImage.color = healthColor;
        healthFillImage.sprite = CreateRoundedRectSprite(32, 6);
        healthFillImage.type = Image.Type.Sliced;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(2f, 2f);
        fillRect.offsetMax = new Vector2(-2f, -2f);

        // Scale ayarla
        canvasRect.sizeDelta = barSize * 100f;
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

        // Sorting Order güncelle (Düşmanın hep önünde olsun)
        if (canvas != null)
        {
            // Kameraya bak
            if (Camera.main != null)
                canvas.transform.rotation = Camera.main.transform.rotation;
                
            SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Düşman hangi kattaysa onun önünde dur
                canvas.sortingLayerID = sr.sortingLayerID;
                canvas.sortingOrder = sr.sortingOrder + 10;
            }
            else
            {
                // Sprite yoksa varsayılan
                canvas.sortingOrder = 100;
            }
        }

        // Smooth health transition
        if (Mathf.Abs(displayedHealth - currentHealth) > 0.01f)
        {
            displayedHealth = Mathf.Lerp(displayedHealth, currentHealth, Time.deltaTime * 10f);
            UpdateHealthBar();
        }

        // Visibility
        UpdateVisibility();
    }

    public void SetHealth(float health)
    {
        currentHealth = Mathf.Clamp(health, 0f, maxHealth);
        lastDamageTime = Time.time;
        isVisible = true;
        
        // Hasar aldığında hemen görünür yap
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
        
        // Anında güncelle
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (healthFillImage == null) return;

        float healthPercent = displayedHealth / maxHealth;
        
        // Fill amount (scale X)
        RectTransform fillRect = healthFillImage.rectTransform;
        fillRect.anchorMax = new Vector2(healthPercent, 1f);

        // Color gradient (yeşil -> kırmızı)
        healthFillImage.color = Color.Lerp(damagedColor, healthColor, healthPercent);
    }

    private void UpdateVisibility()
    {
        if (canvasGroup == null) return;

        // Can tam değilse her zaman göster
        bool shouldShow = currentHealth < maxHealth;
        
        // Can tamsa ve 3 saniye geçtiyse gizle
        if (currentHealth >= maxHealth && Time.time - lastDamageTime > 3f)
        {
            shouldShow = false;
        }

        float targetAlpha = shouldShow ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.deltaTime * fadeSpeed);
    }

    private Sprite CreateRoundedRectSprite(int size, int cornerRadius)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = true;

                if (x < cornerRadius && y < cornerRadius)
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, cornerRadius)) <= cornerRadius;
                else if (x >= size - cornerRadius && y < cornerRadius)
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius - 1, cornerRadius)) <= cornerRadius;
                else if (x < cornerRadius && y >= size - cornerRadius)
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(cornerRadius, size - cornerRadius - 1)) <= cornerRadius;
                else if (x >= size - cornerRadius && y >= size - cornerRadius)
                    inside = Vector2.Distance(new Vector2(x, y), new Vector2(size - cornerRadius - 1, size - cornerRadius - 1)) <= cornerRadius;

                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
    }

    private void OnDestroy()
    {
        if (canvas != null)
        {
            Destroy(canvas.gameObject);
        }
    }
}
