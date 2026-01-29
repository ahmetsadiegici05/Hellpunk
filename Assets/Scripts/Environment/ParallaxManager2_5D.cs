using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 2.5D Parallax Yöneticisi
/// Sahnedeki tüm parallax katmanlarını organize eder.
/// Tek bir objede kullan, child'lar otomatik ayarlanır.
/// </summary>
public class ParallaxManager2_5D : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayerConfig
    {
        public string layerName = "Layer";
        public Transform layerTransform;
        
        [Header("Derinlik")]
        [Tooltip("Z derinliği - büyük = uzak")]
        public float depth = 10f;
        
        [Header("Parallax")]
        [Range(0f, 1f)] public float parallaxX = 0.5f;
        [Range(0f, 1f)] public float parallaxY = 0.2f;
        
        [Header("Görsel")]
        public bool applyTint = false;
        [Range(0f, 1f)] public float tintAmount = 0f;
        
        [HideInInspector] public Vector3 startPosition;
    }
    
    [Header("Katman Ayarları")]
    [SerializeField] private List<ParallaxLayerConfig> layers = new List<ParallaxLayerConfig>();
    
    [Header("Global Ayarlar")]
    [SerializeField] private Color atmosphereTint = new Color(0.7f, 0.8f, 0.95f, 1f);
    [SerializeField] private float globalParallaxScale = 1f;
    
    [Header("Hızlı Kurulum")]
    [SerializeField] private bool autoConfigureChildren = false;
    [SerializeField] private float depthStep = 5f; // Her katman arası mesafe
    
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    
    private void Start()
    {
        cameraTransform = Camera.main?.transform;
        if (cameraTransform == null)
        {
            Debug.LogError("ParallaxManager2_5D: Main Camera bulunamadı!");
            enabled = false;
            return;
        }
        
        lastCameraPosition = cameraTransform.position;
        
        // Otomatik child konfigürasyonu
        if (autoConfigureChildren)
        {
            AutoConfigureFromChildren();
        }
        
        // Başlangıç pozisyonlarını kaydet ve tint uygula
        foreach (var layer in layers)
        {
            if (layer.layerTransform != null)
            {
                layer.startPosition = layer.layerTransform.position;
                
                // Z pozisyonunu ayarla
                Vector3 pos = layer.layerTransform.position;
                pos.z = layer.depth;
                layer.layerTransform.position = pos;
                
                // Tint uygula
                if (layer.applyTint)
                {
                    ApplyTintToLayer(layer);
                }
            }
        }
    }
    
    private void LateUpdate()
    {
        if (cameraTransform == null) return;
        
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;
        
        foreach (var layer in layers)
        {
            if (layer.layerTransform == null) continue;
            
            float moveX = deltaMovement.x * layer.parallaxX * globalParallaxScale;
            float moveY = deltaMovement.y * layer.parallaxY * globalParallaxScale;
            
            layer.layerTransform.position += new Vector3(moveX, moveY, 0);
        }
        
        lastCameraPosition = cameraTransform.position;
    }
    
    private void AutoConfigureFromChildren()
    {
        layers.Clear();
        
        int childIndex = 0;
        foreach (Transform child in transform)
        {
            var config = new ParallaxLayerConfig
            {
                layerName = child.name,
                layerTransform = child,
                depth = childIndex * depthStep,
                parallaxX = CalculateParallaxFromDepth(childIndex * depthStep),
                parallaxY = CalculateParallaxFromDepth(childIndex * depthStep) * 0.5f,
                applyTint = true,
                tintAmount = childIndex * 0.15f
            };
            
            layers.Add(config);
            childIndex++;
        }
        
        Debug.Log($"ParallaxManager2_5D: {childIndex} katman otomatik yapılandırıldı.");
    }
    
    private float CalculateParallaxFromDepth(float depth)
    {
        // depth=0 → parallax≈1 (ön plan, kamerayla hareket)
        // depth yüksek → parallax→0 (arka plan, yavaş)
        return 1f - (1f / (1f + depth * 0.1f));
    }
    
    private void ApplyTintToLayer(ParallaxLayerConfig layer)
    {
        var renderers = layer.layerTransform.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            sr.color = Color.Lerp(Color.white, atmosphereTint, layer.tintAmount);
        }
    }
    
    /// <summary>
    /// Yeni katman ekle (runtime)
    /// </summary>
    public void AddLayer(Transform layerTransform, float depth, float parallaxX = 0.5f, float parallaxY = 0.2f)
    {
        var config = new ParallaxLayerConfig
        {
            layerName = layerTransform.name,
            layerTransform = layerTransform,
            depth = depth,
            parallaxX = parallaxX,
            parallaxY = parallaxY,
            startPosition = layerTransform.position
        };
        
        Vector3 pos = layerTransform.position;
        pos.z = depth;
        layerTransform.position = pos;
        
        layers.Add(config);
    }
    
    /// <summary>
    /// Inspector'da preset uygula
    /// </summary>
    [ContextMenu("Apply Preset: 4-Layer Platformer")]
    private void ApplyPlatformerPreset()
    {
        // Tipik platformer katman yapısı
        Debug.Log("4-Layer Platformer preset için child objeler ekleyin:\n" +
                  "1. Sky (depth: 50, parallax: 0.05)\n" +
                  "2. Mountains (depth: 30, parallax: 0.2)\n" +
                  "3. Trees (depth: 15, parallax: 0.5)\n" +
                  "4. Bushes (depth: 5, parallax: 0.8)");
    }
    
    private void OnDrawGizmosSelected()
    {
        // Her katmanın derinliğini göster
        foreach (var layer in layers)
        {
            if (layer.layerTransform == null) continue;
            
            float t = layer.depth / 50f;
            Gizmos.color = new Color(0, 1 - t, t, 0.5f);
            
            Vector3 pos = layer.layerTransform.position;
            Gizmos.DrawWireSphere(pos, 0.5f);
            
            // Z çizgisi
            Vector3 depthPos = pos;
            depthPos.z = layer.depth;
            Gizmos.DrawLine(pos, depthPos);
        }
    }
}
