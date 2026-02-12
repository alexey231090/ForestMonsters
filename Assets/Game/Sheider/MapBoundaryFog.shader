Shader "Custom/MapBoundaryFog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Fog Color", Color) = (1,1,1,1)
        _InnerRadius ("Inner Radius", Range(0.0, 1.0)) = 0.5
        _Softness ("Edge Softness", Range(0.0, 1.0)) = 0.2
        _SquareShape ("Square Shape", Range(0.0, 1.0)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _InnerRadius;
            float _Softness;
            float _SquareShape;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Смещаемся в центр (0,0) и масштабируем в диапазон [-1, 1]
                float2 d = abs(i.uv * 2.0 - 1.0);
                
                // Вычисляем дистанцию: для круга или для квадрата
                float distCircle = length(d);
                float distSquare = max(d.x, d.y);
                
                float dist = lerp(distCircle, distSquare, _SquareShape);

                // Вычисляем прозрачность (smoothstep для мягкого перехода)
                float alpha = smoothstep(_InnerRadius, _InnerRadius + _Softness, dist);
                
                fixed4 col = _Color;
                col.a *= alpha;
                
                return col;
            }
            ENDCG
        }
    }
}
