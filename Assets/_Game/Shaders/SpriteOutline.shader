Shader "MaouSamaTD/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 50)) = 1
        [MaterialToggle] _OutlineEnabled ("Outline Enabled", Float) = 1
        
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineEnabled;
            float4 _MainTex_TexelSize;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                if (_OutlineEnabled > 0)
                {
                    // 8-Tap Outline (Box/Circle approximation) for better diagonals and thickness
                    // Sampling logic: Top, Bottom, L, R, TL, TR, BL, BR
                    
                    float2 texel = _MainTex_TexelSize.xy * _OutlineWidth;

                    fixed4 pixelUp = tex2D(_MainTex, IN.texcoord + fixed2(0, texel.y));
                    fixed4 pixelDown = tex2D(_MainTex, IN.texcoord - fixed2(0, texel.y));
                    fixed4 pixelRight = tex2D(_MainTex, IN.texcoord + fixed2(texel.x, 0));
                    fixed4 pixelLeft = tex2D(_MainTex, IN.texcoord - fixed2(texel.x, 0));
                    
                    fixed4 pixelTL = tex2D(_MainTex, IN.texcoord + fixed2(-texel.x, texel.y));
                    fixed4 pixelTR = tex2D(_MainTex, IN.texcoord + fixed2(texel.x, texel.y));
                    fixed4 pixelBL = tex2D(_MainTex, IN.texcoord + fixed2(-texel.x, -texel.y));
                    fixed4 pixelBR = tex2D(_MainTex, IN.texcoord + fixed2(texel.x, -texel.y));

                    float alphaSum = pixelUp.a + pixelDown.a + pixelRight.a + pixelLeft.a + 
                                     pixelTL.a + pixelTR.a + pixelBL.a + pixelBR.a;

                    // If transparent but surrounded by alpha
                    if (c.a == 0 && alphaSum > 0)
                    {
                        c = _OutlineColor;
                        c.rgb *= c.a; 
                    }
                }

                c.rgb *= c.a;
                return c;
            }
        ENDCG
        }
    }
}
