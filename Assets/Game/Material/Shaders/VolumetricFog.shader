Shader "Unlit/VolumetricFog"
{
    Properties
    {
        [MainColor] _BaseColor("Fog Color", Color) = (0.8, 0.9, 1, 0.5)
        _FogDensity("Fog Density", Range(0, 5)) = 1.0
        _NoiseScale("Noise Scale", Float) = 2.0
        _Speed("Animation Speed", Vector) = (0.1, 0.05, 0, 0)
        
        [Header(Softness)]
        _EdgeSoftness("Edge Softness", Range(0.1, 10)) = 2.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True"
        }

        Pass
        {
            Name "FogPass"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _FogDensity;
                float _NoiseScale;
                float4 _Speed;
                float _EdgeSoftness;
            CBUFFER_END

            // Простая функция шума на основе синусов
            float Noise(float2 p)
            {
                p = p * _NoiseScale;
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            float SmoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Noise(i);
                float b = Noise(i + float2(1.0, 0.0));
                float c = Noise(i + float2(0.0, 1.0));
                float d = Noise(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.uv = IN.uv;
                OUT.normalWS = normalInputs.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Анимированный шум (два слоя для эффекта пара)
                float2 uv1 = IN.uv + _Time.y * _Speed.xy;
                float2 uv2 = IN.uv * 1.5 - _Time.y * _Speed.xy * 0.7;
                
                float n1 = SmoothNoise(uv1 * 4.0);
                float n2 = SmoothNoise(uv2 * 6.0);
                float combinedNoise = (n1 + n2) * 0.5;

                // Мягкость краев через Fresnel (чтобы туман исчезал на гранях куба)
                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(normal, viewDir)), _EdgeSoftness);

                // Финальный цвет и альфа
                half4 color = _BaseColor;
                color.rgb += combinedNoise * 0.2; // Добавляем текстурность
                
                float alpha = combinedNoise * _FogDensity * fresnel * _BaseColor.a;

                return half4(color.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
