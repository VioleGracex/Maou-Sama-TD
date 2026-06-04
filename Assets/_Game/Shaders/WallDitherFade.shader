Shader "Custom/WallDitherFade"
{
    Properties
    {
        _BaseMap ("Base Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Header(Dynamic Height Settings)]
        _MinHeight ("Minimum Height Multiplier", Range(0.01, 1)) = 0.2
        _PivotY ("Mesh Bottom Y (Pivot Offset)", Float) = 0.5
        _TopDarkness ("Top Face Darkness", Range(0, 1)) = 0.0
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
            #pragma multi_compile _ _FORWARD_PLUS
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
                float _TopDarkness;
                float _MinHeight;
                float _PivotY;
                float4 _BaseMap_ST; 
            CBUFFER_END
            
            float4 _GlobalMapCenter;

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Coordinate-based Dynamic Height Squashing
                float3 objectCenterWS = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
                float3 centerToWall = objectCenterWS - _GlobalMapCenter.xyz;
                
                // Get the camera's forward view direction
                // unity_CameraToWorld[2].xyz is the backward vector in right-handed view space, so - is forward
                float3 viewForward = -UNITY_MATRIX_V[2].xyz;
                
                // Dot product < 0 means the wall is in front of the map center relative to the camera
                float alignment = dot(centerToWall, viewForward);
                
                // Smoothly squash walls that are in front (alignment < -0.1)
                float fade = saturate((alignment + 1.0) / 1.0);
                float heightMult = lerp(_MinHeight, 1.0, fade);
                
                input.positionOS.y = (input.positionOS.y + _PivotY) * heightMult - _PivotY;

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
                
                // Wrapped lighting
                float NdotL = dot(normalWS, mainLight.direction);
                float wrappedNdotL = saturate((NdotL * 0.5) + 0.5);
                
                float directLight = wrappedNdotL * mainLight.shadowAttenuation;
                float3 lighting = mainLight.color * directLight;
                
                // Additional Lights (Point Lights)
                #if defined(_ADDITIONAL_LIGHTS) || defined(_FORWARD_PLUS)
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint i = 0u; i < pixelLightCount; ++i)
                {
                    Light light = GetAdditionalLight(i, input.positionWS, half4(1,1,1,1));
                    lighting += light.color * (saturate(dot(normalWS, light.direction)) * light.distanceAttenuation * light.shadowAttenuation);
                }
                #endif

                // Ambient lighting (SH)
                float3 ambient = SampleSH(normalWS);

                // Combine direct and ambient lighting
                float3 finalLighting = lighting + ambient;
                float3 finalRGB = texColor.rgb * finalLighting;

                // Darken the top face to simulate a closed, unlit roof
                if (normalWS.y > 0.5)
                {
                    finalRGB *= _TopDarkness;
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float _MinHeight;
                float _PivotY;
            CBUFFER_END
            
            float4 _GlobalMapCenter;

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Coordinate-based Dynamic Height Squashing
                float3 objectCenterWS = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;
                float3 centerToWall = objectCenterWS - _GlobalMapCenter.xyz;
                float3 viewForward = -UNITY_MATRIX_V[2].xyz;
                float alignment = dot(centerToWall, viewForward);
                float fade = saturate((alignment + 1.0) / 1.0);
                float heightMult = lerp(_MinHeight, 1.0, fade);
                input.positionOS.y = (input.positionOS.y + _PivotY) * heightMult - _PivotY;

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, 0));
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
