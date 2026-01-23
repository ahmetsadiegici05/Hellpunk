using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed;
    [SerializeField] private float damage = 1f;
    private float direction;
    private bool hit;
    private float lifetime;

    private Animator anim;
    private BoxCollider2D boxCollider;

    #region Time Slow Compensation
    // Mermi slow-mo'da da normal hızda uçsun diye tam telafi uygulanır
    private float TimeCompensation => TimeSlowAbility.Instance != null 
        ? TimeSlowAbility.Instance.PlayerTimeCompensation 
        : 1f;
    #endregion

    private void Awake()
    {
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void Update()
    {
        if (hit) return;

        // Time slow compensation - mermi slow-mo'da da normal hızda hareket etsin
        float compensatedSpeed = speed * TimeCompensation;
        float movementSpeed = compensatedSpeed * Time.deltaTime * direction;
        transform.Translate(movementSpeed, 0, 0);

        // Lifetime da telafi ile artar - 5 saniyelik ömür "gerçek zaman" gibi davransın
        lifetime += Time.deltaTime * TimeCompensation;
        if (lifetime > 5) gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hit) return;

        if (collision.CompareTag("Player")) return;

        if (collision.isTrigger && collision.GetComponent<EnemyHealth>() == null) return;

        EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
        if (enemyHealth != null)
            enemyHealth.TakeDamage(damage);

        hit = true;
        boxCollider.enabled = false;
        anim.SetTrigger("explode");
    }

    public void SetDirection(float _direction)
    {
        lifetime = 0;
        direction = _direction;
        gameObject.SetActive(true);
        hit = false;
        boxCollider.enabled = true;

        transform.rotation = Quaternion.identity;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayAttackSound();
        }

        float localScaleX = transform.localScale.x;
        if (Mathf.Sign(localScaleX) != _direction)
            localScaleX = -localScaleX;

        transform.localScale = new Vector3(localScaleX, transform.localScale.y, transform.localScale.z);
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }
}