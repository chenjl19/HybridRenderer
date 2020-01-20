#include "global.inc"
#include "depthPass.inc"

struct v2f {
	float4 pos : SV_POSITION;
    float2 texcoord : TEXCOORD0;
};

#ifdef __VS__

#include "vertex.inc"

float4 StaticModelDepthOnlyVS(in DRAWVERT_Packed0Input a2v) : SV_POSITION {
    const float4 localPosition = float4(a2v.vertex, 1);
    float3 positionWS;
    OUTPUT_WORLD_POSITION(localPosition, _LocalToWorldMatrix0, _LocalToWorldMatrix1, _LocalToWorldMatrix2, positionWS);
    float4 positionCS;
    OUTPUT_CLIP_POSITION(positionWS, _ViewProjectionMatrix0, _ViewProjectionMatrix1, _ViewProjectionMatrix2, _ViewProjectionMatrix3, positionCS);
    return positionCS;
}

v2f StaticModelAlphaTestVS(in DRAWVERT_Packed0Input a2v) {
    v2f output;
    const float4 vertex = float4(a2v.vertex, 1);
    float3 positionWS;
    OUTPUT_WORLD_POSITION(vertex, _LocalToWorldMatrix0, _LocalToWorldMatrix1, _LocalToWorldMatrix2, positionWS);
    OUTPUT_CLIP_POSITION(positionWS, _ViewProjectionMatrix0, _ViewProjectionMatrix1, _ViewProjectionMatrix2, _ViewProjectionMatrix3, output.pos);
    output.texcoord = a2v.st;
    return output;
}

float4 DepthOnlyVS(in DRAWVERT_Input a2v) : SV_POSITION {
    float4 localPosition = float4(a2v.vertex, 1);
	[branch] if(_ModelIndices.w == 1) {
		SKIN_POSITION_IMPL(_ModelMatrices, _ModelIndices.z, a2v.jointIndices, a2v.jointWeights, localPosition);
	}
    float3 positionWS;
    OUTPUT_WORLD_POSITION(localPosition, _LocalToWorldMatrix0, _LocalToWorldMatrix1, _LocalToWorldMatrix2, positionWS);
    float4 positionCS;
    OUTPUT_CLIP_POSITION(positionWS, _ViewProjectionMatrix0, _ViewProjectionMatrix1, _ViewProjectionMatrix2, _ViewProjectionMatrix3, positionCS);
    return positionCS;
}

v2f AlphaTestVS(in DRAWVERT_Input a2v) {
    v2f output;
    float4 localPosition = float4(a2v.vertex, 1);
	[branch] if(_ModelIndices.w == 1) {
		SKIN_POSITION_IMPL(_ModelMatrices, _ModelIndices.z, a2v.jointIndices, a2v.jointWeights, localPosition);
	}
    float3 positionWS;
    OUTPUT_WORLD_POSITION(localPosition, _LocalToWorldMatrix0, _LocalToWorldMatrix1, _LocalToWorldMatrix2, positionWS);
    OUTPUT_CLIP_POSITION(positionWS, _ViewProjectionMatrix0, _ViewProjectionMatrix1, _ViewProjectionMatrix2, _ViewProjectionMatrix3, output.pos);
    output.texcoord = a2v.st;
    return output;
}

#endif

#ifdef __PS__

Texture2D _MainTex;

void AlphaTestPS(in v2f frag) {
    float4 color = SAMPLE_TEXTURE2D(_MainTex, _LinearSampler, frag.texcoord);
    if(color.a < 0.6f) {
        discard;
    }
}
#endif