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
    [SerializeField] private LayerMask playerLayer; // Oyuncu layer'ını ekle
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
    
    // Player freeze için
    private Vector3 playerPositionBeforeUlti;
    private Rigidbody2D playerRb;
    private RigidbodyType2D savedBodyType;
    private Vector2 savedVelocity;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        startPosition = transform.position;
        
        if (arrowInputImage != null)
            arrowStartPosition = arrowInputImage.transform.position;

        // 🔍 Health bar spawn olana kadar ara
        StartCoroutine(FindPlayerHealthBarRoutine());
    }

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
            Debug.Log("[ULTI-DEBUG] No Boss found for Ultimate!");
            return;
        }

        ultiUsed = false; // Ultimate tekrar kullanılabilir olsun
        StartCoroutine(UltiSlashSequence(bossTargets.ToArray()));
        Debug.Log("[ULTI-DEBUG] Ultimate activated on BOSS!");
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
            arrowStartPosition = arrowInputImage.transform.position;

        if (playerHealthBar != null)
            playerHealthBar.SetActive(false);
        StartCoroutine(EnableHealthBarAfterDelay());

        // 🔒 PLAYER'I DONDUR - En önemli kısım
        PlayerMovement player = PlayerMovement.Instance;
        Collider2D playerCollider = null;
        
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            playerCollider = player.GetComponent<Collider2D>();
            playerPositionBeforeUlti = player.transform.position;
            
            // Player hareketi kilitle
            player.lockMovement = true;
            
            // Rigidbody'yi dondur
            if (playerRb != null)
            {
                savedVelocity = playerRb.linearVelocity;
                savedBodyType = playerRb.bodyType;
                playerRb.linearVelocity = Vector2.zero;
                playerRb.bodyType = RigidbodyType2D.Kinematic;
                Debug.Log($"[ULTI-DEBUG] Player frozen at {playerPositionBeforeUlti}, velocity was {savedVelocity}");
            }
            
            // Slash collider ile player collision'ı kapat
            if (playerCollider != null && slashCollider != null)
            {
                Physics2D.IgnoreCollision(slashCollider, playerCollider, true);
            }
        }
        
        // Debug callback
        if (UltimateDebugger.Instance != null)
            UltimateDebugger.Instance.OnUltimateStarted();

        // Slash sekansı
        foreach (Collider2D target in targets)
        {
            if (target == null) continue;

            yield return StartCoroutine(SlashTarget(target.transform));
        }

        // 🔓 PLAYER'I SERBEST BIRAK
        if (player != null)
        {
            // Önce pozisyonu geri yükle (kayma olduysa düzelt)
            player.transform.position = playerPositionBeforeUlti;
            
            // Rigidbody'yi eski haline getir
            if (playerRb != null)
            {
                playerRb.bodyType = savedBodyType;
                playerRb.linearVelocity = Vector2.zero; // Temiz başlat
                Debug.Log($"[ULTI-DEBUG] Player unfrozen at {player.transform.position}");
            }
            
            // Hareket kilidini aç
            player.lockMovement = false;
            
            // Collision'ı geri aç
            if (playerCollider != null && slashCollider != null)
            {
                Physics2D.IgnoreCollision(slashCollider, playerCollider, false);
            }
        }
        
        // Debug callback
        if (UltimateDebugger.Instance != null)
            UltimateDebugger.Instance.OnUltimateEnded();

        ultiUsed = false;
    }

    IEnumerator SlashTarget(Transform target)
    {
        if (target == null) yield break;
        
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
            if (target == null || (enemyHealth != null && enemyHealth.IsDead))
            {
                Debug.Log("[ULTI-DEBUG] Boss defeated during Ultimate!");
                yield break;
            }
            
            // Sadece görsel efekt oluştur - TRANSFORM HAREKET ETMİYOR
            yield return StartCoroutine(CreateSlashEffect(dir.normalized, target));
            
            // Hasar ver
            if (target != null && enemyHealth != null && !enemyHealth.IsDead)
            {
                enemyHealth.TakeDamage(damagePerSlash);
            }
            
            yield return new WaitForSeconds(slashDelay);
        }
    }

    IEnumerator CreateSlashEffect(Vector2 direction, Transform target)
    {
        if (target == null) yield break;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Animator varsa tetikle (ama transform hareket etmeyecek)
        if (animator != null)
            animator.SetTrigger("Ulti");

        // Görsel efekt oluştur
        if (slashEffectPrefab != null)
        {
            Vector3 effectPos = target.position + (Vector3)(direction * slashEffectOffset);
            GameObject slashFx = Instantiate(slashEffectPrefab, effectPos, Quaternion.Euler(0, 0, angle));
            Destroy(slashFx, 0.5f); // Efekti temizle
        }

        // Slash çizgisi efekti (prefab yoksa basit çizgi)
        Vector3 startPos = target.position - (Vector3)(direction * slashRadius);
        Vector3 endPos = target.position + (Vector3)(direction * slashRadius);
        
        // Kısa bekleme (slash hissi için)
        yield return new WaitForSeconds(0.02f);
        
        // Ekran sarsıntısı
        if (ScreenShake.Instance != null)
        {
            ScreenShake.Instance.Shake(0.05f, 0.03f);
        }
    }

    IEnumerator EnableHealthBarAfterDelay()
    {
        yield return new WaitForSeconds(healthBarDelay);

        if (playerHealthBar != null)
            playerHealthBar.SetActive(true);
    }
}
