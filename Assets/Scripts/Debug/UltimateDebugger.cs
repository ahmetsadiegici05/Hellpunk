using UnityEngine;

/// <summary>
/// Ultimate ve Boss sorunlarını debug etmek için geçici script
/// Console'da [ULTI-DEBUG] prefix'i ile logları görebilirsin
/// </summary>
public class UltimateDebugger : MonoBehaviour
{
    public static UltimateDebugger Instance { get; private set; }
    
    [Header("Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showOnScreenDebug = true;
    [SerializeField] private KeyCode toggleDebugKey = KeyCode.F9;
    
    // Tracking
    private Vector3 lastPlayerPosition;
    private Vector3 playerPositionBeforeUlti;
    private bool ultiInProgress = false;
    private float ultiStartTime;
    private string lastDebugMessage = "";
    private int framesSinceUlti = 0;
    
    // References
    private PlayerMovement player;
    private Rigidbody2D playerRb;
    
    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        player = PlayerMovement.Instance;
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
            lastPlayerPosition = player.transform.position;
        }
        
        Log("UltimateDebugger başlatıldı. F9 ile on/off yapabilirsin.");
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(toggleDebugKey))
        {
            enableDebugLogs = !enableDebugLogs;
            showOnScreenDebug = !showOnScreenDebug;
            Debug.Log($"[ULTI-DEBUG] Debug mode: {(enableDebugLogs ? "ON" : "OFF")}");
        }
        
        if (!enableDebugLogs) return;
        
        // Player tracking
        if (player != null)
        {
            Vector3 currentPos = player.transform.position;
            Vector3 delta = currentPos - lastPlayerPosition;
            
            // Ani hareket tespiti
            if (delta.magnitude > 5f)
            {
                Log($"⚠️ ANİ HAREKET! Delta: {delta}, Magnitude: {delta.magnitude:F2}");
                Log($"   Önceki: {lastPlayerPosition}, Şimdi: {currentPos}");
            }
            
            // Aşağı kayma tespiti
            if (delta.y < -2f && Mathf.Abs(delta.x) < 0.5f)
            {
                Log($"⚠️ AŞAĞI KAYMA! Y delta: {delta.y:F2}");
                if (playerRb != null)
                {
                    Log($"   Velocity: {playerRb.linearVelocity}, Gravity: {playerRb.gravityScale}");
                }
            }
            
            // Ulti sonrası takip
            if (ultiInProgress)
            {
                framesSinceUlti++;
                if (framesSinceUlti <= 60) // 1 saniye boyunca takip et
                {
                    if (framesSinceUlti % 10 == 0) // Her 10 frame'de bir
                    {
                        Log($"[ULTI-TRACK] Frame {framesSinceUlti}: Pos={currentPos}, Vel={playerRb?.linearVelocity}");
                    }
                }
                else
                {
                    ultiInProgress = false;
                    Log($"[ULTI-END] Ulti takibi bitti. Son pos: {currentPos}");
                }
            }
            
            lastPlayerPosition = currentPos;
        }
        
        // Boss durumu kontrolü
        if (Input.GetKeyDown(KeyCode.F10))
        {
            CheckBossStatus();
        }
    }
    
    /// <summary>
    /// Ultimate başladığında çağır
    /// </summary>
    public void OnUltimateStarted()
    {
        if (!enableDebugLogs) return;
        
        if (player != null)
        {
            playerPositionBeforeUlti = player.transform.position;
        }
        
        ultiInProgress = true;
        ultiStartTime = Time.time;
        framesSinceUlti = 0;
        
        Log($"🎯 ULTİMATE BAŞLADI!");
        Log($"   Player pozisyonu: {playerPositionBeforeUlti}");
        Log($"   Time.timeScale: {Time.timeScale}");
        
        if (playerRb != null)
        {
            Log($"   Player velocity: {playerRb.linearVelocity}");
            Log($"   Player gravity: {playerRb.gravityScale}");
            Log($"   BodyType: {playerRb.bodyType}");
        }
        
        // BossSlashUltimate durumu
        if (BossSlashUltimate.Instance != null)
        {
            var bsu = BossSlashUltimate.Instance;
            Log($"   BossSlashUltimate transform: {bsu.transform.position}");
            Log($"   BossSlashUltimate parent: {(bsu.transform.parent != null ? bsu.transform.parent.name : "null")}");
        }
    }
    
    /// <summary>
    /// Ultimate bittiğinde çağır
    /// </summary>
    public void OnUltimateEnded()
    {
        if (!enableDebugLogs) return;
        
        float duration = Time.time - ultiStartTime;
        
        Log($"✅ ULTİMATE BİTTİ! Süre: {duration:F2}s");
        
        if (player != null)
        {
            Vector3 currentPos = player.transform.position;
            Vector3 delta = currentPos - playerPositionBeforeUlti;
            
            Log($"   Player pozisyonu: {currentPos}");
            Log($"   Başlangıçtan fark: {delta} (magnitude: {delta.magnitude:F2})");
            
            if (delta.magnitude > 3f)
            {
                Log($"⚠️ UYARI: Player {delta.magnitude:F2} birim hareket etti!");
            }
        }
        
        if (playerRb != null)
        {
            Log($"   Player velocity: {playerRb.linearVelocity}");
        }
        
        Log($"   Time.timeScale: {Time.timeScale}");
    }
    
    /// <summary>
    /// Boss durumunu kontrol et (F10 ile çağrılır)
    /// </summary>
    public void CheckBossStatus()
    {
        Log("========== BOSS DURUM RAPORU ==========");
        
        // Tüm EnemyHealth'leri bul
        var enemies = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int bossCount = 0;
        
        foreach (var enemy in enemies)
        {
            if (enemy.IsBoss)
            {
                bossCount++;
                Log($"🔴 BOSS BULUNDU: {enemy.name}");
                Log($"   Active: {enemy.gameObject.activeSelf}");
                Log($"   ActiveInHierarchy: {enemy.gameObject.activeInHierarchy}");
                Log($"   Position: {enemy.transform.position}");
                Log($"   IsDead: {enemy.IsDead}");
                
                if (enemy.transform.parent != null)
                {
                    Log($"   Parent: {enemy.transform.parent.name} (active: {enemy.transform.parent.gameObject.activeSelf})");
                }
            }
        }
        
        if (bossCount == 0)
        {
            Log("❌ Sahnede hiç boss bulunamadı!");
        }
        
        // BossActivator durumu
        var activators = FindObjectsByType<BossActivator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var activator in activators)
        {
            Log($"📍 BossActivator: {activator.name}");
            Log($"   Active: {activator.gameObject.activeSelf}");
            Log($"   Position: {activator.transform.position}");
        }
        
        Log("========================================");
    }
    
    private void Log(string message)
    {
        if (!enableDebugLogs) return;
        
        lastDebugMessage = message;
        Debug.Log($"[ULTI-DEBUG] {message}");
    }
    
    private void OnGUI()
    {
        if (!showOnScreenDebug) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.box);
        style.fontSize = 14;
        style.alignment = TextAnchor.UpperLeft;
        
        string info = "=== ULTI DEBUG (F9 toggle, F10 boss check) ===\n";
        
        if (player != null)
        {
            info += $"Player Pos: {player.transform.position:F1}\n";
            if (playerRb != null)
            {
                info += $"Velocity: {playerRb.linearVelocity:F1}\n";
                info += $"Gravity: {playerRb.gravityScale:F1}\n";
            }
        }
        
        info += $"TimeScale: {Time.timeScale:F2}\n";
        
        if (ultiInProgress)
        {
            info += $"⚡ ULTI IN PROGRESS (frame {framesSinceUlti})\n";
        }
        
        if (BossSlashUltimate.Instance != null)
        {
            info += $"BossSlashUlti Pos: {BossSlashUltimate.Instance.transform.position:F1}\n";
        }
        
        info += $"\nLast: {lastDebugMessage}";
        
        GUI.Box(new Rect(10, 10, 400, 180), info, style);
    }
}
