using UnityEngine;

/// <summary>
/// Ultra basit ve stabil Parallax sistemi
/// Titreme yok, spawn yok - sadece pozisyon offset
/// </summary>
public class SimpleParallax : MonoBehaviour
{
    [Header("Parallax")]
    [Range(0f, 1f)]
    [Tooltip("0 = kamerayla tam hareket, 1 = hiç hareket etmez")]
    public float parallaxStrength = 0.5f;
    
    [Header("Görünüm")]
    public int sortingOrder = -50;
    
    [Header("Sabit Y")]
    [Tooltip("Y pozisyonunu sabit tut (zıplamada titreme olmaz)")]
    public bool lockY = true;

    private Camera mainCamera;
    private Vector3 initialPosition;
    private float initialCameraX;
    private SpriteRenderer[] allRenderers;

    void Awake()
    {
        // Başlangıç pozisyonlarını AWAKE'de kaydet
        initialPosition = transform.position;
    }

    void Start()
    {
        mainCamera = Camera.main;
        initialCameraX = mainCamera.transform.position.x;
        
        // Tüm sprite renderer'lara sorting uygula
        allRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in allRenderers)
        {
            sr.sortingOrder = sortingOrder;
        }
    }

    void LateUpdate()
    {
        if (mainCamera == null) return;

        // Kameranın başlangıçtan ne kadar hareket ettiğini hesapla
        float cameraDeltaX = mainCamera.transform.position.x - initialCameraX;
        
        // Parallax: kamera hareket ettikçe, bu obje DAHA AZ hareket eder
        float parallaxOffsetX = cameraDeltaX * parallaxStrength;
        
        // Yeni pozisyon
        float newX = initialPosition.x + parallaxOffsetX;
        float newY = lockY ? initialPosition.y : (initialPosition.y + (mainCamera.transform.position.y - initialCameraX) * parallaxStrength * 0.5f);
        
        transform.position = new Vector3(newX, newY, initialPosition.z);
    }
}
