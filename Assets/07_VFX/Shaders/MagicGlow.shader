Shader "HEXENKNALL/MagicGlow"
{
 Properties { _Tint("Tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
  Pass
  {
   Blend SrcAlpha One
   ZWrite Off
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   CBUFFER_START(UnityPerMaterial)
   float4 _Tint;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float4 color:COLOR; };
   struct Varyings { float4 positionCS:SV_POSITION; float4 color:COLOR; };
   Varyings vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.color=v.color*_Tint;return o;}
   half4 frag(Varyings i):SV_Target{return i.color;}
   ENDHLSL
  }
 }
}
