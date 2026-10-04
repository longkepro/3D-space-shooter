Shader "VirtualRail/HouidiPlasmaBuiltin"
{
    Properties
    {
        [Header(Color and Glow)]
        [HDR] _BoarderColor ("Border / Rim Color (HDR)", Color) = (0.0, 3.0, 0.2, 1.0)
        [HDR] _Boarder_Color ("Secondary Glow Color (HDR)", Color) = (0.35, 2.6, 0.85, 1.0)
        _Color ("Base Plasma Color", Color) = (0.07, 0.95, 0.15, 1.0)
        _Texture2D ("Caustics Texture", 2D) = "white" {}

        [Header(Edge Falloff and Strength)]
        _Boarder_Size ("Border Falloff / Size", Range(0.1, 8.0)) = 2.0
        _Boarder_Strength ("Border Strength / Power", Range(0.5, 10.0)) = 1.91

        [Header(Motion and Turbulence)]
        _Scrolling_Speed ("Scrolling Speed", Range(0.01, 2.0)) = 0.1
        _Distortion_Speed ("Distortion Speed", Range(0.01, 2.0)) = 0.2
        _SurfaceMovSpeed ("Surface Wobble Speed", Range(0.001, 1.0)) = 0.08
        _Noise_Scale ("Vertex Wobble Amplitude", Range(0.0, 0.25)) = 0.07
        _Twirl_Strength ("Twirl Vortex Strength", Range(0.0, 15.0)) = 5.5
        _Pulse_Speed ("Breathing Pulse Speed", Range(0.0, 15.0)) = 6.0
        _Pulse_Amplitude ("Breathing Pulse Amplitude", Range(0.0, 0.3)) = 0.12

        [Header(Sharpness and Contrast)]
        _CausticContrast ("Caustic Power Contrast", Range(1.0, 4.0)) = 1.91
        _BrightnessBoost ("Brightness & Filament Boost", Range(0.8, 3.0)) = 1.4

        [Header(Optical Refraction)]
        [Toggle(_REFRACTION_ON)] _EnableRefraction ("Enable Refraction (GrabPass)", Float) = 1.0
        _RefractionStrength ("Refraction Distortion Strength", Range(0.0, 0.2)) = 0.045
    }
    SubShader
    {
        Tags { "Queue"="Transparent+50" "RenderType"="Transparent" "IgnoreProjector"="True" }

        GrabPass { "_VirtualRailPlasmaGrab" }

        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _REFRACTION_ON
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldViewDir : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 grabPos : TEXCOORD3;
            };

            fixed4 _BoarderColor;
            fixed4 _Boarder_Color;
            fixed4 _Color;
            sampler2D _Texture2D;
            float4 _Texture2D_ST;
            float _Boarder_Size;
            float _Boarder_Strength;
            float _Scrolling_Speed;
            float _Distortion_Speed;
            float _SurfaceMovSpeed;
            float _Noise_Scale;
            float _Twirl_Strength;
            float _Pulse_Speed;
            float _Pulse_Amplitude;
            float _CausticContrast;
            float _BrightnessBoost;
            float _EnableRefraction;
            float _RefractionStrength;

            sampler2D _VirtualRailPlasmaGrab;
            float4 _VirtualRailPlasmaGrab_TexelSize;

            // 1. 3D Value Noise for organic vertex displacement (Houidisoft Simple Noise)
            inline float NoiseHash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                float n000 = NoiseHash(i + float3(0.0, 0.0, 0.0));
                float n100 = NoiseHash(i + float3(1.0, 0.0, 0.0));
                float n010 = NoiseHash(i + float3(0.0, 1.0, 0.0));
                float n110 = NoiseHash(i + float3(1.0, 1.0, 0.0));
                float n001 = NoiseHash(i + float3(0.0, 0.0, 1.0));
                float n101 = NoiseHash(i + float3(1.0, 0.0, 1.0));
                float n011 = NoiseHash(i + float3(0.0, 1.0, 1.0));
                float n111 = NoiseHash(i + float3(1.0, 1.0, 1.0));

                float x0 = lerp(n000, n100, u.x);
                float x1 = lerp(n010, n110, u.x);
                float x2 = lerp(n001, n101, u.x);
                float x3 = lerp(n011, n111, u.x);

                float y0 = lerp(x0, x1, u.y);
                float y1 = lerp(x2, x3, u.y);

                return lerp(y0, y1, u.z);
            }

            // 2. Twirl Vortex Node (Houidisoft Twirl Node)
            float2 Twirl(float2 uv, float2 center, float strength, float angleOffset)
            {
                float2 delta = uv - center;
                float dist = length(delta);
                float angle = strength * (1.0 - saturate(dist * 1.5)) + angleOffset;
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2 rotated = float2(
                    cosA * delta.x - sinA * delta.y,
                    sinA * delta.x + cosA * delta.y
                );
                return rotated + center;
            }

            // 3. Unity Gradient Noise Node
            float2 UnityGradientNoiseDir(float2 p)
            {
                p = p % 289.0;
                float x = (34.0 * p.x + 1.0) * p.x % 289.0 + p.y;
                x = (34.0 * x + 1.0) * x % 289.0;
                x = frac(x / 41.0) * 2.0 - 1.0;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            float UnityGradientNoise(float2 uv, float scale)
            {
                float2 p = uv * scale;
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(UnityGradientNoiseDir(ip), fp);
                float d01 = dot(UnityGradientNoiseDir(ip + float2(0.0, 1.0)), fp - float2(0.0, 1.0));
                float d10 = dot(UnityGradientNoiseDir(ip + float2(1.0, 0.0)), fp - float2(1.0, 0.0));
                float d11 = dot(UnityGradientNoiseDir(ip + float2(1.0, 1.0)), fp - float2(1.0, 1.0));
                fp = fp * fp * fp * (fp * (fp * 6.0 - 15.0) + 10.0);
                return lerp(lerp(d00, d10, fp.x), lerp(d01, d11, fp.x), fp.y) + 0.5;
            }

            v2f vert(appdata v)
            {
                v2f o;

                // Houidisoft: Vertex = Position + Normal * (SimpleNoise(Position + Time * SurfaceMovSpeed) * NoiseScale)
                float3 noiseCoord = v.vertex.xyz * 2.8 + float3(_Time.y * _SurfaceMovSpeed, _Time.y * _SurfaceMovSpeed * 0.7, _Time.y * _SurfaceMovSpeed * 0.5);
                float n = ValueNoise3D(noiseCoord) * 0.65 + ValueNoise3D(noiseCoord * 2.1) * 0.35;
                float displacement = (n - 0.5) * 2.0 * _Noise_Scale;
                v.vertex.xyz += v.normal * displacement;

                o.pos = UnityObjectToClipPos(v.vertex);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldViewDir = normalize(_WorldSpaceCameraPos - worldPos);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 viewDir = normalize(i.worldViewDir);
                float2 baseUV = i.uv;

                // 1. Dual-layer Caustics Texture sampling (Opposing directions)
                float2 uv1 = baseUV * _Texture2D_ST.xy + float2(_Time.y * _Scrolling_Speed, _Time.y * _Scrolling_Speed * 0.7);
                float2 uv2 = baseUV * _Texture2D_ST.xy * 1.15 + float2(1.0 - _Time.y * _Scrolling_Speed * 0.85, -_Time.y * _Scrolling_Speed * 0.9);

                fixed4 c1 = tex2D(_Texture2D, uv1);
                fixed4 c2 = tex2D(_Texture2D, uv2);
                float rawCaustics = (c1.r + c2.r) * 0.5;

                // Houidisoft PowerNode Contrast
                float sharpCaustics = pow(saturate(rawCaustics), max(1.0, _CausticContrast));

                // 2. Twirl Vortex on UV coordinates (Houidisoft Twirl Node)
                float2 twirlCenter = float2(0.5, 0.5);
                float twirlOffset = _Time.y * _Distortion_Speed * 3.14159;
                float2 uvTwirl = Twirl(baseUV, twirlCenter, _Twirl_Strength, twirlOffset);

                // 3. NormalFromHeight from GradientNoise on the twirled vortex
                float gNoise = UnityGradientNoise(uvTwirl, 7.0);
                float2 uvDx = uvTwirl + float2(0.006, 0.0);
                float2 uvDy = uvTwirl + float2(0.0, 0.006);
                float dX = UnityGradientNoise(uvDx, 7.0) - gNoise;
                float dY = UnityGradientNoise(uvDy, 7.0) - gNoise;
                float2 vortexNormal = float2(dX, dY) * 15.0;

                // 4. Dynamic Sine breathing pulse for Fresnel rim
                float pulse = 1.0 + sin(_Time.y * _Pulse_Speed) * _Pulse_Amplitude;
                float dynamicBorderSize = max(0.2, _Boarder_Size + sin(_Time.y * _Distortion_Speed * 2.5) * 0.35);

                // 5. Fresnel calculation (border falloff)
                float NdotV = 1.0 - saturate(dot(normal, viewDir));
                float fresnel = pow(NdotV, dynamicBorderSize);

                // Rìa ngoài HDR và hào quang thứ cấp đan xen caustics
                fixed4 rimGlow = _BoarderColor * pow(fresnel, max(0.5, _Boarder_Strength)) * pulse * 1.3;
                fixed4 secondaryGlow = _Boarder_Color * (fresnel * sharpCaustics * 2.2);

                // Thân plasma phát xạ
                fixed4 plasmaBody = _Color * (sharpCaustics * 1.35 + 0.12) * _BrightnessBoost;

                #if defined(_REFRACTION_ON)
                // 6. Optical background & core refraction: Twirl Vortex distortion
                float2 centerDelta = baseUV - float2(0.5, 0.5);
                float vortexMask = saturate(1.0 - length(centerDelta) * 1.4);
                // Vector xoay tiếp tuyến (tangential vortex spiral)
                float2 spiralDir = float2(-centerDelta.y, centerDelta.x);

                // Screen UV distortion vector:
                float2 screenDistort = (spiralDir * 2.5 + vortexNormal * 0.6) * (_RefractionStrength * vortexMask);
                float2 grabUV = (i.grabPos.xy / max(0.0001, i.grabPos.w)) + screenDistort;
                fixed4 sceneCol = tex2D(_VirtualRailPlasmaGrab, grabUV);

                // Composite: Refracted scene (containing the solid inner core) blended with plasma surface
                fixed4 finalColor;
                finalColor.rgb = sceneCol.rgb * (1.0 - saturate(fresnel * 0.65)) + plasmaBody.rgb + rimGlow.rgb + secondaryGlow.rgb;
                finalColor.a = 1.0;
                return finalColor;
                #else
                fixed4 finalColor = plasmaBody + rimGlow + secondaryGlow;
                finalColor.a = 0.0;
                return finalColor;
                #endif
            }
            ENDCG
        }
    }
    FallBack "Mobile/Particles/Additive"
}
