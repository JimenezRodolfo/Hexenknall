Shader "HEXENKNALL/Prototype" {
 Properties { _Color("Color",Color)=(1,1,1,1) }
 SubShader { Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass { Tags {"LightMode"="UniversalForward"}
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
 struct Varyings {float4 positionCS:SV_POSITION;float3 normalWS:TEXCOORD0;};
 CBUFFER_START(UnityPerMaterial)
 float4 _Color;
 CBUFFER_END
 Varyings vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(v.normalOS);return o;}
 half4 frag(Varyings i):SV_Target {float light=0.45+0.55*saturate(dot(normalize(i.normalWS),normalize(float3(-0.4,1,-0.3))));return half4(_Color.rgb*light,1);}
 ENDHLSL
 } }
}
