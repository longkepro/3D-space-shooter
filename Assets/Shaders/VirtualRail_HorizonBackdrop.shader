Shader "VirtualRail/HorizonBackdrop"
{
    Properties
    {
        _MainTex ("Backdrop Texture (16:9)", 2D) = "black" {}
        _Exposure ("Exposure Boost", Range(0.1, 3.0)) = 1.2
        _Tint ("Color Tint", Color) = (1, 1, 1, 1)
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
            #pragma target 2.0
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

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Tint * _Exposure;
                col.a = 1.0;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Unlit/Texture"
}
