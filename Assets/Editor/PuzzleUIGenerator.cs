using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Puzzle UI prefab'larını otomatik oluşturur.
/// Unity Editor menüsünden: Tools → Puzzle UI → Generate Prefabs
/// 
/// NOT: Prefab'lar Canvas DEĞİL, normal UI Panel'dir.
/// Sahnedeki mevcut Canvas'ın altına child olarak sürükleyip bırakın.
/// </summary>
public class PuzzleUIGenerator : EditorWindow
{
    private static string prefabPath = "Assets/Prefabs/UI/Puzzles";
    
    [MenuItem("Tools/Puzzle UI/Generate All Prefabs")]
    public static void GenerateAllPrefabs()
    {
        // Klasör yoksa oluştur
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI/Puzzles"))
            AssetDatabase.CreateFolder("Assets/Prefabs/UI", "Puzzles");
        
        GenerateCombinationPuzzle();
        GenerateRhythmPuzzle();
        GenerateMemoryPuzzle();
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Başarılı!", 
            "Puzzle UI Prefab'ları oluşturuldu:\n" + prefabPath + 
            "\n\n" +
            "KULLANIM:\n" +
            "1. Prefab'ı sahnedeki Canvas'ın altına sürükleyin\n" +
            "2. PuzzleBox script'ine prefab referansını atayın\n" +
            "3. Test edin!", "Tamam");
    }
    
    [MenuItem("Tools/Puzzle UI/Generate Combination Lock")]
    public static void GenerateCombinationPuzzle()
    {
        EnsureFolders();
        
        // Ana container - CANVAS DEĞİL, normal UI Panel
        GameObject rootObj = CreateRootPanel("CombinationPuzzleUI");
        RectTransform rootRect = rootObj.GetComponent<RectTransform>();
        
        // Background - root'un kendisi zaten background olacak (yarı saydam siyah)
        // Ayrı bir background gerekmiyor, rootObj zaten tam ekran panel
        
        // Panel (ortadaki beyaz kutu)
        GameObject panelObj = CreateImage(rootRect, "Panel", new Color(0.12f, 0.12f, 0.18f, 0.98f));
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(520, 580);
        panelRect.anchoredPosition = Vector2.zero;
        
        // Border
        GameObject borderObj = CreateImage(panelRect, "Border", new Color(1f, 0.6f, 0.1f, 0.4f));
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-3, -3);
        borderRect.offsetMax = new Vector2(3, 3);
        borderObj.transform.SetAsFirstSibling();
        
        // Title
        GameObject titleObj = CreateText(panelRect, "Title", "[ CRACK THE CODE ]", 36, new Color(1f, 0.6f, 0.1f));
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -15);
        titleRect.sizeDelta = new Vector2(-20, 50);
        
        // Decorative line
        GameObject lineObj = CreateImage(panelRect, "Line", new Color(1f, 0.6f, 0.1f, 0.5f));
        RectTransform lineRect = lineObj.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.1f, 1);
        lineRect.anchorMax = new Vector2(0.9f, 1);
        lineRect.pivot = new Vector2(0.5f, 1);
        lineRect.anchoredPosition = new Vector2(0, -65);
        lineRect.sizeDelta = new Vector2(0, 2);
        
        // Digit Container
        GameObject digitContainer = CreateEmpty(panelRect, "DigitContainer");
        RectTransform digitContainerRect = digitContainer.GetComponent<RectTransform>();
        digitContainerRect.anchorMin = new Vector2(0.5f, 1);
        digitContainerRect.anchorMax = new Vector2(0.5f, 1);
        digitContainerRect.pivot = new Vector2(0.5f, 1);
        digitContainerRect.anchoredPosition = new Vector2(0, -85);
        digitContainerRect.sizeDelta = new Vector2(450, 90);
        
        HorizontalLayoutGroup digitLayout = digitContainer.AddComponent<HorizontalLayoutGroup>();
        digitLayout.spacing = 20;
        digitLayout.childAlignment = TextAnchor.MiddleCenter;
        digitLayout.childForceExpandWidth = false;
        digitLayout.childForceExpandHeight = false;
        
        // Digits (4 tane)
        Image[] digitBgs = new Image[4];
        Text[] digitTexts = new Text[4];
        
        for (int i = 0; i < 4; i++)
        {
            GameObject digitObj = CreateImage(digitContainerRect, $"Digit_{i}", new Color(0.25f, 0.25f, 0.3f));
            RectTransform digitRect = digitObj.GetComponent<RectTransform>();
            digitRect.sizeDelta = new Vector2(75, 75);
            digitBgs[i] = digitObj.GetComponent<Image>();
            
            // Layout element ekle
            LayoutElement le = digitObj.AddComponent<LayoutElement>();
            le.preferredWidth = 75;
            le.preferredHeight = 75;
            
            GameObject textObj = CreateText(digitRect, "Text", "0", 42, Color.white);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            digitTexts[i] = textObj.GetComponent<Text>();
            digitTexts[i].fontStyle = FontStyle.Bold;
        }
        
        // History Title
        GameObject historyTitle = CreateText(panelRect, "HistoryTitle", "ATTEMPTS", 18, new Color(0.7f, 0.7f, 0.7f));
        RectTransform htRect = historyTitle.GetComponent<RectTransform>();
        htRect.anchorMin = new Vector2(0, 1);
        htRect.anchorMax = new Vector2(1, 1);
        htRect.pivot = new Vector2(0.5f, 1);
        htRect.anchoredPosition = new Vector2(0, -190);
        htRect.sizeDelta = new Vector2(0, 25);
        historyTitle.GetComponent<Text>().fontStyle = FontStyle.Bold;
        
        // History Container
        GameObject historyContainer = CreateImage(panelRect, "HistoryContainer", new Color(0.08f, 0.08f, 0.1f, 0.8f));
        RectTransform historyRect = historyContainer.GetComponent<RectTransform>();
        historyRect.anchorMin = new Vector2(0.05f, 0.18f);
        historyRect.anchorMax = new Vector2(0.95f, 0.62f);
        historyRect.offsetMin = Vector2.zero;
        historyRect.offsetMax = Vector2.zero;
        
        VerticalLayoutGroup historyLayout = historyContainer.AddComponent<VerticalLayoutGroup>();
        historyLayout.spacing = 4;
        historyLayout.padding = new RectOffset(10, 10, 10, 10);
        historyLayout.childAlignment = TextAnchor.UpperCenter;
        historyLayout.childForceExpandWidth = true;
        historyLayout.childForceExpandHeight = false;
        
        // Attempt texts (8 tane)
        Text[] attemptTexts = new Text[8];
        for (int i = 0; i < 8; i++)
        {
            GameObject rowBg = CreateImage(historyRect, $"Attempt_{i}", i % 2 == 0 ? new Color(0.15f, 0.15f, 0.18f, 0.5f) : new Color(0.1f, 0.1f, 0.12f, 0.5f));
            RectTransform rowRect = rowBg.GetComponent<RectTransform>();
            
            LayoutElement rowLe = rowBg.AddComponent<LayoutElement>();
            rowLe.preferredHeight = 24;
            
            GameObject attemptText = CreateText(rowRect, "Text", $"{i + 1}. ---", 18, new Color(0.5f, 0.5f, 0.5f));
            RectTransform atRect = attemptText.GetComponent<RectTransform>();
            atRect.anchorMin = Vector2.zero;
            atRect.anchorMax = Vector2.one;
            atRect.offsetMin = Vector2.zero;
            atRect.offsetMax = Vector2.zero;
            attemptTexts[i] = attemptText.GetComponent<Text>();
        }
        
        // Hint
        GameObject hintObj = CreateText(panelRect, "Hint", "[UP/DOWN] Digit   [LEFT/RIGHT] Select   [ENTER] Guess   [ESC] Cancel", 18, new Color(0.5f, 0.5f, 0.55f));
        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0, 0);
        hintRect.anchorMax = new Vector2(1, 0);
        hintRect.pivot = new Vector2(0.5f, 0);
        hintRect.anchoredPosition = new Vector2(0, 15);
        hintRect.sizeDelta = new Vector2(-20, 50);
        
        // Script ekle ve referansları ata
        CombinationPuzzleUI puzzleScript = rootObj.AddComponent<CombinationPuzzleUI>();
        
        SerializedObject so = new SerializedObject(puzzleScript);
        so.FindProperty("prefabDigitTexts").arraySize = 4;
        so.FindProperty("prefabDigitBackgrounds").arraySize = 4;
        for (int i = 0; i < 4; i++)
        {
            so.FindProperty("prefabDigitTexts").GetArrayElementAtIndex(i).objectReferenceValue = digitTexts[i];
            so.FindProperty("prefabDigitBackgrounds").GetArrayElementAtIndex(i).objectReferenceValue = digitBgs[i];
        }
        so.FindProperty("prefabAttemptTexts").arraySize = 8;
        for (int i = 0; i < 8; i++)
        {
            so.FindProperty("prefabAttemptTexts").GetArrayElementAtIndex(i).objectReferenceValue = attemptTexts[i];
        }
        so.FindProperty("prefabHintText").objectReferenceValue = hintObj.GetComponent<Text>();
        so.FindProperty("prefabTitleText").objectReferenceValue = titleObj.GetComponent<Text>();
        so.ApplyModifiedProperties();
        
        // Prefab olarak kaydet (ACTIVE olarak - sahnede görünür olsun)
        rootObj.SetActive(true);
        string path = $"{prefabPath}/CombinationPuzzleUI.prefab";
        PrefabUtility.SaveAsPrefabAsset(rootObj, path);
        DestroyImmediate(rootObj);
        
        Debug.Log("Created: " + path);
    }
    
    [MenuItem("Tools/Puzzle UI/Generate Rhythm Puzzle")]
    public static void GenerateRhythmPuzzle()
    {
        EnsureFolders();
        
        // Ana container - CANVAS DEĞİL, normal UI Panel
        GameObject rootObj = CreateRootPanel("RhythmPuzzleUI");
        RectTransform rootRect = rootObj.GetComponent<RectTransform>();
        
        // Panel (ortadaki kutu)
        GameObject panelObj = CreateImage(rootRect, "Panel", new Color(0.08f, 0.08f, 0.12f, 0.98f));
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(700, 350);
        panelRect.anchoredPosition = Vector2.zero;
        
        // Border
        GameObject borderObj = CreateImage(panelRect, "Border", new Color(1f, 0.6f, 0.1f, 0.3f));
        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-3, -3);
        borderRect.offsetMax = new Vector2(3, 3);
        borderObj.transform.SetAsFirstSibling();
        
        // Title
        GameObject titleObj = CreateText(panelRect, "Title", "[ CATCH THE BEAT ]", 36, new Color(1f, 0.6f, 0.1f));
        titleObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -15);
        titleRect.sizeDelta = new Vector2(0, 45);
        
        // Subtitle
        GameObject subObj = CreateText(panelRect, "Subtitle", "Press SPACE when the bar is in the green zone!", 20, new Color(0.6f, 0.6f, 0.7f));
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0, 1);
        subRect.anchorMax = new Vector2(1, 1);
        subRect.pivot = new Vector2(0.5f, 1);
        subRect.anchoredPosition = new Vector2(0, -55);
        subRect.sizeDelta = new Vector2(0, 25);
        
        // Beat Bar
        GameObject beatBar = CreateImage(panelRect, "BeatBar", new Color(0.15f, 0.15f, 0.2f));
        RectTransform beatBarRect = beatBar.GetComponent<RectTransform>();
        beatBarRect.anchorMin = new Vector2(0.5f, 0.5f);
        beatBarRect.anchorMax = new Vector2(0.5f, 0.5f);
        beatBarRect.sizeDelta = new Vector2(600, 60);
        beatBarRect.anchoredPosition = new Vector2(0, 20);
        
        // Hit Zone
        GameObject hitZone = CreateImage(beatBarRect, "HitZone", new Color(0.1f, 0.9f, 0.3f, 0.4f));
        RectTransform hitZoneRect = hitZone.GetComponent<RectTransform>();
        hitZoneRect.anchorMin = new Vector2(0.5f, 0);
        hitZoneRect.anchorMax = new Vector2(0.5f, 1);
        hitZoneRect.sizeDelta = new Vector2(80, -8);
        hitZoneRect.anchoredPosition = Vector2.zero;
        
        // Hit zone borders
        GameObject leftBorder = CreateImage(hitZoneRect, "LeftBorder", Color.white);
        RectTransform leftRect = leftBorder.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0, 1);
        leftRect.pivot = new Vector2(0, 0.5f);
        leftRect.sizeDelta = new Vector2(3, 0);
        leftRect.anchoredPosition = Vector2.zero;
        
        GameObject rightBorder = CreateImage(hitZoneRect, "RightBorder", Color.white);
        RectTransform rightRect = rightBorder.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.pivot = new Vector2(1, 0.5f);
        rightRect.sizeDelta = new Vector2(3, 0);
        rightRect.anchoredPosition = Vector2.zero;
        
        // HIT label
        GameObject hitLabel = CreateText(hitZoneRect, "HitLabel", "HIT", 16, new Color(1f, 1f, 1f, 0.6f));
        hitLabel.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform hitLabelRect = hitLabel.GetComponent<RectTransform>();
        hitLabelRect.anchorMin = new Vector2(0.5f, 1);
        hitLabelRect.anchorMax = new Vector2(0.5f, 1);
        hitLabelRect.pivot = new Vector2(0.5f, 0);
        hitLabelRect.anchoredPosition = new Vector2(0, 2);
        hitLabelRect.sizeDelta = new Vector2(40, 15);
        
        // Beat Indicator
        GameObject beatIndicator = CreateImage(beatBarRect, "BeatIndicator", new Color(1f, 0.4f, 0.1f));
        RectTransform beatRect = beatIndicator.GetComponent<RectTransform>();
        beatRect.anchorMin = new Vector2(0, 0.5f);
        beatRect.anchorMax = new Vector2(0, 0.5f);
        beatRect.sizeDelta = new Vector2(16, 50);
        beatRect.anchoredPosition = new Vector2(20, 0);
        
        // Hit Markers Container
        GameObject markersContainer = CreateEmpty(panelRect, "HitMarkers");
        RectTransform markersRect = markersContainer.GetComponent<RectTransform>();
        markersRect.anchorMin = new Vector2(0.5f, 0.5f);
        markersRect.anchorMax = new Vector2(0.5f, 0.5f);
        markersRect.anchoredPosition = new Vector2(0, 75);
        markersRect.sizeDelta = new Vector2(400, 30);
        
        HorizontalLayoutGroup markersLayout = markersContainer.AddComponent<HorizontalLayoutGroup>();
        markersLayout.spacing = 8;
        markersLayout.childAlignment = TextAnchor.MiddleCenter;
        markersLayout.childForceExpandWidth = false;
        markersLayout.childForceExpandHeight = false;
        
        // Hit markers (5 tane)
        Image[] hitMarkers = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject marker = CreateImage(markersRect, $"Marker_{i}", new Color(0.25f, 0.25f, 0.3f));
            RectTransform markerRect = marker.GetComponent<RectTransform>();
            markerRect.sizeDelta = new Vector2(22, 22);
            
            LayoutElement le = marker.AddComponent<LayoutElement>();
            le.preferredWidth = 22;
            le.preferredHeight = 22;
            
            hitMarkers[i] = marker.GetComponent<Image>();
        }
        
        // Score Text
        GameObject scoreObj = CreateText(panelRect, "Score", "Hits: 0/5     O O O", 24, Color.white);
        RectTransform scoreRect = scoreObj.GetComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0, 0);
        scoreRect.anchorMax = new Vector2(1, 0);
        scoreRect.pivot = new Vector2(0.5f, 0);
        scoreRect.anchoredPosition = new Vector2(0, 50);
        scoreRect.sizeDelta = new Vector2(0, 35);
        
        // ESC Hint
        GameObject escObj = CreateText(panelRect, "EscHint", "[ESC] Cancel", 18, new Color(0.4f, 0.4f, 0.5f));
        RectTransform escRect = escObj.GetComponent<RectTransform>();
        escRect.anchorMin = new Vector2(0, 0);
        escRect.anchorMax = new Vector2(1, 0);
        escRect.pivot = new Vector2(0.5f, 0);
        escRect.anchoredPosition = new Vector2(0, 15);
        escRect.sizeDelta = new Vector2(0, 25);
        
        // Result Text (initially empty)
        GameObject resultObj = CreateText(panelRect, "Result", "", 36, Color.white);
        resultObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform resultRect = resultObj.GetComponent<RectTransform>();
        resultRect.anchorMin = Vector2.zero;
        resultRect.anchorMax = Vector2.one;
        resultRect.offsetMin = Vector2.zero;
        resultRect.offsetMax = Vector2.zero;
        
        // Script ekle ve referansları ata
        RhythmPuzzleUI puzzleScript = rootObj.AddComponent<RhythmPuzzleUI>();
        
        SerializedObject so = new SerializedObject(puzzleScript);
        so.FindProperty("prefabBeatBar").objectReferenceValue = beatBarRect;
        so.FindProperty("prefabBeatIndicator").objectReferenceValue = beatRect;
        so.FindProperty("prefabHitZone").objectReferenceValue = hitZoneRect;
        so.FindProperty("prefabHitZoneImage").objectReferenceValue = hitZone.GetComponent<Image>();
        so.FindProperty("prefabBeatIndicatorImage").objectReferenceValue = beatIndicator.GetComponent<Image>();
        so.FindProperty("prefabScoreText").objectReferenceValue = scoreObj.GetComponent<Text>();
        so.FindProperty("prefabInstructionText").objectReferenceValue = resultObj.GetComponent<Text>();
        so.FindProperty("prefabHitMarkers").arraySize = 5;
        for (int i = 0; i < 5; i++)
        {
            so.FindProperty("prefabHitMarkers").GetArrayElementAtIndex(i).objectReferenceValue = hitMarkers[i];
        }
        so.ApplyModifiedProperties();
        
        // Prefab olarak kaydet (ACTIVE olarak - sahnede görünür olsun)
        rootObj.SetActive(true);
        string path = $"{prefabPath}/RhythmPuzzleUI.prefab";
        PrefabUtility.SaveAsPrefabAsset(rootObj, path);
        DestroyImmediate(rootObj);
        
        Debug.Log("Created: " + path);
    }
    
    [MenuItem("Tools/Puzzle UI/Generate Memory Puzzle")]
    public static void GenerateMemoryPuzzle()
    {
        EnsureFolders();
        
        // Ana container - CANVAS DEĞİL, normal UI Panel
        GameObject rootObj = CreateRootPanel("MemoryPuzzleUI");
        RectTransform rootRect = rootObj.GetComponent<RectTransform>();
        
        // Panel (ortadaki kutu)
        GameObject panelObj = CreateImage(rootRect, "Panel", new Color(0.12f, 0.12f, 0.18f, 0.95f));
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600, 500);
        panelRect.anchoredPosition = Vector2.zero;
        
        // Title
        GameObject titleObj = CreateText(panelRect, "Title", "MEMORY GAME", 36, new Color(1f, 0.8f, 0.2f));
        titleObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -15);
        titleRect.sizeDelta = new Vector2(0, 50);
        
        // Card Container
        GameObject cardContainer = CreateEmpty(panelRect, "CardContainer");
        RectTransform cardContainerRect = cardContainer.GetComponent<RectTransform>();
        cardContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardContainerRect.sizeDelta = new Vector2(500, 250);
        cardContainerRect.anchoredPosition = new Vector2(0, 20);
        
        GridLayoutGroup gridLayout = cardContainer.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(80, 100);
        gridLayout.spacing = new Vector2(15, 15);
        gridLayout.childAlignment = TextAnchor.MiddleCenter;
        
        // Cards (6 tane - 3 çift)
        Image[] cardImages = new Image[6];
        Text[] cardTexts = new Text[6];
        
        for (int i = 0; i < 6; i++)
        {
            GameObject card = CreateImage(cardContainerRect, $"Card_{i}", new Color(0.2f, 0.3f, 0.5f));
            cardImages[i] = card.GetComponent<Image>();
            
            GameObject cardText = CreateText(card.GetComponent<RectTransform>(), "Text", "?", 48, Color.white);
            cardText.GetComponent<Text>().fontStyle = FontStyle.Bold;
            RectTransform textRect = cardText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            cardTexts[i] = cardText.GetComponent<Text>();
        }
        
        // Status Text
        GameObject statusObj = CreateText(panelRect, "Status", "Memorize the cards...", 26, Color.white);
        RectTransform statusRect = statusObj.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 0);
        statusRect.anchorMax = new Vector2(1, 0);
        statusRect.pivot = new Vector2(0.5f, 0);
        statusRect.anchoredPosition = new Vector2(0, 60);
        statusRect.sizeDelta = new Vector2(0, 40);
        
        // Instruction Text
        GameObject instructObj = CreateText(panelRect, "Instruction", "Arrow keys: Select  |  Space: Flip  |  Esc: Cancel", 20, new Color(0.6f, 0.6f, 0.6f));
        RectTransform instructRect = instructObj.GetComponent<RectTransform>();
        instructRect.anchorMin = new Vector2(0, 0);
        instructRect.anchorMax = new Vector2(1, 0);
        instructRect.pivot = new Vector2(0.5f, 0);
        instructRect.anchoredPosition = new Vector2(0, 20);
        instructRect.sizeDelta = new Vector2(0, 35);
        
        // Script ekle ve referansları ata
        MemoryPuzzleUI puzzleScript = rootObj.AddComponent<MemoryPuzzleUI>();
        
        SerializedObject so = new SerializedObject(puzzleScript);
        so.FindProperty("prefabCardImages").arraySize = 6;
        so.FindProperty("prefabCardTexts").arraySize = 6;
        for (int i = 0; i < 6; i++)
        {
            so.FindProperty("prefabCardImages").GetArrayElementAtIndex(i).objectReferenceValue = cardImages[i];
            so.FindProperty("prefabCardTexts").GetArrayElementAtIndex(i).objectReferenceValue = cardTexts[i];
        }
        so.FindProperty("prefabStatusText").objectReferenceValue = statusObj.GetComponent<Text>();
        so.FindProperty("prefabInstructionText").objectReferenceValue = instructObj.GetComponent<Text>();
        so.ApplyModifiedProperties();
        
        // Prefab olarak kaydet (ACTIVE olarak - sahnede görünür olsun)
        rootObj.SetActive(true);
        string path = $"{prefabPath}/MemoryPuzzleUI.prefab";
        PrefabUtility.SaveAsPrefabAsset(rootObj, path);
        DestroyImmediate(rootObj);
        
        Debug.Log("Created: " + path);
    }
    
    // ============= HELPER METHODS =============
    
    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI/Puzzles"))
            AssetDatabase.CreateFolder("Assets/Prefabs/UI", "Puzzles");
    }
    
    /// <summary>
    /// Canvas DEĞİL - normal UI Panel oluşturur.
    /// Bu prefab'ı sahnedeki Canvas'ın altına child olarak eklersiniz.
    /// </summary>
    private static GameObject CreateRootPanel(string name)
    {
        GameObject panelObj = new GameObject(name);
        RectTransform rect = panelObj.AddComponent<RectTransform>();
        
        // Tam ekran stretch ayarı
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        // Yarı saydam siyah background
        Image img = panelObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.85f);
        
        // CanvasGroup ekle (fade in/out için)
        CanvasGroup group = panelObj.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.blocksRaycasts = true;
        
        return panelObj;
    }
    
    private static GameObject CreateEmpty(Transform parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }
    
    private static GameObject CreateImage(Transform parent, string name, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        Image img = obj.AddComponent<Image>();
        img.color = color;
        return obj;
    }
    
    private static GameObject CreateText(Transform parent, string name, string text, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        Text txt = obj.AddComponent<Text>();
        txt.text = text;
        txt.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        txt.fontSize = fontSize;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = color;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        return obj;
    }
}
