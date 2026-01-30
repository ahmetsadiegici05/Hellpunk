using UnityEngine;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    public enum PuzzleType { None, GuitarRiff, Rhythm, Memory }
    
    [Header("Settings")]
    [SerializeField] private bool isBoss = false;
    [SerializeField] private float maxHealth = 9f; // 3 yumrukta ölsün (hasar 3 x 3 = 9)
    [SerializeField] private GameObject deathEffect;
    public bool isDamagableObject = false;

    [Header("Puzzle Settings (Devre Dışı - Kaldırıldı)")]
    [SerializeField] public bool hasPuzzle = false; // Puzzle kaldırıldı - her zaman false
    [SerializeField] public PuzzleType puzzleType = PuzzleType.None;
    [SerializeField] public int puzzleDifficulty = 1; // 1-3
    [SerializeField] public int puzzleRewardCoins = 25;

    [Header("Health Bar")]
    [SerializeField] private bool showHealthBar = true;
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    public ParticleSystem particleSystem;

    private float currentHealth;
    private bool isDead = false;
    private bool puzzleStarted = false;
    private Color defaultColor;
    private SimpleEnemyHealthBar healthBar; // Yeni basit can barı

    [Header("Damage Text")]
    [SerializeField] private Vector3 damageTextOffset = new Vector3(0f, 1.2f, 0f);

    [Header("World Space Canvas")]
    [SerializeField] private Canvas enemyCanvas;



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
        ShowDamageText(amount);
        
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

        // Can 0'a düştüğünde direkt öl/kırıl (puzzle kaldırıldı)
        if (currentHealth <= 0)
        {
            // Puzzle kaldırıldı - sandıklar direkt kırılsın
            // if (isDamagableObject && hasPuzzle && puzzleType != PuzzleType.None)
            // {
            //     StartPuzzleBeforeBreak();
            // }
            // else
            // {
            //     Die();
            // }
            Die(); // Direkt kır/öldür
        }
    }

    /// <summary>
    /// Puzzle'ı başlat, kırılma puzzle sonucuna bağlı
    /// </summary>
    private void StartPuzzleBeforeBreak()
    {
        if (puzzleStarted) return;
        puzzleStarted = true;
        
        switch (puzzleType)
        {
            case PuzzleType.GuitarRiff:
                var guitarRiffUI = FindPuzzleUI<GuitarRiffPuzzleUI>();
                if (guitarRiffUI != null)
                    guitarRiffUI.InitializeFromPrefab(puzzleDifficulty, OnPuzzleSolvedBreak, OnPuzzleFailedNoBreak);
                else
                {
                    Debug.LogWarning("[EnemyHealth] GuitarRiffPuzzleUI bulunamadı! Direkt kırılıyor.");
                    GiveDirectRewardAndDie();
                }
                break;
                
            case PuzzleType.Rhythm:
                var rhythmUI = FindPuzzleUI<RhythmPuzzleUI>();
                if (rhythmUI != null)
                    rhythmUI.InitializeFromPrefab(puzzleDifficulty, OnPuzzleSolvedBreak, OnPuzzleFailedNoBreak);
                else
                {
                    Debug.LogWarning("[EnemyHealth] RhythmPuzzleUI bulunamadı! Direkt kırılıyor.");
                    GiveDirectRewardAndDie();
                }
                break;
                
            case PuzzleType.Memory:
                var memoryUI = FindPuzzleUI<MemoryPuzzleUI>();
                if (memoryUI != null)
                    memoryUI.InitializeFromPrefab(puzzleDifficulty, OnPuzzleSolvedBreak, OnPuzzleFailedNoBreak);
                else
                {
                    Debug.LogWarning("[EnemyHealth] MemoryPuzzleUI bulunamadı! Direkt kırılıyor.");
                    GiveDirectRewardAndDie();
                }
                break;
                
            default:
                GiveDirectRewardAndDie();
                break;
        }
    }

    /// <summary>
    /// Puzzle UI yoksa direkt ödül ver ve kır
    /// </summary>
    private void GiveDirectRewardAndDie()
    {
        GameManager.Instance.coin += 10;
        puzzleStarted = false;
        Die();
    }

    private T FindPuzzleUI<T>() where T : MonoBehaviour
    {
        // Önce aktif olanı ara
        T ui = FindFirstObjectByType<T>();
        if (ui != null) return ui;

        // İnaktif olanları da ara
        T[] allUIs = Resources.FindObjectsOfTypeAll<T>();
        foreach (var foundUI in allUIs)
        {
            if (foundUI != null && foundUI.gameObject.scene.isLoaded)
            {
                return foundUI;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Puzzle başarılı - chest kırılsın ve ödül verilsin
    /// </summary>
    private void OnPuzzleSolvedBreak()
    {
        GameManager.Instance.coin += puzzleRewardCoins;
        Debug.Log($"[EnemyHealth] Puzzle çözüldü! Chest kırılıyor, Ödül: {puzzleRewardCoins} coin");
        puzzleStarted = false;
        Die();
    }

    /// <summary>
    /// Puzzle başarısız - chest kırılmasın, tekrar denenebilir
    /// </summary>
    private void OnPuzzleFailedNoBreak()
    {
        Debug.Log("[EnemyHealth] Puzzle başarısız! Chest kırılmadı, tekrar dene.");
        puzzleStarted = false;
        // Canı geri yükle (tekrar vurulabilsin)
        currentHealth = 1f;
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (isDamagableObject) 
        {
            if (particleSystem != null) particleSystem.Play();
            if (healthBar != null) Destroy(healthBar.gameObject);
            
            // Sandık kırıldığında coin ver
            GiveDirectReward();
            
            StartCoroutine(FadeAndDestroy());
            return;
        }

        GameManager.Instance.coin += 10;
        
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
        if (spriteRenderer == null)
        {
            yield return new WaitForSeconds(0.5f);
            gameObject.SetActive(false);
            yield break;
        }

        float fadeDuration = 0.8f;
        float scaleDuration = 0.5f;
        float elapsed = 0f;
        
        Color startColor = spriteRenderer.color;
        Vector3 startScale = transform.localScale;
        Vector3 targetScale = startScale * 0.3f;
        
        // Hafif yukarı zıplama efekti
        Vector3 startPos = transform.position;
        float jumpHeight = 0.3f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            float smoothT = t * t * (3f - 2f * t); // Smoothstep
            
            // Fade out
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1f, 0f, smoothT);
            spriteRenderer.color = newColor;
            
            // Scale down (sadece ilk yarısında)
            if (t < 0.6f)
            {
                float scaleT = t / 0.6f;
                float scaleSmooth = scaleT * scaleT;
                transform.localScale = Vector3.Lerp(startScale, targetScale, scaleSmooth);
            }
            
            // Hafif yukarı hareket (bounce efekti)
            float bounce = Mathf.Sin(t * Mathf.PI) * jumpHeight;
            transform.position = startPos + Vector3.up * bounce;
            
            yield return null;
        }

        if (isDamagableObject) GameManager.Instance.ReturnPlayerToSavedPosition();
        
        // Son temizlik
        spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 0f);
        gameObject.SetActive(false);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Ulti")
            TakeDamage(100);
    }

    private void ShowDamageText(float damage)
    {
        if (enemyCanvas == null)
        {
            Debug.LogWarning("Enemy canvas atanmadı!");
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("FloatingDamageText");
        if (prefab == null) return;

        // Canvas içinde spawn
        GameObject dmgTextObj = Instantiate(prefab, enemyCanvas.transform);

        RectTransform rect = dmgTextObj.GetComponent<RectTransform>();

        // Düşmana göre pozisyon (LOCAL)
        Vector3 localPos = enemyCanvas.transform.InverseTransformPoint(
            transform.position + damageTextOffset
        );

        rect.localPosition = localPos;
        rect.localRotation = Quaternion.identity;

        FloatingDamageText dmgText = dmgTextObj.GetComponent<FloatingDamageText>();
        if (dmgText != null)
            dmgText.Initialize(damage);
    }

    IEnumerator ReturnToSavedPosition()
    {
        yield return new WaitForSeconds(1f);
        GameManager.Instance.ReturnPlayerToSavedPosition();
    }

    #region Puzzle System
    
    private void GiveDirectReward()
    {
        GameManager.Instance.coin += 10;
        Debug.Log($"[EnemyHealth] Direkt ödül verildi: 10 coin");
    }
    
    #endregion
}