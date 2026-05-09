Shader "Custom/TileGlow"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Header(Glow Settings)]
        _GlowColor ("Glow Color", Color) = (0,1,0,1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 0.5
        _BorderWidth ("Border Width", Range(0,0.5)) = 0.05
        [Toggle(_USE_FULL_FILL)] _UseFullFill ("Use Full Fill", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Defines for Shadows and Lighting
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fog

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
                float _UseFullFill;
                float4 _BaseMap_ST; 
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;

                // Lighting with Shadows
                float3 normalWS = normalize(input.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                
                Light mainLight = GetMainLight(shadowCoord);
                
                // Diffuse Lighting (Main)
                float NdotL = saturate(dot(normalWS, mainLight.direction));
                float3 lighting = mainLight.color * (NdotL * mainLight.shadowAttenuation);
                
                // Additional Lights
                #if defined(_ADDITIONAL_LIGHTS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < pixelLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, input.positionWS, shadowCoord);
                    lighting += light.color * (saturate(dot(normalWS, light.direction)) * light.distanceAttenuation * light.shadowAttenuation);
                }
                #endif

                // Ambient lighting (SH)
                float3 ambient = SampleSH(normalWS);

                float3 finalRGB = texColor.rgb * (lighting + ambient);

                // --- Top Face Outline Glow Logic ---
                // Only apply glow to faces pointing upwards
                if (normalWS.y > 0.5)
                {
                    float2 centeredUV = abs(input.uv - 0.5) * 2.0;
                    float maxDist = max(centeredUV.x, centeredUV.y);
                    
                    float isBorder = (maxDist > (1.0 - _BorderWidth)) ? 1.0 : 0.0;
                    float glowFactor = lerp(isBorder, 1.0, _UseFullFill);
                    
                    // Blend/stain the underlying texture using lerp based on the glow color's alpha channel.
                    // This ensures the highlight is highly visible and retains saturation even on pure white path surfaces,
                    // rather than washing out to faint white.
                    float blendAlpha = saturate(_GlowColor.a * glowFactor);
                    // Softer blend: mix the glow color rather than replacing the texture color entirely
                    finalRGB = lerp(finalRGB, finalRGB + (_GlowColor.rgb * _GlowIntensity), blendAlpha * 0.5);
                }

                return half4(finalRGB, texColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                // Standard URP Shadow Bias handling
                // Note: _MainLightPosition is not always available in ShadowCaster pass in older URP
                // We use ApplyShadowBias with GetMainLight().direction as fallback if needed
                // But typically _MainLightPosition is defined in Input.hlsl
                float3 lightDir = _MainLightPosition.xyz;
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDir));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                output.positionCS = positionCS;
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
