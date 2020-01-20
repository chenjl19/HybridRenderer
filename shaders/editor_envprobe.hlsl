#include "global.inc"
#include "viewConstants.inc"
#include "instanceConstants.inc"

struct v2f {
	float4 pos : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
};

#ifdef __VS__

#include "vertex.inc"

void main(in DRAWVERT_Input a2v, out v2f o) {
    DECODE_POSITION_AND_NORMALS(a2v.vertex, a2v.normal, a2v.tangent, localPosition, localNormal, localTangent, tangentW);
    OUTPUT_WORLD_POSITION(localPosition, _LocalToWorldMatrix0, _LocalToWorldMatrix1, _LocalToWorldMatrix2, o.positionWS);
    OUTPUT_WORLD_NORMAL(localNormal, _LocalToWorldMatrix0, _LocalToWorldMatrix1, _LocalToWorldMatrix2, o.normalWS);
    OUTPUT_CLIP_POSITION(o.positionWS, _ViewProjectionMatrix0, _ViewProjectionMatrix1, _ViewProjectionMatrix2, _ViewProjectionMatrix3, o.pos);
    o.normalWS = normalize(o.normalWS);
}
#endif

#ifdef __PS__

#include "lightingPass.inc"

cbuffer Constants {
    float4 _ProbeID;
};

TextureCubeArray _MainTex;

float4 main(v2f i) : SV_TARGET0 {
    float3 view = (i.positionWS - _ViewOrigin.xyz);
    float3 vec = reflect(view, i.normalWS);
    float4 specularProbe = SAMPLE_TEXTURECUBE_ARRAY(_MainTex, _AnisoSampler, vec.xzy, _ProbeID.x);
    //specularProbe.rgb = specularProbe.rgb * specularProbe.a * 6;
    return float4(specularProbe.rgb, 1);
}

#endif