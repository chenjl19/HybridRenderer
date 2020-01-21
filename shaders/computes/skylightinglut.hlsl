#include "global.inc"

cbuffer Constants : register(b0) {
    float4 _LUTTexelSize;
    float4 _ZNearFar;
    float4 _GlobalViewOrigin;
    float4 _RadiusPlanet;
    float4 _RadiusAtmosphere;
    float4 _MieDensityScale;
    float4 _RayleighHeight;
    float4 _RayleighColor;
    float4 _MieColor;
    float4 _MieHeight;
    float4 _MieAnisotropy;
    float4 _SunLatitude;
    float4 _SunLongitude;
    float4 _ScatteringDensityScale;
    float4 _SunIntensity;
    float4 _DecoupleSunColorFromSky;
    float4 _SunColor;
    float4 _SunForward;
};

#include "skylighting.inc"

[numthreads(8, 8, 8)]
void ComputeSkylightingLUT(uint3 id : SV_DispatchThreadID) {
    const float3 tc = (float3(id.xyz) + 0.5) * _LUTTexelSize.xyz;
    float3 scatterColor;
    SkyLightingLUT(tc, id, scatterColor);

}