Shader "Game/ToonShader"
{
    Properties
    {
        _MainTex        ("Albedo (RGB)", 2D)              = "white" {}
        _Color          ("Base Color", Color)             = (1,1,1,1)
        _ShadowColor    ("Shadow Color", Color)           = (0.3,0.3,0.5,1)
        _RimColor       ("Rim Color", Color)              = (0.8,0.9,1.0,1)
        _RimPower       ("Rim Power", Range(0.5,8))       = 3.0
        _Steps          ("Shading Steps", Range(1,4))     = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "TOON_LIT"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _ShadowColor;
                float4 _RimColor;
                float  _RimPower;
                float  _Steps;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos    : TEXCOORD2;
                float4 shadowCoord : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.worldPos    = posInputs.positionWS;
                OUT.worldNormal = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.shadowCoord = GetShadowCoord(posInputs);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Albedo
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _Color;

                // Main light + shadow
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 normal   = normalize(IN.worldNormal);
                float  NdotL    = dot(normal, mainLight.direction) * 0.5 + 0.5;

                // Quantize into N steps
                float stepSize  = 1.0 / _Steps;
                float quantized = floor(NdotL / stepSize) * stepSize;
                float light     = saturate(quantized * mainLight.shadowAttenuation);

                // Shadow blend
                half4 litColor  = lerp(_ShadowColor, half4(1,1,1,1), light);

                // Rim
                float3 viewDir  = normalize(GetWorldSpaceViewDir(IN.worldPos));
                float  rim      = 1.0 - saturate(dot(viewDir, normal));
                rim             = pow(rim, _RimPower) * light;
                half4  rimC     = _RimColor * rim;

                half4 finalColor = texColor * litColor + rimC;
                finalColor.a = 1.0;
                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
