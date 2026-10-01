Shader "VirtualRail/PlasmaOrbCore"
{
    Properties
    {
        [HDR] _CoreColor ("Core Color (Center)", Color) = (3.0, 2.8, 2.2, 1.0)
        [HDR] _RimColor ("Rim Color (Edge)", Color) = (2.5, 1.0, 0.1, 1.0)
        _CoreFalloff ("Core Sharpness", Range(0.5, 8.0)) = 2.5
        _PulseSpeed ("Pulsing Speed", Range(0.0, 20.0)) = 8.0
        _PulseIntensity ("Pulsing Depth", Range(0.0, 0.5)) = 0.15
    }

    SubShader
    {
        Tags { "Queue"="Transparent+10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend One One
        ZWrite Off
        Cull Off
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

                // Phối trộn lõi trắng nóng trung tâm ra rìa cam rực rỡ
                float core = pow(ndotv, _CoreFalloff);
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseIntensity;
                fixed4 finalCol = lerp(_RimColor, _CoreColor, core) * pulse;
                return finalCol;
            }
            ENDCG
        }
    }
    FallBack Off
}
