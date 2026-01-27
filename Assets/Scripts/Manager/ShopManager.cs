using UnityEngine;
using System.Collections;

public class ShopManager : MonoBehaviour
{
    public Health playerHealth;
    public PlayerAttack playerAttack;
    public PlayerMovement playerMovement;

    public void AddMaxHealth()
    {
        playerHealth.maxHealth += 1;
        playerHealth.AddHealth(playerHealth.maxHealth - playerHealth.currentHealth);
    }

    public void AddDamage()
    {
        playerAttack.damage += 1;
    }

    public void AddJumpForce()
    {
        playerMovement.jumpPower += 0.5f;
    }

    public void AddSpeed()
    {
        playerMovement.speed += 0.3f;
    }

    public void AddRevive()
    {
        playerHealth.reviveCount += 1;
    }
}