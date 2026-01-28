using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool isBoss = false;
    [SerializeField] private float maxHealth = 9f; // 3 yumrukta ölsün (hasar 3 x 3 = 9)
    [SerializeField] private GameObject deathEffect;
    public bool isDamagableObject = false;

    [Header("Health Bar")]
    [SerializeField] private bool showHealthBar = true;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    public ParticleSystem particleSystem;

    private float currentHealth;
    private bool isDead = false;
    private Color defaultColor;
    private SimpleEnemyHealthBar healthBar; // Yeni basit can barı

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null) defaultColor = spriteRenderer.color;
        
        // Boss değilse canı mutlaka 9 yap (3 yumrukta ölsün)
        if (!isBoss)
        {
            maxHealth = 9f; // Her zaman 9 yap (Inspector override'ı önle)
        }
        
        currentHealth = maxHealth;
        
        Debug.Log($"[EnemyHealth] {gameObject.name} başlatıldı - MaxHealth: {maxHealth}, CurrentHealth: {currentHealth}");
        
        // Can barı oluştur
        if (showHealthBar)
        {
            CreateHealthBar();
        }
    }
    
    private void CreateHealthBar()
    {
        GameObject healthBarObj = new GameObject($"{gameObject.name}_HealthBar");
        healthBar = healthBarObj.AddComponent<SimpleEnemyHealthBar>(); // Yeni basit versiyon
        healthBar.Initialize(transform, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        
        Debug.Log($"{gameObject.name} hasar aldı: {amount}, Kalan can: {currentHealth}");
        
        // Can barını güncelle
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
            Debug.Log($"{gameObject.name} can barı güncellendi");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} can barı bulunamadı!");
        }

        if (animator != null) animator.SetTrigger("Hurt");

        if (spriteRenderer != null)
        {
            StopCoroutine(nameof(FlashEffect));
            StartCoroutine(nameof(FlashEffect));
        }

        if (!isBoss && GameManager.Instance != null && !isDamagableObject)
        {
            GameManager.Instance.PlayEnemyHitSound();
        }

        if (currentHealth <= 0) Die();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (isDamagableObject) 
        {
            GameManager.Instance.coin += 10;
            particleSystem.Play();
            ShopManager.Instance.UpdateCoinText();
            Destroy(healthBar.gameObject);
            StartCoroutine(FadeAndDestroy());
            return;
        }

        GameManager.Instance.coin += 10;
        ShopManager.Instance.UpdateCoinText();
        
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

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Ulti")
            TakeDamage(100);
    }
}