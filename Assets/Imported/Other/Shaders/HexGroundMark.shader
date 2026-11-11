Shader "Nexus/HexGroundMark"
{
    Properties
    {
        _Color ("Color", Color) = (0.1, 0.9, 1, 1)
        _Intensity ("Intensity", Range(0, 10)) = 3.5
        _BorderWidth ("Border Width", Range(0.02, 0.35)) = 0.08
        _FillAlpha ("Fill Alpha", Range(0, 1)) = 0.18
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 1.1
        _ScanSpeed ("Scan Speed", Range(-20, 20)) = 2.5
        _RayCount ("Ray Count", Range(0, 24)) = 6
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
            float _BorderWidth;
            float _FillAlpha;
            float _PulseSpeed;
            float _ScanSpeed;
            float _RayCount;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float hexDistance(float2 p)
            {
                p = abs(p);
                return max(p.x * 0.8660254 + p.y * 0.5, p.y) / 0.8660254;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 i_p = i.uv * 2.0 - 1.0;
                float i_radius = hexDistance(i_p);

                float i_border =
                    smoothstep(1.0 - _BorderWidth, 1.0 - _BorderWidth + 0.03, i_radius) *
                    (1.0 - smoothstep(0.97, 1.0, i_radius));

                float i_scan = 0.5 + 0.5 * sin(i_radius * 22.0 - _Time.y * _ScanSpeed);

                float i_fill =
                    (1.0 - smoothstep(0.0, 0.98, i_radius)) *
                    _FillAlpha * (0.45 + 0.55 * i_scan);

                float i_angle = atan2(i_p.y, i_p.x);

                float i_rays =
                    pow(0.5 + 0.5 * sin(i_angle * _RayCount + _Time.y * 0.5), 13.0) *
                    (1.0 - smoothstep(0.08, 0.95, i_radius)) * 0.32;

                float i_pulse = 0.78 + 0.22 * sin(_Time.y * _PulseSpeed * 6.2831853);

                float i_alpha = (i_border + i_fill + i_rays) * i_pulse * _Color.a;

                float3 i_color = _Color.rgb * _Intensity * (0.75 + 0.35 * i_scan);

                return float4(i_color, saturate(i_alpha));
            }
            ENDCG
        }
    }

    Fallback Off
}
