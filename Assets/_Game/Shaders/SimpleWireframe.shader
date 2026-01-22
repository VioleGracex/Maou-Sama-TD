Shader "Universal Render Pipeline/Custom/SimpleWireframe"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0,0,0,0.5)
        [HDR] _WireColor ("Wire Color", Color) = (0,1,1,1)
        _Width ("Wire Width", Range(0, 0.5)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Pass
        {
            Name "Wireframe"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _WireColor;
                float _Width;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                // Simple edge check based on UV coordinates for a Cube (UVs 0-1)
                // This works best on default Unity Cube where each face is 0-1
                float2 edgeDist = min(uv, 1.0 - uv);
                float minDist = min(edgeDist.x, edgeDist.y);

                half4 color = _BaseColor;
                
                if (minDist < _Width)
                {
                    color = _WireColor;
                }

                return color;
            }
            ENDHLSL
        }
    }
}
