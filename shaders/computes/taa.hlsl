cbuffer ComputeConstants : register(b0) {
	float4 _GlobalViewOrigin;
    float4 _FrustumVectorLT;
    float4 _FrustumVectorRT;
    float4 _FrustumVectorLB;
    float4 _FrustumVectorRB;
    float4 _ScreenParms;
    float4 _DepthBufferParms;
    float4 _SunForward;
	float4 _ClusteredLightingSize;
    float4 _ClusteredLightingParms;
    float4 _ShadowsAtlasResolution;
	float4 _TimeParms;
	float4x4 _ViewMatrix;
	float4x4 _InverseViewMatrix;
    float4x4 _InverseProjectionMatrix;
    float4x4 _PrevViewProjectionMatrixNoJitter;
};

cbuffer Constants : register(b1) {
    float4 _RenderPositionToViewTexture;
    float4 _FilterWeights0;
	float4 _FilterWeights1;
	float4 _FilterWeights3;
	float4 _FilterWeights4;
	float4 _FilterWeights2;
	float4 _FilterWeights5;
};

Texture2D _ViewColorTex;
Texture2D _ViewPrevColorTex;
Texture2D _ViewOpaqueColorTex;
Texture2D _ViewDepthTex;
SamplerState _LinearClampSampler;
RWTexture2D<float4> _UAV;

#include "global.inc"
#include "taa.inc"

[numthreads(8, 8, 1)]
void main(uint3 dispatchThreadId : SV_DispatchThreadID) {
    const float2 tc = (float2(dispatchThreadId.xy) + 0.5) * _RenderPositionToViewTexture.zw;
    float4 resultColor;
    ComputeTAA(tc, dispatchThreadId, resultColor);
    _UAV[dispatchThreadId.xy] = resultColor;
}