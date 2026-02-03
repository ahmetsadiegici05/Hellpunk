using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Mini-Map UI'ını sahnede otomatik oluşturur.
/// Unity Editor'da sağ tık > UI > Create MiniMap UI ile kullanılabilir.
/// </summary>
public class MiniMapUIGenerator : MonoBehaviour
{
#if UNITY_EDITOR
    [MenuItem("GameObject/UI/Create MiniMap UI", false, 10)]
    static void CreateMiniMapUI(MenuCommand menuCommand)
    {
        // Canvas bul veya oluştur
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Ana container
        GameObject miniMapContainer = new GameObject("MiniMap_Container");
        miniMapContainer.transform.SetParent(canvas.transform, false);
        
        RectTransform containerRect = miniMapContainer.AddComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(320, 55);
        
        // Alt orta pozisyon
        containerRect.anchorMin = new Vector2(0.5f, 0);
        containerRect.anchorMax = new Vector2(0.5f, 0);
        containerRect.pivot = new Vector2(0.5f, 0);
        containerRect.anchoredPosition = new Vector2(0, 20);
        
        // Arka plan (yumuşak köşeli)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(miniMapContainer.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.08f, 0.02f, 0.12f, 0.85f);
        bgImage.raycastTarget = false;
        
        // Rounded corners için sprite ayarla (varsa)
        // bgImage.sprite = ... // Rounded rect sprite atanabilir
        // bgImage.type = Image.Type.Sliced;
        
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        
        // Border/Frame
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(miniMapContainer.transform, false);
        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = new Color(0.6f, 0.2f, 0.7f, 0.9f);
        borderImage.raycastTarget = false;
        
        // Outline component ekle (daha yumuşak görünüm için)
        Outline outline = borderObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.1f, 0.5f, 0.5f);
        outline.effectDistance = new Vector2(2, 2);
        
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-2, -2);
        borderRect.offsetMax = new Vector2(2, 2);
        borderObj.transform.SetAsFirstSibling(); // Arka plana at
        
        // İç içerik alanı (mask için)
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(miniMapContainer.transform, false);
        Image contentMask = contentObj.AddComponent<Image>();
        contentMask.color = Color.white;
        contentMask.raycastTarget = false;
        
        // Mask ekle
        Mask mask = contentObj.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(4, 4);
        contentRect.offsetMax = new Vector2(-4, -4);
        
        // Oyuncu ikonu
        GameObject playerIconObj = new GameObject("PlayerIcon");
        playerIconObj.transform.SetParent(contentObj.transform, false);
        Image playerIcon = playerIconObj.AddComponent<Image>();
        playerIcon.color = new Color(1f, 0.5f, 0.2f, 1f); // Turuncu
        playerIcon.raycastTarget = false;
        
        RectTransform playerRect = playerIconObj.GetComponent<RectTransform>();
        playerRect.sizeDelta = new Vector2(12, 12);
        playerRect.anchoredPosition = Vector2.zero;
        
        // Glow efekti (opsiyonel)
        Shadow playerGlow = playerIconObj.AddComponent<Shadow>();
        playerGlow.effectColor = new Color(1f, 0.5f, 0.2f, 0.5f);
        playerGlow.effectDistance = new Vector2(0, 0);
        
        // Pozisyon text (sağ üst köşede)
        GameObject textObj = new GameObject("PositionText");
        textObj.transform.SetParent(miniMapContainer.transform, false);
        
        // TextMeshPro varsa onu kullan
        var tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        tmpText.text = "X:0 Y:0";
        tmpText.fontSize = 9;
        tmpText.color = new Color(1f, 1f, 1f, 0.4f);
        tmpText.alignment = TMPro.TextAlignmentOptions.TopRight;
        tmpText.raycastTarget = false;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(1, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(1, 1);
        textRect.anchoredPosition = new Vector2(-5, -3);
        textRect.sizeDelta = new Vector2(80, 15);
        
        // MiniMap2D script'ini ekle
        MiniMap2D miniMap = miniMapContainer.AddComponent<MiniMap2D>();
        
        // Referansları SerializedObject ile ata
        SerializedObject so = new SerializedObject(miniMap);
        so.FindProperty("useExistingUI").boolValue = true;
        so.FindProperty("existingContainer").objectReferenceValue = containerRect;
        so.FindProperty("existingPlayerIcon").objectReferenceValue = playerIcon;
        so.FindProperty("existingPositionText").objectReferenceValue = tmpText;
        so.ApplyModifiedProperties();
        
        // Seç
        Selection.activeGameObject = miniMapContainer;
        Undo.RegisterCreatedObjectUndo(miniMapContainer, "Create MiniMap UI");
        
        Debug.Log("[MiniMapUIGenerator] Mini-Map UI oluşturuldu! Inspector'dan ayarları düzenleyebilirsiniz.");
    }
#endif
}
