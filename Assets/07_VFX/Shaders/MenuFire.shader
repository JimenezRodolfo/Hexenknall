Shader "HEXENKNALL/MenuFire"
{
 Properties { _MainTex("Video",2D)="black"{} _Clock("Clock",Float)=0 _Embers("Embers",Float)=.65 _Defeat("Defeat",Float)=0 _Prepared("Prepared",Float)=0 _BackdropOnly("Backdrop only",Float)=0 }
 SubShader
 {
  Cull Off ZWrite Off ZTest Always
  Pass
  {
   CGPROGRAM
   #pragma vertex vert_img
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;float _Clock,_Embers,_Defeat,_Prepared,_BackdropOnly;
   float hash(float p){return frac(sin(p*127.1)*43758.5453);}
   fixed4 frag(v2f_img i):SV_Target
   {
    float2 uv=i.uv;float2 top=float2(uv.x,1-uv.y);
    float3 color=tex2D(_MainTex,uv).rgb;
    float2 low=lerp(float2(.377,.505),float2(.332,.434),_Defeat);
    float2 high=lerp(float2(.624,.872),float2(.663,.715),_Defeat);
    float mask=smoothstep(low.x-.012,low.x,top.x)*(1-smoothstep(high.x,high.x+.012,top.x));
    mask*=smoothstep(low.y-.015,low.y,top.y)*(1-smoothstep(high.y,high.y+.015,top.y));
    color=lerp(color,float3(.017,.008,.031),mask*.98);
    if(_BackdropOnly>.5)color*=.16;
    if(_Prepared<.5)
    {
     float height=.12+.09*sin(top.x*27+_Clock*1.8)+.04*sin(top.x*61-_Clock*3);
     float flame=pow(saturate((top.y-(1-height))/.25),2);
     color=float3(.015,.008,.03)+flame*float3(.6,.12,1.0);
    }
    float sparks=0;
    for(int n=0;n<28;n++)
    {
     float x=hash(n+3.0),speed=.08+.08*hash(n+51.0);
     float y=1-frac(_Clock*speed+hash(n+17.0));
     float2 p=float2(x+.015*sin(_Clock+n*2),y);
     float2 d=(top-p)*float2(1.7778,1);
     sparks+=exp(-dot(d,d)*80000)*(.45+.55*pow(sin(_Clock*2+n),2));
    }
    color+=sparks*_Embers*float3(.8,.3,1.4);
    return float4(color,1);
   }
   ENDCG
  }
 }
}
