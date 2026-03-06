Shader "Game/HologramCage"
{
    Properties
    {
        [Header(Base Settings)]
        [MainColor] _BaseColor("Base Color", Color) = (0.0, 1.0, 1.0, 1.0)
        _MainTex("Base Map (RGB)", 2D) = "white" {}
        _Alpha("Master Alpha", Range(0.0, 1.0)) = 0.5
        
        [Header(Rim Lighting)]
        _RimColor("Rim Color", Color) = (0.5, 1.0, 1.0, 1.0)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 3.0
        
        [Header(Effects)]
        _ScanlineSpeed("Scanline Speed", Range(0.0, 10.0)) = 1.0
        _ScanlineIntensity("Scanline Intensity", Range(0.0, 1.0)) = 0.5
        _ScanlineDensity("Scanline Density", Range(1.0, 100.0)) = 50.0
        _BlinkSpeed("Blink Speed", Range(0.0, 5.0)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
        }

        Pass
        {
            Name "HOLOGRAM"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos    : TEXCOORD2;
                float2 uv          : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor;
                float4 _RimColor;
                float  _Alpha;
                float  _RimPower;
                float  _ScanlineSpeed;
                float  _ScanlineIntensity;
                float  _ScanlineDensity;
                float  _BlinkSpeed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.worldPos    = posInputs.positionWS;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normal = normalize(IN.worldNormal);
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.worldPos));
                
                // Texture + Base Color
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 baseRGB = _BaseColor.rgb * texColor.rgb;
                
                // Rim Effect (Fresnel)
                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, _RimPower);
                half3 rimRGB = _RimColor.rgb * rim;
                
                // Scanlines Effect
                float scanlines = sin(IN.worldPos.y * _ScanlineDensity + _Time.y * _ScanlineSpeed);
                scanlines = lerp(1.0, scanlines, _ScanlineIntensity);
                
                // Blink / Pulse
                float blink = sin(_Time.y * _BlinkSpeed) * 0.1 + 0.9;
                
                // Final Color Calculation
                half3 finalRGB = (baseRGB + rimRGB) * scanlines * blink;
                
                // Calculate Alpha: combined Rim and base alpha
                float finalAlpha = (rim + _Alpha) * blink * _BaseColor.a;
                
                return half4(finalRGB, saturate(finalAlpha));
            }
            ENDHLSL
        }
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
