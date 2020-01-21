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
    float4x4 _ViewProjectionMatrix;
    float4x4 _PrevViewProjectionMatrix;
    float4x4 _InverseViewProjectionMatrix;
};

cbuffer Constants : register(b1) {
    float4 _RenderPositionToViewTexture;
    float4 _SSRParms; // smoothnessThreshold, scale, 0, ditherSampleOffset
};

Texture2D _ViewColorTex;
Texture2D _ViewDepthTex;
Texture2D _ViewNormalTex;
Texture2D _ViewSpecularTex;
SamplerState _PointClampSampler;
RWTexture2D<float4> _UAV;
RWTexture2D<float> _UAVMask;

#include "global.inc"
#include "ssr.inc"

[numthreads(8, 8, 1)]
void main(uint2 dispatchThreadId : SV_DispatchThreadID) {
    const float2 tc = (float2(dispatchThreadId.xy) + 0.5) * _RenderPositionToViewTexture.zw;
    float4 resultColor;
    ComputeSSR(tc, dispatchThreadId, resultColor);
    _UAV[dispatchThreadId.xy] = float4(resultColor.rgb, 1);
    _UAVMask[dispatchThreadId.xy] = resultColor.a;
}