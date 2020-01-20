#define USE_ROUGHNESS
#include "global.inc"
#include "viewConstants.inc"
#include "instanceConstants.inc"

struct v2f {
	float4 pos : SV_POSITION;
    float4 texcoord0 : TEXCOORD0;
    float3 positionWS : TEXCOORD1;
    float3 tangentWS : TEXCOORD2;
    float3 bitangentWS : TEXCOORD3;
    float3 normalWS : TEXCOORD4;
#ifdef USE_LIGHTMAPS
	float2 texcoord1 : TEXCOORD5;
#endif
};

cbuffer MaterialConstants {
    float4 _MainTex_ST;
    float4 _Color;
	float4 _EmissionColor;
	float4 _Metal;
	float4 _Roughness;
	float4 _Specularity;
	float4 _Occlusion;
#ifdef USE_FORWARD_SKIN
	float4 _TransWeight;
	float4 _TransPower;
	float4 _TransDistortion;
	float4 _TransShadowWeight;
	float4 _SssMaskCutoff;
	float4 _SssWeight;
	float4 _SssScale;
	float4 _SssBias;
	float4 _SssBumpBlur;
	float4 _SssColorBleedAoWeights;
	float4 _SssTransmissionAbsorption;
#endif
};

#ifdef __VS__

#include "vertex.inc"

v2f main(in DRAWVERT_Input a2v) {
	v2f o = (v2f)0;
    DECODE_POSITION_AND_NORMALS(a2v.vertex, a2v.normal, a2v.tangent, localPosition, localNormal, localTangent, tangentW);
#ifdef USE_LIGHTMAPS
	o.texcoord1.xy = a2v.lightmapST.xy * _Lightmap_ST.xy + _Lightmap_ST.zw;
#else
	if(_ModelIndices.w == 1) {
		SKIN_POSITION_AND_NORMALS(_ModelMatrices, _ModelIndices.z, a2v.jointIndices, a2v.jointWeights, localPosition, localNormal, localTangent);
	}
#endif
    OUTPUT_WORLD_NORMALS_SCALED(localNormal, localTangent, _WorldToLocalMatrix0, _WorldToLocalMatrix1, _WorldToLocalMatrix2, o.normalWS, o.tangentWS);
	OUTPUT_WORLD_POSITION(localPosition, _LocalToWorldMatrix0, _LocalToWorldMatrix1, _LocalToWorldMatrix2, o.positionWS);
	OUTPUT_CLIP_POSITION(o.positionWS, _ViewProjectionMatrix0, _ViewProjectionMatrix1, _ViewProjectionMatrix2, _ViewProjectionMatrix3, o.pos);
	o.texcoord0.xy = a2v.st.xy * _MainTex_ST.xy + _MainTex_ST.zw;
	return o;
}
#endif

#ifdef __PS__

Texture2D _MainTex;
Texture2D _SpecTex;
Texture2D _BumpMap;
Texture2D _EmissionMap;
Texture2D _SssBrdfTex;
Texture2D _LightmapDirTex;
Texture2D _LightmapColorTex;

#include "lightingPass.inc"
#include "lit.inc"

[earlydepthstencil]
GBuffer main(in v2f frag) {
    GBuffer gbuffer = (GBuffer)0;
    ForwardLit(frag, gbuffer);
    return gbuffer;
}
#endif