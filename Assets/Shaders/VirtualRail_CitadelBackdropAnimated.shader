Shader "VirtualRail/CitadelBackdropAnimated"
{
    Properties
    {
        _MainTex ("Backdrop Texture (16:9)", 2D) = "black" {}
        _Exposure ("Exposure Boost", Range(0.1, 3.0)) = 1.0
        _Tint ("Color Tint", Color) = (1, 1, 1, 1)

        [Header(Mothership Core Vortex Swirl)]
        _VortexCenter ("Vortex Center (UV)", Vector) = (0.72698, 0.64782, 0, 0)
        _VortexRadiusInner ("Inner Radius (Full Rotation)", Range(0.01, 0.2)) = 0.0440
        _VortexRadiusOuter ("Outer Radius (Zero Seam Blend)", Range(0.01, 0.2)) = 0.0556
        _VortexSpeed ("Vortex Rotation Speed", Range(-10.0, 10.0)) = 1.5708
        _PlasmaPulseSpeed ("Core Luminescence Speed", Range(0.5, 8.0)) = 3.1416
        _PlasmaPulseAmount ("Core Pulse Intensity", Range(0.0, 0.5)) = 0.12

        [Header(Emerald Planet Atmosphere)]
        _PlanetAtmosphereGlow ("Planet Atmosphere Breathing", Range(0.0, 0.3)) = 0.08

        [Header(Deep Space Starfield)]
        _StarTwinkleRate ("Star Twinkle Rate", Range(0.5, 4.0)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Geometry+501" 
            "RenderType" = "Opaque" 
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
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
            fixed _Exposure;
            fixed4 _Tint;

            float4 _VortexCenter;
            float _VortexRadiusInner;
            float _VortexRadiusOuter;
            float _VortexSpeed;
            float _PlasmaPulseSpeed;
            float _PlasmaPulseAmount;

            float _PlanetAtmosphereGlow;
            float _StarTwinkleRate;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1. Tọa độ hiệu chỉnh tỉ lệ 16:9 để vòng tròn đẳng hướng (Aspect-corrected space)
                float aspect = 16.0 / 9.0;
                float2 delta = i.uv - _VortexCenter.xy;
                delta.x *= aspect;
                float r = length(delta);

                // 2. Lấy mẫu màu tĩnh gốc
                fixed4 baseCol = tex2D(_MainTex, i.uv);
                fixed3 color = baseCol.rgb;

                // 3. Xoay chuyển động xoáy ốc cho lõi tàu mẹ (Mothership Core Vortex Swirl)
                // GPU tính toán theo thời gian liên tục _Time.y -> 100% không bao giờ giật cục (Zero Loop Stutter)
                if (r < _VortexRadiusOuter)
                {
                    float angle = atan2(delta.y, delta.x);
                    float rotation = -_Time.y * _VortexSpeed;
                    float newAngle = angle + rotation;

                    float2 rotDelta = float2(cos(newAngle), sin(newAngle)) * r;
                    rotDelta.x /= aspect;
                    float2 rotatedUV = _VortexCenter.xy + rotDelta;

                    fixed4 rotatedCol = tex2D(_MainTex, rotatedUV);

                    // Hàm nội suy bậc 3 Smooth Hermite để triệt tiêu hoàn toàn đường viền tiếp giáp cơ khí
                    float t = saturate((_VortexRadiusOuter - r) / max(_VortexRadiusOuter - _VortexRadiusInner, 0.0001));
                    float blendMask = t * t * (3.0 - 2.0 * t);

                    color = lerp(color, rotatedCol.rgb, blendMask);

                    // Xung nhịp phát quang nhịp tim của lò phản ứng plasma
                    float pulse = 1.0 + _PlasmaPulseAmount * sin(_Time.y * _PlasmaPulseSpeed) * blendMask;
                    color *= pulse;
                }

                // 4. Nhịp thở khí quyển hành tinh ngọc lục bảo (Emerald Planet Horizon Atmosphere)
                // Vùng khí quyển chân trời ở nửa dưới i.uv.y < 0.32 và mang sắc lục cyan
                if (i.uv.y < 0.32 && baseCol.g > 0.30 && baseCol.g > baseCol.r)
                {
                    float atmoPulse = sin(_Time.y * 1.2) * _PlanetAtmosphereGlow * saturate((0.32 - i.uv.y) * 4.0);
                    color += fixed3(0.0, atmoPulse * 0.7, atmoPulse * 0.5);
                }

                // 5. Sao vũ trụ sâu lấp lánh tự nhiên (Subtle Starfield Twinkle)
                if (i.uv.x < 0.50 && i.uv.y > 0.35)
                {
                    if (baseCol.r > 0.40 && baseCol.g > 0.40 && baseCol.b > 0.40)
                    {
                        float starHash = frac(sin(dot(floor(i.uv * 500.0), float2(12.9898, 78.233))) * 43758.5453);
                        float twinkle = sin(_Time.y * _StarTwinkleRate * 3.0 + starHash * 6.28) * 0.15;
                        color += fixed3(twinkle, twinkle * 1.1, twinkle * 1.3);
                    }
                }

                fixed4 finalCol = fixed4(color * _Tint.rgb * _Exposure, 1.0);
                return finalCol;
            }
            ENDCG
        }
    }
    FallBack "VirtualRail/HorizonBackdrop"
}
