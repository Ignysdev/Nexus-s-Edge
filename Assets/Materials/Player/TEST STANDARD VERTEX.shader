Shader "Custom/TEST STANDARD VERTEX"
{
     Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)

        // Emission
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission Mask", 2D) = "white" {}
        _EmissionIntensity ("Emission Intensity", Range(0, 10)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            // Emission
            sampler2D _EmissionMap;
            float4 _EmissionMap_ST;
            fixed4 _EmissionColor;
            float _EmissionIntensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Base color + Vertex Color
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.rgb *= i.color.rgb;

                // Emission
                fixed3 emission = tex2D(_EmissionMap, i.uv).rgb * _EmissionColor.rgb * _EmissionIntensity;
                col.rgb += emission;          // ← this is the emission line

                return col;
            }
            ENDCG
        }
    }
}