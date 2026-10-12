Shader "VirtualRail/ThermalReactorTrench"
{
    Properties
    {
        [Header(Base Energy Core)]
        [HDR] _CoreColor ("Idle Core Color (Incandescent Orange)", Color) = (3.2, 1.3, 0.2, 1.0)
        [HDR] _EdgeColor ("Trench Edge Glow (Deep Amber)", Color) = (1.8, 0.45, 0.05, 1.0)
        [HDR] _PulseColor ("Overcharge Pulse Color (Hot White Yellow)", Color) = (5.5, 4.5, 2.2, 1.0)
        [HDR] _StunColor ("Short-Circuit Stun Color (Electric Blue)", Color) = (0.8, 2.0, 4.0, 1.0)

        [Header(Structure and Motion)]
        _RibFrequency ("Ribbed Slats Frequency", Float) = 18.0
        _RibDepth ("Rib Contrast Depth", Range(0.0, 1.0)) = 0.65
        _FlowSpeed ("Plasma Flow Speed", Float) = 3.5

        [Header(Interactive Dynamic States)]
        _PhaseActive ("Phase Active (0 = Dormant, 1 = Exposed)", Range(0, 1)) = 0.0
        _ChargeProgress ("Charge Progress (0 = Idle, 1 = Max Charge)", Range(0, 1)) = 0.0
        _StunIntensity ("Stun Intensity (0 = Normal, 1 = Disabled)", Range(0, 1)) = 0.0
        _MeltdownIntensity ("Phase 3 Meltdown Runaway", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue"="Geometry+50" 
            "RenderType"="Opaque" 
            "IgnoreProjector"="True" 
        }
        LOD 200

        Pass
        {
            Name "REACTOR_CORE_FORWARD"
            ZWrite On
            Cull Back
            Lighting Off

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
            };

            fixed4 _CoreColor;
            fixed4 _EdgeColor;
            fixed4 _PulseColor;
            fixed4 _StunColor;

            float _RibFrequency;
            float _RibDepth;
            float _FlowSpeed;

            float _PhaseActive;
            float _ChargeProgress;
            float _StunIntensity;
            float _MeltdownIntensity;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Neu o Pha 1 (Dormant): Loi nang luong mo toi duoi vo giap
                float activeFac = saturate(_PhaseActive + _MeltdownIntensity);
                if (activeFac < 0.01)
                {
                    return fixed4(0.04, 0.02, 0.01, 1.0);
                }

                float time = _Time.y;
                float zCoord = i.uv.y;

                // 1. Hoa tiet cac thanh nan kim loai chan nhiet (Ribbed Slats)
                float ribPattern = abs(sin(zCoord * _RibFrequency * 3.14159));
                ribPattern = smoothstep(0.2, 0.8, ribPattern);
                float ribDarkening = lerp(1.0 - _RibDepth, 1.0, ribPattern);

                // 2. Dong chay plasma ben trong ranh (Conduit Flow)
                float flow = sin((zCoord * 12.0) - (time * _FlowSpeed));
                float flowPattern = 0.85 + 0.15 * flow;

                // 3. Fresnel vien ranh
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float ndotv = saturate(dot(normalize(i.worldNormal), viewDir));
                float fresnel = pow(1.0 - ndotv, 2.0);

                // 4. Mau co ban cua lo phan ung (Idle Orange/Amber)
                fixed4 baseOutput = lerp(_CoreColor, _EdgeColor, fresnel * 0.6) * (flowPattern * ribDarkening);

                // 5. Trang thai Meltdown Pha 3: Nen vo tau chay do ruc qua nhiet
                if (_MeltdownIntensity > 0.001)
                {
                    float meltPulse = 0.7 + 0.3 * sin(time * 8.0);
                    fixed4 meltdownBase = lerp(_EdgeColor * 2.2, _PulseColor * 1.2, meltPulse);
                    baseOutput = lerp(baseOutput, meltdownBase, _MeltdownIntensity);
                }

                // 6. Xung nhip nap nang luong (Charge Pulse) - Uu tien hien thi ro ret tren nen vo
                if (_ChargeProgress > 0.001)
                {
                    // Tan so nhap nhay tang toc ti le thuan voi tien trinh sac (4Hz -> 26Hz)
                    float pulseRate = lerp(4.0, 26.0, _ChargeProgress);
                    float pulseWave = 0.5 + 0.5 * sin(time * pulseRate);

                    // O Pha 3: Nap Hyper Laser phat ra luong dien quang sieu cao ap mau Xanh dien Neon (Blinding Electric Cyan Flare)
                    fixed4 hyperLaserChargeColor = fixed4(3.2, 4.8, 7.5, 1.0);
                    fixed4 activeChargeTarget = lerp(_PulseColor * 2.0, hyperLaserChargeColor, _MeltdownIntensity);

                    baseOutput = lerp(baseOutput, activeChargeTarget, pulseWave * _ChargeProgress);
                }

                // 7. Trang thai Stun / Chap mach chap tat roi toi den
                if (_StunIntensity > 0.001)
                {
                    float glitch = step(0.4, frac(sin(time * 35.0) * 43758.5453));
                    fixed4 stunOutput = _StunColor * glitch * 2.0;
                    fixed4 deadCore = fixed4(0.08, 0.07, 0.06, 1.0); // Loi kim loai chay den
                    baseOutput = lerp(baseOutput, stunOutput, _StunIntensity * 0.7);
                    baseOutput = lerp(baseOutput, deadCore, _StunIntensity * (1.0 - glitch * 0.4));
                }

                return baseOutput * activeFac;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
