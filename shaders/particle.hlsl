#include "global.inc"
#include "viewConstants.inc"
#include "instanceConstants.inc"

struct v2f {
	float4 pos : SV_POSITION;
    float2 texcoord0 : TEXCOORD0;
    float2 texcoord1 : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    float3 normalWS : TEXCOORD3;
    float4 color0 : TEXCOORD4;
    float4 color1 : TEXCOORD5;
};

#ifdef __VS__

#include "vertex.inc"

v2f main(in PARTICLEVERT_Input a2v) {
	v2f o = (v2f)0;
    DECODE_PARTICLE_POSITION_AND_NORMAL(a2v.vertex, a2v.normal, localPosition, localNormal);
    //OUTPUT_WORLD_NORMAL(localNormal, _LocalToWorldMatrix0, _LocalToWorldMatrix1, _LocalToWorldMatrix2, o.normalWS);
	//OUTPUT_WORLD_POSITION(localPosition, _LocalToWorldMatrix0, _LocalToWorldMatrix1, _LocalToWorldMatrix2, o.positionWS);
	OUTPUT_CLIP_POSITION(localPosition, _ViewProjectionMatrix0, _ViewProjectionMatrix1, _ViewProjectionMatrix2, _ViewProjectionMatrix3, o.pos);
    o.texcoord0.xy = a2v.st0.xy;
    o.texcoord0.y = 1 - o.texcoord0.y;
    o.texcoord1.xy = a2v.st1.xy;
    o.texcoord1.y = 1 - o.texcoord1.y;
    o.color0 = a2v.color0;
    o.color1 = a2v.color1;
	return o;
}
#endif

#ifdef __PS__

#include "lightingPass.inc"

float4 main(in v2f frag) : SV_TARGET0 {
    const float4 currentFrame = _TransatlasTex.Sample(_AnisoSampler, frag.texcoord0.xy);
    const float4 nextFrame = _TransatlasTex.Sample(_AnisoSampler, frag.texcoord1.xy);
    float3 albedo = lerp(SRGBlinear(currentFrame.rrr), SRGBlinear(nextFrame.rrr), frag.color1.x) * SRGBlinear(frag.color0.rgb);
    float opacity = lerp(currentFrame.a, nextFrame.a, frag.color1.x) * frag.color0.a;
	clip( opacity - 1e-6 );
    float softZScale = 1.0;
    const float2 screenUV = frag.pos.xy * _ScreenParms.zw;
    const float sceneDepth = _ViewDepthTex.SampleLevel(_LinearClampSampler, screenUV, 0).x;
    const float r1 = 1 / (1.0 - sceneDepth);
    const float r2 = 1 / (1.0 - frag.pos.z);
    softZScale = saturate((r1 - r2) * 0.1);

    float4 color;
    color.rgb = albedo * 10;
    color.a = saturate(opacity * softZScale);
    return float4(color.rgb * color.a, color.a);
}
#endif