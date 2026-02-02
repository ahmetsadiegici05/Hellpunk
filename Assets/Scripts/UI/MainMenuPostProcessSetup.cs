using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// MainMenu sahnesinde Post-Processing'in düzgün çalışmasını sağlar.
/// Bu script Main Camera'ya eklenir.
/// </summary>
[RequireComponent(typeof(Camera))]
public class MainMenuPostProcessSetup : MonoBehaviour
{
    [Header("Post Processing Settings")]
    [SerializeField] private bool enablePostProcessing = true;
    [SerializeField] private bool enableAntiAliasing = true;
    [SerializeField] private AntialiasingMode antiAliasingMode = AntialiasingMode.FastApproximateAntialiasing;
    
    private Camera mainCamera;
    private UniversalAdditionalCameraData cameraData;
    
    private void Awake()
    {
        mainCamera = GetComponent<Camera>();
        SetupPostProcessing();
    }
    
    private void SetupPostProcessing()
    {
        // Universal Additional Camera Data bileşenini al veya ekle
        cameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
        
        if (cameraData == null)
        {
            cameraData = mainCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
            Debug.Log("[MainMenuPostProcessSetup] UniversalAdditionalCameraData eklendi.");
        }
        
        // Post Processing'i aktif et
        cameraData.renderPostProcessing = enablePostProcessing;
        
        // Anti-aliasing ayarları
        if (enableAntiAliasing)
        {
            cameraData.antialiasing = antiAliasingMode;
        }
        else
        {
            cameraData.antialiasing = AntialiasingMode.None;
        }
        
        Debug.Log($"[MainMenuPostProcessSetup] Post Processing: {enablePostProcessing}");
    }
    
    /// <summary>
    /// Runtime'da post processing'i toggle etmek için
    /// </summary>
    public void SetPostProcessing(bool enabled)
    {
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = enabled;
        }
    }
}
