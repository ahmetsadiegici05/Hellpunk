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

    public void AddMaxHealth()
    {
        if (GameManager.Instance.coin >= 10)
        {
            playerHealth.maxHealth += 1;
            playerHealth.AddHealth(playerHealth.maxHealth - playerHealth.currentHealth);
            GameManager.Instance.coin -= 10;
        }
        UpdateCoinText();
    }

    public void AddDamage()
    {
        if (GameManager.Instance.coin >= 10)
        {
            playerAttack.damage += 1;
            GameManager.Instance.coin -= 10;
        }
        UpdateCoinText();
    }

    public void AddJumpForce()
    {
        if (GameManager.Instance.coin >= 10)
        {
            playerMovement.jumpPower += 0.5f;
            GameManager.Instance.coin -= 10;
        }
        UpdateCoinText();
    }

    public void AddSpeed()
    {
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
        if (GameManager.Instance.coin >= 10)
        {
            playerHealth.reviveCount += 1;
            GameManager.Instance.coin -= 10;
        }
        UpdateCoinText();
    }

    public void AddHealSkill()
    {
        if (GameManager.Instance.coin >= 100)
        {
            GuitarSkillSystem.Instance.healCharges += 1;
            GameManager.Instance.coin -= 100;
            healChargesText.text = GuitarSkillSystem.Instance.healCharges.ToString();
        }
        UpdateCoinText();

    }

    public void AddFireballSkill()
    {
        if (GameManager.Instance.coin >= 100)
        {
            GuitarSkillSystem.Instance.fireballCharges += 1;
            GameManager.Instance.coin -= 100;
            fireballChargesText.text = GuitarSkillSystem.Instance.fireballCharges.ToString();
        }
        UpdateCoinText();
    }

    public void UpdateCoinText()
    {
        coinText.text = "Coin: " + GameManager.Instance.coin;
        coinTextGame.text = "Coin: " + GameManager.Instance.coin;
    }
}