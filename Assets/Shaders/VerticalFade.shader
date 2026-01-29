Shader "Custom/VerticalFade"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _FadeStart ("Fade Start", Range(0,1)) = 0.3
        _FadeEnd ("Fade End", Range(0,1)) = 0.8
        _MinAlpha ("Min Alpha", Range(0,1)) = 0
        _MaxAlpha ("Max Alpha", Range(0,1)) = 1
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
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
            #include "UnityCG.cginc"

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
                // fadeStart'tan fadeEnd'e kadar smooth geçiş
                float fade = smoothstep(_FadeStart, _FadeEnd, i.uv.y);
                float alpha = lerp(_MaxAlpha, _MinAlpha, fade);
                
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
