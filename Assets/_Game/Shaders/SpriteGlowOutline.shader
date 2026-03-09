Shader "MaouSamaTD/SpriteGlowOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Outline)]
        _OutlineColor ("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth ("Outline Width", Range(0, 10)) = 1
        [Toggle] _OutlineEnabled ("Outline Enabled", Float) = 1
        
        [Header(Glow)]
        [HDR] _GlowColor ("Glow Color", Color) = (0,1,1,1)
        _GlowDistance ("Glow Distance", Range(0, 50)) = 30
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 3
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 2.5
        _VerticalStretch ("Vertical Stretch", Range(1, 10)) = 5
        _GlowFade ("Glow Decay", Range(0.1, 4.0)) = 1.6
        
        [Header(Selection)]
        _SelectionLevel ("Selection Level", Range(0, 1)) = 0
        
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
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
            #pragma multi_compile_instancing
            #include "UnitySprites.cginc"

            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineEnabled;
            
            fixed4 _GlowColor;
            float _GlowDistance;
            float _GlowIntensity;
            float _PulseSpeed;
            float _VerticalStretch;
            float _GlowFade;
            float _SelectionLevel;
            
            float4 _MainTex_TexelSize;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 mainColor = SampleSpriteTexture(IN.texcoord);
                float alpha = mainColor.a;
                
                // Premultiplied alpha for standard sprite rendering
                fixed4 finalColor = mainColor * IN.color;
                finalColor.rgb *= finalColor.a;

                float time = _Time.y * _PulseSpeed;
                float selection = _SelectionLevel;

                if (_OutlineEnabled > 0 && selection > 0.01)
                {
                    float2 texel = _MainTex_TexelSize.xy;
                    
                    // 1. Sharp Outline (4-tap radial)
                    float outlineAlpha = 0;
                    float2 outlineOff = texel * _OutlineWidth;
                    outlineAlpha += tex2D(_MainTex, IN.texcoord + float2(outlineOff.x, 0)).a;
                    outlineAlpha += tex2D(_MainTex, IN.texcoord - float2(outlineOff.x, 0)).a;
                    outlineAlpha += tex2D(_MainTex, IN.texcoord + float2(0, outlineOff.y)).a;
                    outlineAlpha += tex2D(_MainTex, IN.texcoord - float2(0, outlineOff.y)).a;
                    
                    if (alpha < 0.1 && outlineAlpha > 0.01)
                    {
                        fixed4 outColor = _OutlineColor;
                        outColor.rgb *= (1.0 + sin(time * 3.14) * 0.2) * selection;
                        finalColor = outColor;
                        finalColor.rgb *= finalColor.a;
                    }
                    
                    // 2. High-Fidelity Soft Vertical Glow
                    float glowAlpha = 0;
                    float baseDist = _GlowDistance * 0.001 * selection;
                    
                    // Sample 8 times for a very smooth gradient
                    for(int i = 1; i <= 8; i++)
                    {
                        float step = (float)i / 8.0;
                        float weight = pow(1.0 - step, _GlowFade);
                        float vertOff = step * baseDist * _VerticalStretch;
                        float horizOff = step * baseDist * 0.5;
                        
                        glowAlpha += tex2D(_MainTex, IN.texcoord + float2(0, vertOff)).a * weight;
                        glowAlpha += tex2D(_MainTex, IN.texcoord + float2(horizOff, vertOff * 0.6)).a * 0.4 * weight;
                        glowAlpha += tex2D(_MainTex, IN.texcoord + float2(-horizOff, vertOff * 0.6)).a * 0.4 * weight;
                    }
                    
                    glowAlpha = saturate(glowAlpha * 0.28);
                    float pulse = (1.0 + 0.35 * sin(time + IN.texcoord.x * 20.0 + IN.texcoord.y * 10.0));
                    
                    if (alpha < 0.95)
                    {
                        fixed4 glowColor = _GlowColor * glowAlpha * _GlowIntensity * pulse * selection;
                        // Additive blending for light aura
                        finalColor.rgb += glowColor.rgb * (1.1 - alpha);
                        finalColor.a = max(finalColor.a, (glowAlpha * 0.6) * selection);
                    }
                }

                return finalColor;
            }
        ENDCG
        }
    }
}
