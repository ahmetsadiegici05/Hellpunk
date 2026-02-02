using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Oyun temasına uygun coin UI - mor/turuncu tema
/// Sol üst köşede coin ikonu ve animasyonlu text gösterir
/// </summary>
public class CoinUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI coinText;
    [SerializeField] private Image coinIcon;
    [SerializeField] private Image backgroundPanel;
    
    [Header("Animation Settings")]
    [SerializeField] private float punchScale = 1.3f;
    [SerializeField] private float punchDuration = 0.2f;
    
    private int lastCoinValue = 0;
    private Coroutine punchCoroutine;
    private Vector3 originalScale;

    private void Start()
    {
        if (coinText != null)
            originalScale = coinText.transform.localScale;
            
        // İlk değeri al
        if (GameManager.Instance != null)
            lastCoinValue = GameManager.Instance.coin;
        
        // Sağ üst köşeye taşı
        PositionToTopRight();
        
        // Runtime'da sprite'ları oluştur (prefab'dan yüklendiğinde kaybolabilir)
        EnsureSprites();
            
        UpdateDisplay();
    }
    
    /// <summary>
    /// Sprite'ların mevcut olduğundan emin ol, yoksa oluştur
    /// </summary>
    private void EnsureSprites()
    {
        // Background panel sprite'ı kontrol et
        if (backgroundPanel != null && backgroundPanel.sprite == null)
        {
            backgroundPanel.sprite = CreateRoundedRectSprite(64, 16);
            backgroundPanel.type = Image.Type.Sliced;
        }
        
        // Inner panel
        Transform innerPanelT = transform.Find("InnerPanel");
        if (innerPanelT != null)
        {
            Image innerImg = innerPanelT.GetComponent<Image>();
            if (innerImg != null && innerImg.sprite == null)
            {
                innerImg.sprite = CreateRoundedRectSprite(64, 14);
                innerImg.type = Image.Type.Sliced;
            }
        }
        
        // Coin icon container background
        Transform iconContainerT = transform.Find("CoinIconContainer");
        if (iconContainerT != null)
        {
            Image iconBg = iconContainerT.GetComponent<Image>();
            if (iconBg != null && iconBg.sprite == null)
            {
                iconBg.sprite = CreateCircleSprite(64);
            }
            
            // Coin icon
            Transform coinIconT = iconContainerT.Find("CoinIcon");
            if (coinIconT != null)
            {
                Image iconImg = coinIconT.GetComponent<Image>();
                if (iconImg != null && iconImg.sprite == null)
                {
                    iconImg.sprite = CreateCircleSprite(64);
                }
            }
        }
    }
    
    /// <summary>
    /// CoinUI'ı sağ üst köşeye taşır (HealthBar sol üstte olduğu için)
    /// </summary>
    private void PositionToTopRight()
    {
        RectTransform rt = GetComponent<RectTransform>();
        if (rt == null) return;
        
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-20, -20);
    }

    private void Update()
    {
        // Her frame'de güncelle (Time.timeScale = 0 olsa bile)
        UpdateCoinDisplay();
    }
    
    private void UpdateCoinDisplay()
    {
        if (GameManager.Instance == null) return;
        
        // Coin değişti mi kontrol et
        if (GameManager.Instance.coin != lastCoinValue)
        {
            int difference = GameManager.Instance.coin - lastCoinValue;
            lastCoinValue = GameManager.Instance.coin;
            UpdateDisplay();
            
            // Artış olduysa animasyon yap
            if (difference > 0)
            {
                PlayPunchAnimation();
            }
        }
    }
    
    /// <summary>
    /// Dışarıdan coin gösterimini zorla güncelle (Shop için)
    /// </summary>
    public void ForceUpdateDisplay()
    {
        if (GameManager.Instance != null)
            lastCoinValue = GameManager.Instance.coin;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (coinText == null) return;
        
        if (GameManager.Instance != null)
            coinText.text = GameManager.Instance.coin.ToString();
        else
            coinText.text = "0";
    }

    private void PlayPunchAnimation()
    {
        if (coinText == null) return;
        
        if (punchCoroutine != null)
            StopCoroutine(punchCoroutine);
            
        punchCoroutine = StartCoroutine(PunchScaleCoroutine());
    }

    private IEnumerator PunchScaleCoroutine()
    {
        // Büyüt
        float elapsed = 0f;
        float halfDuration = punchDuration / 2f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            coinText.transform.localScale = Vector3.Lerp(originalScale, originalScale * punchScale, t);
            yield return null;
        }
        
        // Küçült
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / halfDuration;
            coinText.transform.localScale = Vector3.Lerp(originalScale * punchScale, originalScale, t);
            yield return null;
        }
        
        coinText.transform.localScale = originalScale;
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Coin UI")]
    private void GenerateUI()
    {
        // Eski child'ları temizle
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
        
        // Tema renkleri - oyunun mor/turuncu teması
        Color bgColor = new Color(0.1f, 0.06f, 0.15f, 0.85f);
        Color borderColor = new Color(0.6f, 0.3f, 0.8f, 0.9f);
        Color coinGold = new Color(1f, 0.8f, 0.2f);
        Color textColor = Color.white;
        
        // Ana panel - RectTransform ayarla
        RectTransform myRect = GetComponent<RectTransform>();
        if (myRect == null) myRect = gameObject.AddComponent<RectTransform>();
        
        // Sağ üst köşe (health bar sol üstte olduğu için)
        myRect.anchorMin = new Vector2(1, 1);
        myRect.anchorMax = new Vector2(1, 1);
        myRect.pivot = new Vector2(1, 1);
        myRect.anchoredPosition = new Vector2(-20, -20);
        myRect.sizeDelta = new Vector2(180, 60);
        myRect.localScale = Vector3.one;
        
        // Background panel
        backgroundPanel = GetComponent<Image>();
        if (backgroundPanel == null) backgroundPanel = gameObject.AddComponent<Image>();
        backgroundPanel.color = bgColor;
        backgroundPanel.sprite = CreateRoundedRectSprite(64, 16);
        backgroundPanel.type = Image.Type.Sliced;
        
        // İç panel
        GameObject innerPanel = new GameObject("InnerPanel");
        innerPanel.transform.SetParent(transform, false);
        RectTransform innerRect = innerPanel.AddComponent<RectTransform>();
        innerRect.anchorMin = Vector2.zero;
        innerRect.anchorMax = Vector2.one;
        innerRect.offsetMin = new Vector2(3, 3);
        innerRect.offsetMax = new Vector2(-3, -3);
        innerRect.localScale = Vector3.one;
        Image innerImg = innerPanel.AddComponent<Image>();
        innerImg.color = new Color(0.08f, 0.04f, 0.12f, 0.95f);
        innerImg.sprite = CreateRoundedRectSprite(64, 14);
        innerImg.type = Image.Type.Sliced;
        
        // Coin icon container
        GameObject iconContainer = new GameObject("CoinIconContainer");
        iconContainer.transform.SetParent(transform, false);
        RectTransform iconContainerRect = iconContainer.AddComponent<RectTransform>();
        iconContainerRect.anchorMin = new Vector2(0, 0.5f);
        iconContainerRect.anchorMax = new Vector2(0, 0.5f);
        iconContainerRect.pivot = new Vector2(0, 0.5f);
        iconContainerRect.anchoredPosition = new Vector2(12, 0);
        iconContainerRect.sizeDelta = new Vector2(40, 40);
        iconContainerRect.localScale = Vector3.one;
        
        // Coin circle background
        Image iconBg = iconContainer.AddComponent<Image>();
        iconBg.color = new Color(0.15f, 0.1f, 0.2f);
        iconBg.sprite = CreateCircleSprite(64);
        
        // Coin icon (altın rengi daire)
        GameObject iconObj = new GameObject("CoinIcon");
        iconObj.transform.SetParent(iconContainer.transform, false);
        RectTransform iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(32, 32);
        iconRect.localScale = Vector3.one;
        coinIcon = iconObj.AddComponent<Image>();
        coinIcon.color = coinGold;
        coinIcon.sprite = CreateCircleSprite(64);
        
        // Coin sembolu (TMP)
        GameObject coinSymbol = new GameObject("CoinSymbol");
        coinSymbol.transform.SetParent(iconObj.transform, false);
        RectTransform symbolRect = coinSymbol.AddComponent<RectTransform>();
        symbolRect.anchorMin = Vector2.zero;
        symbolRect.anchorMax = Vector2.one;
        symbolRect.offsetMin = Vector2.zero;
        symbolRect.offsetMax = Vector2.zero;
        symbolRect.localScale = Vector3.one;
        TextMeshProUGUI symbolText = coinSymbol.AddComponent<TextMeshProUGUI>();
        symbolText.text = "¢";
        symbolText.fontSize = 24;
        symbolText.fontStyle = FontStyles.Bold;
        symbolText.alignment = TextAlignmentOptions.Center;
        symbolText.color = new Color(0.2f, 0.15f, 0.05f);
        
        // Coin text
        GameObject textObj = new GameObject("CoinText");
        textObj.transform.SetParent(transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0.5f);
        textRect.anchorMax = new Vector2(1, 0.5f);
        textRect.pivot = new Vector2(0, 0.5f);
        textRect.anchoredPosition = new Vector2(60, 0);
        textRect.sizeDelta = new Vector2(110, 40);
        textRect.localScale = Vector3.one;
        coinText = textObj.AddComponent<TextMeshProUGUI>();
        coinText.text = "0";
        coinText.fontSize = 32;
        coinText.fontStyle = FontStyles.Bold;
        coinText.alignment = TextAlignmentOptions.Left;
        coinText.color = textColor;
        
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("CoinUI Generated!");
    }
#endif
    
    private Sprite CreateRoundedRectSprite(int size, int cornerRadius)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool inside = IsInsideRoundedRect(x, y, size, size, cornerRadius);
                pixels[y * size + x] = inside ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;

        float border = cornerRadius;
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
    }

    private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
    {
        if (x < radius && y < radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) <= radius;
        if (x >= width - radius && y < radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, radius)) <= radius;
        if (x < radius && y >= height - radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius - 1)) <= radius;
        if (x >= width - radius && y >= height - radius)
            return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, height - radius - 1)) <= radius;
        return true;
    }

    private Sprite CreateCircleSprite(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((radius - dist) / 2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;

        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
