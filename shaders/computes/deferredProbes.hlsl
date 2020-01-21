#include "global.inc"

cbuffer Constants : register(b0) {
	float4 _GlobalViewOrigin;
    float4 _FrustumVectorLT;
    float4 _FrustumVectorRT;
    float4 _FrustumVectorLB;
    float4 _FrustumVectorRB;
    float4 _DepthBufferParms;
    float4 _ScreenParms;
    float4 _ClusteredLightingSize;
    float4 _ClusteredLightingParms;
};

#include "deferredProbes.inc"

RWTexture2D<float4> _UAV;

[numthreads(8, 8, 1)]
void ComputeDeferredProbes(uint2 id : SV_DISPATCHTHREADID) {
    if(id.x < (uint)_ScreenParms.x && id.y < (uint)_ScreenParms.y) {
        const float2 tc = (float2(id.xy) + 0.5) * _ScreenParms.zw;
        float3 outputColor;
        DeferredProbes(tc.xy, id.xy, outputColor);
        _UAV[id.xy] = float4(outputColor.xyz, 0);
    }
}