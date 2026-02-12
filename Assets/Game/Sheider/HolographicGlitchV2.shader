Shader "Unlit/HolographicGlitchV2"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex("Base Map", 2D) = "white" {}
        
        [Header(Glitch Settings)]
        _GlitchIntensity("Glitch Intensity", Range(0, 1)) = 0.1
        _GlitchSpeed("Glitch Speed", Float) = 10.0
        _GlitchFrequency("Glitch Frequency", Float) = 5.0
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
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half _GlitchIntensity;
                half _GlitchSpeed;
                half _GlitchFrequency;
            CBUFFER_END

            // Simple random function
            float random(float2 st)
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 positionOS = IN.positionOS.xyz;
                
                // Glitch effect on vertices
                float time = _Time.y * _GlitchSpeed;
                float glitchTrigger = step(0.9, random(float2(floor(time), 0)));
                
                if (glitchTrigger > 0.5)
                {
                    float noise = random(float2(positionOS.y * _GlitchFrequency, time));
                    float offset = (noise - 0.5) * 2.0 * _GlitchIntensity * 0.5;
                    positionOS.x += offset;
                }

                VertexPositionInputs positionInputs = GetVertexPositionInputs(positionOS);
                OUT.positionHCS = positionInputs.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample texture with possible UV glitch
                float2 uv = IN.uv;
                
                // UV glitch effect
                float time = _Time.y * _GlitchSpeed;
                float uvGlitchTrigger = step(0.85, random(float2(floor(time * 2), 1)));
                
                if (uvGlitchTrigger > 0.5)
                {
                    float noiseY = random(float2(floor(IN.uv.y * 20), time));
                    uv.x += (noiseY - 0.5) * _GlitchIntensity * 0.2;
                }
                
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                
                // Keep original color, just apply alpha
                half4 finalColor = texColor * _BaseColor;
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}
