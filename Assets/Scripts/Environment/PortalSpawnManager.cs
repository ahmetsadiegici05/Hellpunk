using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Portal spawn noktalarını yönetmek için kullanılan sistem.
/// Sahnede "PortalPoint" tag'li objeleri kullanır.
/// Inspector'dan portal sayısını ve spawn oranını ayarlayabilirsiniz.
/// </summary>
public class PortalSpawnManager : MonoBehaviour
{
    public static PortalSpawnManager Instance { get; private set; }

    [Header("Portal Ayarları")]
    [Tooltip("Her sahnede spawn edilecek maksimum portal sayısı")]
    [SerializeField] private int maxPortalsPerScene = 5;
    
    [Tooltip("Her spawn noktasında portal çıkma şansı (%100 = kesin çıkar)")]
    [Range(0f, 1f)]
    [SerializeField] private float portalSpawnChance = 0.6f;
    
    [Tooltip("Minimum portal sayısı (şans ne olursa olsun en az bu kadar spawn olur)")]
    [SerializeField] private int minPortals = 2;

    [Header("Spawn Noktaları (Otomatik Bulunur)")]
    [SerializeField] private List<Transform> portalSpawnPoints = new List<Transform>();
    
    [Header("Bölge Bazlı Ayarlar")]
    [Tooltip("Belirli bölgelerde daha fazla portal spawn etmek için")]
    [SerializeField] private List<PortalZone> portalZones = new List<PortalZone>();

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0.5f, 0f, 1f, 0.5f);
    [SerializeField] private float gizmoRadius = 0.5f;

    private List<GameObject> spawnedPortals = new List<GameObject>();

    [System.Serializable]
    public class PortalZone
    {
        public string zoneName = "Zone";
        public Bounds bounds;
        [Range(0f, 2f)]
        public float spawnMultiplier = 1.5f; // Bu bölgede spawn şansı çarpanı
        public Color gizmoColor = new Color(0f, 1f, 0.5f, 0.3f);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        RefreshSpawnPoints();
    }

    /// <summary>
    /// Sahnedeki tüm portal spawn noktalarını bul
    /// </summary>
    public void RefreshSpawnPoints()
    {
        portalSpawnPoints.Clear();
        
        // "PortalPoint" tag'li objeleri bul
        GameObject[] points = GameObject.FindGameObjectsWithTag("PortalPoint");
        foreach (var point in points)
        {
            if (point != null)
                portalSpawnPoints.Add(point.transform);
        }

        Debug.Log($"[PortalSpawnManager] {portalSpawnPoints.Count} portal spawn noktası bulundu");
    }

    /// <summary>
    /// Portalları spawn et (GameManager tarafından çağrılır)
    /// </summary>
    public void SpawnPortals(GameObject portalPrefab)
    {
        if (portalPrefab == null)
        {
            Debug.LogWarning("[PortalSpawnManager] Portal prefab null!");
            return;
        }

        // Eski portalları temizle
        ClearPortals();

        if (portalSpawnPoints.Count == 0)
        {
            RefreshSpawnPoints();
            if (portalSpawnPoints.Count == 0)
            {
                Debug.LogWarning("[PortalSpawnManager] Spawn noktası bulunamadı!");
                return;
            }
        }

        // Her nokta için spawn şansını hesapla
        List<Transform> eligiblePoints = new List<Transform>();
        
        foreach (var point in portalSpawnPoints)
        {
            if (point == null) continue;

            float chance = portalSpawnChance;
            
            // Bölge çarpanı uygula
            foreach (var zone in portalZones)
            {
                if (zone.bounds.Contains(point.position))
                {
                    chance *= zone.spawnMultiplier;
                    break;
                }
            }

            // Şansa göre listeye ekle
            if (Random.value < chance)
            {
                eligiblePoints.Add(point);
            }
        }

        // Minimum portal sayısını garanti et
        if (eligiblePoints.Count < minPortals)
        {
            // Eksik kalanları rastgele ekle
            List<Transform> remaining = new List<Transform>(portalSpawnPoints);
            foreach (var added in eligiblePoints)
                remaining.Remove(added);

            // Shuffle remaining
            for (int i = 0; i < remaining.Count; i++)
            {
                int rnd = Random.Range(i, remaining.Count);
                var temp = remaining[i];
                remaining[i] = remaining[rnd];
                remaining[rnd] = temp;
            }

            int needed = minPortals - eligiblePoints.Count;
            for (int i = 0; i < needed && i < remaining.Count; i++)
            {
                eligiblePoints.Add(remaining[i]);
            }
        }

        // Maksimum sayıyı aşmasın
        if (eligiblePoints.Count > maxPortalsPerScene)
        {
            // Shuffle ve kes
            for (int i = 0; i < eligiblePoints.Count; i++)
            {
                int rnd = Random.Range(i, eligiblePoints.Count);
                var temp = eligiblePoints[i];
                eligiblePoints[i] = eligiblePoints[rnd];
                eligiblePoints[rnd] = temp;
            }
            eligiblePoints = eligiblePoints.GetRange(0, maxPortalsPerScene);
        }

        // Portalları spawn et
        foreach (var point in eligiblePoints)
        {
            GameObject portal = Instantiate(portalPrefab, point.position, Quaternion.identity, point);
            spawnedPortals.Add(portal);
        }

        Debug.Log($"[PortalSpawnManager] {spawnedPortals.Count} portal spawn edildi (Toplam nokta: {portalSpawnPoints.Count})");
    }

    /// <summary>
    /// Tüm portalları temizle
    /// </summary>
    public void ClearPortals()
    {
        foreach (var portal in spawnedPortals)
        {
            if (portal != null)
                Destroy(portal);
        }
        spawnedPortals.Clear();

        // Tag ile de temizle (eski sistemden kalanlar için)
        GameObject[] oldPortals = GameObject.FindGameObjectsWithTag("Portal");
        foreach (var portal in oldPortals)
            Destroy(portal);
    }

    /// <summary>
    /// Belirli bir bölgeye daha fazla portal ekle
    /// </summary>
    public void AddPortalZone(string name, Vector3 center, Vector3 size, float multiplier = 1.5f)
    {
        PortalZone zone = new PortalZone
        {
            zoneName = name,
            bounds = new Bounds(center, size),
            spawnMultiplier = multiplier
        };
        portalZones.Add(zone);
    }

    /// <summary>
    /// Portal spawn ayarlarını değiştir
    /// </summary>
    public void SetSpawnSettings(int maxPortals, float spawnChance, int minCount)
    {
        maxPortalsPerScene = maxPortals;
        portalSpawnChance = Mathf.Clamp01(spawnChance);
        minPortals = minCount;
    }

    /// <summary>
    /// Mevcut portal sayısını döndür
    /// </summary>
    public int GetActivePortalCount()
    {
        int count = 0;
        foreach (var portal in spawnedPortals)
        {
            if (portal != null)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Spawn noktası sayısını döndür
    /// </summary>
    public int GetSpawnPointCount()
    {
        return portalSpawnPoints.Count;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        // Portal spawn noktalarını göster
        Gizmos.color = gizmoColor;
        foreach (var point in portalSpawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, gizmoRadius);
                Gizmos.DrawIcon(point.position, "Portal", true);
            }
        }

        // Bölgeleri göster
        foreach (var zone in portalZones)
        {
            Gizmos.color = zone.gizmoColor;
            Gizmos.DrawWireCube(zone.bounds.center, zone.bounds.size);
            Handles.Label(zone.bounds.center, $"{zone.zoneName}\nx{zone.spawnMultiplier}");
        }
    }

    /// <summary>
    /// Editor'da spawn noktalarını manuel olarak yenile
    /// </summary>
    [ContextMenu("Spawn Noktalarını Yenile")]
    public void EditorRefreshSpawnPoints()
    {
        RefreshSpawnPoints();
        Debug.Log($"[Editor] {portalSpawnPoints.Count} spawn noktası bulundu");
    }

    /// <summary>
    /// Editor'da seçili objeleri spawn noktası olarak işaretle
    /// </summary>
    [ContextMenu("Seçilileri PortalPoint Yap")]
    public void EditorMarkSelectedAsPortalPoints()
    {
        foreach (var obj in Selection.gameObjects)
        {
            obj.tag = "PortalPoint";
            Debug.Log($"[Editor] {obj.name} PortalPoint olarak işaretlendi");
        }
    }
#endif
}

#if UNITY_EDITOR
/// <summary>
/// Portal spawn noktası oluşturmak için editor tool
/// </summary>
public class PortalPointCreator : EditorWindow
{
    [MenuItem("Tools/Portal/Portal Spawn Noktası Oluştur")]
    public static void CreatePortalPoint()
    {
        GameObject point = new GameObject("PortalPoint");
        point.tag = "PortalPoint";
        
        // Seçili obje varsa onun yanına oluştur
        if (Selection.activeGameObject != null)
        {
            point.transform.position = Selection.activeGameObject.transform.position + Vector3.right * 2f;
        }
        else
        {
            // Scene view'ın ortasına oluştur
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                point.transform.position = sceneView.camera.transform.position + sceneView.camera.transform.forward * 10f;
                point.transform.position = new Vector3(point.transform.position.x, point.transform.position.y, 0f);
            }
        }
        
        // Görsel için bir icon ekle (opsiyonel)
        var iconContent = EditorGUIUtility.IconContent("Portal");
        
        Selection.activeGameObject = point;
        Undo.RegisterCreatedObjectUndo(point, "Create Portal Point");
        
        Debug.Log($"[Editor] Portal spawn noktası oluşturuldu: {point.transform.position}");
    }

    [MenuItem("Tools/Portal/Seçilileri PortalPoint Olarak İşaretle")]
    public static void MarkSelectedAsPortalPoints()
    {
        int count = 0;
        foreach (var obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj, "Mark as PortalPoint");
            obj.tag = "PortalPoint";
            count++;
        }
        Debug.Log($"[Editor] {count} obje PortalPoint olarak işaretlendi");
    }

    [MenuItem("Tools/Portal/Sahnedeki Portal Noktalarını Göster")]
    public static void ShowAllPortalPoints()
    {
        GameObject[] points = GameObject.FindGameObjectsWithTag("PortalPoint");
        Debug.Log($"[Editor] Sahnede {points.Length} portal noktası var:");
        
        foreach (var point in points)
        {
            Debug.Log($"  - {point.name} @ {point.transform.position}");
        }

        // İlkini seç
        if (points.Length > 0)
        {
            Selection.objects = points;
        }
    }
}
#endif
