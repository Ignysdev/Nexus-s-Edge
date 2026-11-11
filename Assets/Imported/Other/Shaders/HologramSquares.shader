Shader "Nexus/HologramSquares"
{
    Properties
    {
        _MainTex ("Squares", 2D) = "white" {}
        _Color ("Color", Color) = (0, 0.95, 0.5, 1)
        _Intensity ("Intensity", Float) = 4
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off

        Stencil
        {
            Ref 1
            Comp Equal
        }

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _Intensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                float mask = smoothstep(0.15, 0.5, max(tex.r, max(tex.g, tex.b)));
                return fixed4(_Color.rgb * _Intensity, mask);
            }
            ENDCG
        }
    }

    Fallback Off
}
