using UnityEngine;
using System.Collections;
using TMPro;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance;

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
        Instance = this;
    }
    
    private void Start()
    {
        // Otomatik olarak oyuncuyu bul (Inspector'da atanmamışsa)
        FindPlayerComponents();
        UpdateCoinText();
    }
    
    private void OnEnable()
    {
        // Her açıldığında oyuncuyu tekrar bul (level değişikliği için)
        FindPlayerComponents();
        UpdateCoinText();
    }
    
    private void FindPlayerComponents()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            if (playerHealth == null)
                playerHealth = player.GetComponent<Health>();
            if (playerAttack == null)
                playerAttack = player.GetComponent<PlayerAttack>();
            if (playerMovement == null)
                playerMovement = player.GetComponent<PlayerMovement>();
                
            Debug.Log($"[ShopManager] Oyuncu bulundu - Health: {playerHealth != null}, Attack: {playerAttack != null}, Movement: {playerMovement != null}");
        }
        else
        {
            Debug.LogWarning("[ShopManager] Player bulunamadı!");
        }
    }

    public void AddMaxHealth()
    {
        if (playerHealth == null) FindPlayerComponents();
        if (playerHealth == null || GameManager.Instance == null) return;
        
        if (GameManager.Instance.coin >= 10)
        {
            playerHealth.maxHealth += 1;
            playerHealth.AddHealth(playerHealth.maxHealth - playerHealth.currentHealth);
            GameManager.Instance.coin -= 10;
            Debug.Log("[ShopManager] Max Health artırıldı!");
        }
        UpdateCoinText();
    }

    public void AddDamage()
    {
        if (playerAttack == null) FindPlayerComponents();
        if (playerAttack == null || GameManager.Instance == null) return;
        
        if (GameManager.Instance.coin >= 10)
        {
            playerAttack.damage += 1;
            GameManager.Instance.coin -= 10;
            Debug.Log("[ShopManager] Damage artırıldı!");
        }
        UpdateCoinText();
    }

    public void AddJumpForce()
    {
        if (playerMovement == null) FindPlayerComponents();
        if (playerMovement == null || GameManager.Instance == null) return;
        
        if (GameManager.Instance.coin >= 10)
        {
            playerMovement.jumpPower += 0.5f;
            GameManager.Instance.coin -= 10;
            Debug.Log("[ShopManager] Jump force artırıldı!");
        }
        UpdateCoinText();
    }

    public void AddSpeed()
    {
        if (playerMovement == null) FindPlayerComponents();
        if (playerMovement == null || GameManager.Instance == null) return;
        
        if (GameManager.Instance.coin >= 10)
        {
            Debug.Log("AddSpeed CALLED");
            Debug.Log("Old speed: " + playerMovement.speed);

            playerMovement.speed += 0.3f;
            GameManager.Instance.coin -= 10;

            Debug.Log("New speed: " + playerMovement.speed);
        }
        UpdateCoinText();
    }

    public void AddRevive()
    {
        if (playerHealth == null) FindPlayerComponents();
        if (playerHealth == null || GameManager.Instance == null) return;
        
        if (GameManager.Instance.coin >= 10)
        {
            playerHealth.reviveCount += 1;
            GameManager.Instance.coin -= 10;
            Debug.Log("[ShopManager] Revive artırıldı!");
        }
        UpdateCoinText();
    }

    public void AddHealSkill()
    {
        if (GuitarSkillSystem.Instance == null || GameManager.Instance == null) return;
        
        if (GameManager.Instance.coin >= 100)
        {
            GuitarSkillSystem.Instance.healCharges += 1;
            GameManager.Instance.coin -= 100;
            if (healChargesText != null)
                healChargesText.text = GuitarSkillSystem.Instance.healCharges.ToString();
            Debug.Log("[ShopManager] Heal skill eklendi!");
        }
        UpdateCoinText();

    }

    public void AddFireballSkill()
    {
        if (GuitarSkillSystem.Instance == null || GameManager.Instance == null) return;
        
        if (GameManager.Instance.coin >= 100)
        {
            GuitarSkillSystem.Instance.fireballCharges += 1;
            GameManager.Instance.coin -= 100;
            if (fireballChargesText != null)
                fireballChargesText.text = GuitarSkillSystem.Instance.fireballCharges.ToString();
            Debug.Log("[ShopManager] Fireball skill eklendi!");
        }
        UpdateCoinText();
    }

    public void UpdateCoinText()
    {
        if (GameManager.Instance == null) return;
        
        if (coinText != null)
            coinText.text = "Coin: " + GameManager.Instance.coin;
        if (coinTextGame != null)
            coinTextGame.text = "Coin: " + GameManager.Instance.coin;
    }
}