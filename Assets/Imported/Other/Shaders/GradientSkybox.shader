Shader "Nexus/GradientSkybox"
{
    Properties
    {
        _TopColor ("Zenith", Color) = (0.003, 0.006, 0.02, 1)
        _HorizonColor ("Horizon", Color) = (0.02, 0.11, 0.14, 1)
        _BottomColor ("Nadir", Color) = (0.001, 0.002, 0.006, 1)
        _GlowColor ("Horizon Glow", Color) = (0.1, 0.9, 1, 1)
        _GlowPower ("Glow Power", Range(1, 20)) = 6
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 1.2

        _GridColor ("Grid Color", Color) = (0.05, 0.8, 1, 1)
        _GridScale ("Grid Scale", Range(1, 64)) = 18
        _GridIntensity ("Grid Intensity", Range(0, 3)) = 0.35

        _ScanColor ("Scan Color", Color) = (0.2, 0.9, 1, 1)
        _ScanIntensity ("Scan Intensity", Range(0, 2)) = 0.12
        _ScanSpeed ("Scan Speed", Range(-2, 2)) = 0.08

        _StarColor ("Star Color", Color) = (0.7, 0.95, 1, 1)
        _StarDensity ("Star Density", Range(20, 300)) = 140
        _StarIntensity ("Star Intensity", Range(0, 3)) = 0.9

        _SunColor ("Sun Color", Color) = (0.5, 1, 1, 1)
        _SunIntensity ("Sun Intensity", Range(0, 5)) = 1.2
        _SunPower ("Sun Halo Power", Range(8, 256)) = 80

        _NebulaColor ("Nebula Color", Color) = (0.2, 0.05, 0.35, 1)
        _NebulaIntensity ("Nebula Intensity", Range(0, 2)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #define NEXUS_PI 3.14159265359

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
            };

            float4 _TopColor;
            float4 _HorizonColor;
            float4 _BottomColor;
            float4 _GlowColor;
            float _GlowPower;
            float _GlowIntensity;

            float4 _GridColor;
            float _GridScale;
            float _GridIntensity;

            float4 _ScanColor;
            float _ScanIntensity;
            float _ScanSpeed;

            float4 _StarColor;
            float _StarDensity;
            float _StarIntensity;

            float4 _SunColor;
            float _SunIntensity;
            float _SunPower;

            float4 _NebulaColor;
            float _NebulaIntensity;

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float noise21(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float y = d.y;
                float h = saturate(y * 0.5 + 0.5);

                // Gradiente base: nadir quase preto, horizonte teal, zênite índigo.
                float3 col = lerp(_BottomColor.rgb, _HorizonColor.rgb, smoothstep(0.0, 0.48, h));
                col = lerp(col, _TopColor.rgb, smoothstep(0.48, 1.0, h));

                // Brilho do horizonte.
                float glow = pow(saturate(1.0 - abs(y)), _GlowPower) * _GlowIntensity;
                col += _GlowColor.rgb * glow;

                // Abaixo do horizonte fica mais profundo e escuro.
                float below = 1.0 - smoothstep(-0.45, 0.0, y);
                col = lerp(col, _BottomColor.rgb * 0.35, below * 0.85);

                // Sol/núcleo do Animus, baixo no horizonte, com halo e anéis.
                float3 sunDir = normalize(float3(0.35, 0.08, 0.93));
                float sunDot = saturate(dot(d, sunDir));
                float sunDisc = smoothstep(0.9985, 0.9995, sunDot);
                float sunHalo = pow(sunDot, _SunPower) * 0.7 + pow(sunDot, 12.0) * 0.12;
                float ringPhase = acos(sunDot) * 110.0;
                float sunRing = smoothstep(0.90, 0.98, sin(ringPhase)) *
                                (1.0 - smoothstep(0.0, 0.30, acos(sunDot))) *
                                (1.0 - sunDisc);

                col += _SunColor.rgb * (sunDisc * _SunIntensity +
                                        sunHalo * _SunIntensity +
                                        sunRing * 0.12);

                // Estrelas/dados no hemisfério superior.
                float2 starUV = float2(atan2(d.z, d.x) / NEXUS_PI,
                                       asin(clamp(y, -1.0, 1.0)) / NEXUS_PI);
                float2 starCell = floor(starUV * _StarDensity);
                float starHash = hash21(starCell);
                float star = step(0.985, starHash) * smoothstep(-0.02, 0.08, y);
                float twinkle = 0.6 + 0.4 * sin(_Time.y * (2.0 + starHash * 6.0) + starHash * 40.0);
                col += _StarColor.rgb * star * twinkle * _StarIntensity;

                // Grade digital próxima ao horizonte (Tron).
                float2 gridUV = float2(atan2(d.z, d.x) / NEXUS_PI,
                                       asin(clamp(y, -1.0, 1.0)) / NEXUS_PI);
                float2 g = abs(frac(gridUV * _GridScale + float2(_Time.y * 0.01, _Time.y * 0.02)) - 0.5);
                float gridLine = smoothstep(0.48, 0.5, max(g.x, g.y));
                float gridMask = 1.0 - smoothstep(0.02, 0.42, abs(y));
                col += _GridColor.rgb * gridLine * gridMask * _GridIntensity;

                // Faixas de varredura/scanlines.
                float scan = sin((y * 9.0 + _Time.y * _ScanSpeed) * NEXUS_PI * 2.0) * 0.5 + 0.5;
                col += _ScanColor.rgb * scan * _ScanIntensity *
                       (1.0 - smoothstep(0.55, 1.0, abs(y)));

                // Névoa/aurora sutil, com ruído procedural.
                float2 nebulaUV = float2(atan2(d.z, d.x) / NEXUS_PI * 2.0,
                                         y * 3.0 + _Time.y * 0.02);
                float nebula = noise21(nebulaUV * 2.5);
                float aurora = smoothstep(0.55, 0.95, nebula) *
                               (1.0 - smoothstep(0.05, 0.70, abs(y)));
                col += _NebulaColor.rgb * aurora * _NebulaIntensity;

                // Feixes verticais discretos.
                float angle = atan2(d.z, d.x);
                float beams = pow(saturate(sin(angle * 5.0 + _Time.y * 0.05)), 64.0) *
                              (1.0 - smoothstep(0.0, 0.55, abs(y)));
                col += _GlowColor.rgb * beams * 0.35;

                return float4(col, 1.0);
            }
            ENDCG
        }
    }

    Fallback Off
}
