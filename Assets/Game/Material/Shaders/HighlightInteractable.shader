Shader "Game/HighlightInteractable"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map (Albedo)", 2D) = "white" {}
        
        [Header(Highlight Settings)]
        [HDR]_HighlightColor ("Highlight Color", Color) = (0, 1, 0.5, 1)
        _HighlightPower ("Highlight Power (Rim)", Range(0.5, 8.0)) = 3.0
        _HighlightIntensity ("Highlight Intensity", Range(0, 10)) = 2.0
        
        [Header(Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.0
    }

    SubShader
    {
        // Указываем UniversalPipeline, чтобы Unity понимала, что это для URP
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "Unlit"
            // Не указываем LightMode, чтобы URP использовал проход по умолчанию (SRPDefaultUnlit)
            
            Cull Off // Отключаем отсечение задних граней, чтобы видеть объект со всех сторон
            ZWrite On
            Blend One Zero

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
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 positionWS   : TEXCOORD2;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float4 _HighlightColor;
                float _HighlightPower;
                float _HighlightIntensity;
                float _PulseSpeed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Базовый цвет текстуры
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                
                // Рассчитываем Rim Light без участия реальных источников света (гарантия что будет видно даже в темноте)
                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, _HighlightPower);
                
                // Пульсация
                float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                
                // Добавляем свечение к базовому цвету
                float3 glow = _HighlightColor.rgb * rim * _HighlightIntensity * pulse;
                
                return half4(texColor.rgb + glow, 1.0);
            }
            ENDHLSL
        }
    }
    
    // Фаллбек на стандартный URP Lit если что-то пойдет не так
    FallBack "Universal Render Pipeline/Lit"
}
