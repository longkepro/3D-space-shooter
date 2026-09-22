Shader "VirtualRail/HexShieldBarrier"
{
    Properties
    {
        _MainTex ("Hexagon Pattern (Opt)", 2D) = "white" {}
        [HDR] _BaseColor ("Shield Inner Color", Color) = (0.05, 0.45, 0.85, 0.35)
        [HDR] _EdgeColor ("Hex Edge Neon Color", Color) = (0.25, 0.95, 1.0, 1.0)
        [HDR] _HitColor ("Hit Flash Color", Color) = (1.5, 1.5, 1.5, 1.0)
        _Tiling ("Grid Tiling (X, Y)", Vector) = (7.0, 4.0, 0, 0)
        _PulseSpeed ("Pulse Wave Speed", Float) = 2.0
        _PulseFreq ("Pulse Wave Frequency", Float) = 4.0
        _PerimeterFade ("Outer Border Fadeout", Range(0.2, 3.0)) = 1.4
        _RimPower ("Fresnel Rim Power", Range(0.5, 4.0)) = 1.8
        _RimGlow ("Fresnel Rim Intensity", Range(0.0, 1.5)) = 0.5
        _HitFlash ("Hit Flash Intensity", Range(0, 1)) = 0.0
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _BaseColor;
            fixed4 _EdgeColor;
            fixed4 _HitColor;
            float4 _Tiling;
            float _PulseSpeed;
            float _PulseFreq;
            float _PerimeterFade;
            float _RimPower;
            float _RimGlow;
            float _HitFlash;

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

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. Sample hexagon pattern with tiling
                float2 tiledUV = i.uv * _Tiling.xy;
                fixed4 tex = tex2D(_MainTex, tiledUV);

                // HexagonPattern_Opt has black hex borders (~0.0) and white cell interior (~1.0)
                float edgeLine = saturate(1.0 - tex.r);
                edgeLine = smoothstep(0.08, 0.45, edgeLine);

                // 2. Interior cell glow & energy pulse
                float scan = sin(_Time.y * _PulseSpeed + i.uv.y * _PulseFreq) * 0.5 + 0.5;
                float cellGlow = (1.0 - edgeLine) * (0.35 + 0.65 * scan);

                // 3. Fresnel Rim effect (glowing edges when viewed at grazing angle)
                float rim = 1.0 - saturate(abs(dot(normalize(i.worldNormal), i.viewDir)));
                rim = pow(rim, _RimPower) * _RimGlow;

                // 4. Perimeter Masking (Superellipse soft fade towards edges of the shield panel)
                float2 centered = (i.uv - 0.5) * 2.0;
                float dist = pow(abs(centered.x), 2.4) + pow(abs(centered.y), 2.4);
                float perimeter = saturate((1.0 - dist) * _PerimeterFade);

                // 5. Combine Colors
                fixed3 finalColor = _BaseColor.rgb * cellGlow + _EdgeColor.rgb * edgeLine * 1.75 + _EdgeColor.rgb * rim;
                float finalAlpha = (_BaseColor.a * cellGlow + edgeLine * 0.95 + rim * 0.5) * perimeter;

                // 6. Hit Flash Blend
                if (_HitFlash > 0.001)
                {
                    finalColor = lerp(finalColor, _HitColor.rgb, _HitFlash);
                    finalAlpha = lerp(finalAlpha, perimeter, _HitFlash * 0.85);
                }

                return fixed4(finalColor * finalAlpha, finalAlpha);
            }
            ENDCG
        }
    }
    FallBack Off
}
