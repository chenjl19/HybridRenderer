#include "global.inc"
#include "debugPass.inc"

struct a2v {
    float3 position : POSITION;
    float4 color : COLOR;
};

struct v2f {
    float4 positionCS : SV_POSITION;
    float4 color : TEXCOORD0;
};

struct v2f_pic {
    float4 pos : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float4 color : TEXCOORD1;
};

#ifdef __VS__

#include "vertex.inc"

v2f main(in a2v vertex) {
    v2f output;
    OUTPUT_CLIP_POSITION(float4(vertex.position, 1), _ViewProjectionMatrix0, _ViewProjectionMatrix1, _ViewProjectionMatrix2, _ViewProjectionMatrix3, output.positionCS);
    output.color = vertex.color;
    return output;
}

v2f_pic ScreenPicVS(in DRAWVERT_Input a2v) {
    v2f_pic output;
    OUTPUT_CLIP_POSITION(float4(a2v.vertex, 1), _ViewProjectionMatrix0, _ViewProjectionMatrix1, _ViewProjectionMatrix2, _ViewProjectionMatrix3, output.pos);
    output.texcoord = a2v.st;
    output.color = a2v.normal;
    return output;
}

#endif

#ifdef __PS__

float4 main(v2f frag) : SV_TARGET0 {
    return frag.color;
}

Texture2D _MainTex;
SamplerState _LinearSampler;

float4 ScreenPicPS(in v2f_pic frag) : SV_TARGET0 {
    const float4 color = _MainTex.Sample(_LinearSampler, frag.texcoord) * frag.color;
    return float4(color.rgb * color.a, 1);
}

#endif