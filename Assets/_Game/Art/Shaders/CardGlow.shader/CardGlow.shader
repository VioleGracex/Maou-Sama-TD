Shader "Custom/CardGlow"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
        [HDR] _Color("Glow Color", Color) = (0, 1, 0, 1)
        _Speed("Animation Speed", Float) = 1.0
        _NoiseScale("Noise Scale", Float) = 20.0
        _GlowWidth("Glow Width", Float) = 0.1
        _GlowPower("Glow Power", Float) = 1.5
        _EdgeSoftness("Edge Softness", Float) = 0.05
        _CornerRadius("Corner Radius", Float) = 0.1
        _GlowBaseAlpha("Base Opacity", Range(0,1)) = 1.0
        _ActiveSides("Active Sides (L,B,R,T)", Vector) = (1,1,1,1)
        _RandomOffset("Random Offset", Float) = 0.0
        _CustomTime("Custom Time", Float) = 0.0
        
        // UI Masking
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            float _Speed;
            float _NoiseScale;
            float _GlowWidth;
            float _GlowPower;
            float _EdgeSoftness;
            float _CornerRadius;
            
            float4 _ActiveSides; // x=Left, y=Bottom, z=Right, w=Top
            float _GlowBaseAlpha;
            float _RandomOffset;
            float _CustomTime;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            // Simple pseudo-random hash
            float hash(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            // 2D Value Noise
            float value_noise(float2 p) {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                
                float a = hash(i + float2(0.0, 0.0));
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // Signed Distance Function for a rounded rectangle
            float sdRoundedBox(float2 p, float2 b, float4 r)
            {
                r.xy = (p.x > 0.0) ? r.xy : r.zw;
                r.x = (p.y > 0.0) ? r.x : r.y;
                float2 q = abs(p) - b + r.x;
                return min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float2 centeredUV = uv * 2.0 - 1.0;
                
                // --- Side Masking Logic ---
                float sideMask = 1.0;
                float xMask = lerp(_ActiveSides.x, _ActiveSides.z, step(0, centeredUV.x));
                float yMask = lerp(_ActiveSides.y, _ActiveSides.w, step(0, centeredUV.y));
                sideMask = min(xMask, yMask);

                float time = (_CustomTime > 0 ? _CustomTime : _Time.y) * _Speed + _RandomOffset;
                float noiseVal = value_noise(uv * _NoiseScale + time * 0.5 + _RandomOffset);
                
                float2 bounds = float2(1.0 - _GlowWidth * 2.0, 1.0 - _GlowWidth * 2.0); 
                float dist = sdRoundedBox(centeredUV, bounds, float4(_CornerRadius, _CornerRadius, _CornerRadius, _CornerRadius));
                
                float noiseEffect = (noiseVal - 0.5) * 0.1; 
                
                float outerGlow = 1.0 - smoothstep(0.0, _GlowWidth, dist + noiseEffect);
                float innerCut = smoothstep(-_EdgeSoftness, 0.0, dist + noiseEffect);
                
                float finalAlpha = outerGlow * innerCut;
                finalAlpha = pow(finalAlpha, _GlowPower);
                finalAlpha *= _GlowBaseAlpha; 
                finalAlpha *= sideMask;

                fixed4 color = IN.color; 
                color.a *= finalAlpha;
                
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}