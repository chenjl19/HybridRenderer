#include "global.inc"

cbuffer ComputeConstants : register(b0) {
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

cbuffer VolumetricLightingConstants : register(b1) {
    float4 _VolumeTextureSize;
    float4 _ZNearFar;
    float4 _ScatteringParms;
};

cbuffer ScatteringVolumeCB : register(b2) {
    float4 _ScatteringVolumeCB[128];
};

struct ScatteringVolume {
    float4 worldToVolumeMatrix0;    
    float4 worldToVolumeMatrix1;    
    float4 worldToVolumeMatrix2;    
    uint parms0;
    uint parms1;
    uint colorPacked;
    uint padding;
};

#define LIGHT_SCATTERING_VOLUME_SIZE 4
#define UNPACK_SCATTERING_VOLUME(ls, volume) \
{ \
	volume.worldToVolumeMatrix0 = _ScatteringVolumeCB[ LIGHT_SCATTERING_VOLUME_SIZE * ( ls ) + 0 ]; \
	volume.worldToVolumeMatrix1 = _ScatteringVolumeCB[ LIGHT_SCATTERING_VOLUME_SIZE * ( ls ) + 1 ]; \
    volume.worldToVolumeMatrix2 = _ScatteringVolumeCB[ LIGHT_SCATTERING_VOLUME_SIZE * ( ls ) + 2 ]; \
    volume.parms0 = asuint( _ScatteringVolumeCB[ LIGHT_SCATTERING_VOLUME_SIZE * ( ls ) + 3 ].x ); \
    volume.parms1 = asuint( _ScatteringVolumeCB[ LIGHT_SCATTERING_VOLUME_SIZE * ( ls ) + 3 ].y ); \
    volume.colorPacked = asuint( _ScatteringVolumeCB[ LIGHT_SCATTERING_VOLUME_SIZE * ( ls ) + 3 ].z ); \
} 

StructuredBuffer<ClusterData> _ClusterList;
StructuredBuffer<uint> _ClusterItemList;
StructuredBuffer<Light> _LightList;
StructuredBuffer<Shadow> _ShadowList;
Texture2D _LightsAtlas;
Texture2D<float> _ShadowsAtlas;

Texture2D _BlueNoiseTex;
Texture2D _ValueNoiseTex;
Texture3D _NoiseVolumeTex;
Texture3D _PrevLightScatteringPacked0Tex;
Texture3D _PrevLightScatteringPacked1Tex;
RWTexture3D<float3> _UAVLightScatteringPacked0Tex : register(u0);
RWTexture3D<float3> _UAVLightScatteringPacked1Tex : register(u1);

Texture2D _ViewDepthDownres16xTex;
Texture3D _LightScatteringPacked0Tex;
Texture3D _LightScatteringPacked1Tex;
RWTexture3D<float3> _UAVFinalLightScatteringPacked0Tex : register(u0);
RWTexture3D<float3> _UAVFinalLightScatteringPacked1Tex : register(u1);

SamplerComparisonState _ShadowsAtlasSampler : register(s0);
SamplerState _LinearClampSampler : register(s1);
SamplerState _LinearSampler : register(s2);

#define FRAME_COUNT _TimeParms.w

#include "volumetricLighting.inc"

[numthreads(8, 8, 4)]
void ComputeLightScatteringClear(uint3 dispatchThreadId : SV_DispatchThreadID) {
    if(dispatchThreadId.x >= (uint)_VolumeTextureSize.x || dispatchThreadId.y >= (uint)_VolumeTextureSize.y) {
        return;
    }  
    _UAVLightScatteringPacked0Tex[dispatchThreadId] = float3(0, 0, 0);
    _UAVLightScatteringPacked1Tex[dispatchThreadId] = float3(0, 0, 0);   
}

#if 0
[numthreads(8, 8, 1)]
void ComputeLightScatteringIntegral(uint3 dispatchThreadId : SV_DispatchThreadID) {
    if(dispatchThreadId.x >= (uint)_VolumeTextureSize.x || dispatchThreadId.y >= (uint)_VolumeTextureSize.y) {
        return;
    }    

    const float zNear = _ZNearFar.x;
    const float zFar = _ZNearFar.y;
    int numZSlices = (int)_VolumeTextureSize.z;

    float tileMaxNDCZ = _ViewDepthDownres16xTex[(int2)dispatchThreadId.xy >> 1].x;
    float tileMaxLinearZ = GetLinearDepth(tileMaxNDCZ);
    float tileMaxVolumeZ = pow((tileMaxLinearZ - zNear) / (zFar - zNear), 1.0 / 2.0);
    tileMaxVolumeZ = (tileMaxVolumeZ * (numZSlices - 1)) / numZSlices;
    numZSlices = clamp(numZSlices, 0, min(numZSlices, int(tileMaxVolumeZ * numZSlices + 2)));

    float4 scatterAcc = float4(0, 0, 0, 0);
    float shadowAcc = 0;
    for(int z = 0; z < numZSlices; z++) {
        const float3 dstColor = _LightScatteringPacked0Tex[int3(dispatchThreadId.xy, z)].rgb;
        const float2 dstColor2 = _LightScatteringPacked1Tex[int3(dispatchThreadId.xy, z)].rg;
        scatterAcc.xyz += dstColor * saturate(exp(-scatterAcc.w));
        scatterAcc.w += dstColor2.r;
        shadowAcc += dstColor2.g;
        float2 outputColor2 = float2(saturate(exp(-scatterAcc.w)), shadowAcc / float(z + 1));
        _UAVFinalLightScatteringPacked0Tex[int3(dispatchThreadId.xy, z)] = scatterAcc.xyz;
        _UAVFinalLightScatteringPacked1Tex[int3(dispatchThreadId.xy, z)] = float3(outputColor2.xy, 0);     
    }
}

[numthreads(8, 8, 4)]
void ComputeLightScattering(uint3 dispatchThreadId : SV_DispatchThreadID) {
    if(dispatchThreadId.x >= (uint)_VolumeTextureSize.x || dispatchThreadId.y >= (uint)_VolumeTextureSize.y) {
        return;
    }

    const float clipMin = 1.0 / 255.0;
    const float temporalBlendRatio = 0.05;
    const float2 tc = (float2(dispatchThreadId.xy) + 0.5) / _VolumeTextureSize.xy;
    const float2 tcNorm = float2(dispatchThreadId.xy) / _VolumeTextureSize.xy;
    const float zNear = _ZNearFar.x;
    const float zFar = _ZNearFar.y;
    const float numZSlices = _VolumeTextureSize.z;
    const float sliceZ = (float)dispatchThreadId.z;

    // linearZ
    float normalizedZ = pow((sliceZ + 0.5) / (numZSlices - 1), 2.0);
    float currLinearZ = zNear + normalizedZ * (zFar - zNear);
    float prevSliceZ = max(0, float(dispatchThreadId.z) - 1);
    normalizedZ = pow((prevSliceZ + 0.5) / (numZSlices - 1), 2.0);
    float prevLinearZ = zNear + normalizedZ * (zFar - zNear);

    // world position
    const uint subSample = temporalBlendRatio < 1.0 ? (int(FRAME_COUNT + 0) & 0xF) : 0 & 0xf;
    const int2 noiseTC = (int2(dispatchThreadId.xy) + int(dispatchThreadId.z) * int2(32, 1)) & 511;
	const float offset = _BlueNoiseTex.Load(int3(noiseTC, 0)).x;
    const float jitter = float(ReverseBits4(uint(subSample + int(offset * 15)) & 15)) / 16.0;
    const float linearZ = lerp(prevLinearZ, currLinearZ, jitter);
    const float3 positionWS = UVToWorldPosition(tc.xy, linearZ);    

    // reproj
    const float3 positionWSReproj = UVToWorldPosition(tc.xy, currLinearZ);
    float4 reproj = mul(_PrevViewProjectionMatrix, float4(positionWSReproj, 1));
    reproj.xyz /= reproj.w;
	reproj.xy = reproj.xy * float2(0.5, -0.5) + 0.5;    
    float w = reproj.z < 0 ? 1 : 0;
    w += ( min( reproj.x, reproj.y ) < 0.0 || max( reproj.x, reproj.y ) > 1.0 ) ? 0.15 : 0.0;
    float3 tcReproj = reproj.xyz;
    tcReproj.z = GetLinearDepth(tcReproj.z);
    tcReproj.z = pow((tcReproj.z  - zNear) / (zFar - zNear), 1.0 / 2.0);
    tcReproj.z = (tcReproj.z  * (numZSlices - 1)) / numZSlices;
    const float3 prevVx = tcReproj.xyz * _VolumeTextureSize.xyz;
	const float3 currVx = float3(tc.xy * _VolumeTextureSize.xy, sliceZ);
	const float vxy_size = 48.0;
	w += sqr(saturate(length(currVx.xy - prevVx.xy) / vxy_size));
    w += saturate(w + temporalBlendRatio);

    float sunVolumeShadow = 1.0;
    float3 cellAmbient = _float3( 0.0 );
    float3 cellAbsorption = _float3( 0.0 );
    float cellDensity = 0.0;
    float3 diffuseLighting = _float3(0.25);

    bool occluded = true;
	const float tileMaxNdcZ = _ViewDepthDownres16xTex[(int2)dispatchThreadId.xy >> 1].x;
	const float tileMaxLinearZ = GetLinearDepth(tileMaxNdcZ);
    if(prevLinearZ <= tileMaxLinearZ + 1.0)
    {
        occluded = false;
        float cellThickness = saturate(currLinearZ - prevLinearZ);
        {

			float densityVariation = 0.0;
			{
				float3 p = floor(positionWS * 1.0 );
				float3 f = frac( positionWS * 1.0 );
				f = f * f * ( 3.0 - 2.0 * f );
				float2 uv = ( p.xy + float2( 37.0, 17.0 ) * p.z ) + f.xy;
				float2 rg = _ValueNoiseTex.SampleLevel(_LinearClampSampler, (uv + 0.5 ) / 256.0, 0).yx;
				densityVariation += lerp( rg.x, rg.y, f.z ) * 16.0;
			};
			{
				float3 p = floor( positionWS * 2.0 );
				float3 f = frac( positionWS * 2.0 );
				f = f * f * ( 3.0 - 2.0 * f );
				float2 uv = ( p.xy + float2( 37.0, 17.0 ) * p.z ) + f.xy;
				float2 rg = _ValueNoiseTex.SampleLevel(_LinearClampSampler, (uv + 0.5) / 256.0, 0).yx;
				densityVariation += lerp( rg.x, rg.y, f.z ) * 8.0;
			};
			{
				float3 p = floor( positionWS * 4.0 );
				float3 f = frac( positionWS * 4.0 );
				f = f * f * ( 3.0 - 2.0 * f );
				float2 uv = ( p.xy + float2( 37.0, 17.0 ) * p.z ) + f.xy;
				float2 rg = _ValueNoiseTex.SampleLevel(_LinearClampSampler, (uv + 0.5) / 256.0, 0).yx;
				densityVariation += lerp( rg.x, rg.y, f.z ) * 4.0;
			};
			{
				float3 p = floor( positionWS * 8.0 );
				float3 f = frac( positionWS * 8.0 );
				f = f * f * ( 3.0 - 2.0 * f );
				float2 uv = ( p.xy + float2( 37.0, 17.0 ) * p.z ) + f.xy;
				float2 rg = _ValueNoiseTex.SampleLevel(_LinearClampSampler, (uv + 0.5) / 256.0, 0).yx;
				densityVariation += lerp( rg.x, rg.y, f.z ) * 2.0;
			};
			densityVariation /= ( 2.0 + 4.0 + 8.0 + 16.0 );
            const int numVolumes = _ScatteringParms.w;
            for(int i = 0; i < numVolumes; i++) {
                ScatteringVolume volume;
                UNPACK_SCATTERING_VOLUME(i, volume);
                const float4 projS = volume.worldToVolumeMatrix0;
                const float4 projR = volume.worldToVolumeMatrix1;
                const float4 projT = volume.worldToVolumeMatrix2;
                const float3 projTC = float3(
                    positionWS.x * projS.x + positionWS.y * projS.y + positionWS.z * projS.z + projS.w,
                    positionWS.x * projR.x + positionWS.y * projR.y + positionWS.z * projR.z + projR.w,
                    positionWS.x * projT.x + positionWS.y * projT.y + positionWS.z * projT.z + projT.w
                );
                if(fmin3(projTC.x, projTC.y, projTC.z) <= clipMin || fmax3(projTC.x, projTC.y, projTC.z) >= 1.0 - clipMin) {
                    continue;
                }
				uint volumeParms0 = uint(volume.parms0);
				uint volumeParms1 = uint(volume.parms1);
                uint volumeFlags = volumeParms0 & 0xFF;
				float densityHeightK = sqr( float( ( volumeParms1 >> 16 ) & 0xFF ) / 255.0 );
				float densityVariationMax = sqr( float( ( volumeParms1 >> 8 ) & 0xFF ) / 255.0 );
				float densityVariationMin = sqr( float( ( volumeParms1 ) & 0xFF ) / 255.0 );
				float finalDensityVariation = smoothstep( densityVariationMin, densityVariationMax, densityVariation );
				float3 norm_pos_cube = projTC.xyz * 2.0 - _float3( 1.0 );
                uint flags = volumeParms0 & 0xff;
                float density = sqr( float( ( volumeParms1 >> 24 ) & 0xFF ) / 255.0 );
                if(flags & 1 << 1) {
                    float attenuation = saturate(1 - norm_pos_cube.z);
                    attenuation = ApproxPow(saturate(attenuation), 0.25);
                    float heightAbsorption = exp(-max(0, positionWS.z - 2) * densityHeightK);
                    attenuation = min(attenuation * finalDensityVariation, density);
                    cellDensity = attenuation;
                    cellAbsorption = unpackRGBE( volume.colorPacked ) * attenuation * heightAbsorption;
                    break;
                } else {
                    float attenuation = saturate( 1.0 - fmax3( norm_pos_cube.x , norm_pos_cube.y , norm_pos_cube.z ) );
                    float densityFalloff = saturate( sqr( float( ( volumeParms0 >> 8 ) & 0xFF ) / 255.0 ) ); 
                    if ( densityFalloff > 1e-6 ) {
                        float3 norm_pos_sphere = norm_pos_cube * sqrt( saturate( _float3( 1.0 ) - norm_pos_cube.yzx * norm_pos_cube.yzx * 0.5 - norm_pos_cube.zxy * norm_pos_cube.zxy * 0.5 + ( norm_pos_cube.yzx * norm_pos_cube.yzx * norm_pos_cube.zxy * norm_pos_cube.zxy / 3.0 ) ) );
                        attenuation = saturate( 1 - ( length( norm_pos_sphere.xyz ) - densityFalloff ) / ( 1.0 - densityFalloff ) );
                        attenuation *= attenuation;
                    }
                    attenuation = min( attenuation * finalDensityVariation, density );
				    cellDensity += attenuation;
				    cellAbsorption += unpackRGBE( volume.colorPacked ) * attenuation;      
                }       
            }

            cellDensity *= cellThickness;
            cellAbsorption *= cellThickness;
        }

        // cluster data
        uint clusterIndex = GetClusterIndex(tc.xy, linearZ);
        const ClusterData clusterData = _ClusterList[clusterIndex];
        uint lightsMin = clusterData.offset;
        const uint lightsMax = lightsMin + CLUSTER_NUM_LIGHTS(clusterData.count);

        const float3 view = normalize(positionWS.xyz - _ViewOrigin.xyz);

        float3 lightColor = _float3(0);

        [branch] if(_SunForward.w > 0) {
            const Light light = GLightList[0];
            lightColor = unpackRGBE(light.colorPacked);
            [branch] if(light.flags & LF_CAST_SHADOWS) {
                int shadowIndex = SHADOW_MATRIX_INDEX(light.flags);
                uint idx = 0;
                float3 shadowTC = GetShadowTC(shadowIndex, positionWS);
                float2 st = abs(shadowTC.xy);
                if(st.x >= 1.0 || st.y >= 1.0) {
                    idx = 1;
                    shadowTC = GetShadowTC(shadowIndex + 1, positionWS);
                    st = abs(shadowTC.xy);
                    if(st.x >= 1.0 || st.y >= 1.0) {
                        idx = 2;
                        shadowTC = GetShadowTC(shadowIndex + 2, positionWS);
                        st = abs(shadowTC.xy);
                        if(st.x >= 1.0 || st.y >= 1.0) {
                            idx = 3;
                            shadowTC = GetShadowTC(shadowIndex + 3, positionWS);
                        }
                    }
                }
                sunVolumeShadow = ComputeShadow(shadowIndex + idx, float3(shadowTC.xy, shadowTC.z + jitter * 0.00025));
            }
            diffuseLighting += max(0, HGPhase(saturate(dot(view, -_SunForward.xyz)), _ScatteringParms.x)) * lightColor * sunVolumeShadow * light.lightScattering;        
        }

        if(cellDensity > 0) {
            [loop] for(uint lightIndex = lightsMin; lightIndex < lightsMax; ++lightIndex) {
                const Light light = GLightList[CLUSTER_LIGHT_ID(GItemList[lightIndex])];
                const uint lightFlags = light.flags;
                float3 lightVector = light.positionWS.xyz - positionWS.xyz;
                const float distance = length(lightVector);	
                lightVector /= distance;

                float attenuation; 
                {
                    float shadow = 1.0f;
                    [branch] if(lightFlags & LF_CAST_SHADOWS) {
                        int shadowIndex = SHADOW_MATRIX_INDEX(light.flags);
                        [branch] if((lightFlags & LF_MASK) == LF_POINT) {
                            shadowIndex += GetCubeMapFaceID(positionWS - light.positionWS);
                        }
                        shadow = ComputeShadow(shadowIndex, positionWS, jitter * 0.00005);
                    }

                    [branch] if((lightFlags & LF_MASK) == LF_SPOT) {
                        float4 lightCoord = mul(light.worldToLightMatrix, float4(positionWS, 1));
                        lightCoord.xyz /= lightCoord.w;
                        lightCoord.xy = lightCoord.xy * float2(0.5, -0.5) + float2(0.5, 0.5);
                        [branch] if((min(lightCoord.x, lightCoord.y) <= clipMin || max(lightCoord.x, lightCoord.y) >= 1 - clipMin) || (lightCoord.z < 0 || lightCoord.z > 1)) {
                            continue;
                        } 

                        const float4 falloffScaleBias = unpackR15G15B15A15(light.falloffScaleBias);
                        const float4 projectorScaleBias = unpackR15G15B15A15(light.projectorScaleBias);
                        const float projector = SAMPLE_LIGHTSATLAS(lightCoord.xy * projectorScaleBias.xy + projectorScaleBias.zw);
                        shadow *= projector;
                    }

                    const float falloff = DoAttenuation(light.range, distance);
                    attenuation = falloff * shadow * light.lightScattering;
                    [branch] if(attenuation <= clipMin / 256.0) {
                        continue;
                    }
                }

                lightColor = unpackRGBE(light.colorPacked) * attenuation;
                diffuseLighting += max(0, HGPhase(saturate(dot(view, lightVector)), _ScatteringParms.x)) * lightColor;  
            }  
        }
    }

    float4 resultColor = float4(diffuseLighting.xyz * cellAbsorption, cellDensity);
    const float3 prevColorPacked0 = _PrevLightScatteringPacked0Tex.SampleLevel(_LinearClampSampler, tcReproj.xyz, 0).xyz;
    float3 resultColor2 = _PrevLightScatteringPacked1Tex.SampleLevel(_LinearClampSampler, tcReproj.xyz, 0).xyz;
    float ws = w;
    if(resultColor2.z > 0 && occluded == false) {
        ws = 1;
        w = 0.5;
    }
    resultColor.xyz = lerp(prevColorPacked0, resultColor.xyz, saturate(w));
    resultColor2.xy = lerp(resultColor2.xy, float2(resultColor.a, sunVolumeShadow), float2(saturate(w), saturate(ws)));
    resultColor2.z = occluded == true ? 1 : 0;
    _UAVLightScatteringPacked0Tex[dispatchThreadId.xyz] = resultColor.xyz;
    _UAVLightScatteringPacked1Tex[dispatchThreadId.xyz] = resultColor2.xyz;
}
#else
[numthreads(8, 8, 1)]
void ComputeLightScatteringIntegral(uint3 dispatchThreadId : SV_DispatchThreadID) {
    if(dispatchThreadId.x >= (uint)_VolumeTextureSize.x || dispatchThreadId.y >= (uint)_VolumeTextureSize.y) {
        return;
    }    

    LightScatteringIntegral(dispatchThreadId);
}

[numthreads(8, 8, 4)]
void ComputeLightScattering(uint3 dispatchThreadId : SV_DispatchThreadID) {
    if(dispatchThreadId.x >= (uint)_VolumeTextureSize.x || dispatchThreadId.y >= (uint)_VolumeTextureSize.y) {
        return;
    }
    
    LightScattering(dispatchThreadId);
}
#endif