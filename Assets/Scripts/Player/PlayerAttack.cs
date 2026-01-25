using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Melee Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float damage = 3f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private Transform attackPoint;

    [Header("Fireball Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject[] fireballs;
    [SerializeField] private float fireballCooldown = 0.3f;
    [SerializeField] private float fireballModeDuration = 10f; // 10 saniye fireball modu

    private Animator anim;
    private PlayerMovement playerMovement;
    private float cooldownTimer = Mathf.Infinity;
    private float fireballCooldownTimer = Mathf.Infinity;
    
    // Fireball modu
    private bool isFireballModeActive = false;
    private float fireballModeTimer = 0f;
    
    /// <summary>
    /// Fireball modu aktif mi? (UI için)
    /// </summary>
    public bool IsFireballModeActive => isFireballModeActive;
    public float FireballModeTimeRemaining => fireballModeTimer;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        
        // Hasar değerini zorla 3 yap (Inspector override'ı önle)
        damage = 3f;
        
        if (attackPoint == null && firePoint != null) attackPoint = firePoint;
        
        if (enemyLayers.value == 0)
        {
            int layer1 = LayerMask.NameToLayer("Enemies");
            int layer2 = LayerMask.NameToLayer("Enemy");
            if (layer1 != -1) enemyLayers |= (1 << layer1);
            if (layer2 != -1) enemyLayers |= (1 << layer2);
        }
    }

    private void Update()
    {
        bool isAttackHeld = Input.GetButton("Fire1") || Input.GetKey(KeyCode.RightControl);
        bool isInSkillInput = GuitarSkillSystem.Instance != null && GuitarSkillSystem.Instance.IsInSkillInput;

        // Sol tık her zaman punch (skill input modunda değilken)
        if (isAttackHeld && cooldownTimer > attackCooldown && playerMovement.canAttack() && !isInSkillInput)
            Attack();

        // Fireball modu aktifken Q tuşu ile fireball at
        if (isFireballModeActive)
        {
            fireballModeTimer -= Time.deltaTime;
            if (fireballModeTimer <= 0)
            {
                isFireballModeActive = false;
                Debug.Log("Fireball modu bitti!");
            }
            else if (Input.GetKeyDown(KeyCode.Q) && fireballCooldownTimer > fireballCooldown && playerMovement.canAttack())
            {
                FireballAttack();
            }
        }

        cooldownTimer += Time.deltaTime;
        fireballCooldownTimer += Time.deltaTime;
    }

    private void Attack()
    {
        anim.SetTrigger("punch");
        cooldownTimer = 0;

        if (attackPoint != null)
        {
            Collider2D[] allColliders = Physics2D.OverlapCircleAll(attackPoint.position, attackRange);
            
            Debug.Log($"[PlayerAttack] Yumruk! {allColliders.Length} collider bulundu. Hasar: {damage}");
            
            foreach(Collider2D col in allColliders)
            {
                EnemyHealth enemyHealth = col.GetComponent<EnemyHealth>();
                if(enemyHealth != null)
                {
                    Debug.Log($"[PlayerAttack] {col.name} düşmanına {damage} hasar veriliyor!");
                    enemyHealth.TakeDamage(damage);
                }
            }
        }
    }

    /// <summary>
    /// Fireball modunu aktif et (skill başarılı olunca çağrılır)
    /// </summary>
    public void ActivateFireballMode()
    {
        isFireballModeActive = true;
        fireballModeTimer = fireballModeDuration;
        Debug.Log($"Fireball modu aktif! {fireballModeDuration} saniye boyunca fireball atabilirsin!");
    }

    /// <summary>
    /// Fireball saldırısı
    /// </summary>
    public void FireballAttack()
    {
        anim.SetTrigger("attack");
        fireballCooldownTimer = 0;

        int fireballIndex = FindFireball();
        if (fireballIndex != -1 && firePoint != null)
        {
            fireballs[fireballIndex].transform.position = firePoint.position;
            fireballs[fireballIndex].GetComponent<Projectile>().SetDirection(Mathf.Sign(transform.localScale.x));
        }
    }

    private int FindFireball()
    {
        if (fireballs == null) return -1;
        
        for (int i = 0; i < fireballs.Length; i++)
        {
            if (fireballs[i] != null && !fireballs[i].activeInHierarchy)
                return i;
        }
        return -1;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}