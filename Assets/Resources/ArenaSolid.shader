Shader "DigitalArena/Solid"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 position : SV_POSITION; float3 normal : TEXCOORD0; float3 world : TEXCOORD1; };
            fixed4 _Color;
            v2f vert(appdata v)
            {
                v2f o; o.position = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.world = mul(unity_ObjectToWorld, v.vertex).xyz; return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float light = saturate(dot(normalize(i.normal), normalize(float3(-0.5,1,-0.6))));
                float3 color = _Color.rgb * (0.48 + 0.52 * light);
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}
