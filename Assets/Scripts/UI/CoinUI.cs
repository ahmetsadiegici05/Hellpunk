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
            
        UpdateDisplay();
    }

    private void Update()
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
        
        // Sol üst köşe
        myRect.anchorMin = new Vector2(0, 1);
        myRect.anchorMax = new Vector2(0, 1);
        myRect.pivot = new Vector2(0, 1);
        myRect.anchoredPosition = new Vector2(20, -20);
        myRect.sizeDelta = new Vector2(180, 60);
        myRect.localScale = Vector3.one;
        
        // Background panel
        backgroundPanel = GetComponent<Image>();
        if (backgroundPanel == null) backgroundPanel = gameObject.AddComponent<Image>();
        backgroundPanel.color = bgColor;
        
        // Border/Glow efekti
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(transform, false);
        RectTransform borderRect = borderObj.AddComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2, -2);
        borderRect.offsetMax = new Vector2(2, 2);
        borderRect.localScale = Vector3.one;
        Image borderImg = borderObj.AddComponent<Image>();
        borderImg.color = borderColor;
        borderObj.transform.SetAsFirstSibling(); // Arkaya al
        
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
        
        // Glow/shine efekti (dekoratif çizgi)
        GameObject shine = new GameObject("Shine");
        shine.transform.SetParent(transform, false);
        RectTransform shineRect = shine.AddComponent<RectTransform>();
        shineRect.anchorMin = new Vector2(0, 1);
        shineRect.anchorMax = new Vector2(1, 1);
        shineRect.pivot = new Vector2(0.5f, 1);
        shineRect.anchoredPosition = new Vector2(0, -3);
        shineRect.sizeDelta = new Vector2(-20, 2);
        shineRect.localScale = Vector3.one;
        Image shineImg = shine.AddComponent<Image>();
        shineImg.color = new Color(1f, 0.6f, 0.3f, 0.6f); // Turuncu glow
        
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log("CoinUI Generated!");
    }
#endif
}
