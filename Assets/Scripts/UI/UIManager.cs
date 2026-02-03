using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    [Header("Game Over")]
    [SerializeField] private GameObject gameOverScreen;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI enemiesKilledText;
    [SerializeField] private TextMeshProUGUI scoreText;

    [Header("Game Win")]
    [SerializeField] private GameObject victoryScreen;

    [Header("Leaderboard Save")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private GameObject saveScoreButton;
    [SerializeField] private TextMeshProUGUI savedMessageText;

    [Header("Pause")]
    [SerializeField] private GameObject pauseScreen;
    
    [Header("Pause Screen Overlay")]
    [SerializeField] [Range(0f, 1f)] private float pauseDimAmount = 0.7f;  // Karartma miktarı (0-1) - varsayılan %70
    [SerializeField] private Color pauseOverlayColor = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private Color gameOverOverlayColor = new Color(0.02f, 0.01f, 0.03f, 0.92f);  // Çok koyu mor-siyah
    [SerializeField] private bool useBlurEffect = false;  // Blur gri yapıyor, kapalı
    [SerializeField] [Range(1, 10)] private int blurIterations = 3;
    private GameObject pauseOverlay;
    private Image pauseOverlayImage;

    [Header("UI Elements to Hide on Pause")]
    [SerializeField] private GameObject[] hideOnPauseElements;  // Inspector'dan atanabilir
    private GameObject skillInputUI;
    private GameObject soulUI;
    private GameObject timeSlowUI;
    private GameObject coinUI;
    private GameObject skillCooldownUI;
    private GameObject uiHealthBar;

    [Header("Button Animations")]
    [SerializeField] private bool useMinimalistButtons = true;
    [Tooltip("Eğer false ise, eski MenuButtonAnimation kullanılır")]

    private bool isPaused;

    // Singleton instance for easy access
    public static UIManager Instance { get; private set; }
    
    // Public properties for TimeSlowUI and other systems
    public bool IsPaused => isPaused;
    public bool IsGameOver => gameOverScreen != null && gameOverScreen.activeSelf;
    public bool IsVictory => victoryScreen != null && victoryScreen.activeSelf;

    [Header("Shop")]
    public GameObject shopPanel;
    public ShopManager shopManager;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
            
        Time.timeScale = 1f;
        isPaused = false;

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);

        if (victoryScreen != null)
            victoryScreen.SetActive(false);

        if (pauseScreen != null)
            pauseScreen.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Overlay oluştur (tüm menüler için ortak)
        CreateOverlay();
        
        // UI elemanlarını bul
        FindUIElements();
        
        // Tüm ekranlardaki butonlara animasyon ekle
        SetupButtonAnimations();
    }
    
    private void CreateOverlay()
    {
        // Tüm menüler için ortak karartma overlay'i oluştur
        // İlk bulunan canvas'a ekle
        Canvas canvas = null;
        
        if (pauseScreen != null)
            canvas = pauseScreen.GetComponentInParent<Canvas>();
        else if (gameOverScreen != null)
            canvas = gameOverScreen.GetComponentInParent<Canvas>();
        else if (victoryScreen != null)
            canvas = victoryScreen.GetComponentInParent<Canvas>();
            
        if (canvas == null) return;
        
        pauseOverlay = new GameObject("DimOverlay");
        pauseOverlay.transform.SetParent(canvas.transform);
        
        // Overlay'i en arkaya koy (sibling index 0)
        pauseOverlay.transform.SetAsFirstSibling();
        
        RectTransform rt = pauseOverlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        
        // Ana karartma katmanı
        pauseOverlayImage = pauseOverlay.AddComponent<Image>();
        pauseOverlayImage.color = pauseOverlayColor;
        pauseOverlayImage.raycastTarget = false;
        
        // Blur simülasyonu için ek katmanlar (radial gradient efekti)
        if (useBlurEffect)
        {
            CreateBlurLayers(pauseOverlay.transform);
        }
        
        pauseOverlay.SetActive(false);
    }
    
    /// <summary>
    /// Overlay gösterildiğinde ilgili menüyü en öne taşır
    /// </summary>
    private void ShowOverlayForScreen(GameObject screen)
    {
        if (pauseOverlay == null || screen == null) return;
        
        pauseOverlay.SetActive(true);
        
        // Game Over ekranı için farklı renk kullan
        if (pauseOverlayImage != null)
        {
            if (screen == gameOverScreen)
            {
                pauseOverlayImage.color = gameOverOverlayColor;
            }
            else
            {
                pauseOverlayImage.color = pauseOverlayColor;
            }
        }
        
        // Overlay'i aktif ekranın hemen altına taşı
        Transform parent = screen.transform.parent;
        if (parent != null)
        {
            // Önce overlay'i ekranın altına koy
            int screenIndex = screen.transform.GetSiblingIndex();
            pauseOverlay.transform.SetSiblingIndex(screenIndex);
            // Sonra ekranı overlay'in üstüne taşı
            screen.transform.SetSiblingIndex(screenIndex + 1);
        }
    }
    
    private void CreateBlurLayers(Transform parent)
    {
        // Birden fazla yarı-saydam katman ile blur benzeri efekt oluştur
        // Gerçek blur için shader gerekir, bu bir simülasyon
        
        for (int i = 0; i < blurIterations; i++)
        {
            GameObject blurLayer = new GameObject($"BlurLayer_{i}");
            blurLayer.transform.SetParent(parent);
            blurLayer.transform.SetAsFirstSibling();
            
            RectTransform blurRT = blurLayer.AddComponent<RectTransform>();
            blurRT.anchorMin = Vector2.zero;
            blurRT.anchorMax = Vector2.one;
            
            // Her katman biraz daha geniş - blur hissi verir
            float expand = (i + 1) * 2f;
            blurRT.offsetMin = new Vector2(-expand, -expand);
            blurRT.offsetMax = new Vector2(expand, expand);
            blurRT.localScale = Vector3.one;
            
            Image blurImg = blurLayer.AddComponent<Image>();
            // Her katman daha az opak
            float alpha = pauseDimAmount / (blurIterations * 2f);
            blurImg.color = new Color(0f, 0f, 0f, alpha);
            blurImg.raycastTarget = false;
        }
        
        // Vignette efekti - kenarlar daha koyu
        GameObject vignette = new GameObject("Vignette");
        vignette.transform.SetParent(parent);
        vignette.transform.SetAsLastSibling();
        
        RectTransform vigRT = vignette.AddComponent<RectTransform>();
        vigRT.anchorMin = Vector2.zero;
        vigRT.anchorMax = Vector2.one;
        vigRT.offsetMin = Vector2.zero;
        vigRT.offsetMax = Vector2.zero;
        vigRT.localScale = Vector3.one;
        
        Image vigImg = vignette.AddComponent<Image>();
        vigImg.sprite = CreateVignetteSprite();
        vigImg.color = new Color(0f, 0f, 0f, 0.4f);
        vigImg.raycastTarget = false;
    }
    
    /// <summary>
    /// Score text'in altına ve üstüne estetik çizgiler ekler
    /// </summary>
    private void AddScoreDecorationLines(TextMeshProUGUI scoreTextElement)
    {
        if (scoreTextElement == null) return;
        
        Transform parent = scoreTextElement.transform.parent;
        if (parent == null) parent = scoreTextElement.transform;
        
        // Çizgi rengi - lava orange tema
        Color lineColor = new Color(1f, 0.45f, 0.1f, 0.9f);
        float lineWidth = 200f;
        float lineHeight = 2f;
        float lineOffset = 25f;  // Text'ten uzaklık
        
        // Üst çizgi
        string topLineName = "ScoreTopLine";
        Transform existingTopLine = scoreTextElement.transform.Find(topLineName);
        if (existingTopLine == null)
        {
            GameObject topLine = CreateDecorationLine(topLineName, scoreTextElement.transform, lineColor, lineWidth, lineHeight);
            RectTransform topRT = topLine.GetComponent<RectTransform>();
            topRT.anchoredPosition = new Vector2(0, lineOffset);
        }
        
        // Alt çizgi
        string bottomLineName = "ScoreBottomLine";
        Transform existingBottomLine = scoreTextElement.transform.Find(bottomLineName);
        if (existingBottomLine == null)
        {
            GameObject bottomLine = CreateDecorationLine(bottomLineName, scoreTextElement.transform, lineColor, lineWidth, lineHeight);
            RectTransform bottomRT = bottomLine.GetComponent<RectTransform>();
            bottomRT.anchoredPosition = new Vector2(0, -lineOffset);
        }
    }
    
    private GameObject CreateDecorationLine(string name, Transform parent, Color color, float width, float height)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(parent);
        line.transform.localScale = Vector3.one;
        
        RectTransform rt = line.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        
        Image img = line.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        
        // Gradient efekti için CanvasGroup ekle (fade kenarlar)
        // Basit bir gradient sprite oluştur
        img.sprite = CreateGradientLineSprite();
        
        return line;
    }
    
    private Sprite CreateGradientLineSprite()
    {
        int width = 128;
        int height = 4;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Kenarlardan merkeze doğru fade
                float normalizedX = (float)x / width;
                float alpha = 1f - Mathf.Abs(normalizedX - 0.5f) * 2f;
                alpha = Mathf.Pow(alpha, 0.5f);  // Daha yumuşak geçiş
                
                pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
    
    private Sprite CreateVignetteSprite()
    {
        int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxRadius = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float normalizedDistance = distance / maxRadius;
                
                // Kenarlar daha koyu, merkez daha açık
                float alpha = Mathf.Pow(normalizedDistance, 1.5f);
                alpha = Mathf.Clamp01(alpha);
                
                pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
            }
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
    
    private void FindUIElements()
    {
        // Sahnedeki UI elemanlarını bul
        var soulUIComponent = FindFirstObjectByType<SoulUI>();
        if (soulUIComponent != null)
            soulUI = soulUIComponent.gameObject;
            
        var timeSlowUIComponent = FindFirstObjectByType<TimeSlowUI>();
        if (timeSlowUIComponent != null)
            timeSlowUI = timeSlowUIComponent.gameObject;
            
        // Coin UI
        var coinUIComponent = FindFirstObjectByType<CoinUI>();
        if (coinUIComponent != null)
            coinUI = coinUIComponent.gameObject;
            
        // Skill Cooldown UI
        var skillCooldownUIComponent = FindFirstObjectByType<SkillCooldownUI>();
        if (skillCooldownUIComponent != null)
            skillCooldownUI = skillCooldownUIComponent.gameObject;
        
        // UI Health Bar
        var uiHealthBarComponent = FindFirstObjectByType<UIHealthBar>();
        if (uiHealthBarComponent != null)
            uiHealthBar = uiHealthBarComponent.gameObject;
            
        // GuitarSkillSystem'daki UI
        var guitarSkill = FindFirstObjectByType<GuitarSkillSystem>();
        if (guitarSkill != null)
        {
            // SkillInputUI child olarak olabilir
            Transform skillUI = guitarSkill.transform.Find("SkillInputUI");
            if (skillUI != null)
                skillInputUI = skillUI.gameObject;
        }
    }
    
    /// <summary>
    /// Game Over, Victory ve Pause ekranlarındaki butonlara minimalist animasyon ekler
    /// </summary>
    private void SetupButtonAnimations()
    {
        // Game Over Screen butonları
        if (gameOverScreen != null)
        {
            AddAnimationsToButtons(gameOverScreen);
        }
        
        // Victory Screen butonları
        if (victoryScreen != null)
        {
            AddAnimationsToButtons(victoryScreen);
        }
        
        // Pause Screen butonları
        if (pauseScreen != null)
        {
            AddAnimationsToButtons(pauseScreen);
        }
        
        // Shop Panel butonları
        if (shopPanel != null)
        {
            AddAnimationsToButtons(shopPanel);
        }
    }
    
    private void AddAnimationsToButtons(GameObject screen)
    {
        Button[] buttons = screen.GetComponentsInChildren<Button>(true);
        
        foreach (Button btn in buttons)
        {
            // Zaten animasyon varsa ekleme
            if (btn.GetComponent<MinimalistButtonAnimation>() != null)
                continue;
            if (btn.GetComponent<MenuButtonAnimation>() != null)
                continue;
                
            if (useMinimalistButtons)
            {
                btn.gameObject.AddComponent<MinimalistButtonAnimation>();
            }
            else
            {
                btn.gameObject.AddComponent<MenuButtonAnimation>();
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Shop açıksa önce shop'u kapat
            if (shopPanel != null && shopPanel.activeSelf)
            {
                CloseShopPanel();
                return;
            }
            
            // Değilse pause toggle
            TogglePause();
        }
    }

    public void GameWin()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.StopTracking();
            UpdateGameOverStats();
        }

        // Karartma overlay'i göster ve victory screen'i öne taşı
        if (victoryScreen != null)
        {
            victoryScreen.SetActive(true);
            ShowOverlayForScreen(victoryScreen);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Oyun UI elemanlarını gizle
        SetGameUIVisibility(false);

        Time.timeScale = 0f;
    }

    public void GameOver()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.StopTracking();
            UpdateGameOverStats();
        }

        // Karartma overlay'i göster ve game over screen'i öne taşı
        if (gameOverScreen != null)
        {
            // GameOverScreen'in kendi Image'ını şeffaf yap (gri görüntüyü kaldır)
            Image gameOverBg = gameOverScreen.GetComponent<Image>();
            if (gameOverBg != null)
            {
                gameOverBg.color = new Color(0f, 0f, 0f, 0f);  // Tamamen şeffaf
            }
            
            gameOverScreen.SetActive(true);
            ShowOverlayForScreen(gameOverScreen);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Oyun UI elemanlarını gizle
        SetGameUIVisibility(false);

        Time.timeScale = 0f;
    }

    private void UpdateGameOverStats()
    {
        if (ScoreManager.Instance == null)
            return;

        if (distanceText != null)
            distanceText.text = $"Distance: {ScoreManager.Instance.DistanceTraveled:F1} m";

        if (enemiesKilledText != null)
            enemiesKilledText.text = $"Enemies Killed: {ScoreManager.Instance.EnemiesKilled}";

        if (scoreText != null)
        {
            scoreText.text = $"Score: {ScoreManager.Instance.TotalScore}";
            
            // Score text için estetik çizgiler ekle
            AddScoreDecorationLines(scoreText);
        }

        if (saveScoreButton != null)
            saveScoreButton.SetActive(true);

        if (savedMessageText != null)
            savedMessageText.gameObject.SetActive(false);

        if (playerNameInput != null)
        {
            playerNameInput.text = "";
            playerNameInput.interactable = true;
        }
    }

    public void SaveScoreToLeaderboard()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogError("ScoreManager bulunamadı!");
            return;
        }

        string playerName = "Player";
        if (playerNameInput != null && !string.IsNullOrEmpty(playerNameInput.text))
        {
            playerName = playerNameInput.text;
            Debug.Log($"İsim girildi: {playerName}");
        }
        else
        {
            Debug.LogWarning("PlayerNameInput bağlı değil veya boş!");
        }

        ScoreManager.Instance.SaveToLeaderboard(playerName);
        Debug.Log($"Skor kaydedildi: {playerName} - {ScoreManager.Instance.TotalScore}");

        if (saveScoreButton != null)
            saveScoreButton.SetActive(false);
        else
            Debug.LogWarning("SaveScoreButton bağlı değil!");

        if (playerNameInput != null)
            playerNameInput.interactable = false;

        if (savedMessageText != null)
        {
            savedMessageText.text = "Score Saved!";
            savedMessageText.gameObject.SetActive(true);
            Debug.Log("Score Saved mesajı gösterildi");
        }
        else
        {
            Debug.LogWarning("SavedMessageText bağlı değil!");
        }
    }

    public void TogglePause()
    {
        if ((gameOverScreen != null && gameOverScreen.activeSelf) ||
            (victoryScreen != null && victoryScreen.activeSelf))
            return;

        if (pauseScreen == null)
            return;

        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        if (pauseScreen == null)
            return;

        isPaused = true;
        
        // Pause screen'i aktif et ve overlay'i göster
        pauseScreen.SetActive(true);
        ShowOverlayForScreen(pauseScreen);
        
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Oyun UI elemanlarını gizle
        SetGameUIVisibility(false);
    }

    public void Resume()
    {
        isPaused = false;
        
        // Karartma overlay'i gizle
        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
            
        if (pauseScreen != null)
            pauseScreen.SetActive(false);

        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Oyun UI elemanlarını tekrar göster
        SetGameUIVisibility(true);
    }
    
    /// <summary>
    /// Pause sırasında gizlenmesi gereken UI elemanlarının görünürlüğünü ayarlar
    /// </summary>
    private void SetGameUIVisibility(bool visible)
    {
        // Soul UI
        if (soulUI != null)
            soulUI.SetActive(visible);
            
        // Time Slow UI
        if (timeSlowUI != null)
            timeSlowUI.SetActive(visible);
            
        // Coin UI
        if (coinUI != null)
            coinUI.SetActive(visible);
            
        // Skill Cooldown UI
        if (skillCooldownUI != null)
            skillCooldownUI.SetActive(visible);
            
        // Skill Input UI
        if (skillInputUI != null)
            skillInputUI.SetActive(visible);
        
        // UI Health Bar
        if (uiHealthBar != null)
            uiHealthBar.SetActive(visible);
        
        // Mini-Map (MiniMap2D kendi kontrolünü yapıyor ama yine de burada da çağıralım)
        if (MiniMap2D.Instance != null)
        {
            if (visible)
                MiniMap2D.Instance.Show();
            else
                MiniMap2D.Instance.Hide();
        }
            
        // Inspector'dan atanan diğer elemanlar
        if (hideOnPauseElements != null)
        {
            foreach (var element in hideOnPauseElements)
            {
                if (element != null)
                    element.SetActive(visible);
            }
        }
    }

    public void ContinueFromCheckpoint()
    {
        isPaused = false;
        Time.timeScale = 1f;
        
        // Restart öncesi cleanup
        CleanupBeforeRestart();
        
        // Damage vignette sıfırla
        if (DamageVignette.Instance != null)
            DamageVignette.Instance.ResetVignette();
        
        // Checkpoint'ten devam - ability'ler sıfırlanmaz, checkpoint'teki haliyle yüklenir
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        var player = FindFirstObjectByType<PlayerMovement>();
        if (player != null) player.lockMovement = false;
    }

    public void RestartFromBeginning()
    {
        isPaused = false;
        Time.timeScale = 1f;
        CheckpointData.ResetData(); // Checkpoint VE düşman ölümlerini sıfırlar
        ResetAbilitiesForRestart();
        
        // Restart öncesi cleanup
        CleanupBeforeRestart();
        
        // Damage vignette sıfırla
        if (DamageVignette.Instance != null)
            DamageVignette.Instance.ResetVignette();
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        var player = FindFirstObjectByType<PlayerMovement>();
        if (player != null) player.lockMovement = false;
    }

    public void Restart()
    {
        isPaused = false;
        Time.timeScale = 1f;
        ResetAbilitiesForRestart();
        
        // Restart öncesi cleanup
        CleanupBeforeRestart();
        
        // Damage vignette sıfırla
        if (DamageVignette.Instance != null)
            DamageVignette.Instance.ResetVignette();
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        var player = FindFirstObjectByType<PlayerMovement>();
        if (player != null) player.lockMovement = false;
    }
    
    /// <summary>
    /// Restart yapıldığında ability'leri başlangıç değerlerine sıfırla
    /// </summary>
    /// <summary>
    /// Restart öncesi tüm sistemleri temizle ve kamerayı sıfırla
    /// Bu metod ekran bölünmesi ve görsel bugları önler
    /// </summary>
    private void CleanupBeforeRestart()
    {
        // 1. Kamera rotasyonunu sıfırla (transition'dan kalmış olabilir)
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.rotation = Quaternion.identity;
            // Kamera size'ı da varsayılana döndür
            if (mainCam.orthographic)
            {
                mainCam.orthographicSize = 7f; // Varsayılan değer
            }
        }
        
        // 2. SceneTransitionController blackout'u temizle
        if (SceneTransitionController.Instance != null)
        {
            SceneTransitionController.Instance.ClearBlackout();
        }
        
        // 3. ScreenEffects sıfırla
        if (ScreenEffects.Instance != null)
        {
            ScreenEffects.Instance.UpdateHealthVignette(1f);
        }
        
        // 4. Time slow'u kapat (aktifse)
        if (TimeSlowAbility.Instance != null && TimeSlowAbility.Instance.IsSlowMotionActive)
        {
            TimeSlowAbility.Instance.ForceStopSlowMotion();
        }
        
        // 5. Cursor'ı gizle (menüden çıkış)
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // 6. Overlay'leri kapat
        if (pauseOverlay != null)
            pauseOverlay.SetActive(false);
        
        // 7. Level rotasyonunu sıfırla (Level 2 restart için)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.lastTransformRotationValue = 0f;
        }
            
        Debug.Log("[UIManager] Restart cleanup tamamlandı");
    }
    
    private void ResetAbilitiesForRestart()
    {
        // Sunum için yüksek başlangıç değerleri
        PlayerPrefs.SetInt("HEAL", 5);      // Başlangıçta 5 Heal
        PlayerPrefs.SetInt("FIREBALL", 4);  // Başlangıçta 4 Fireball
        
        // Stat upgrade'leri de sıfırla
        PlayerPrefs.SetInt("MAX_HEALTH", 0);
        PlayerPrefs.SetInt("DAMAGE", 0);
        PlayerPrefs.SetInt("JUMP", 0);
        PlayerPrefs.SetInt("SPEED", 0);
        PlayerPrefs.SetInt("REVIVE", 0);
        
        PlayerPrefs.Save();
        Debug.Log("[UIManager] Restart - Değerler sıfırlandı: Heal=5, Fireball=4, Coin=100");
        
        // Soul (kill sayacı) sıfırla
        if (SoulSystem.Instance != null)
        {
            SoulSystem.Instance.ResetKills();
        }
        
        // Coin sıfırla (başlangıç değerine - 100)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResetCoin();
        }
        
        // GuitarSkillSystem'ı da güncelle
        if (GuitarSkillSystem.Instance != null)
        {
            GuitarSkillSystem.Instance.healCharges = 5;
            GuitarSkillSystem.Instance.fireballCharges = 4;
        }
    }

    public void MainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void OpenShopPanel()
    {
        if (shopPanel == null)
            return;

        shopPanel.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Oyunu duraklat (opsiyonel - fireball'lar da durur)
        Time.timeScale = 0f;
        isPaused = true;
        
        // Shop açıldığında tüm bileşenleri yenile - panel aktif olduktan SONRA
        if (shopManager != null)
        {
            shopManager.RefreshAllComponents();
        }
        
        // Shop butonlarını ShopManager'a bağla
        SetupShopButtons();
        
        // Shop panelindeki coin text'i direkt güncelle
        UpdateShopCoinText();
    }
    
    /// <summary>
    /// Shop panelindeki butonları ShopManager fonksiyonlarına bağla
    /// </summary>
    private void SetupShopButtons()
    {
        if (shopPanel == null) return;
        
        // ShopManager'ı bul
        ShopManager shop = shopManager;
        if (shop == null)
            shop = ShopManager.Instance;
        if (shop == null)
            shop = FindFirstObjectByType<ShopManager>();
            
        if (shop == null)
        {
            Debug.LogWarning("[UIManager] ShopManager bulunamadı! Shop butonları çalışmayacak.");
            return;
        }
        
        // Shop panelindeki tüm butonları bul
        Button[] buttons = shopPanel.GetComponentsInChildren<Button>(true);
        
        foreach (Button btn in buttons)
        {
            string btnName = btn.name.ToUpper();
            Debug.Log($"[UIManager] Buton bulundu: {btn.name} -> {btnName}");
            
            // Zaten listener varsa temizle ve yeniden ekle
            btn.onClick.RemoveAllListeners();
            
            // Buton ismine göre fonksiyon bağla
            if (btnName.Contains("HEALTH") || btnName.Contains("HP") || btnName.Contains("CAN"))
            {
                btn.onClick.AddListener(() => {
                    shop.AddMaxHealth();
                    UpdateShopCoinText();
                    Debug.Log("[Shop] Health satın alındı!");
                });
            }
            else if (btnName.Contains("DAMAGE") || btnName.Contains("HASAR") || btnName.Contains("ATTACK"))
            {
                btn.onClick.AddListener(() => {
                    shop.AddDamage();
                    UpdateShopCoinText();
                    Debug.Log("[Shop] Damage satın alındı!");
                });
            }
            else if (btnName.Contains("JUMP") || btnName.Contains("ZIPLA"))
            {
                btn.onClick.AddListener(() => {
                    shop.AddJumpForce();
                    UpdateShopCoinText();
                    Debug.Log("[Shop] Jump satın alındı!");
                });
            }
            else if (btnName.Contains("SPEED") || btnName.Contains("HIZ"))
            {
                btn.onClick.AddListener(() => {
                    shop.AddSpeed();
                    UpdateShopCoinText();
                    Debug.Log("[Shop] Speed satın alındı!");
                });
            }
            else if (btnName.Contains("REVIVE") || btnName.Contains("DIRIL") || btnName.Contains("CANLAN"))
            {
                btn.onClick.AddListener(() => {
                    shop.AddRevive();
                    UpdateShopCoinText();
                    Debug.Log("[Shop] Revive satın alındı!");
                });
            }
            else if (btnName.Contains("HEAL") || btnName.Contains("IYILE") || btnName.Contains("SAGLIK"))
            {
                btn.onClick.AddListener(() => {
                    shop.AddHealSkill();
                    UpdateShopCoinText();
                    Debug.Log("[Shop] Heal skill satın alındı!");
                });
                Debug.Log($"[UIManager] ✅ Heal butonu bağlandı: {btn.name}");
            }
            else if (btnName.Contains("FIREBALL") || btnName.Contains("FIRE") || btnName.Contains("ATES") || btnName.Contains("ALEV"))
            {
                btn.onClick.AddListener(() => {
                    shop.AddFireballSkill();
                    UpdateShopCoinText();
                    Debug.Log("[Shop] Fireball skill satın alındı!");
                });
                Debug.Log($"[UIManager] ✅ Fireball butonu bağlandı: {btn.name}");
            }
            else if (btnName.Contains("CLOSE") || btnName.Contains("KAPAT") || btnName.Contains("EXIT") || btnName.Contains("BACK"))
            {
                btn.onClick.AddListener(() => {
                    CloseShopPanel();
                });
            }
        }
        
        // Items container'ındaki butonları sırayla bağla (isim eşleşmezse)
        SetupItemsButtonsByOrder(shop);
        
        // Fiyatları güncelle
        UpdateShopPrices();
        
        Debug.Log($"[UIManager] Shop butonları bağlandı. Toplam: {buttons.Length}");
    }
    
    /// <summary>
    /// Shop UI'daki fiyat text'lerini güncelle
    /// </summary>
    private void UpdateShopPrices()
    {
        if (shopPanel == null) return;
        
        // Items container'ını bul (recursive)
        Transform itemsContainer = FindChildRecursive(shopPanel.transform, "Items");
        if (itemsContainer == null)
        {
            Debug.LogWarning("[UIManager] Items container bulunamadı!");
            // Alternatif: ShopPanel altındaki tüm butonları tara
            UpdateShopPricesAlternative();
            return;
        }
        
        // Fiyat listesi (sırayla: Health, Damage, Jump, Speed, Revive, Heal, Fireball)
        int[] prices = { 75, 75, 75, 75, 100, 40, 40 };
        string[] names = { "HEALTH", "DAMAGE", "JUMP", "SPEED", "REVIVE", "HEAL", "FIREBALL" };
        
        int buttonIndex = 0;
        foreach (Transform child in itemsContainer)
        {
            if (buttonIndex >= prices.Length) break;
            
            // Bu butonun altındaki tüm text'leri bul
            TMPro.TMP_Text[] texts = child.GetComponentsInChildren<TMPro.TMP_Text>(true);
            foreach (var text in texts)
            {
                string upperText = text.text.ToUpper();
                
                // "COIN" içeren text'i bul ve güncelle
                if (upperText.Contains("COIN"))
                {
                    text.text = "COIN:" + prices[buttonIndex];
                    Debug.Log($"[UIManager] {names[buttonIndex]} fiyatı güncellendi: {prices[buttonIndex]}");
                }
            }
            
            buttonIndex++;
        }
    }
    
    /// <summary>
    /// Alternatif fiyat güncelleme - Items container bulunamazsa tüm COIN text'lerini güncelle
    /// </summary>
    private void UpdateShopPricesAlternative()
    {
        if (shopPanel == null) return;
        
        // Fiyat eşleştirme (isim bazlı)
        var priceMap = new System.Collections.Generic.Dictionary<string, int>
        {
            { "HEALTH", 75 },
            { "DAMAGE", 75 },
            { "JUMP", 75 },
            { "SPEED", 75 },
            { "REVIVE", 100 },
            { "HEAL", 40 },
            { "FIREBALL", 40 }
        };
        
        // Tüm butonları tara
        Button[] allButtons = shopPanel.GetComponentsInChildren<Button>(true);
        foreach (var btn in allButtons)
        {
            string btnName = btn.name.ToUpper();
            
            // Hangi ürüne ait olduğunu bul
            int price = -1;
            foreach (var kvp in priceMap)
            {
                if (btnName.Contains(kvp.Key))
                {
                    price = kvp.Value;
                    break;
                }
            }
            
            if (price > 0)
            {
                // Bu butonun altındaki COIN text'ini güncelle
                TMPro.TMP_Text[] texts = btn.GetComponentsInChildren<TMPro.TMP_Text>(true);
                foreach (var text in texts)
                {
                    if (text.text.ToUpper().Contains("COIN"))
                    {
                        text.text = "COIN:" + price;
                        Debug.Log($"[UIManager] {btn.name} fiyatı güncellendi: {price}");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Items container'ındaki butonları sıralarına göre bağla
    /// Sıra: 0-Health, 1-Damage, 2-Jump, 3-Speed, 4-Revive, 5-Heal, 6-Fireball
    /// </summary>
    private void SetupItemsButtonsByOrder(ShopManager shop)
    {
        if (shopPanel == null || shop == null) return;
        
        // Items container'ını bul (recursive)
        Transform itemsContainer = FindChildRecursive(shopPanel.transform, "Items");
        if (itemsContainer == null)
        {
            Debug.Log("[UIManager] Items container bulunamadı, sıralı bağlama atlanıyor.");
            return;
        }
        
        // Items altındaki butonları al
        List<Button> itemButtons = new List<Button>();
        foreach (Transform child in itemsContainer)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
                itemButtons.Add(btn);
        }
        
        Debug.Log($"[UIManager] Items altında {itemButtons.Count} buton bulundu.");
        
        // Sırayla bağla
        for (int i = 0; i < itemButtons.Count; i++)
        {
            Button btn = itemButtons[i];
            int index = i; // Closure için kopyala
            
            // Eğer zaten listener varsa atla (isim eşleşmesiyle bağlanmış olabilir)
            // Ama güvenlik için yine de ekleyelim
            
            switch (i)
            {
                case 0: // Health
                    if (!HasListener(btn, "Health"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => { shop.AddMaxHealth(); UpdateShopCoinText(); });
                        Debug.Log($"[UIManager] Sıra {i}: Health bağlandı - {btn.name}");
                    }
                    break;
                case 1: // Damage
                    if (!HasListener(btn, "Damage"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => { shop.AddDamage(); UpdateShopCoinText(); });
                        Debug.Log($"[UIManager] Sıra {i}: Damage bağlandı - {btn.name}");
                    }
                    break;
                case 2: // Jump
                    if (!HasListener(btn, "Jump"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => { shop.AddJumpForce(); UpdateShopCoinText(); });
                        Debug.Log($"[UIManager] Sıra {i}: Jump bağlandı - {btn.name}");
                    }
                    break;
                case 3: // Speed
                    if (!HasListener(btn, "Speed"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => { shop.AddSpeed(); UpdateShopCoinText(); });
                        Debug.Log($"[UIManager] Sıra {i}: Speed bağlandı - {btn.name}");
                    }
                    break;
                case 4: // Revive
                    if (!HasListener(btn, "Revive"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => { shop.AddRevive(); UpdateShopCoinText(); });
                        Debug.Log($"[UIManager] Sıra {i}: Revive bağlandı - {btn.name}");
                    }
                    break;
                case 5: // Heal
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { shop.AddHealSkill(); UpdateShopCoinText(); });
                    Debug.Log($"[UIManager] Sıra {i}: Heal bağlandı - {btn.name}");
                    break;
                case 6: // Fireball
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => { shop.AddFireballSkill(); UpdateShopCoinText(); });
                    Debug.Log($"[UIManager] Sıra {i}: Fireball bağlandı - {btn.name}");
                    break;
            }
        }
    }
    
    private bool HasListener(Button btn, string type)
    {
        // Basit kontrol - isim eşleşmişse true döndür
        return btn.name.ToUpper().Contains(type.ToUpper());
    }
    
    /// <summary>
    /// Shop panelindeki coin text'ini direkt güncelle
    /// </summary>
    public void UpdateShopCoinText()
    {
        if (shopPanel == null) return;
        
        int coinAmount = 0;
        if (GameManager.Instance != null)
        {
            coinAmount = GameManager.Instance.coin;
        }
        
        // Recursive olarak "CoinValue" isimli objeyi bul
        Transform coinValueTransform = FindChildRecursive(shopPanel.transform, "CoinValue");
        if (coinValueTransform != null)
        {
            var coinText = coinValueTransform.GetComponent<TMPro.TMP_Text>();
            if (coinText != null)
            {
                coinText.text = "Coin: " + coinAmount;
                Debug.Log($"[UIManager] CoinValue güncellendi: {coinAmount}");
                
                // Ana ekran CoinUI'ı da güncelle
                var coinUI = FindFirstObjectByType<CoinUI>();
                if (coinUI != null)
                {
                    coinUI.ForceUpdateDisplay();
                }
                return;
            }
        }
        
        // Bulunamadıysa tüm text'leri tara
        TMPro.TMP_Text[] allTexts = shopPanel.GetComponentsInChildren<TMPro.TMP_Text>(true);
        foreach (var text in allTexts)
        {
            string upperName = text.name.ToUpper();
            
            // İsmi "COINVALUE" veya "COIN" içeriyorsa
            if (upperName.Contains("COINVALUE") || upperName == "COINVALUE" || upperName == "COIN_VALUE")
            {
                text.text = "Coin: " + coinAmount;
                Debug.Log($"[UIManager] Shop coin text ({text.name}) güncellendi: {coinAmount}");
                break;
            }
        }
        
        // Ana ekran CoinUI'ı da güncelle
        var coinUI2 = FindFirstObjectByType<CoinUI>();
        if (coinUI2 != null)
        {
            coinUI2.ForceUpdateDisplay();
        }
    }
    
    /// <summary>
    /// Transform içinde recursive olarak isimle child ara
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    public void CloseShopPanel()
    {
        if (shopPanel == null)
            return;

        shopPanel.SetActive(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Oyunu devam ettir
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}