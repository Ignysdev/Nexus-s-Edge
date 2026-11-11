Shader "Nexus/GlassReflection"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.0006, 0.0012, 0.002, 1)
        _ReflectionTex ("Reflection", 2D) = "black" {}
        _ReflectionStrength ("Reflection Strength", Range(0, 2)) = 0.55
        _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Range(0, 1)) = 0.16
        _HologramTex ("Hologram Fade", 2D) = "black" {}
        _HologramTiling ("Hologram Tiling", Float) = 32
        _HologramColor ("Hologram Color", Color) = (0, 0.95, 0.5, 1)
        _HologramStrength ("Hologram Fade Strength", Range(0, 1)) = 0.18
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

        Stencil
        {
            Ref 1
            Comp Always
            Pass Replace
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

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
                float4 screenPos : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _ReflectionTex;
            float4 _ReflectionTex_TexelSize;
            sampler2D _BumpMap;
            float4 _BumpMap_ST;
            float _BumpScale;
            sampler2D _HologramTex;
            float _HologramTiling;
            fixed4 _BaseColor;
            fixed4 _HologramColor;
            float _ReflectionStrength;
            float _HologramStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BumpMap);
                o.screenPos = ComputeScreenPos(o.pos);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 i_normal = UnpackNormal(tex2D(_BumpMap, i.uv)).xyz;
                float2 i_screenUV = i.screenPos.xy / i.screenPos.w;
                i_screenUV.x = 1.0 - i_screenUV.x;

                #if UNITY_UV_STARTS_AT_TOP
                if (_ReflectionTex_TexelSize.y < 0)
                    i_screenUV.y = 1.0 - i_screenUV.y;
                #endif

                i_screenUV += i_normal.xy * _BumpScale * 0.15f;

                fixed3 i_reflection = tex2D(_ReflectionTex, i_screenUV).rgb;
                float3 i_viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float i_fresnel = pow(1.0 - saturate(dot(i_viewDir, float3(0, 1, 0))), 3.0);
                float i_strength = _ReflectionStrength * lerp(0.35, 1.0, i_fresnel);

                fixed3 i_color = _BaseColor.rgb + i_reflection * i_strength;

                fixed4 i_hologram = tex2D(_HologramTex, i.uv * _HologramTiling);
                float i_mask = smoothstep(0.15, 0.5, max(i_hologram.r, max(i_hologram.g, i_hologram.b)));
                i_color += _HologramColor.rgb * i_mask * _HologramStrength;

                return fixed4(i_color, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
