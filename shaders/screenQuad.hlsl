#include "global.inc"
#include "presentPass.inc"

struct a2v {
    float2 position : POSITION;
    float2 texcoord : TEXCOORD;
};

struct v2f {
    float2 texcoord : TEXCOORD0;
    float4 positionCS : SV_POSITION;
};

#ifdef __VS__

v2f main(in a2v vertex) {
    v2f frag = (v2f)0;
	frag.positionCS = float4(vertex.position, 0, 1);
	frag.texcoord = vertex.texcoord;
    return frag;
}

#endif

#ifdef __PS__

Texture2D _MainTex;

cbuffer Constants {
    float4 _DebugTypeParms;
};

float4 main(in v2f frag) : SV_TARGET0 {
    const float2 uv = frag.texcoord.xy;
	float4 result;
	if (_DebugTypeParms.x == 0) {
		float dz = SAMPLE_TEXTURE2D(_MainTex, _LinearClampSampler, uv).r;
		float lz = GetLinear01Depth(dz);
		result = float4(lz, lz, lz, 1);
	}
	else if (_DebugTypeParms.x == 1) {
		float2 encoded = SAMPLE_TEXTURE2D(_MainTex, _LinearClampSampler, uv).xy;
		float3 normal = NormalOctDecode(encoded, false);
		result = float4(normal * 0.5 + 0.5, 1);
	} else if(_DebugTypeParms.x == 2) {
		float4 specular = SAMPLE_TEXTURE2D(_MainTex, _LinearClampSampler, uv);
		float smoothness = SmoothnessDecode(specular.w);
		result = float4(sqr(specular.xyz), abs(smoothness));
	} else if(_DebugTypeParms.x == 3) {
		float4 color = SAMPLE_TEXTURE2D(_MainTex, _LinearClampSampler, uv);
		if(_DebugTypeParms.w == 0) {
			result = color.xxxx;
		} else if(_DebugTypeParms.w == 1) {
			result = color.yyyy;
		} else if(_DebugTypeParms.w == 2) {
			result = color.zzzz;
		} else if(_DebugTypeParms.w == 3) {
			result = color.wwww;
		} else {
			result = color;
		}
	} else {
		result = float4(0, 0, 0, 1);
	}
	return linearSRGB(result);
}

#endif