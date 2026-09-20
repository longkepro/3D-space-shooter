Shader "VirtualRail/ArcadeHullOutlineStandard"
{
    Properties
    {
        _Color ("Albedo Tint", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _GlossMapScale ("Smoothness Scale", Range(0,1)) = 1.0
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _MetallicGlossMap ("Metallic", 2D) = "white" {}
        
        _BumpScale ("Bump Scale", Float) = 1.0
        _BumpMap ("Normal Map", 2D) = "bump" {}
        
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _EmissionMap ("Emission", 2D) = "black" {}

        // Arcade Inverted Hull Outline Settings
        _OutlineColor ("Outline Color", Color) = (0.85, 0.20, 1.20, 1.0)
        _OutlineWidth ("Outline Width", Range(0.0005, 0.02)) = 0.0035
        _OutlineZBias ("Outline Z Bias", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 250

        // PASS 1: INVERTED HULL OUTLINE PASS (SCREEN-SPACE CONSTANT PIXEL WIDTH)
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="Always" }
            Cull Front
            ZWrite On
            ZTest LEqual
            Offset 2, 2

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
            };

            float4 _OutlineColor;
            float _OutlineWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);

                // Screen-space extrusion: transform normal to view space, then project to clip space
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float2 offset = float2(viewNormal.x * UNITY_MATRIX_P[0][0], viewNormal.y * UNITY_MATRIX_P[1][1]);

                float len = length(offset);
                if (len > 1e-5)
                {
                    offset /= len;
                }

                // Aspect ratio compensation for isotropic screen pixel thickness
                float aspect = (_ScreenParams.y > 0.0) ? (_ScreenParams.x / _ScreenParams.y) : 1.7777778;
                offset.x /= aspect;

                // Extrude in clip space proportional to w (yielding constant screen pixel width)
                o.pos.xy += offset * _OutlineWidth * o.pos.w;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }

        // PASS 2: STANDARD SURFACE SHADER BASE PASS
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        #pragma shader_feature _NORMALMAP
        #pragma shader_feature _METALLICGLOSSMAP
        #pragma shader_feature _EMISSION

        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _MetallicGlossMap;
        sampler2D _EmissionMap;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_BumpMap;
        };

        fixed4 _Color;
        fixed4 _EmissionColor;
        half _Glossiness;
        half _GlossMapScale;
        half _Metallic;
        half _BumpScale;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;

            #if defined(_METALLICGLOSSMAP)
                fixed4 mg = tex2D (_MetallicGlossMap, IN.uv_MainTex);
                o.Metallic = mg.r;
                o.Smoothness = mg.a * _GlossMapScale;
            #else
                o.Metallic = _Metallic;
                o.Smoothness = _Glossiness;
            #endif

            #if defined(_NORMALMAP)
                o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_BumpMap), _BumpScale);
            #endif

            #if defined(_EMISSION)
                fixed4 e = tex2D (_EmissionMap, IN.uv_MainTex) * _EmissionColor;
                o.Emission = e.rgb;
            #endif

            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Standard"
}
