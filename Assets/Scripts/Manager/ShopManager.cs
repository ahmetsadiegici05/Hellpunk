using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

    // -------- PLAYER PREFS KEYS --------
    public const string MAX_HEALTH = "MAX_HEALTH";
    public const string DAMAGE = "DAMAGE";
    public const string JUMP = "JUMP";
    public const string SPEED = "SPEED";
    public const string REVIVE = "REVIVE";
    public const string HEAL = "HEAL";
    public const string FIREBALL = "FIREBALL";
    private const string FIRST_LAUNCH = "FIRST_LAUNCH";

    // Base değerler (başlangıç değerleri)
    [Header("Base Values")]
    public float baseMaxHealth = 100f;
    public float baseDamage = 10f;
    public float baseJumpPower = 5f;
    public float baseSpeed = 5f;
    public int baseReviveCount = 0;

    [Header("Player Components")]
    public Health playerHealth;
    public PlayerAttack playerAttack;
    public PlayerMovement playerMovement;

    [Header("UI Components")]
    public TMP_Text coinText;
    public TMP_Text coinTextGame;
    public TMP_Text healChargesText;
    public TMP_Text fireballChargesText;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Scene change listener
        SceneManager.sceneLoaded += OnSceneLoaded;
        
        // Initialize shop
        InitializeFirstLaunch();
        RefreshAllComponents();
    }

    private void OnEnable()
    {
        RefreshAllComponents();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Yeni sahne yüklendiğinde çağrılır
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshAllComponents();
    }

    // İlk oyun başlatıldığında PlayerPrefs'i başlat
    void InitializeFirstLaunch()
    {
        if (!PlayerPrefs.HasKey(FIRST_LAUNCH))
        {
            PlayerPrefs.SetInt(MAX_HEALTH, 0);
            PlayerPrefs.SetInt(DAMAGE, 0);
            PlayerPrefs.SetInt(JUMP, 0);
            PlayerPrefs.SetInt(SPEED, 0);
            PlayerPrefs.SetInt(REVIVE, 0);
            PlayerPrefs.SetInt(HEAL, 0);
            PlayerPrefs.SetInt(FIREBALL, 0);
            PlayerPrefs.SetInt(FIRST_LAUNCH, 1);
            PlayerPrefs.Save();
            
            Debug.Log("PlayerPrefs initialized for first launch");
        }
    }

    // Tüm bileşenleri yenile
    public void RefreshAllComponents()
    {
        FindPlayerComponents();
        FindUIComponents();
        LoadUpgrades();
        UpdateCoinText();
        
        DebugPlayerPrefs(); // Debug için
    }

    void FindPlayerComponents()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("Player not found in scene!");
            return;
        }

        playerHealth = player.GetComponent<Health>();
        playerAttack = player.GetComponent<PlayerAttack>();
        playerMovement = player.GetComponent<PlayerMovement>();

        if (playerHealth == null) Debug.LogWarning("Health component not found!");
        if (playerAttack == null) Debug.LogWarning("PlayerAttack component not found!");
        if (playerMovement == null) Debug.LogWarning("PlayerMovement component not found!");
    }

    void FindUIComponents()
    {
        // Coin UI'larını bul
        GameObject[] coinUIs = GameObject.FindGameObjectsWithTag("CoinUI");
        foreach (GameObject obj in coinUIs)
        {
            TMP_Text text = obj.GetComponent<TMP_Text>();
            if (text != null)
            {
                if (obj.name.Contains("Shop") || obj.name.Contains("Menu"))
                    coinText = text;
                else if (obj.name.Contains("Game") || obj.name.Contains("HUD"))
                    coinTextGame = text;
            }
        }

        // Skill UI'larını bul
        GameObject healUI = GameObject.Find("HealChargesText");
        if (healUI != null)
            healChargesText = healUI.GetComponent<TMP_Text>();

        GameObject fireballUI = GameObject.Find("FireballChargesText");
        if (fireballUI != null)
            fireballChargesText = fireballUI.GetComponent<TMP_Text>();
    }

    // -------- LOAD --------
    void LoadUpgrades()
    {
        // Health yükseltmelerini yükle
        if (playerHealth != null)
        {
            int maxHealthUpgrades = PlayerPrefs.GetInt(MAX_HEALTH, 0);
            int reviveUpgrades = PlayerPrefs.GetInt(REVIVE, 0);
            
            playerHealth.maxHealth = baseMaxHealth + maxHealthUpgrades;
            playerHealth.reviveCount = baseReviveCount + reviveUpgrades;
            
            // Sağlığı tam doldur
            playerHealth.currentHealth = playerHealth.maxHealth;
            
            Debug.Log($"Health loaded: Max={playerHealth.maxHealth}, Revives={playerHealth.reviveCount}");
        }

        // Damage yükseltmelerini yükle
        if (playerAttack != null)
        {
            int damageUpgrades = PlayerPrefs.GetInt(DAMAGE, 0);
            playerAttack.damage = baseDamage + damageUpgrades;
            Debug.Log($"Damage loaded: {playerAttack.damage}");
        }

        // Movement yükseltmelerini yükle
        if (playerMovement != null)
        {
            int jumpUpgrades = PlayerPrefs.GetInt(JUMP, 0);
            int speedUpgrades = PlayerPrefs.GetInt(SPEED, 0);
            
            playerMovement.jumpPower = baseJumpPower + (jumpUpgrades * 0.5f);
            playerMovement.speed = baseSpeed + (speedUpgrades * 0.3f);
            
            Debug.Log($"Movement loaded: Jump={playerMovement.jumpPower}, Speed={playerMovement.speed}");
        }

        // GuitarSkillSystem yükseltmelerini yükle
        LoadGuitarSkills();
    }

    void LoadGuitarSkills()
    {
        // GuitarSkillSystem'i bul
        GuitarSkillSystem guitarSkillSystem = FindObjectOfType<GuitarSkillSystem>();
        if (guitarSkillSystem == null)
        {
            // Tag ile dene
            GameObject guitarObj = GameObject.FindGameObjectWithTag("GuitarSkillSystem");
            if (guitarObj != null)
                guitarSkillSystem = guitarObj.GetComponent<GuitarSkillSystem>();
        }

        if (guitarSkillSystem != null)
        {
            int healCharges = PlayerPrefs.GetInt(HEAL, 0);
            int fireballCharges = PlayerPrefs.GetInt(FIREBALL, 0);
            
            guitarSkillSystem.healCharges = healCharges;
            guitarSkillSystem.fireballCharges = fireballCharges;
            
            Debug.Log($"Guitar skills loaded: Heal={healCharges}, Fireball={fireballCharges}");
            
            // UI güncelle
            UpdateSkillUI();
        }
        else
        {
            Debug.LogWarning("GuitarSkillSystem not found in scene!");
        }
    }

    void UpdateSkillUI()
    {
        if (healChargesText != null)
            healChargesText.text = PlayerPrefs.GetInt(HEAL, 0).ToString();
        
        if (fireballChargesText != null)
            fireballChargesText.text = PlayerPrefs.GetInt(FIREBALL, 0).ToString();
    }

    // -------- SHOP ACTIONS --------
    public void AddMaxHealth()
    {
        if (GameManager.Instance == null || GameManager.Instance.coin < 10) 
        {
            Debug.Log("Not enough coins or GameManager not found!");
            return;
        }

        PlayerPrefs.SetInt(MAX_HEALTH, PlayerPrefs.GetInt(MAX_HEALTH, 0) + 1);
        PlayerPrefs.Save();

        if (playerHealth != null)
        {
            playerHealth.maxHealth += 1;
            playerHealth.currentHealth += 1;
        }
        
        GameManager.Instance.coin -= 10;
        UpdateCoinText();
        Debug.Log("Max Health upgraded!");
    }

    public void AddDamage()
    {
        if (GameManager.Instance == null || GameManager.Instance.coin < 10) 
        {
            Debug.Log("Not enough coins or GameManager not found!");
            return;
        }

        PlayerPrefs.SetInt(DAMAGE, PlayerPrefs.GetInt(DAMAGE, 0) + 1);
        PlayerPrefs.Save();

        if (playerAttack != null)
            playerAttack.damage += 1;
        
        GameManager.Instance.coin -= 10;
        UpdateCoinText();
        Debug.Log("Damage upgraded!");
    }

    public void AddJumpForce()
    {
        if (GameManager.Instance == null || GameManager.Instance.coin < 10) 
        {
            Debug.Log("Not enough coins or GameManager not found!");
            return;
        }

        PlayerPrefs.SetInt(JUMP, PlayerPrefs.GetInt(JUMP, 0) + 1);
        PlayerPrefs.Save();

        if (playerMovement != null)
            playerMovement.jumpPower += 0.5f;
        
        GameManager.Instance.coin -= 10;
        UpdateCoinText();
        Debug.Log("Jump Force upgraded!");
    }

    public void AddSpeed()
    {
        if (GameManager.Instance == null || GameManager.Instance.coin < 10) 
        {
            Debug.Log("Not enough coins or GameManager not found!");
            return;
        }

        PlayerPrefs.SetInt(SPEED, PlayerPrefs.GetInt(SPEED, 0) + 1);
        PlayerPrefs.Save();

        if (playerMovement != null)
            playerMovement.speed += 0.3f;
        
        GameManager.Instance.coin -= 10;
        UpdateCoinText();
        Debug.Log("Speed upgraded!");
    }

    public void AddRevive()
    {
        if (GameManager.Instance == null || GameManager.Instance.coin < 10) 
        {
            Debug.Log("Not enough coins or GameManager not found!");
            return;
        }

        PlayerPrefs.SetInt(REVIVE, PlayerPrefs.GetInt(REVIVE, 0) + 1);
        PlayerPrefs.Save();

        if (playerHealth != null)
            playerHealth.reviveCount += 1;
        
        GameManager.Instance.coin -= 10;
        UpdateCoinText();
        Debug.Log("Revive upgraded!");
    }

    public void AddHealSkill()
    {
        if (GameManager.Instance == null || GameManager.Instance.coin < 100) 
        {
            Debug.Log("Not enough coins or GameManager not found!");
            return;
        }

        int newHealCount = PlayerPrefs.GetInt(HEAL, 0) + 1;
        PlayerPrefs.SetInt(HEAL, newHealCount);
        PlayerPrefs.Save();

        // GuitarSkillSystem'i güncelle
        GuitarSkillSystem guitarSkillSystem = FindObjectOfType<GuitarSkillSystem>();
        if (guitarSkillSystem != null)
            guitarSkillSystem.healCharges = newHealCount;
        
        // UI güncelle
        if (healChargesText != null)
            healChargesText.text = newHealCount.ToString();
        
        GameManager.Instance.coin -= 100;
        UpdateCoinText();
        Debug.Log("Heal Skill upgraded!");
    }

    public void AddFireballSkill()
    {
        if (GameManager.Instance == null || GameManager.Instance.coin < 100) 
        {
            Debug.Log("Not enough coins or GameManager not found!");
            return;
        }

        int newFireballCount = PlayerPrefs.GetInt(FIREBALL, 0) + 1;
        PlayerPrefs.SetInt(FIREBALL, newFireballCount);
        PlayerPrefs.Save();

        // GuitarSkillSystem'i güncelle
        GuitarSkillSystem guitarSkillSystem = FindObjectOfType<GuitarSkillSystem>();
        if (guitarSkillSystem != null)
            guitarSkillSystem.fireballCharges = newFireballCount;
        
        // UI güncelle
        if (fireballChargesText != null)
            fireballChargesText.text = newFireballCount.ToString();
        
        GameManager.Instance.coin -= 100;
        UpdateCoinText();
        Debug.Log("Fireball Skill upgraded!");
    }

    public void UpdateCoinText()
    {
        if (GameManager.Instance == null) 
        {
            Debug.LogWarning("GameManager not found!");
            return;
        }

        if (coinText != null)
            coinText.text = "Coin: " + GameManager.Instance.coin;

        if (coinTextGame != null)
            coinTextGame.text = "Coin: " + GameManager.Instance.coin;
    }

    // -------- DEBUG --------
    void DebugPlayerPrefs()
    {
        Debug.Log("=== PlayerPrefs Values ===");
        Debug.Log($"MAX_HEALTH: {PlayerPrefs.GetInt(MAX_HEALTH, 0)}");
        Debug.Log($"DAMAGE: {PlayerPrefs.GetInt(DAMAGE, 0)}");
        Debug.Log($"JUMP: {PlayerPrefs.GetInt(JUMP, 0)}");
        Debug.Log($"SPEED: {PlayerPrefs.GetInt(SPEED, 0)}");
        Debug.Log($"REVIVE: {PlayerPrefs.GetInt(REVIVE, 0)}");
        Debug.Log($"HEAL: {PlayerPrefs.GetInt(HEAL, 0)}");
        Debug.Log($"FIREBALL: {PlayerPrefs.GetInt(FIREBALL, 0)}");
        Debug.Log("==========================");
    }

    public void ResetAllUpgrades()
    {
        PlayerPrefs.DeleteAll();
        InitializeFirstLaunch();
        RefreshAllComponents();
        Debug.Log("All upgrades reset!");
    }

    // Manuel yenileme için public metod
    public void ManualRefresh()
    {
        RefreshAllComponents();
    }
}