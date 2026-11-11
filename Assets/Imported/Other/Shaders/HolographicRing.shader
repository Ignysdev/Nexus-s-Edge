Shader "Nexus/HolographicRing"
{
    Properties
    {
        _Color ("Color", Color) = (0.1, 0.9, 1, 1)
        _Intensity ("Intensity", Range(0, 10)) = 4
        _Thickness ("Ring Thickness", Range(0.02, 1)) = 0.28
        _Softness ("Edge Softness", Range(0.001, 0.2)) = 0.03
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
            float _Thickness;
            float _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Desenha uma faixa fina dentro do mesh do anel.
                float i_radial = i.uv.y;
                float i_halfThickness = _Thickness * 0.5;
                float i_inner = 0.5 - i_halfThickness;
                float i_outer = 0.5 + i_halfThickness;
                float i_soft = min(_Softness, i_halfThickness * 0.9);

                float i_edge =
                    smoothstep(i_inner, i_inner + i_soft, i_radial) *
                    (1.0 - smoothstep(i_outer - i_soft, i_outer, i_radial));

                float3 i_color = _Color.rgb * _Intensity;
                return float4(i_color, i_edge * _Color.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
