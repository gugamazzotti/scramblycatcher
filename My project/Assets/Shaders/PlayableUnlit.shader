Shader "Playable/UnlitColor"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float wrap : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.wrap = saturate(worldNormal.y * 0.5 + 0.55);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float shade = 0.62 + 0.38 * i.wrap;
                return fixed4(_Color.rgb * shade, 1);
            }
            ENDCG
        }
    }
    Fallback Off
}
