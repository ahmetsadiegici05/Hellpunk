using UnityEngine;

public class LavaController : MonoBehaviour
{
    [Header("Lav Ayarları")]
    [SerializeField] private float riseSpeed = 1f; // Lavın yükselme hızı
    [SerializeField] private float stopY = 10f;    // Lavın duracağı Yüksekli (Y koordinatı)
    [SerializeField] private float startDelay = 2f; // Oyun başlayınca lav kaç saniye beklesin?

    [Header("Hasar")]
    [SerializeField] private float damageAmount = 100f; // Tek seferde öldürmek için yüksek hasar

    private bool canMove = false;

    private void Start()
    {
        // Başlangıç gecikmesi
        Invoke(nameof(StartMoving), startDelay);
    }

    private void StartMoving()
    {
        canMove = true;
    }

    private void Update()
    {
        if (!canMove) return;

        // Eğer lav belirlediğimiz yüksekliğe (stopY) ulaşmadıysa yukarı çıkmaya devam et
        if (transform.position.y < stopY)
        {
            // Vector3.up = (0, 1, 0) demektir. Y ekseninde yukarı taşır.
            transform.Translate(Vector3.up * riseSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Oyuncuya çarparsa
        if (collision.CompareTag("Player"))
        {
            Health playerHealth = collision.GetComponent<Health>();
            if (playerHealth != null)
            {
                // Oyuncuyu öldür (veya çok hasar ver)
                playerHealth.TakeDamage(damageAmount);
            }
        }
    }

    // Editörde lavın nerede duracağını çizgi ile gösterir (Yardımcı Görsel)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        // Lavın duracağı noktaya yatay bir çizgi çizer
        Gizmos.DrawLine(new Vector3(-100, stopY, 0), new Vector3(100, stopY, 0));
    }
}