Shader "MaouSamaTD/GlowMarker"
{
    Properties
    {
        _MainColor ("Color", Color) = (1,1,0,1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 2.0
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _MainColor;
            float _GlowIntensity;
            float _PulseSpeed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Simple pulse
                float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                float4 col = _MainColor;
                col.a *= pulse * _GlowIntensity;
                return col;
            }
            ENDCG
        }
    }
}
