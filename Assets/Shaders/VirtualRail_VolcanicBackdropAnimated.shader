Shader "VirtualRail/VolcanicBackdropAnimated"
{
    Properties
    {
        _MainTex ("Backdrop Texture (16:9)", 2D) = "black" {}
        _Exposure ("Exposure Boost", Range(0.1, 3.0)) = 1.15
        _Tint ("Color Tint", Color) = (1, 1, 1, 1)

        [Header(Subtle Natural Animations)]
        _LavaPulseSpeed ("Lava Breathing Speed", Range(0.2, 3.0)) = 1.0
        _LavaGlowBoost ("Lava Glow Intensity", Range(0.0, 1.5)) = 0.5
        _SunPulseSpeed ("Sun Pulse Speed", Range(0.2, 3.0)) = 0.8
        _SunPulseAmount ("Sun Pulse Intensity", Range(0.0, 0.5)) = 0.15
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

            float _LavaPulseSpeed;
            float _LavaGlowBoost;
            float _SunPulseSpeed;
            float _SunPulseAmount;
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

                // 2. Dòng dung nham bập bùng nhẹ nhàng (Subtle Lava Breathing)
                float lavaScore = saturate((baseCol.r - baseCol.b * 1.5) * 2.5) * step(0.38, baseCol.r);
                float t_lava = _Time.y * _LavaPulseSpeed;
                float flowWave = sin(t_lava * 2.0 + (i.uv.x + i.uv.y) * 18.0) * 0.12 + 
                                 cos(t_lava * 1.0 - (i.uv.x - i.uv.y) * 25.0) * 0.08;
                color += baseCol.rgb * lavaScore * (flowWave * _LavaGlowBoost);

                // 3. Mặt trời: Nhịp phát sáng tự nhiên thuần túy (Luminance Breathing - 0 Màu mè)
                // Chỉ nhận diện vùng mặt trời ở góc trên-trái (UV: x < 0.25, y > 0.65) có độ sáng cao
                if (i.uv.x < 0.25 && i.uv.y > 0.65)
                {
                    float sunIntensity = saturate((baseCol.r + baseCol.g + baseCol.b) / 3.0 - 0.4);
                    float sunPulse = sin(_Time.y * _SunPulseSpeed) * _SunPulseAmount;
                    color += baseCol.rgb * (sunIntensity * sunPulse);
                }

                // 4. Sao vũ trụ sâu lấp lánh vi mô (Subtle Star Twinkle)
                if (baseCol.b > 0.45 && baseCol.r < 0.6 && i.uv.y > 0.4)
                {
                    float starHash = frac(sin(dot(floor(i.uv * 350.0), float2(12.9898, 78.233))) * 43758.5453);
                    float twinkle = sin(_Time.y * _StarTwinkleRate * 3.0 + starHash * 6.28) * 0.25;
                    color += fixed3(twinkle, twinkle, twinkle * 1.2);
                }

                fixed4 finalCol = fixed4(color * _Tint.rgb * _Exposure, 1.0);
                return finalCol;
            }
            ENDCG
        }
    }
    FallBack "VirtualRail/HorizonBackdrop"
}
