Shader "VirtualRail/HexShieldBarrier"
{
    Properties
    {
        [Header(Hexagon Grid Properties)]
        _HexRadius ("Hexagon Radius (Meters)", Float) = 0.55
        _LineWidth ("Hex Line Width (Meters)", Float) = 0.045
        _Dimensions ("Shield Dimensions (X, Y)", Vector) = (5.6, 3.2, 0, 0)

        [Header(Colors and Glow)]
        [HDR] _BaseColor ("Shield Field Base Color", Color) = (0.05, 0.45, 0.85, 0.45)
        [HDR] _EdgeColor ("Hex Edge Neon Color", Color) = (0.35, 1.25, 1.50, 1.00)
        [HDR] _HitColor ("Hit Flash Color", Color) = (2.0, 2.0, 2.0, 1.0)

        [Header(Energy Pulse and Fresnel)]
        _PulseSpeed ("Pulse Wave Speed", Float) = 2.0
        _PulseFreq ("Pulse Wave Frequency", Float) = 3.0
        _PerimeterFade ("Outer Border Fade", Range(0.5, 3.0)) = 1.6
        _RimPower ("Fresnel Rim Power", Range(0.5, 4.0)) = 1.8
        _RimGlow ("Fresnel Rim Intensity", Range(0.0, 2.0)) = 0.65

        [Header(Interactive States)]
        _HitFlash ("Hit Flash Intensity", Range(0, 1)) = 0.0
        _FarBoost ("Distance Visibility Boost", Range(0.0, 2.0)) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "Queue"="Transparent+50" 
            "RenderType"="Transparent" 
            "IgnoreProjector"="True" 
        }
        LOD 200

        Pass
        {
            Name "HEX_SHIELD_FORWARD"
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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
            };

            float _HexRadius;
            float _LineWidth;
            float4 _Dimensions;
            fixed4 _BaseColor;
            fixed4 _EdgeColor;
            fixed4 _HitColor;
            float _PulseSpeed;
            float _PulseFreq;
            float _PerimeterFade;
            float _RimPower;
            float _RimGlow;
            float _HitFlash;
            float _FarBoost;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(UnityWorldSpaceViewDir(o.worldPos));
                return o;
            }

            // Regular hexagon distance in metric coordinates
            float HexDist(float2 p)
            {
                p = abs(p);
                float d = max(p.x, p.x * 0.5 + p.y * (1.7320508 * 0.5));
                return d;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. Metric coordinates centered on the shield panel (in meters)
                float2 posMeters = (i.uv - 0.5) * _Dimensions.xy;

                // 2. Regular hexagon grid tiling basis
                float radius = max(_HexRadius, 0.1);
                float r = 1.73205080757; // sqrt(3)
                float2 p_scaled = posMeters / radius;

                float2 a = fmod(p_scaled, float2(1.0, r));
                if (a.x < 0.0) a.x += 1.0;
                if (a.y < 0.0) a.y += r;
                a -= float2(0.5, r * 0.5);

                float2 b = fmod(p_scaled - float2(0.5, r * 0.5), float2(1.0, r));
                if (b.x < 0.0) b.x += 1.0;
                if (b.y < 0.0) b.y += r;
                b -= float2(0.5, r * 0.5);

                float2 gv = (dot(a, a) < dot(b, b)) ? a : b;

                // Distance from center to hexagon edge
                float dHex = HexDist(gv);
                float edgeDistMeters = (0.5 - dHex) * radius;

                // 3. Screen-space adaptive line width with fwidth anti-aliasing
                float aaWidth = max(fwidth(edgeDistMeters) * 1.25, 0.002);
                float effLineWidth = max(_LineWidth, aaWidth * 0.85); // Never thinner than ~1 pixel on screen
                float edgeLine = 1.0 - smoothstep(effLineWidth - aaWidth, effLineWidth + aaWidth, edgeDistMeters);

                // 4. Perimeter Masking (Superellipse soft fade)
                float2 centered = (i.uv - 0.5) * 2.0;
                float dist = pow(abs(centered.x), 2.6) + pow(abs(centered.y), 2.6);
                float perimeter = saturate((1.0 - dist) * _PerimeterFade);

                // 5. Distance-adaptive macro field boost (increases field visibility at long ranges)
                float camDist = length(_WorldSpaceCameraPos - i.worldPos);
                float farFactor = saturate((camDist - 12.0) / 40.0) * _FarBoost; // 0 at <=12m, 1 at >=52m

                // 6. Cell interior glow & energy pulse
                float scan = sin(_Time.y * _PulseSpeed + i.uv.y * _PulseFreq) * 0.5 + 0.5;
                float cellGlow = (1.0 - edgeLine * 0.7) * (0.4 + 0.6 * scan);

                // 7. Fresnel Rim Glow
                float rim = 1.0 - saturate(abs(dot(normalize(i.worldNormal), i.viewDir)));
                rim = pow(rim, _RimPower) * _RimGlow;

                // 8. Color synthesis with macro field floor
                // At distance, macroField guarantees the shield silhouette is boldly readable
                float macroField = (1.0 - saturate(dist)) * 0.65 * farFactor;
                fixed3 finalColor = _BaseColor.rgb * (cellGlow + macroField) 
                                  + _EdgeColor.rgb * edgeLine * 1.8 
                                  + _EdgeColor.rgb * rim;

                float finalAlpha = (_BaseColor.a * (cellGlow + macroField) 
                                  + edgeLine * 0.95 
                                  + rim * 0.5) * perimeter;

                // 9. Hit Flash Blend
                if (_HitFlash > 0.001)
                {
                    finalColor = lerp(finalColor, _HitColor.rgb * 1.5, _HitFlash);
                    finalAlpha = lerp(finalAlpha, perimeter * 1.1, _HitFlash * 0.85);
                }

                // 10. Correct output for Blend SrcAlpha One: do NOT multiply finalColor by finalAlpha
                return fixed4(finalColor, saturate(finalAlpha));
            }
            ENDCG
        }
    }
    FallBack Off
}
