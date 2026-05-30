Shader "Custom/SeamlessFog"
{
    Properties
    {
        _BaseMap ("Fog Texture (Noise)", 2D) = "white" {}
        [HDR] _BaseColor ("Dense Cloud Color", Color) = (0.35, 0.15, 0.55, 1.0)
        [HDR] _CloudColor2 ("Wispy Edge Color", Color) = (0.15, 0.05, 0.25, 1.0)
        _GlobalScale ("Global Cloud Size Scale", Float) = 0.02
        _ScrollSpeed1 ("Layer 1 Scroll Speed (X, Y)", Vector) = (-0.03, -0.005, 0, 0)
        _ScrollSpeed2 ("Layer 2 Scroll Speed (X, Y)", Vector) = (-0.02, 0.005, 0, 0)
        _DistortionSpeed ("Turbulence Speed (X, Y)", Vector) = (-0.015, 0.01, 0, 0)
        _DistortionStrength ("Turbulence Wave Strength", Float) = 0.18
        
        [Header(Volumetric Parallax)]
        _ParallaxStrength ("3D Depth Shift Strength", Range(0, 1)) = 0.25
        _LayerHeight2 ("Layer 2 Parallax Height", Float) = 0.12
        _LayerHeight3 ("Layer 3 Parallax Height", Float) = 0.25
        
        [Header(Density and Alpha Controls)]
        _Thickness ("Cloud Threshold Cutoff", Range(0, 0.9)) = 0.1
        _Softness ("Cloud Edge Softness", Range(0.01, 1.0)) = 0.4
        _MaxAlpha ("Master Opacity Limit", Range(0, 1)) = 0.85
    }
    SubShader
    {
        Tags 
        { 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True"
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Unlit"
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
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
            };

            Texture2D _BaseMap;
            SamplerState sampler_BaseMap;

            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float4 _CloudColor2;
            float _GlobalScale;
            float4 _ScrollSpeed1;
            float4 _ScrollSpeed2;
            float4 _DistortionSpeed;
            float _DistortionStrength;
            float _ParallaxStrength;
            float _LayerHeight2;
            float _LayerHeight3;
            float _Thickness;
            float _Softness;
            float _MaxAlpha;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Calculate camera view direction in world-space
                float3 viewDirWS = normalize(_WorldSpaceCameraPos - input.positionWS);
                
                // Project view vector onto the XZ horizontal plane for horizontal cloud parallax shifts
                float2 parallaxOffset = viewDirWS.xz * _ParallaxStrength;

                // Base world UV coordinates scaled down for large, sweeping global features
                float2 baseUV = input.positionWS.xz * _GlobalScale;

                // 1. Sample the dynamic wave turbulence / fluid refraction noise
                float2 distUV = baseUV * 0.8 + _DistortionSpeed.xy * _Time.y;
                half4 distNoise = _BaseMap.Sample(sampler_BaseMap, distUV);
                
                // Translate the 0-1 noise into a beautiful centered wavy offset vector
                float2 waveOffset = (distNoise.rg * 2.0 - 1.0) * _DistortionStrength;

                // 2. Compute three independent scrolling and parallax-shifted layers
                // Layer 1: Base slow moving deep layer, no parallax offset
                float2 uv1 = baseUV * 1.0 + _ScrollSpeed1.xy * _Time.y + waveOffset;
                
                // Layer 2: Medium height layer, shifted by a medium parallax factor
                float2 uv2 = baseUV * 1.45 + _ScrollSpeed2.xy * _Time.y - (waveOffset * 1.3) + (parallaxOffset * _LayerHeight2);
                
                // Layer 3: High wispy detail layer, shifted by a large parallax factor
                float2 uv3 = baseUV * 2.1 + float2(_ScrollSpeed1.x * 1.3, _ScrollSpeed2.y * 1.2) * _Time.y + (waveOffset * 0.7) + (parallaxOffset * _LayerHeight3);

                // Sample the individual noise channels
                half noiseL1 = _BaseMap.Sample(sampler_BaseMap, uv1).r;
                half noiseL2 = _BaseMap.Sample(sampler_BaseMap, uv2).g;
                half noiseL3 = _BaseMap.Sample(sampler_BaseMap, uv3).b;

                // 3. Volumetric density blending
                // Merge the three layers together at different weight factors to simulate complex cloud volume
                half combinedNoise = saturate(noiseL1 * 0.52 + noiseL2 * 0.33 + noiseL3 * 0.15);

                // Apply mathematical smoothstep for silk-soft organic transitions instead of hard blocks
                half cloudIntensity = smoothstep(_Thickness, _Thickness + _Softness, combinedNoise);

                // Linearly interpolate between background edge color and deep dense core color
                half4 finalColor = lerp(_CloudColor2, _BaseColor, cloudIntensity);
                
                // Apply final master alpha scale to make sure it remains a soft background overlay
                finalColor.a = cloudIntensity * _MaxAlpha;

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
