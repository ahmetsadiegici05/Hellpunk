using System.Collections;
using UnityEngine;

public class BossSlashUltimate : MonoBehaviour
{

    public static BossSlashUltimate Instance;

    [Header("Detection")]
    [SerializeField] private float activateDistance = 6f;

    [Header("Slash Settings")]
    [SerializeField] private float slashRadius = 1.5f;
    [SerializeField] private float slashSpeed = 25f;
    [SerializeField] private float slashDelay = 0.05f;
    [SerializeField] private float damagePerSlash = 2f; // Her kesme başına hasar (8 kesme = 16 toplam)
    [SerializeField] private BoxCollider2D slashCollider;
    public Animator animator;

    [Header("Visual Slash Effect")]
    [SerializeField] private GameObject slashEffectPrefab;
    [SerializeField] private float slashEffectOffset = 0.2f;

    [Header("UI")]
    [SerializeField] private float healthBarDelay = 2f;
    private GameObject playerHealthBar;

    private bool ultiUsed = false;
    private Vector3 startPosition;
    public GameObject arrowInputImage;
    private Vector3 arrowStartPosition;

    [Header("Target Settings")]
    [SerializeField] private LayerMask ultiTargetLayer; // UseUlti layer

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        startPosition = transform.position;
        arrowStartPosition = arrowInputImage.transform.position;

        // 🔍 Health bar spawn olana kadar ara
        StartCoroutine(FindPlayerHealthBarRoutine());
    }

    // void Update()
    // {
    //     if (bossTarget == null || ultiUsed)
    //         return;

    //     float dist = Vector2.Distance(transform.position, bossTarget.position);

    //     if (dist <= activateDistance)
    //     {
    //         StartCoroutine(UltiSlashSequence());
    //     }
    // }

    public void ActivateUltimate()
    {
        // Sadece Boss'ları bul
        EnemyHealth[] allEnemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
        
        System.Collections.Generic.List<Collider2D> bossTargets = new System.Collections.Generic.List<Collider2D>();
        foreach (var enemy in allEnemies)
        {
            // Sadece Boss'ları hedefle
            if (enemy.IsBoss)
            {
                Collider2D col = enemy.GetComponent<Collider2D>();
                if (col != null)
                    bossTargets.Add(col);
            }
        }
        
        if (bossTargets.Count == 0)
        {
            Debug.Log("No Boss found for Ultimate!");
            return;
        }

        ultiUsed = false; // Ultimate tekrar kullanılabilir olsun
        StartCoroutine(UltiSlashSequence(bossTargets.ToArray()));
        Debug.Log("Ultimate activated on BOSS!");
    }


    IEnumerator FindPlayerHealthBarRoutine()
    {
        while (playerHealthBar == null)
        {
            playerHealthBar = GameObject.Find("PlayerHealthBar");
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator UltiSlashSequence(Collider2D[] targets)
    {
        ultiUsed = true;

        if (arrowInputImage != null)
            arrowInputImage.transform.position = arrowStartPosition;

        if (playerHealthBar != null)
            playerHealthBar.SetActive(false);
        StartCoroutine(EnableHealthBarAfterDelay());

        foreach (Collider2D target in targets)
        {
            if (target == null) continue;

            yield return StartCoroutine(SlashTarget(target.transform));
        }

        yield return StartCoroutine(ReturnToStart());
    }

    IEnumerator SlashTarget(Transform target)
    {
        transform.position = target.position;
        
        // Hedefin EnemyHealth'ini al
        EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();

        Vector2[] slashDirections =
        {
            Vector2.left,
            Vector2.right,
            new Vector2(-1, 1),
            new Vector2(1, -1),
            Vector2.right,
            Vector2.left,
            new Vector2(1, 1),
            new Vector2(-1, -1)
        };

        foreach (Vector2 dir in slashDirections)
        {
            yield return StartCoroutine(SlashMove(dir.normalized, target));
            
            // Her kesmede hasar ver
            if (enemyHealth != null && target != null)
            {
                enemyHealth.TakeDamage(damagePerSlash);
                Debug.Log($"Ultimate slash! {target.name} took {damagePerSlash} damage");
            }
            
            yield return new WaitForSeconds(slashDelay);
        }
    }


    IEnumerator SlashMove(Vector2 direction, Transform target)
    {
        slashCollider.enabled = false;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        animator.SetTrigger("Ulti");

        if (slashEffectPrefab != null)
        {
            Vector3 effectPos = target.position + (Vector3)(direction * slashEffectOffset);
            GameObject slashFx = Instantiate(slashEffectPrefab, effectPos, Quaternion.identity);
            slashFx.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        Vector3 startPos = target.position - (Vector3)(direction * slashRadius);
        Vector3 endPos   = target.position + (Vector3)(direction * slashRadius);

        transform.position = startPos;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * slashSpeed;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
    }

    IEnumerator ReturnToStart()
    {
        transform.rotation = Quaternion.identity;
        slashCollider.enabled = true;

        float t = 0f;
        Vector3 from = transform.position;

        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            transform.position = Vector3.Lerp(from, startPosition, t);
            yield return null;
        }
    }

    IEnumerator EnableHealthBarAfterDelay()
    {
        yield return new WaitForSeconds(healthBarDelay);

        if (playerHealthBar != null)
            playerHealthBar.SetActive(true);
    }
}
