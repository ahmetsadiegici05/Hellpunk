using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool isBoss = false;
    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private GameObject deathEffect;

    [Header("Health Bar")]
    [SerializeField] private bool showHealthBar = true;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private float currentHealth;
    private bool isDead = false;
    private Color defaultColor;
    private EnemyHealthBar healthBar;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null) defaultColor = spriteRenderer.color;
        currentHealth = maxHealth;
        
        // Can barı oluştur
        if (showHealthBar)
        {
            CreateHealthBar();
        }
    }
    
    private void CreateHealthBar()
    {
        GameObject healthBarObj = new GameObject($"{gameObject.name}_HealthBar");
        healthBar = healthBarObj.AddComponent<EnemyHealthBar>();
        healthBar.Initialize(transform, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        
        // Can barını güncelle
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        if (animator != null) animator.SetTrigger("Hurt");

        if (spriteRenderer != null)
        {
            StopCoroutine(nameof(FlashEffect));
            StartCoroutine(nameof(FlashEffect));
        }

        if (!isBoss && GameManager.Instance != null)
        {
            GameManager.Instance.PlayEnemyHitSound();
        }

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        
        // Can barını yok et
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }

        if (animator != null) animator.SetTrigger("Die");

        if (isBoss)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayBossDeathSound();
            }

#if UNITY_2023_1_OR_NEWER
            UIManager uiManager = FindAnyObjectByType<UIManager>();
#else
            UIManager uiManager = FindObjectOfType<UIManager>();
#endif
            if (uiManager != null)
            {
                uiManager.GameWin();
            }
        }

        var rushingTrap = GetComponent<RushingTrap>();
        if (rushingTrap != null)
        {
            rushingTrap.StopAndDisable();
        }

        GetComponent<Collider2D>().enabled = false;
        if (GetComponent<Rigidbody2D>() != null)
        {
#if UNITY_6000_0_OR_NEWER
            GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
#else
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
#endif
            GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        }

        if (ScoreManager.Instance != null) ScoreManager.Instance.AddEnemyKill();
        
        // Ruh sistemi - düşman öldürüldüğünde ruh ver
        if (SoulSystem.Instance != null)
        {
            int soulAmount = isBoss ? 5 : 1; // Boss 5 ruh verir
            SoulSystem.Instance.CollectSoul(soulAmount);
        }
        
        if (deathEffect != null) Instantiate(deathEffect, transform.position, Quaternion.identity);

        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FlashEffect()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.15f);
        if (!isDead) spriteRenderer.color = defaultColor;
    }

    private IEnumerator FadeAndDestroy()
    {
        yield return new WaitForSeconds(1f);
        gameObject.SetActive(false);
    }
}