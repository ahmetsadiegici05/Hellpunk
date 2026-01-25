using UnityEngine;

/// <summary>
/// Gitar skill sistemi için fireball projectile.
/// Normal saldırıdan daha güçlü, skill ile atılır.
/// </summary>
public class GuitarFireball : MonoBehaviour
{
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 5f;
    
    private float direction;
    private float damage;
    private bool hit;
    private float timer;
    
    private Animator anim;
    private Collider2D col;
    private SpriteRenderer spriteRenderer;

    #region Time Slow Compensation
    private float TimeCompensation => TimeSlowAbility.Instance != null 
        ? TimeSlowAbility.Instance.PlayerTimeCompensation 
        : 1f;
    #endregion

    private void Awake()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Player ile çarpışmayı devre dışı bırak (0.1 saniye sonra aktif et)
        StartCoroutine(EnableColliderDelayed());
    }
    
    private System.Collections.IEnumerator EnableColliderDelayed()
    {
        if (col != null) col.enabled = false;
        yield return new WaitForSeconds(0.1f);
        if (col != null && !hit) col.enabled = true;
    }

    public void Initialize(float dir, float dmg)
    {
        direction = dir;
        damage = dmg;
        hit = false;
        timer = 0f;

        if (col != null)
            col.enabled = true;

        // Yönü ayarla
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x) * dir,
            transform.localScale.y,
            transform.localScale.z
        );

        // Ses
        if (GameManager.Instance != null)
            GameManager.Instance.PlayAttackSound();
    }

    private void Update()
    {
        if (hit) return;

        // Hareket - time slow compensation ile
        float compensatedSpeed = speed * TimeCompensation;
        transform.Translate(compensatedSpeed * Time.deltaTime * direction, 0, 0);

        // Lifetime
        timer += Time.deltaTime * TimeCompensation;
        if (timer > lifetime)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hit) return;

        // Oyuncuyu yoksay
        if (collision.CompareTag("Player")) return;

        // Trigger collider'ları yoksay (EnemyHealth yoksa)
        if (collision.isTrigger && collision.GetComponent<EnemyHealth>() == null) return;

        // Düşmana hasar ver
        EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }

        hit = true;
        
        if (col != null)
            col.enabled = false;

        // Patlama animasyonu
        if (anim != null)
        {
            anim.SetTrigger("explode");
            // Animasyon bitince destroy
            Destroy(gameObject, 0.5f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
