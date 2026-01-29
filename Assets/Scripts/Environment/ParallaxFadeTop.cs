using UnityEngine;

/// <summary>
/// Sprite'ın üst kısmını gradient şeffaflık ile fade eder
/// Midground için kullanışlı - üst kısım şeffaf, alt kısım opak
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxFadeTop : MonoBehaviour
{
    [Header("Fade Ayarları")]
    [Range(0f, 1f)]
    [Tooltip("Fade başlangıç noktası (0 = alt, 1 = üst). 0.3 = alttan %30'dan başla")]
    public float fadeStart = 0.3f;
    
    [Range(0f, 1f)]
    [Tooltip("Fade bitiş noktası. 0.8 = üstten %20'ye kadar fade")]
    public float fadeEnd = 0.8f;
    
    [Range(0f, 1f)]
    [Tooltip("Minimum alpha (en şeffaf nokta)")]
    public float minAlpha = 0f;
    
    [Range(0f, 1f)]
    [Tooltip("Maximum alpha (en opak nokta)")]
    public float maxAlpha = 1f;

    private SpriteRenderer spriteRenderer;
    private Material fadeMaterial;

    private static readonly string ShaderCode = @"
Shader ""Custom/VerticalFade""
{
    Properties
    {
        _MainTex (""Sprite Texture"", 2D) = ""white"" {}
        _Color (""Tint"", Color) = (1,1,1,1)
        _FadeStart (""Fade Start"", Range(0,1)) = 0.3
        _FadeEnd (""Fade End"", Range(0,1)) = 0.8
        _MinAlpha (""Min Alpha"", Range(0,1)) = 0
        _MaxAlpha (""Max Alpha"", Range(0,1)) = 1
    }
    SubShader
    {
        Tags
        {
            ""Queue""=""Transparent""
            ""IgnoreProjector""=""True""
            ""RenderType""=""Transparent""
            ""PreviewType""=""Plane""
            ""CanUseSpriteAtlas""=""True""
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _FadeStart;
            float _FadeEnd;
            float _MinAlpha;
            float _MaxAlpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                
                // Dikey fade hesapla (UV.y: 0=alt, 1=üst)
                float fade = smoothstep(_FadeStart, _FadeEnd, i.uv.y);
                float alpha = lerp(_MaxAlpha, _MinAlpha, fade);
                
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}";

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        CreateFadeMaterial();
        ApplySettings();
    }

    void CreateFadeMaterial()
    {
        // Runtime'da shader oluştur
        Shader shader = Shader.Find("Custom/VerticalFade");
        
        if (shader == null)
        {
            // Shader yoksa Sprites/Default kullan ve sadece alpha ile çalış
            shader = Shader.Find("Sprites/Default");
            Debug.LogWarning("[ParallaxFadeTop] Custom shader bulunamadı. Manuel shader dosyası oluşturun.");
        }
        
        fadeMaterial = new Material(shader);
        fadeMaterial.mainTexture = spriteRenderer.sprite.texture;
        spriteRenderer.material = fadeMaterial;
    }

    void ApplySettings()
    {
        if (fadeMaterial == null) return;
        
        fadeMaterial.SetFloat("_FadeStart", fadeStart);
        fadeMaterial.SetFloat("_FadeEnd", fadeEnd);
        fadeMaterial.SetFloat("_MinAlpha", minAlpha);
        fadeMaterial.SetFloat("_MaxAlpha", maxAlpha);
    }

    void OnValidate()
    {
        if (Application.isPlaying && fadeMaterial != null)
        {
            ApplySettings();
        }
    }

    void OnDestroy()
    {
        if (fadeMaterial != null)
        {
            Destroy(fadeMaterial);
        }
    }
}
