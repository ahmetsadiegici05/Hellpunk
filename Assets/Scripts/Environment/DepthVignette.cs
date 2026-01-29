using UnityEngine;

/// <summary>
/// Vignette ve Derinlik Efekti
/// Ekran kenarlarını karartarak ve merkeze odaklanarak 2.5D his yaratır.
/// Sprite-based, post-processing gerektirmez.
/// </summary>
public class DepthVignette : MonoBehaviour
{
    [Header("Vignette Ayarları")]
    [SerializeField] private bool enableVignette = true;
    [SerializeField] private Color vignetteColor = new Color(0f, 0f, 0f, 0.4f);
    [SerializeField] private float vignetteIntensity = 0.5f;
    [SerializeField] private float vignetteSoftness = 0.5f;
    
    [Header("Atmosferik Renk")]
    [SerializeField] private bool enableAtmosphericTint = true;
    [SerializeField] private Color atmosphericColor = new Color(0.2f, 0.3f, 0.5f, 0.1f);
    
    [Header("Hareket Bazlı Efekt")]
    [SerializeField] private bool intensifyOnMovement = true;
    [SerializeField] private float movementIntensityBoost = 0.2f;
    
    // Internal
    private GameObject vignetteQuad;
    private Material vignetteMaterial;
    private Camera cam;
    private Transform player;
    private Vector3 lastPlayerPos;
    private float currentIntensity;
    
    private static readonly string VignetteShaderCode = @"
        Shader ""Hidden/SpriteVignette""
        {
            Properties
            {
                _MainTex (""Texture"", 2D) = ""white"" {}
                _Color (""Color"", Color) = (0,0,0,0.5)
                _Intensity (""Intensity"", Range(0,1)) = 0.5
                _Softness (""Softness"", Range(0,1)) = 0.5
            }
            SubShader
            {
                Tags { ""Queue""=""Overlay"" ""RenderType""=""Transparent"" }
                Blend SrcAlpha OneMinusSrcAlpha
                ZWrite Off
                Cull Off
                
                Pass
                {
                    CGPROGRAM
                    #pragma vertex vert
                    #pragma fragment frag
                    #include ""UnityCG.cginc""
                    
                    struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
                    struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };
                    
                    fixed4 _Color;
                    float _Intensity;
                    float _Softness;
                    
                    v2f vert (appdata v)
                    {
                        v2f o;
                        o.vertex = UnityObjectToClipPos(v.vertex);
                        o.uv = v.uv;
                        return o;
                    }
                    
                    fixed4 frag (v2f i) : SV_Target
                    {
                        float2 center = i.uv - 0.5;
                        float dist = length(center) * 2.0;
                        float vignette = smoothstep(1.0 - _Softness, 1.0, dist);
                        vignette *= _Intensity;
                        return fixed4(_Color.rgb, vignette * _Color.a);
                    }
                    ENDCG
                }
            }
        }
    ";
    
    private void Start()
    {
        cam = Camera.main;
        if (cam == null)
        {
            enabled = false;
            return;
        }
        
        // Player bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            lastPlayerPos = player.position;
        }
        
        if (enableVignette)
        {
            CreateVignetteOverlay();
        }
        
        currentIntensity = vignetteIntensity;
    }
    
    private void CreateVignetteOverlay()
    {
        // Screen-space quad oluştur
        vignetteQuad = new GameObject("VignetteOverlay");
        vignetteQuad.transform.SetParent(cam.transform);
        vignetteQuad.transform.localPosition = new Vector3(0, 0, cam.nearClipPlane + 0.1f);
        
        // Quad mesh
        MeshFilter mf = vignetteQuad.AddComponent<MeshFilter>();
        MeshRenderer mr = vignetteQuad.AddComponent<MeshRenderer>();
        
        // Basit quad mesh
        Mesh mesh = new Mesh();
        float size = 10f; // Kamerayı kaplayacak kadar büyük
        mesh.vertices = new Vector3[]
        {
            new Vector3(-size, -size, 0),
            new Vector3(size, -size, 0),
            new Vector3(size, size, 0),
            new Vector3(-size, size, 0)
        };
        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        mesh.RecalculateNormals();
        mf.mesh = mesh;
        
        // Material - basit gradient texture ile
        vignetteMaterial = CreateVignetteMaterial();
        mr.material = vignetteMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        
        UpdateVignetteScale();
    }
    
    private Material CreateVignetteMaterial()
    {
        // Vignette texture oluştur
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                float vignette = Mathf.SmoothStep(1f - vignetteSoftness, 1f, dist);
                vignette *= vignetteIntensity;
                
                pixels[y * size + x] = new Color(
                    vignetteColor.r,
                    vignetteColor.g,
                    vignetteColor.b,
                    vignette * vignetteColor.a
                );
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        
        // Unlit transparent material
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.mainTexture = tex;
        mat.color = Color.white;
        mat.renderQueue = 4000; // Overlay
        
        return mat;
    }
    
    private void UpdateVignetteScale()
    {
        if (vignetteQuad == null || cam == null) return;
        
        // Kamerayı kaplayacak şekilde scale
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        
        vignetteQuad.transform.localScale = new Vector3(width / 10f, height / 10f, 1f);
    }
    
    private void LateUpdate()
    {
        if (!enableVignette || vignetteQuad == null) return;
        
        // Hareket bazlı intensity
        if (intensifyOnMovement && player != null)
        {
            float velocity = (player.position - lastPlayerPos).magnitude / Time.deltaTime;
            lastPlayerPos = player.position;
            
            float targetIntensity = vignetteIntensity;
            if (velocity > 3f)
            {
                targetIntensity += movementIntensityBoost * Mathf.Clamp01(velocity / 15f);
            }
            
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * 3f);
            
            // Material güncelle (basit versiyon - alpha ile)
            if (vignetteMaterial != null)
            {
                Color c = vignetteMaterial.color;
                c.a = currentIntensity;
                vignetteMaterial.color = c;
            }
        }
        
        // Scale'i güncelle (zoom değişirse)
        UpdateVignetteScale();
    }
    
    /// <summary>
    /// Vignette'i aç/kapat
    /// </summary>
    public void SetVignetteEnabled(bool enabled)
    {
        enableVignette = enabled;
        if (vignetteQuad != null)
        {
            vignetteQuad.SetActive(enabled);
        }
    }
    
    /// <summary>
    /// Yoğunluğu ayarla
    /// </summary>
    public void SetIntensity(float intensity)
    {
        vignetteIntensity = Mathf.Clamp01(intensity);
        currentIntensity = vignetteIntensity;
    }
    
    private void OnDestroy()
    {
        if (vignetteMaterial != null && vignetteMaterial.mainTexture != null)
        {
            Destroy(vignetteMaterial.mainTexture);
            Destroy(vignetteMaterial);
        }
    }
}
