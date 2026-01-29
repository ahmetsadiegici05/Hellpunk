using UnityEngine;

public class PortalToChestRoom : MonoBehaviour
{
    public static PortalToChestRoom Instance;

    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool onlyOnce = true;

    [Header("Teleport")]
    [SerializeField] private Transform chestRoomSpawnPoint;

    [Header("Enemy Spawn")]
    [SerializeField] private Transform enemySpawnPoint;
    [SerializeField] private GameObject[] enemyPrefabs;

    [Header("Chest Spawn")]
    [SerializeField] private Transform chestSpawnPoint;
    [SerializeField] private GameObject chestPrefab;

    private bool activated = false;
    private Transform player;

    private void Awake()
    {
        Instance = this;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated && onlyOnce) return;

        if (other.CompareTag(playerTag))
        {
            activated = true;

            // ✅ PLAYER REFERANSI ALINIYOR
            player = other.transform;

            // 🔐 Geri dönüş noktası kaydedilir
            GameManager.Instance.SavePlayerReturnPoint(player);

            // 🚪 Teleport
            TeleportPlayerToChestRoom();

            // 👾 Spawnlar
            SpawnEnemy();
            SpawnChest();
        }
    }

    private void TeleportPlayerToChestRoom()
    {
        if (player == null || GameManager.Instance.chestRoomSpawnPoint == null)
        {
            Debug.LogError("[Portal] Player veya ChestRoomSpawnPoint NULL!");
            return;
        }

        player.position = GameManager.Instance.chestRoomSpawnPoint.position;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        Debug.Log("[Portal] Player chest room'a ışınlandı");
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0 || GameManager.Instance.enemySpawnPoint == null) return;

        int randomIndex = Random.Range(0, enemyPrefabs.Length);
        Instantiate(enemyPrefabs[randomIndex], GameManager.Instance.enemySpawnPoint.position, Quaternion.identity);
    }

    private void SpawnChest()
    {
        if (chestPrefab == null || GameManager.Instance.chestSpawnPoint == null) return;

        Instantiate(chestPrefab, GameManager.Instance.chestSpawnPoint.position, Quaternion.identity);
    }
}
