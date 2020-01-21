#include "global.inc"
#include "blur.inc"
#include "sampleing.inc"

cbuffer ComputeShared : register(b0) {
	float4 _RenderPositionToViewTexture;
	float4 _InputMainTextureSize;
	float4 _DownsampleParms;
    float4 _BlurStep;
    float4 _SSAOParms;
	float4 _AutoExposureParms;
	float4 _AutoExposureMinMaxParms;
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

Texture2D _MainTex : register(t0);
Texture2D _PrevTex : register(t1);
RWTexture2D<float4> _Result : register(u0);
SamplerState _LinearClampSampler : register(s0);
SamplerState _PointClampSampler : register(s1);

[numthreads(8, 8, 1)]
void ComputeDownsampleDepth(uint2 id : SV_DispatchThreadID) {
	if(id.x < (uint)_RenderPositionToViewTexture.x && id.y < (uint)_RenderPositionToViewTexture.y) {
		float result = 0;
		if(_DownsampleParms.w < 2) {
			float dz0 = _MainTex.Load(int3(id.xy * 2 + int2(0, 0), 0)).x;
			float dz1 = _MainTex.Load(int3(id.xy * 2 + int2(1, 0), 0)).x;
			float dz2 = _MainTex.Load(int3(id.xy * 2 + int2(0, 1), 0)).x;
			float dz3 = _MainTex.Load(int3(id.xy * 2 + int2(1, 1), 0)).x;
			result = max(max(max(dz0, dz1), dz2), dz3);
		} else {
			for(int y = -1; y <= 1; y++) { 
				for(int x = -1; x <= 1; x++) {
					result = max(result, _MainTex.Load(int3(id.xy * 2 + int2(x, y), 0)).x);
				}
			}
		}
		_Result[id.xy] = result;
	}
}

[numthreads(8, 8, 1)]
void ComputeDownsample(uint2 id : SV_DispatchThreadID) {
	const float2 tc = (float2(id.xy) + 0.5) * _RenderPositionToViewTexture.zw;
	float4 resultColor = float4(0, 0, 0, 0);
	if (id.x < (uint)_RenderPositionToViewTexture.x && id.y < (uint)_RenderPositionToViewTexture.y) {
		float4 c, c0, c1, c2, c3;
		Downsample(_MainTex, _LinearClampSampler, tc, _InputMainTextureSize.zw, c, c0, c1, c2, c3);
		if (_DownsampleParms.x == 0) {
			resultColor = max(0, (c + c0 + c1 + c2 + c3) * 0.2);
		} else if (_DownsampleParms.x == 1) { // Luminance downscale type. First downsample
			float luma = (GetLuma(c.xyz) + 1e-6);
			float luma0 = (GetLuma(c0.xyz) + 1e-6);
			float luma1 = (GetLuma(c1.xyz) + 1e-6);
			float luma2 = (GetLuma(c2.xyz) + 1e-6);
			float luma3 = (GetLuma(c3.xyz) + 1e-6);
			luma = max(0, (luma + luma0 + luma1 + luma2 + luma3) * 0.2);
			resultColor = float4(luma, luma, luma, luma);
		} else if (_DownsampleParms.x == 2) { // Downscale + brightpass (this is the first bloom downscale pass)
			resultColor.xyz = max(0, (c.xyz + c0.xyz + c1.xyz + c2.xyz + c3.xyz) * 0.2);
			const float luma = GetLuma(resultColor.xyz);
			resultColor.xyz *= smoothstep(0, 1, saturate(luma - _DownsampleParms.w));
		}
		_Result[id.xy] = resultColor;
	}
}

[numthreads(8, 8, 1)]
void ComputeGaussianBlur(uint2 id : SV_DispatchThreadID) {
    if (id.x < (uint)_RenderPositionToViewTexture.x && id.y < (uint)_RenderPositionToViewTexture.y) {
	    const float2 tc = (float2(id.xy) + 0.5) * _RenderPositionToViewTexture.zw;
        float4 resultColor;
		const float2 step = _BlurStep.xy /* _InputMainTextureSize.zw*/; // $blurStep.xy will be { 1, 0 } or { 0, 1 } for horizontal / vertical separation
		const float w[8] = { 0.027062858, 0.088897429, 0.18941596, 0.26192293, 0.23510250, 0.13697305, 0.051778302, 0.0088469516 };
		const float b_off[8] = { -6.3269038, -4.3775406, -2.4309988, -0.48611468, 1.4584296, 3.4039848, 5.3518057, 7.00000000 };
		float4 sum = _float4(0);
		for(int s = 0; s < 8; s++) {
			const float4 c = SAMPLE_TEXTURE2D_LOD(_MainTex, _LinearClampSampler, tc + step * b_off[s], 0);
			sum.xyz += c.xyz * w[s];
		}
		resultColor = float4(sum.xyz, 1);
		if(_BlurStep.w == 1) {
			resultColor.xyz = resultColor.xyz * _BlurStep.z + SAMPLE_TEXTURE2D_LOD(_PrevTex, _LinearClampSampler, tc, 0).xyz;
		}
        _Result[id.xy] = resultColor;
    }
}

[numthreads(8, 8, 1)]
void ComputeScalableGaussianBlur(uint2 id : SV_DispatchThreadID) {
	if (id.x < (uint)_RenderPositionToViewTexture.x && id.y < (uint)_RenderPositionToViewTexture.y) {
        const float2 tc = (float2(id.xy) + 0.5) * _RenderPositionToViewTexture.zw;
        float4 resultColor = float4(0, 0, 0, 0);
		ScalableGaussianBlur(_MainTex, _LinearClampSampler, tc, _BlurStep, resultColor);
		_Result[id.xy] = resultColor;
	}
}

[numthreads(1, 1, 1)]
void ComputeAutoExposure(uint2 id : SV_DISPATCHTHREADID) {
#if 0
	const float avgLuminance = _MainTex[uint2(0, 0)];
	const float adjusted = lerp(0.5, avgLuminance * 0.3, 1);
	const float rcp_log2_10 = 1.0f / log2(10);
	const float sceneKey = 1.03 - 2.0 / (2.0 + (log2((adjusted) + 1.0) * rcp_log2_10));
	const float currentExposure = clamp(sceneKey / adjusted, 0.5, 20);
	const float prevExposure = clamp(_PrevTex[uint2(0, 0)], 0, 64);
	const float finalExposure = lerp(prevExposure, currentExposure, _ExposureParms1.w * 1.5);
	_Result[uint2(0, 0)] = finalExposure;
#else
	const float avgLuminance = _MainTex[uint2(0, 0)];
	const float adjusted = lerp(_AutoExposureParms.x, avgLuminance * _AutoExposureParms.w, _AutoExposureParms.y);
	const float rcp_log2_10 = 1.0f / log2(10);
	const float sceneKey = 1.03 - 2.0 / (2.0 + (log2((adjusted) + 1.0) * rcp_log2_10));
	const float currentExposure = clamp(sceneKey / adjusted, _AutoExposureMinMaxParms.x, _AutoExposureMinMaxParms.y);
	const float prevExposure = clamp(_PrevTex[uint2(0, 0)], 0, 64);
	const float finalExposure = lerp(prevExposure, currentExposure, _AutoExposureParms.z);
	_Result[uint2(0, 0)] = finalExposure;
#endif
}

#include "ssao.inc"

RWTexture2D<unorm float> _UAVSSAO : register(u0);

[numthreads(8, 8, 1)]
void ComputeSSAO(uint2 id : SV_DispatchThreadID) {
	if (id.x < (uint)_RenderPositionToViewTexture.x && id.y < (uint)_RenderPositionToViewTexture.y) {
        const float2 tc = (float2(id.xy) + 0.5) * _RenderPositionToViewTexture.zw;
        float result;
		SSAO(tc, id, result);
		_UAVSSAO[id.xy] = result;
	}
}

[numthreads(8, 8, 1)]
void ComputeSSGI(uint2 id : SV_DispatchThreadID) {
	if (id.x < (uint)_RenderPositionToViewTexture.x && id.y < (uint)_RenderPositionToViewTexture.y) {
        const float2 tc = (float2(id.xy) + 0.5) * _RenderPositionToViewTexture.zw;
        float3 result;
		SSGI(tc, id, result);
		_Result[id.xy] = float4(result, 1);
	}
}

