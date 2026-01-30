using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float startingHealth;
    public float currentHealth;
    public float maxHealth;
    private Animator anim;
    private bool dead;

    [Header("UI")]
    [SerializeField] private UIManager uiManager;

    [Header("iFrames")]
    [SerializeField] private float iFramesDuration;
    [SerializeField] private int numberOfFlashes;
    private SpriteRenderer spriteRend;

    public int reviveCount = 0;

    // DarkenWorld sistemi kaldırıldı - sürekli siyahlaşma sorununa neden oluyordu
    // private static int darkStep = 0;
    // private const int maxSteps = 10;

    private void Awake()
    {
        currentHealth = startingHealth;
        maxHealth = startingHealth;
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();

#if UNITY_2023_1_OR_NEWER
        if (uiManager == null)
            uiManager = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
#else
        if (uiManager == null)
            uiManager = FindObjectOfType<UIManager>(true);
#endif
    }

    public void TakeDamage(float _damage)
    {
        if (dead) return;
        
        // Shop açıkken hasar alma
        if (IsShopOpen()) return;

        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);

        if (GameManager.Instance != null)
            GameManager.Instance.PlayPlayerHitSound();

        if (ScreenEffects.Instance != null)
            ScreenEffects.Instance.UpdateHealthVignette(currentHealth / startingHealth);

        if (ScreenShake.Instance != null)
            ScreenShake.Instance.ShakeMedium();

        // DarkenWorld efekti kaldırıldı - kümülatif siyahlaşmaya neden oluyordu
        // DarkenWorld();

        if (currentHealth > 0)
        {
            anim.SetTrigger("hurt");
            GetComponent<PlayerMovement>().lockMovement = true;
            Invoke(nameof(UnlockMovement), 0.35f);
            StartCoroutine(Invunerability());
        }
        else
        {
            dead = true;
            anim.SetTrigger("die");

            GetComponent<PlayerMovement>().enabled = false;
            GetComponent<PlayerMovement>().lockMovement = true;

            StartCoroutine(DeathSequence());
        }
    }

    // DarkenWorld efekti kaldırıldı - kümülatif RGB çarpımı sürekli siyahlaşmaya neden oluyordu
    // Orijinal renkleri saklamadan çarpma işlemi yapıldığından her hasar alımında renkler kalıcı olarak koyulaşıyordu
    // Alternatif olarak ScreenEffects.UpdateHealthVignette() kullanılabilir
    /*
    private void DarkenWorld()
    {
        if (darkStep < maxSteps)
            darkStep++;

        float t = (float)darkStep / maxSteps;
        float darkenAmount = Mathf.Lerp(1f, 0.3f, t); // 1 = normal, 0.3 = karanlık

        if (GameManager.Instance == null) return;

        foreach (var sr in GameManager.Instance.sprites)
        {
            if (sr != null)
            {
                // Mevcut rengi koru, sadece parlaklığı düşür
                Color c = sr.color;
                sr.color = new Color(c.r * darkenAmount, c.g * darkenAmount, c.b * darkenAmount, c.a);
            }
        }

        // Tilemap'leri karartma - özel renkleri bozmasın
        foreach (var tm in GameManager.Instance.tilemaps)
        {
            if (tm != null)
            {
                // TilemapColorKeeper varsa ona bırak
                var keeper = tm.GetComponent<TilemapColorKeeper>();
                if (keeper == null || !keeper.enforceColor)
                {
                    Color c = tm.color;
                    tm.color = new Color(c.r * darkenAmount, c.g * darkenAmount, c.b * darkenAmount, c.a);
                }
            }
        }
    }
    */

    public void AddHealth(float _value)
    {
        currentHealth = Mathf.Clamp(currentHealth + _value, 0, startingHealth);

        if (GameManager.Instance != null)
            GameManager.Instance.PlayHealSound();

        if (ScreenEffects.Instance != null)
            ScreenEffects.Instance.UpdateHealthVignette(currentHealth / startingHealth);
    }

    private IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(1f);

        if (uiManager != null && reviveCount <= 0)
            uiManager.GameOver();
        else
            uiManager.ContinueFromCheckpoint();
    }

    private IEnumerator Invunerability()
    {
        Physics2D.IgnoreLayerCollision(8, 9, true);

        for (int i = 0; i < numberOfFlashes; i++)
        {
            spriteRend.color = new Color(1, 0, 0, 0.5f);
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
            spriteRend.color = Color.white;
            yield return new WaitForSeconds(iFramesDuration / (numberOfFlashes * 2));
        }

        Physics2D.IgnoreLayerCollision(8, 9, false);
    }

    void UnlockMovement()
    {
        GetComponent<PlayerMovement>().lockMovement = false;
    }
    
    /// <summary>
    /// Shop paneli açık mı kontrol et
    /// </summary>
    private bool IsShopOpen()
    {
        if (uiManager != null && uiManager.shopPanel != null)
        {
            return uiManager.shopPanel.activeInHierarchy;
        }
        return false;
    }
}
