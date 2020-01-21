#include "global.inc"

cbuffer CompositeConstants : register(b0) {
	float4 _RenderPositionToViewTexture;
	float4 _InputMainTextureSize;
	float4 _SSAOParms;
    float4 _ZNearFar;
    float4 _SunLatitude;
    float4 _SunLongitude;
    float4 _SunDiskScale;
    float4 _SunColor;
    float4 _SunIntensity;
    float4 _DecoupleSunColorFromSky;
	float4 _VolumetricLightingResolution;
	float4 _VolumetricLightingZNearFar;
	float4 _ScatteringFlags;
	float4 _SSRMode;
};

cbuffer ComputeConstants : register(b1) {
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

Texture2D _ViewOpaqueColorTex : register(t0);
Texture2D _ViewDepthTex : register(t1);
Texture2D _ViewDepthDownres2xTex : register(t2);
Texture2D _SSAOTex : register(t3);
Texture3D _SkyLightingLUT : register(t4);
Texture2D _IndirectSpecularTex : register(t5);
Texture3D _ScatteringPacked0Tex : register(t6);
Texture3D _ScatteringPacked1Tex : register(t7);
Texture2D _SSRColorTex : register(t8);
Texture2D _SSRMaskTex : register(t9);
SamplerState _PointClampSampler : register(s0);
SamplerState _LinearSampler : register(s1);
SamplerState _LinearClampSampler : register(s2);
RWTexture2D<float4> _UAVViewColor : register(u0);

groupshared float g_depths[8 * 8];
groupshared float g_ao[8 * 8];

#include "composite.inc"

[numthreads(8, 8, 1)]
void main(uint2 id : SV_DISPATCHTHREADID, uint2 localID : SV_GROUPTHREADID, uint groupIndex : SV_GROUPINDEX) {
	float2 tcCenter = (float2)id.xy - (float2)localID.xy - _float2(2.0) + (float2)localID.xy * _float2(2.0);
	tcCenter *= _RenderPositionToViewTexture.zw;
	g_depths[groupIndex] = SAMPLE_TEXTURE2D_LOD(_ViewDepthDownres2xTex, _PointClampSampler, tcCenter.xy, 0).x;
	g_ao[groupIndex] = SAMPLE_TEXTURE2D_LOD(_SSAOTex, _PointClampSampler, tcCenter.xy, 0).x;
    GroupMemoryBarrierWithGroupSync();

	float4 outputColor;
	const float2 tc = (float2(id.xy) + 0.5) * _RenderPositionToViewTexture.zw;
	ComputeComposite(tc, id, localID, outputColor);
	_UAVViewColor[id.xy] = outputColor;
}

[numthreads(8, 8, 1)]
void ComputeComposite(uint2 id : SV_DISPATCHTHREADID, uint2 localID : SV_GROUPTHREADID, uint groupIndex : SV_GROUPINDEX) {
	//if (id.x >= (uint)_RenderPositionToViewTexture.x || id.y >= (uint)_RenderPositionToViewTexture.y) {
	//	return;
	//}   

	float2 tcCenter = (float2)id.xy - (float2)localID.xy - _float2(2.0) + (float2)localID.xy * _float2(2.0);
	tcCenter *= _RenderPositionToViewTexture.zw;
	g_depths[groupIndex] = SAMPLE_TEXTURE2D_LOD(_ViewDepthDownres2xTex, _PointClampSampler, tcCenter.xy, 0).x;
	g_ao[groupIndex] = SAMPLE_TEXTURE2D_LOD(_SSAOTex, _PointClampSampler, tcCenter.xy, 0).x;
    GroupMemoryBarrierWithGroupSync();

	const float2 tc = (float2(id.xy) + 0.5) * _RenderPositionToViewTexture.zw;
	const float2 tcNorm = float2(id.xy) * _RenderPositionToViewTexture.zw;

	float3 outputColor = _float3(0);
    const float ndcZ = SAMPLE_TEXTURE2D_LOD(_ViewDepthTex, _PointClampSampler, tc.xy, 0).x;
	[branch] if(ndcZ < 1.0) {
		float2 ao = _float2(1);
		float2 sum = _float2(0);
		const uint localHalfIndex = int(localID.x) / 2 + (int(localID.y) / 2) * 8;
		for(uint y = 0; y < 4; y++) {
			for(uint x = 0; x < 4; x++) {
				uint a = localHalfIndex + x + y * 8;
				float z = g_depths[a] - ndcZ;
				float w = exp(-z * z * 1000000.0);
				sum += float2(g_ao[a] * w, w);
			}
		}
		const float2 ssaoParms = float2(1, 1);
		ao = saturate(pow(_float2(sum.x / sum.y), ssaoParms));

		const float3 viewColor = SAMPLE_TEXTURE2D_LOD(_ViewOpaqueColorTex, _PointClampSampler, tc.xy, 0).xyz;
		const float3 indirectSpecular = SAMPLE_TEXTURE2D_LOD(_IndirectSpecularTex, _PointClampSampler, tc.xy, 0).xyz;
		outputColor = float4(viewColor.xyz * ao.y + indirectSpecular, ao.x);
	}

	const uint scatteringFlags = (uint)_ScatteringFlags.x;
    float4 scattering = float4(0, 0, 0, 1);
    {
		const uint2 idxPix = id.xy & 3;
		float sunVolumeShadow = 1;
		const float linearZ = GetLinearDepth(ndcZ);		
		float2 tcJitteredLocalScattering = _float2(0);// Hammersley2D_4bits(16, (idxPix.x + idxPix.y * 4 + int(_TimeParms.w)) & 0x7) * 2.0 - 1.0;// float2(0, 0);
		tcJitteredLocalScattering = (tcJitteredLocalScattering + float2(0.5, 0.5)) / _VolumetricLightingResolution.xy;
		const float2 tcLocalScattering = tcNorm.xy + tcJitteredLocalScattering;
		[branch] if(scatteringFlags & (1 << 1)) {
			const float zNear = _VolumetricLightingZNearFar.x;
			const float zFar = _VolumetricLightingZNearFar.y;
			const float numSlices = _VolumetricLightingResolution.z;
			float volumeZ = pow((linearZ - zNear) / (zFar - zNear), 1.0 / 2.0);
			volumeZ = (volumeZ * (numSlices - 1)) / numSlices;
			const float2 scatteringPacked0 = SAMPLE_TEXTURE3D_LOD(_ScatteringPacked0Tex, _LinearClampSampler, float3(tcLocalScattering.xy, volumeZ), 0).rg;
			scattering.rgb = SAMPLE_TEXTURE3D_LOD(_ScatteringPacked1Tex, _LinearClampSampler, float3(tcLocalScattering.xy, volumeZ), 0).rgb;
			scattering.a = scatteringPacked0.r;
			sunVolumeShadow = scatteringPacked0.g;
		}

		float3 skyScattering = _float3(0);
		[branch] if(scatteringFlags & (1 << 0)) {
            const float3 frustumVecX0 = lerp(_FrustumVectorLT.xyz, _FrustumVectorRT.xyz, tcNorm.x); 
            const float3 frustumVecX1 = lerp(_FrustumVectorLB.xyz, _FrustumVectorRB.xyz, tcNorm.x);
            const float3 frustumVec = lerp(frustumVecX0, frustumVecX1, tcNorm.y);
			float3 skylightingInputsLUT = _float3(0);
			float3 viewNormalized = normalize(frustumVec);
			skylightingInputsLUT.x = clamp(fastAcos(viewNormalized.z) / PI, 0.5 / 128.0, 1.0 - 0.5 / 128.0);
			skylightingInputsLUT.y = fastAtan2(viewNormalized.y, viewNormalized.x) / (PI * 2.0);
			skylightingInputsLUT.z = sqrt(linearZ * _ZNearFar.w);
			skylightingInputsLUT.z = clamp(skylightingInputsLUT.z, 0.5 / 32.0, 1.0 - 0.5 / 32.0);
			skyScattering.xyz = SAMPLE_TEXTURE3D_LOD(_SkyLightingLUT, _LinearSampler, skylightingInputsLUT, 0).xyz;
			float sunDiskScale = _SunDiskScale.x; 
			if(sunDiskScale > 0) {
				float theta = _SunLatitude.x * (PI * 0.5);
				float alpha = _SunLongitude.x * PI;
				float3 sunDir = -_SunForward.xyz;// float3(sin(theta) * cos(alpha), sin(theta) * sin (alpha), cos(theta));
				float VdotL = dot(normalize(frustumVec), sunDir);
				float3 sun = 10.0 * _SunIntensity.x * (_DecoupleSunColorFromSky.x > 0.0 ? _float3(1.0) : _SunColor.xyz);
				skyScattering.xyz += sunDiskScale * 10.0 * sun * saturate(pow(saturate(VdotL) , 10000.0) * (ndcZ == 1.0 ? 1.0 : 0.0));
			}
			outputColor.xyz += skyScattering.xyz * sunVolumeShadow;
		}

		outputColor.xyz = outputColor.xyz * scattering.a + scattering.xyz;
    }

	_UAVViewColor[id.xy] = float4(outputColor.xyz, 1);
}