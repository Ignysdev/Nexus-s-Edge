Shader "Nexus/HolographicBeam"
{
    Properties
    {
        _Color ("Color", Color) = (0.1, 0.9, 1, 1)
        _Intensity ("Intensity", Range(0, 10)) = 1.5
        _FadePower ("Fade Power", Range(0.5, 8)) = 2.2
        _StripeFrequency ("Stripe Frequency", Range(1, 64)) = 14
        _ScrollSpeed ("Scroll Speed", Range(-10, 10)) = 2
        _RadialSegments ("Radial Segments", Range(1, 16)) = 6
        _BaseGlow ("Base Glow", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4 _Color;
            float _Intensity;
            float _FadePower;
            float _StripeFrequency;
            float _ScrollSpeed;
            float _RadialSegments;
            float _BaseGlow;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float i_height = saturate(i.uv.y);
                float i_angle = i.uv.x * 6.2831853;

                float i_fade = pow(1.0 - i_height, _FadePower);

                float i_stripes =
                    0.5 + 0.5 * sin((i_height * _StripeFrequency - _Time.y * _ScrollSpeed) * 6.2831853);

                float i_segments =
                    0.55 + 0.45 * sin(i_angle * _RadialSegments + _Time.y * 0.8);

                float i_base = (1.0 - smoothstep(0.0, 0.10, i_height)) * _BaseGlow;

                float i_alpha =
                    (i_fade * (0.28 + 0.72 * i_stripes) * i_segments + i_base) * _Color.a;

                float3 i_color = _Color.rgb * _Intensity * (0.7 + 0.5 * i_stripes);

                return float4(i_color, saturate(i_alpha));
            }
            ENDCG
        }
    }

    Fallback Off
}
