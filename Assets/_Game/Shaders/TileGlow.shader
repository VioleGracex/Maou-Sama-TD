Shader "MaouSamaTD/TileGlow"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Header(Glow Settings)]
        _GlowColor ("Glow Color", Color) = (0,1,0,1)
        _GlowIntensity ("Glow Intensity", Float) = 1
        _BorderWidth ("Border Width", Range(0,0.5)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Core URP Includes
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD1;
                float3 normalWS     : TEXCOORD2;
                float2 uv           : TEXCOORD0;
            };

            // Material Properties
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GlowColor;
                float _GlowIntensity;
                float _BorderWidth;
                float4 _BaseMap_ST; 
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // Basic Lighting
                Light mainLight = GetMainLight();
                float3 N = normalize(input.normalWS);
                float3 L = normalize(mainLight.direction);
                float NdotL = max(0, dot(N, L));
                float3 lighting = mainLight.color * NdotL;
                
                // Simple Ambient (fallback if Environment lighting is tricky in custom shader without complex setup)
                // We'll add a small constant ambient to prevent pitch black shadow areas
                lighting += float3(0.1, 0.1, 0.1); 

                float3 finalRGB = texColor.rgb * lighting;

                // --- Top Face Outline Glow Logic ---
                // World Up is (0,1,0)
                if (N.y > 0.9)
                {
                    // Calculate distance from center (0.5, 0.5)
                    // Assuming Plane UVs (0..1)
                    float2 centeredUV = abs(input.uv - 0.5) * 2.0;
                    float maxDist = max(centeredUV.x, centeredUV.y);
                    
                    if (maxDist > (1.0 - _BorderWidth))
                    {
                        finalRGB += (_GlowColor.rgb * _GlowIntensity);
                    }
                }

                return half4(finalRGB, texColor.a);
            }
            ENDHLSL
        }
    }
}
