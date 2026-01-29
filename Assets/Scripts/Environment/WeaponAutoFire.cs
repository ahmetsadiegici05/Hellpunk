using UnityEngine;

public class WeaponAutoFire : MonoBehaviour
{
    [Header("Fire Settings")]
    [SerializeField] private float fireInterval = 0.5f; // Ateş aralığı
    [SerializeField] private bool fireOnStart = true;

    [Header("Bullet")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletSpeed = 10f;

    [Header("Optional Target")]
    [SerializeField] private Transform target; // boş bırakılırsa ileri atar

    private float fireTimer;

    private void Start()
    {
        if (fireOnStart)
            fireTimer = fireInterval;
    }

    private void Update()
    {
        fireTimer += Time.deltaTime;

        if (fireTimer >= fireInterval)
        {
            Fire();
            fireTimer = 0f;
        }
    }

    public void Fire()
    {
        if (bulletPrefab == null || firePoint == null) return;

        GameObject bullet = Instantiate(
            bulletPrefab,
            firePoint.position,
            firePoint.rotation
        );

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 dir;

            if (target != null)
                dir = (target.position - firePoint.position).normalized;
            else
                dir = firePoint.right; // 2D için sağ yön

            rb.linearVelocity = dir * bulletSpeed;
        }
    }

    public void StartFire() => enabled = true;
    public void StopFire() => enabled = false;
}
