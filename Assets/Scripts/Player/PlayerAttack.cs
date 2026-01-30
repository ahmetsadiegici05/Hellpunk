using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    [Header("Melee Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] public float damage = 3f;
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
        // Puzzle aktifken hiçbir saldırı input'u kabul etme
        if (GameManager.IsPuzzleActive)
            return;
            
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
        playerMovement.lockMovement = true;
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

        Invoke(nameof(UnlockMovement), 0.35f);
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
        Debug.Log($"[PlayerAttack] FireballAttack çağrıldı!");
        Debug.Log($"[PlayerAttack] fireballs dizisi: {(fireballs != null ? fireballs.Length.ToString() : "NULL")}");
        Debug.Log($"[PlayerAttack] firePoint: {(firePoint != null ? firePoint.name : "NULL")}");
        
        playerMovement.lockMovement = true; 
        anim.SetTrigger("attack");
        fireballCooldownTimer = 0;

        int fireballIndex = FindFireball();
        Debug.Log($"[PlayerAttack] FindFireball sonucu: {fireballIndex}");
        
        if (fireballIndex != -1 && firePoint != null)
        {
            GameObject fireball = fireballs[fireballIndex];
            Debug.Log($"[PlayerAttack] Fireball objesi: {fireball.name}, aktif mi: {fireball.activeInHierarchy}");
            
            fireball.transform.position = firePoint.position;
            fireball.SetActive(true); // Fireball'u aktif et!
            
            Projectile projectile = fireball.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.SetDirection(Mathf.Sign(transform.localScale.x));
                Debug.Log($"[PlayerAttack] Fireball atıldı! Pozisyon: {firePoint.position}, Yön: {Mathf.Sign(transform.localScale.x)}");
            }
            else
            {
                Debug.LogError("[PlayerAttack] Fireball'da Projectile komponenti yok!");
            }
        }
        else
        {
            if (fireballs == null || fireballs.Length == 0)
                Debug.LogError("[PlayerAttack] fireballs dizisi boş! Inspector'da FireballHolder'ı ata!");
            else if (fireballIndex == -1)
                Debug.LogWarning("[PlayerAttack] Tüm fireball'lar kullanımda!");
            if (firePoint == null)
                Debug.LogError("[PlayerAttack] firePoint null! Inspector'da ata!");
        }

        Invoke(nameof(UnlockMovement), 0.35f);
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

    void UnlockMovement()
    {
        playerMovement.lockMovement = false;
    }
}