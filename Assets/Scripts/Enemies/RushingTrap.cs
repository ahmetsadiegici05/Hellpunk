using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(EnemyHealth))]
public class RushingTrap : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("Does the original sprite face right? Check if yes.")]
    [SerializeField] private bool spriteFaceRight = true;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Movement and Distance")]
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float stopDistance = 1.5f;
    [SerializeField] private float moveSpeed = 6f;

    [Header("Attack Settings")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float attackRate = 1f;
    [SerializeField] private float pushForce = 5f;

    [Header("Physics")]
    [SerializeField] private LayerMask playerLayer;

    private Rigidbody2D rb;
    private Transform targetPlayer;
    private bool isDead = false;
    private float nextAttackTime = 0f;

    private const string ANIM_RUN = "Run";
    private const string ANIM_ATTACK = "Attack";

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isDead) return;

        FindPlayer();

        if (targetPlayer != null)
        {
            FacePlayer();
            CheckDistanceAndAttack();
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        if (targetPlayer != null)
        {
            MoveLogic();
        }
        else
        {
            StopMoving();
        }
    }

    private void FindPlayer()
    {
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, detectionRange, playerLayer);
        targetPlayer = playerCollider != null ? playerCollider.transform : null;
    }

    private void MoveLogic()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer > stopDistance)
        {
            float directionX = Mathf.Sign(targetPlayer.position.x - transform.position.x);

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = new Vector2(directionX * moveSpeed, rb.linearVelocity.y);
#else
            rb.velocity = new Vector2(directionX * moveSpeed, rb.velocity.y);
#endif
            if (animator != null) animator.SetBool(ANIM_RUN, true);
        }
        else
        {
            StopMoving();
        }
    }

    private void StopMoving()
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
#else
        rb.velocity = new Vector2(0, rb.velocity.y);
#endif

        if (animator != null) animator.SetBool(ANIM_RUN, false);
    }

    private void FacePlayer()
    {
        if (Mathf.Abs(targetPlayer.position.x - transform.position.x) < 0.5f)
            return;

        if (targetPlayer.position.x > transform.position.x)
            spriteRenderer.flipX = !spriteFaceRight;
        else if (targetPlayer.position.x < transform.position.x)
            spriteRenderer.flipX = spriteFaceRight;
    }

    private void CheckDistanceAndAttack()
    {
        float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);

        if (distanceToPlayer <= stopDistance + 0.2f && Time.time >= nextAttackTime)
        {
            StopMoving();
            PerformAttack();
            nextAttackTime = Time.time + attackRate;
        }
    }

    private void PerformAttack()
    {
        if (animator != null) animator.SetTrigger(ANIM_ATTACK);

        Health playerHealth = targetPlayer.GetComponent<Health>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(damage);
        }

        Rigidbody2D pRb = targetPlayer.GetComponent<Rigidbody2D>();
        if (pRb != null)
        {
            float pushDirX = (targetPlayer.position.x > transform.position.x) ? 1f : -1f;
            Vector2 knockback = new Vector2(pushDirX, 0.2f).normalized * pushForce;
            pRb.AddForce(knockback, ForceMode2D.Impulse);
        }
    }

    public void TriggerDeath()
    {
        isDead = true;
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
#else
        rb.velocity = new Vector2(0, rb.velocity.y);
#endif
        this.enabled = false;
    }

    public void StopAndDisable()
    {
        TriggerDeath();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}