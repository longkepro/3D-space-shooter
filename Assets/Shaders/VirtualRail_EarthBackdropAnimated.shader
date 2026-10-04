Shader "VirtualRail/EarthBackdropAnimated"
{
    Properties
    {
        _MainTex ("Backdrop Texture (16:9)", 2D) = "black" {}
        _Exposure ("Exposure Boost", Range(0.1, 3.0)) = 1.0
        _Tint ("Color Tint", Color) = (1, 1, 1, 1)

        [Header(Subtle Natural Animations)]
        _StarPulseSpeed ("Star Breathing Speed", Range(0.2, 3.0)) = 1.0
        _StarPulseAmount ("Star Breathing Intensity", Range(0.0, 0.4)) = 0.12
        _AtmoShimmerSpeed ("Atmosphere Shimmer Speed", Range(0.2, 3.0)) = 2.0
        _AtmoShimmerAmount ("Atmosphere Shimmer Intensity", Range(0.0, 0.4)) = 0.08
        _CloudPulseSpeed ("Cloud Breathing Speed", Range(0.2, 3.0)) = 1.0
        _CloudPulseAmount ("Cloud Breathing Intensity", Range(0.0, 0.3)) = 0.06
        _StarTwinkleRate ("Star Twinkle Rate", Range(0.0, 3.0)) = 1.0
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

            float _StarPulseSpeed;
            float _StarPulseAmount;
            float _AtmoShimmerSpeed;
            float _AtmoShimmerAmount;
            float _CloudPulseSpeed;
            float _CloudPulseAmount;
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
                // 1. Ảnh gốc 100% (Solid celestial body - Không méo, không vẽ thêm vật thể lạ)
                fixed4 baseCol = tex2D(_MainTex, i.uv);
                fixed3 color = baseCol.rgb;

                // 2. Ngôi sao mẹ: Nhịp phát sáng tự nhiên thuần túy (Star Luminance Breathing)
                // Tâm sao tại UV (0.802, 0.724)
                float2 starUV = float2(0.802, 0.724);
                float distStar = distance(i.uv, starUV);
                if (distStar < 0.35)
                {
                    float starIntensity = saturate(1.0 - distStar * 3.5) * saturate((baseCol.r + baseCol.g + baseCol.b) / 3.0 - 0.3);
                    float starPulse = sin(_Time.y * _StarPulseSpeed) * _StarPulseAmount;
                    color += baseCol.rgb * (starIntensity * starPulse);
                }

                // 3. Khí quyển Trái Đất: Vành tán xạ Rayleigh bập bùng nhẹ (Rayleigh Shimmer)
                // Đường chân trời khí quyển dạng parabol chuẩn xác trong UV
                float horizonUV = 0.2056 + 0.7397 * i.uv.x - 0.7652 * (i.uv.x * i.uv.x);
                float distHorizon = abs(i.uv.y - horizonUV);
                if (distHorizon < 0.05)
                {
                    float atmoMask = exp(-(distHorizon * distHorizon) / 0.0004) * saturate(baseCol.b * 1.8);
                    float atmoPulse = sin(_Time.y * _AtmoShimmerSpeed) * _AtmoShimmerAmount;
                    color += fixed3(0.12, 0.25, 0.45) * (atmoMask * atmoPulse);
                }

                // 4. Dải mây khí quyển: Nhịp phản quang dịu nhẹ (Cloud Breathing)
                if (i.uv.y < horizonUV + 0.01)
                {
                    float cloudScore = saturate((baseCol.r + baseCol.g + baseCol.b) / 3.0 - 0.28) * 1.5;
                    float cloudPulse = sin(_Time.y * _CloudPulseSpeed) * _CloudPulseAmount;
                    color += baseCol.rgb * (cloudScore * cloudPulse);
                }

                // 5. Sao vũ trụ sâu: Lấp lánh vi mô (Subtle Star Twinkle)
                if (i.uv.y > horizonUV + 0.04 && distStar > 0.12)
                {
                    if (baseCol.r > 0.4 && baseCol.g > 0.4 && baseCol.b > 0.4)
                    {
                        float starHash = frac(sin(dot(floor(i.uv * 400.0), float2(12.9898, 78.233))) * 43758.5453);
                        float twinkle = sin(_Time.y * _StarTwinkleRate * 3.0 + starHash * 6.28) * 0.20;
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
