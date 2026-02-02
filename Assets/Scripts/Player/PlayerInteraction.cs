using UnityEngine;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.G; // G tuşu - diğer tuşlar kullanılıyor
    [SerializeField] private Vector3 popupOffset = new Vector3(0, 2.5f, 0); // Shop'un üstünde
    
    private bool isNearShop = false;
    private Transform currentShop;
    private GameObject interactionPopup;
    private TextMeshProUGUI popupText;
    private Canvas worldCanvas;
    
    private void Start()
    {
        CreateInteractionPopup();
    }
    
    private void Update()
    {
        // Shop yakınındayken G'ye basınca aç
        if (isNearShop && Input.GetKeyDown(interactKey))
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenShopPanel();
                HidePopup();
            }
        }
        
        // Popup'ı shop'un üstünde tut
        if (isNearShop && currentShop != null && interactionPopup != null && interactionPopup.activeSelf)
        {
            worldCanvas.transform.position = currentShop.position + popupOffset;
        }
    }
    
    private void CreateInteractionPopup()
    {
        // World Space Canvas oluştur
        GameObject canvasObj = new GameObject("InteractionWorldCanvas");
        worldCanvas = canvasObj.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.sortingOrder = 100;
        
        RectTransform canvasRt = canvasObj.GetComponent<RectTransform>();
        canvasRt.sizeDelta = new Vector2(400, 100);
        canvasRt.localScale = new Vector3(0.01f, 0.01f, 0.01f); // World space için küçült
        
        // Popup container (şeffaf arka plan)
        interactionPopup = new GameObject("InteractionPopup");
        interactionPopup.transform.SetParent(worldCanvas.transform, false);
        
        RectTransform rt = interactionPopup.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(350, 70);
        rt.anchoredPosition = Vector2.zero;
        
        // Text (arka plan yok - sadece yazı)
        GameObject textObj = new GameObject("PopupText");
        textObj.transform.SetParent(interactionPopup.transform, false);
        
        popupText = textObj.AddComponent<TextMeshProUGUI>();
        popupText.text = $"Press [{interactKey}] to enter Shop";
        popupText.fontSize = 36;
        popupText.alignment = TextAlignmentOptions.Center;
        popupText.color = new Color(1f, 0.85f, 0.4f); // Altın sarısı
        popupText.fontStyle = FontStyles.Bold;
        
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        textRt.anchoredPosition = Vector2.zero;
        
        // Outline efekti (okunabilirlik için)
        var outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 1f); // Siyah outline
        outline.effectDistance = new Vector2(3, -3);
        
        // İkinci outline daha iyi görünüm için
        var outline2 = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline2.effectColor = new Color(0.3f, 0.1f, 0.5f); // Mor outline
        outline2.effectDistance = new Vector2(1.5f, -1.5f);
        
        // Başlangıçta gizle
        interactionPopup.SetActive(false);
    }
    
    private void ShowPopup(Transform shopTransform)
    {
        if (interactionPopup != null && worldCanvas != null)
        {
            currentShop = shopTransform;
            interactionPopup.SetActive(true);
            
            // İlk pozisyonu ayarla
            worldCanvas.transform.position = shopTransform.position + popupOffset;
            
            // Kameraya baksın (billboard effect)
            if (Camera.main != null)
            {
                worldCanvas.transform.LookAt(worldCanvas.transform.position + Camera.main.transform.forward);
            }
        }
    }
    
    private void HidePopup()
    {
        if (interactionPopup != null)
        {
            interactionPopup.SetActive(false);
            currentShop = null;
        }
    }
    
    private void LateUpdate()
    {
        // Billboard effect - her zaman kameraya baksın
        if (isNearShop && worldCanvas != null && Camera.main != null)
        {
            worldCanvas.transform.LookAt(worldCanvas.transform.position + Camera.main.transform.forward);
        }
    }
    
    public void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Shop"))
        {
            isNearShop = true;
            ShowPopup(other.transform);
            Debug.Log($"[PlayerInteraction] Shop yakınında - {interactKey} tuşuna basarak gir");
        }
    }
    
    public void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Shop"))
        {
            isNearShop = false;
            HidePopup();
            Debug.Log("[PlayerInteraction] Shop'tan uzaklaştı");
        }
    }
}