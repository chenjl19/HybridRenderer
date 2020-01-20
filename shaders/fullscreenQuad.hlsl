#include "global.inc"
#include "presentPass.inc"

struct v2f {
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD;
};

#ifdef __VS__

static const float4 QUAD[4] = {
	float4(-1, 1, 0, 0),
	float4(1, 1, 1, 0),
	float4(-1, -1, 0, 1),
	float4(1, -1, 1, 1)
};

v2f main(in uint vertexID : SV_VERTEXID) {
    v2f frag;
	frag.positionCS = float4(float4(QUAD[vertexID].xy, 0, 1));
	frag.texcoord = QUAD[vertexID].zw;
    return frag;
}

#endif

#ifdef __PS__

float3 ACES(float3 color) {
	const float A = 2.51f;
	const float B = 0.03f;
	const float C = 2.43f;
	const float D = 0.59f;
	const float E = 0.14f;
	return (color * (A * color + B)) / (color * (C * color + D) + E);
}

float3 ACESToneMapping(float3 color, float exposure) {
	return ACES(color * exposure);
}

float3 F(float3 x) {
    const float A = 0.22f;
    const float B = 0.30f;
    const float C = 0.10f;
    const float D = 0.20f;
    const float E = 0.01f;
    const float F = 0.30f;
    return ((x * (A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E / F;
}

float3 Uncharted2ToneMapping(float3 color, float adapted_lum) {
    const float WHITE = 11.2f;
    return F(1.6f * adapted_lum * color) / F(WHITE);
}

Texture2D _MainTex;
Texture2D _BloomTex;
Texture2D _AutoExposureTex;

float4 main(in v2f frag) : SV_TARGET0 {
    const float3 linearHDR = _MainTex.SampleLevel(_PointClampSampler, frag.texcoord.xy, 0).rgb;
	const float3 bloom = _BloomTex.SampleLevel(_LinearClampSampler, frag.texcoord.xy, 0).rgb;
	const float autoExposure = _AutoExposureTex[uint2(0, 0)];
	const float3 toneMapped = ACESToneMapping(linearHDR + bloom * 0.1, autoExposure);
	const float dither = (BayerDither4x4((int2)frag.positionCS.xy, 0 ) * 2.0 - 1.0) / 255.0;
	return float4(saturate(linearSRGB(toneMapped) + dither), 1);
}

#endif