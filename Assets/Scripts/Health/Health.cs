using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float startingHealth;
    public float currentHealth { get; private set; }
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

    private void Awake()
    {
        currentHealth = startingHealth;
        maxHealth = startingHealth;
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();

        if (uiManager == null)
        {
#if UNITY_2023_1_OR_NEWER
            uiManager = FindAnyObjectByType<UIManager>(FindObjectsInactive.Include);
#else
            uiManager = FindObjectOfType<UIManager>(true);
#endif
        }
    }

    public void TakeDamage(float _damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayPlayerHitSound();
        }
        
        // Ekran efektleri
        if (ScreenEffects.Instance != null)
        {
            ScreenEffects.Instance.UpdateHealthVignette(currentHealth / startingHealth);
        }
        
        // Ekran sarsıntısı
        if (ScreenShake.Instance != null)
        {
            ScreenShake.Instance.ShakeMedium();
        }

        if (currentHealth > 0)
        {
            anim.SetTrigger("hurt");
            GetComponent<PlayerMovement>().lockMovement = true;
            Invoke(nameof(UnlockMovement), 0.35f);
            StartCoroutine(Invunerability());
        }
        else
        {
            if (!dead)
            {
                anim.SetTrigger("die");
                GetComponent<PlayerMovement>().enabled = false;
                GetComponent<PlayerMovement>().lockMovement = true;
                Invoke(nameof(UnlockMovement), 0.35f);
                dead = true;

                StartCoroutine(DeathSequence());
            }
        }
    }

    public void AddHealth(float _value)
    {
        currentHealth = Mathf.Clamp(currentHealth + _value, 0, startingHealth);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayHealSound();
        }
        
        // İyileşme efektleri
        if (ScreenEffects.Instance != null)
        {
            ScreenEffects.Instance.UpdateHealthVignette(currentHealth / startingHealth);
        }
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
}