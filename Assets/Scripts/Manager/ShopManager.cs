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
    private const string SAVE_VERSION_KEY = "SAVE_VERSION";
    private const int SAVE_VERSION = 4;

    [Header("Base Values")]
    public float baseMaxHealth = 3f;
    public float baseDamage = 3f;
    public float baseJumpPower = 14f;
    public float baseSpeed = 7f;
    public int baseReviveCount = 0;

    [Header("Player Components")]
    public Health playerHealth;
    public PlayerAttack playerAttack;
    public PlayerMovement playerMovement;

    [Header("UI")]
    public TMP_Text coinText;
    public TMP_Text coinTextGame;
    public TMP_Text healChargesText;
    public TMP_Text fireballChargesText;

    // ---------------------------------------------------
    // LIFECYCLE
    // ---------------------------------------------------
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InitializeFirstLaunch();
        RefreshAllComponents();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ---------------------------------------------------
    // SCENE LOAD
    // ---------------------------------------------------
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isLevel1 = scene.name == "Level1" || scene.name == "Level 1";

        FindPlayerComponents();
        FindUIComponents();

        if (isLevel1)
        {
            ResetForNewGame();
            LoadUpgrades();

            if (playerHealth != null)
                playerHealth.currentHealth = playerHealth.maxHealth;

            Debug.Log("[ShopManager] Level1 → Yeni oyun resetlendi");
        }
        else
        {
            LoadUpgrades();
            Debug.Log("[ShopManager] Level2+ → Prefs yüklendi");
        }

        UpdateCoinText();
    }

    // ---------------------------------------------------
    // FIRST LAUNCH
    // ---------------------------------------------------
    void InitializeFirstLaunch()
    {
        int savedVersion = PlayerPrefs.GetInt(SAVE_VERSION_KEY, 0);
        if (savedVersion < SAVE_VERSION)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.SetInt(SAVE_VERSION_KEY, SAVE_VERSION);
        }

        if (!PlayerPrefs.HasKey(FIRST_LAUNCH))
        {
            PlayerPrefs.SetInt(MAX_HEALTH, 0);
            PlayerPrefs.SetInt(DAMAGE, 0);
            PlayerPrefs.SetInt(JUMP, 0);
            PlayerPrefs.SetInt(SPEED, 0);
            PlayerPrefs.SetInt(REVIVE, 0);
            PlayerPrefs.SetInt(HEAL, 3);
            PlayerPrefs.SetInt(FIREBALL, 2);
            PlayerPrefs.SetInt(FIRST_LAUNCH, 1);
            PlayerPrefs.Save();
        }
    }

    // ---------------------------------------------------
    // RESET (SADECE LEVEL 1)
    // ---------------------------------------------------
    public void ResetForNewGame()
    {
        // --- STAT UPGRADE RESET ---
        PlayerPrefs.SetInt(MAX_HEALTH, 0);
        PlayerPrefs.SetInt(DAMAGE, 0);
        PlayerPrefs.SetInt(JUMP, 0);
        PlayerPrefs.SetInt(SPEED, 0);
        PlayerPrefs.SetInt(REVIVE, 0);

        // --- SKILL RESET ---
        PlayerPrefs.SetInt(HEAL, 3);
        PlayerPrefs.SetInt(FIREBALL, 2);

        // --- GAME SYSTEM RESET ---
        if (GameManager.Instance != null)
            GameManager.Instance.ResetCoin();

        if (SoulSystem.Instance != null)
            SoulSystem.Instance.ResetKills();

        PlayerPrefs.Save();

        // --- ANINDA PLAYER'A YANSIT ---
        LoadUpgrades();

        if (playerHealth != null)
            playerHealth.currentHealth = playerHealth.maxHealth;

        Debug.Log("[ShopManager] Yeni oyun → Tüm statlar sıfırlandı");
    }

    // ---------------------------------------------------
    // LOAD UPGRADES
    // ---------------------------------------------------
    void LoadUpgrades()
    {
        if (playerHealth != null)
        {
            playerHealth.maxHealth = baseMaxHealth + PlayerPrefs.GetInt(MAX_HEALTH, 0);
            playerHealth.reviveCount = baseReviveCount + PlayerPrefs.GetInt(REVIVE, 0);

            if (playerHealth.currentHealth > playerHealth.maxHealth)
                playerHealth.currentHealth = playerHealth.maxHealth;
        }

        if (playerAttack != null)
            playerAttack.damage = baseDamage + PlayerPrefs.GetInt(DAMAGE, 0);

        if (playerMovement != null)
        {
            playerMovement.jumpPower = baseJumpPower + PlayerPrefs.GetInt(JUMP, 0) * 0.5f;
            playerMovement.speed = baseSpeed + PlayerPrefs.GetInt(SPEED, 0) * 0.3f;
        }

        LoadGuitarSkills();
    }

    void LoadGuitarSkills()
    {
        GuitarSkillSystem guitar = FindObjectOfType<GuitarSkillSystem>();
        if (guitar == null) return;

        guitar.healCharges = PlayerPrefs.GetInt(HEAL, 0);
        guitar.fireballCharges = PlayerPrefs.GetInt(FIREBALL, 0);

        UpdateSkillUI();
    }

    // ---------------------------------------------------
    // SHOP ACTIONS
    // ---------------------------------------------------
    public void AddMaxHealth() => BuyUpgrade(100, MAX_HEALTH, () =>
    {
        playerHealth.maxHealth += 1;
        playerHealth.currentHealth += 1;
    });

    public void AddDamage() => BuyUpgrade(100, DAMAGE, () =>
    {
        playerAttack.damage += 1;
    });

    public void AddJumpForce() => BuyUpgrade(100, JUMP, () =>
    {
        playerMovement.jumpPower += 0.5f;
    });

    public void AddSpeed() => BuyUpgrade(100, SPEED, () =>
    {
        playerMovement.speed += 0.3f;
    });

    public void AddRevive() => BuyUpgrade(100, REVIVE, () =>
    {
        playerHealth.reviveCount += 1;
    });

    public void AddHealSkill() => BuyUpgrade(500, HEAL, () =>
    {
        UpdateSkillUI();
    });

    public void AddFireballSkill() => BuyUpgrade(500, FIREBALL, () =>
    {
        UpdateSkillUI();
    });

    void BuyUpgrade(int cost, string key, System.Action apply)
    {
        if (GameManager.Instance == null || !GameManager.Instance.SpendCoin(cost))
            return;

        PlayerPrefs.SetInt(key, PlayerPrefs.GetInt(key, 0) + 1);
        PlayerPrefs.Save();

        apply?.Invoke();
        LoadUpgrades();
        UpdateCoinText();
    }

    // ---------------------------------------------------
    // FIND & UI
    // ---------------------------------------------------
    void FindPlayerComponents()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (!player) return;

        playerHealth = player.GetComponent<Health>();
        playerAttack = player.GetComponent<PlayerAttack>();
        playerMovement = player.GetComponent<PlayerMovement>();
    }

    void FindUIComponents()
    {
        coinText = GameObject.Find("CoinText")?.GetComponent<TMP_Text>();
        coinTextGame = GameObject.Find("CoinTextGame")?.GetComponent<TMP_Text>();
        healChargesText = GameObject.Find("HealChargesText")?.GetComponent<TMP_Text>();
        fireballChargesText = GameObject.Find("FireballChargesText")?.GetComponent<TMP_Text>();
    }

    void UpdateSkillUI()
    {
        if (healChargesText)
            healChargesText.text = PlayerPrefs.GetInt(HEAL, 0).ToString();

        if (fireballChargesText)
            fireballChargesText.text = PlayerPrefs.GetInt(FIREBALL, 0).ToString();
    }

    public void UpdateCoinText()
    {
        if (GameManager.Instance == null) return;

        if (coinText)
            coinText.text = GameManager.Instance.coin.ToString();

        if (coinTextGame)
            coinTextGame.text = GameManager.Instance.coin.ToString();
    }

    public void RefreshAllComponents()
    {
        FindPlayerComponents();
        FindUIComponents();
        LoadUpgrades();
        UpdateCoinText();
    }
}
