using UnityEngine;

/// <summary>
/// Karanlık orman efekti - Kamera post-processing yöntemi.
/// Oyuncunun etrafında daire şeklinde görüş alanı oluşturur.
/// F10 ile test edebilirsin.
/// </summary>
[RequireComponent(typeof(Camera))]
public class DarkVisionEffect : MonoBehaviour
{
    public static DarkVisionEffect Instance { get; private set; }
    
    [Header("Görüş Ayarları")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float visionRadius = 0.15f; // Ekran yüzdesi olarak
    [Range(0f, 0.2f)]
    [SerializeField] private float softEdge = 0.05f;
    
    [Header("Karanlık Rengi")]
    [SerializeField] private Color darkColor = new Color(0.02f, 0.01f, 0.03f, 1f);
    
    [Header("Geçiş")]
    [SerializeField] private float fadeSpeed = 2f;
    
    // State
    private bool isActive = false;
    private float currentIntensity = 0f;
    private float targetIntensity = 0f;
    
    // Material
    private Material darkMaterial;
    private Transform playerTransform;
    
    // Shader property IDs
    private static readonly int PlayerPosID = Shader.PropertyToID("_PlayerPos");
    private static readonly int RadiusID = Shader.PropertyToID("_Radius");
    private static readonly int SoftEdgeID = Shader.PropertyToID("_SoftEdge");
    private static readonly int IntensityID = Shader.PropertyToID("_Intensity");
    private static readonly int DarkColorID = Shader.PropertyToID("_DarkColor");
    
    public bool IsActive => isActive;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[DarkVision] Instance oluşturuldu!");
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }
        
        CreateMaterial();
    }
    
    private void Start()
    {
        FindPlayer();
    }
    
    private void CreateMaterial()
    {
        // Inline shader - harici dosyaya gerek yok
        string shaderCode = @"
Shader ""Hidden/DarkVisionShader""
{
    Properties
    {
        _MainTex (""Texture"", 2D) = ""white"" {}
        _PlayerPos (""Player Position"", Vector) = (0.5, 0.5, 0, 0)
        _Radius (""Vision Radius"", Float) = 0.15
        _SoftEdge (""Soft Edge"", Float) = 0.05
        _Intensity (""Intensity"", Float) = 0
        _DarkColor (""Dark Color"", Color) = (0.02, 0.01, 0.03, 1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; };
            
            sampler2D _MainTex;
            float2 _PlayerPos;
            float _Radius;
            float _SoftEdge;
            float _Intensity;
            float4 _DarkColor;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Aspect ratio düzeltmesi
                float2 uv = i.uv;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                uv.x *= aspect;
                float2 playerPos = _PlayerPos;
                playerPos.x *= aspect;
                
                // Oyuncuya mesafe
                float dist = distance(uv, playerPos);
                
                // Görüş alanı maskesi
                float innerRadius = _Radius;
                float outerRadius = _Radius + _SoftEdge;
                float mask = smoothstep(innerRadius, outerRadius, dist);
                
                // Karanlık uygula
                fixed4 darkCol = _DarkColor;
                col = lerp(col, darkCol, mask * _Intensity);
                
                return col;
            }
            ENDCG
        }
    }
}";
        
        // Shader'ı runtime'da oluştur
        Shader shader = Shader.Find("Hidden/DarkVisionShader");
        
        if (shader == null)
        {
            // Shader yoksa basit bir fallback material kullan
            // Bu durumda shader dosyasını oluşturmamız gerekecek
            Debug.LogWarning("[DarkVision] Shader bulunamadı, oluşturuluyor...");
            darkMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        }
        else
        {
            darkMaterial = new Material(shader);
        }
        
        // Default değerler
        if (darkMaterial != null)
        {
            darkMaterial.SetVector(PlayerPosID, new Vector4(0.5f, 0.5f, 0, 0));
            darkMaterial.SetFloat(RadiusID, visionRadius);
            darkMaterial.SetFloat(SoftEdgeID, softEdge);
            darkMaterial.SetFloat(IntensityID, 0f);
            darkMaterial.SetColor(DarkColorID, darkColor);
        }
    }
    
    private void FindPlayer()
    {
        if (playerTransform != null) return;
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            Debug.Log("[DarkVision] Player bulundu!");
        }
    }
    
    private void Update()
    {
        // F10 ile test
        if (Input.GetKeyDown(KeyCode.F10))
        {
            if (isActive)
                Deactivate();
            else
                Activate();
            
            Debug.Log($"[DarkVision] F10 basıldı - Aktif: {isActive}");
        }
        
        // Intensity geçişi
        if (Mathf.Abs(currentIntensity - targetIntensity) > 0.001f)
        {
            currentIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, fadeSpeed * Time.unscaledDeltaTime);
            
            if (darkMaterial != null)
                darkMaterial.SetFloat(IntensityID, currentIntensity);
        }
    }
    
    private void LateUpdate()
    {
        if (!isActive || darkMaterial == null) return;
        
        FindPlayer();
        
        if (playerTransform != null)
        {
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                Vector3 screenPos = cam.WorldToScreenPoint(playerTransform.position);
                Vector2 normalizedPos = new Vector2(screenPos.x / Screen.width, screenPos.y / Screen.height);
                darkMaterial.SetVector(PlayerPosID, new Vector4(normalizedPos.x, normalizedPos.y, 0, 0));
            }
        }
        
        // Parametreleri güncelle
        darkMaterial.SetFloat(RadiusID, visionRadius);
        darkMaterial.SetFloat(SoftEdgeID, softEdge);
        darkMaterial.SetColor(DarkColorID, darkColor);
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (darkMaterial != null && currentIntensity > 0.001f)
        {
            Graphics.Blit(source, destination, darkMaterial);
        }
        else
        {
            Graphics.Blit(source, destination);
        }
    }
    
    /// <summary>
    /// Karanlık efektini aktifleştir
    /// </summary>
    public void Activate()
    {
        isActive = true;
        targetIntensity = 1f;
        Debug.Log("[DarkVision] Efekt AKTİF!");
    }
    
    /// <summary>
    /// Karanlık efektini deaktifleştir
    /// </summary>
    public void Deactivate()
    {
        isActive = false;
        targetIntensity = 0f;
        Debug.Log("[DarkVision] Efekt KAPALI!");
    }
    
    /// <summary>
    /// Görüş yarıçapını ayarla (0.05 - 0.5 arası)
    /// </summary>
    public void SetVisionRadius(float radius)
    {
        visionRadius = Mathf.Clamp(radius, 0.05f, 0.5f);
    }
    
    private void OnDestroy()
    {
        if (darkMaterial != null)
        {
            DestroyImmediate(darkMaterial);
        }
        
        if (Instance == this)
            Instance = null;
    }
}
