Shader "Unlit/HolographicGlitchV2"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0, 0.8, 1, 1)
        [MainTexture] _MainTex("Base Map", 2D) = "white" {}
        
        [Header(Glow Settings)]
        _GlowColor("Glow Color", Color) = (0, 1, 1, 1)
        _FresnelPower("Fresnel Power", Range(0.1, 10)) = 2.0
        
        [Header(Scanlines)]
        _ScanlineSpeed("Scanline Speed", Float) = 2.0
        _ScanlineDensity("Scanline Density", Float) = 50.0
        
        [Header(Glitch)]
        _GlitchIntensity("Glitch Intensity", Range(0, 1)) = 0.05
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "SurfaceType"="Transparent"
            "IgnoreProjector"="True"
            "UniversalMaterialType"="Unlit"
        }

        Pass
        {
            Name "ForwardLit"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half4 _GlowColor;
                half _FresnelPower;
                half _ScanlineSpeed;
                half _ScanlineDensity;
                half _GlitchIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float time = _Time.y * 10.0;
                float offset = sin(time + IN.positionOS.y * 10) * _GlitchIntensity * 0.1;
                IN.positionOS.x += offset;

                VertexPositionInputs positionInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = positionInputs.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS = normalInputs.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(positionInputs.positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 baseCol = _BaseColor;

                float3 normal = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);
                float NdotV = saturate(dot(normal, viewDir));
                float fresnel = pow(1.0 - NdotV, _FresnelPower);
                
                half3 finalColor = baseCol.rgb * texColor.rgb;
                finalColor += _GlowColor.rgb * fresnel;
                
                float scan = sin(IN.uv.y * _ScanlineDensity - _Time.y * _ScanlineSpeed);
                float scanLine = smoothstep(0.4, 0.6, scan);
                finalColor *= lerp(0.5, 1.0, scanLine);

                float finalAlpha = saturate(baseCol.a * 0.8 + fresnel * 0.4);
                
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
