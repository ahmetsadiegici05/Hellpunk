using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Coin kazanıldığında ekranda gösterilen popup.
/// </summary>
public class CoinPopup : MonoBehaviour
{
    private Text coinText;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    
    public static void Show(int amount, Vector3 worldPosition)
    {
        // Canvas bul veya oluştur
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("PopupCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Popup oluştur
        GameObject popupObj = new GameObject("CoinPopup");
        popupObj.transform.SetParent(canvas.transform, false);
        
        CoinPopup popup = popupObj.AddComponent<CoinPopup>();
        popup.Initialize(amount, worldPosition);
    }
    
    private void Initialize(int amount, Vector3 worldPosition)
    {
        rectTransform = gameObject.AddComponent<RectTransform>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        // Pozisyonu ayarla (dünya koordinatından ekran koordinatına)
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        rectTransform.position = screenPos;
        rectTransform.sizeDelta = new Vector2(200, 60);
        
        // Arka plan
        Image bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        // Coin metni
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(transform, false);
        coinText = textObj.AddComponent<Text>();
        coinText.text = $"+{amount} 🪙";
        coinText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        coinText.fontSize = 28;
        coinText.alignment = TextAnchor.MiddleCenter;
        coinText.color = new Color(1f, 0.85f, 0.2f);
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // Animasyon başlat
        StartCoroutine(AnimatePopup());
    }
    
    private IEnumerator AnimatePopup()
    {
        float duration = 1.5f;
        float elapsed = 0f;
        Vector2 startPos = rectTransform.anchoredPosition;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Yukarı hareket
            rectTransform.anchoredPosition = startPos + Vector2.up * (t * 80f);
            
            // Fade out (son %30'da)
            if (t > 0.7f)
            {
                canvasGroup.alpha = 1f - ((t - 0.7f) / 0.3f);
            }
            
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
