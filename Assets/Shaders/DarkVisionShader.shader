Shader "Hidden/DarkVisionShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PlayerPos ("Player Position", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Vision Radius", Float) = 0.15
        _SoftEdge ("Soft Edge", Float) = 0.05
        _Intensity ("Intensity", Float) = 0
        _DarkColor ("Dark Color", Color) = (0.02, 0.01, 0.03, 1)
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        
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
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
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
                // Orijinal renk
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Aspect ratio düzeltmesi (daire düzgün görünsün)
                float2 uv = i.uv;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                uv.x *= aspect;
                
                float2 playerPos = _PlayerPos;
                playerPos.x *= aspect;
                
                // Oyuncuya olan mesafe
                float dist = distance(uv, playerPos);
                
                // Görüş alanı maskesi - yumuşak kenar
                float innerRadius = _Radius;
                float outerRadius = _Radius + _SoftEdge;
                float mask = smoothstep(innerRadius, outerRadius, dist);
                
                // Karanlık rengi uygula
                fixed4 darkCol = _DarkColor;
                col = lerp(col, darkCol, mask * _Intensity);
                
                return col;
            }
            ENDCG
        }
    }
}
