Shader "DigitalArena/TypeBadge"
{
    Properties { _MainTex("Type PNG",2D)="white"{} _ElementColor("Symbol element color",Color)=(1,0,0,1) }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _ElementColor, _IconScale, _IconRect;
            float SymbolMask(float2 positionUV)
            {
                float2 local=(positionUV-.5)/_IconScale.xy+.5;
                float2 uv=_IconRect.xy+local*_IconRect.zw;
                fixed4 sample=tex2D(_MainTex,uv);
                float inside=step(0,local.x)*step(local.x,1)*step(0,local.y)*step(local.y,1);
                return saturate((max(sample.r,sample.g)-sample.b)*1.5)*sample.a*inside;
            }
            fixed4 frag(v2f_img i):SV_Target
            {
                float radius=length((i.uv-.5)*2);
                float mask=SymbolMask(i.uv);
                float outline=max(max(SymbolMask(i.uv+float2(.012,0)),SymbolMask(i.uv-float2(.012,0))),max(SymbolMask(i.uv+float2(0,.012)),SymbolMask(i.uv-float2(0,.012))));
                float3 color=lerp(float3(.84,.86,.88),float3(.20,.23,.27),outline);
                color=lerp(color,_ElementColor.rgb,mask);
                color=lerp(color,float3(.29,.33,.38),smoothstep(.88,.92,radius));
                return float4(color,1-smoothstep(.965,1,radius));
            }
            ENDCG
        }
    }
}
