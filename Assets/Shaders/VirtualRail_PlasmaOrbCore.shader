Shader "VirtualRail/PlasmaOrbCore"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color (Center - Hot White)", Color) = (2.5, 3.5, 2.8, 1.0)
        [HDR] _RimColor ("Rim Color (Edge - HDR Glow)", Color) = (0.168, 3.211, 0.426, 1.0)
        _CoreFalloff ("Core Falloff / Sharpness", Range(0.5, 8.0)) = 1.8
        _PulseSpeed ("Pulsing Speed", Range(0.0, 20.0)) = 8.0
        _PulseIntensity ("Pulsing Depth", Range(0.0, 0.5)) = 0.1
    }

    SubShader
    {
        // Render in Geometry queue so GrabPass in the outer sphere captures the solid luminous core
        Tags { "Queue"="Geometry+10" "RenderType"="Opaque" "IgnoreProjector"="True" }
        ZWrite On
        Cull Back
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            fixed4 _CoreColor;
            fixed4 _RimColor;
            float _CoreFalloff;
            float _PulseSpeed;
            float _PulseIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 norm = normalize(i.normal);
                float3 view = normalize(i.viewDir);
                float ndotv = saturate(dot(norm, view));

                // Nhân lõi đặc: tâm trắng nóng cực sáng, rìa xanh ngọc rực rỡ
                float core = pow(ndotv, max(0.1, _CoreFalloff));
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseIntensity;
                fixed4 finalCol = lerp(_RimColor, _CoreColor, core) * pulse;
                finalCol.a = 1.0;
                return finalCol;
            }
            ENDCG
        }
    }
    FallBack Off
}
