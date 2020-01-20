struct DRAWVERT_Input {
#ifdef USE_LIGHTMAPS
	float3 vertex : POSITION;
	float2 st : TEXCOORD0;
    float2 lightmapST : TEXCOORD1;
    float4 normal : TEXCOORD2;
    float4 tangent : TEXCOORD3; 
    float4 color0 : TEXCOORD4;
#else
	float3 vertex : POSITION;
	float2 st : TEXCOORD0;
    float4 normal : TEXCOORD1;
    float4 tangent : TEXCOORD2;
    float4 jointIndices : TEXCOORD3;
    float4 jointWeights : TEXCOORD4;
#endif
};

struct PARTICLEVERT_Input {
	float3 vertex : POSITION;
	float2 st0 : TEXCOORD0;
    float2 st1 : TEXCOORD1;
    float4 normal : TEXCOORD2;
    float4 color0 : TEXCOORD3;
    float4 color1 : TEXCOORD4;
};

struct DRAWVERT_Packed0Input {
	float3 vertex : POSITION;
	float2 st : TEXCOORD0;    
};

#define DECODE_POSITION_AND_NORMALS(vertex, normal, tangent, vPosition, vNormal, vTangent, outputSign) \
    float4 vPosition = float4(vertex, 1); \
    float4 vNormal = normal * 2.0 - 1.0; \
	float4 vTangent = tangent * 2.0 - 1.0; \
    const float outputSign = vTangent.w

#define DECODE_PARTICLE_POSITION_AND_NORMAL(vertex, normal, vPosition, vNormal) \
    float4 vPosition = float4(vertex, 1); \
    float4 vNormal = normal * 2.0 - 1.0;

#define CALC_SKINNING_LINEAR_MAT(matrices, MODEL_INDEX, JOINT_INDICES, JOINT_WEIGHTS, matX, matY, matZ) \
{ \
    const uint4 indices = ( uint4(MODEL_INDEX, MODEL_INDEX, MODEL_INDEX, MODEL_INDEX) + uint4(JOINT_INDICES * 255.0) ) * 3; \
    const float4 weights = JOINT_WEIGHTS; \
    uint i; \
    [loop] for(i = 0; i < 4; i++) { \
        matX += matrices[indices[i] + 0] * weights[i]; \
    } \
    [loop] for(i = 0; i < 4; i++) { \
        matY += matrices[indices[i] + 1] * weights[i]; \
    } \
    [loop] for(i = 0; i < 4; i++) { \
        matZ += matrices[indices[i] + 2] * weights[i]; \
    } \
}

#define SKIN_POSITION_IMPL(matrices, matrixOffset, jointIndices, jointWeights, localPosition) \
{ \
    float4 matX = float4(0, 0, 0, 0), matY = float4(0, 0, 0, 0), matZ = float4(0, 0, 0, 0); \
    CALC_SKINNING_LINEAR_MAT(matrices, matrixOffset, jointIndices, jointWeights, matX, matY, matZ); \
    float4 temp = localPosition; \
    localPosition.x = dot4(temp, matX); \
    localPosition.y = dot4(temp, matY); \
    localPosition.z = dot4(temp, matZ); \
    localPosition.w = 1; \
}

#define SKIN_POSITION_AND_NORMALS(matrices, matrixOffset, jointIndices, jointWeights, localPosition, localNormal, localTangent) \
{ \
    float4 matX = float4(0, 0, 0, 0), matY = float4(0, 0, 0, 0), matZ = float4(0, 0, 0, 0); \
    CALC_SKINNING_LINEAR_MAT(matrices, matrixOffset, jointIndices, jointWeights, matX, matY, matZ); \
    float4 temp = localPosition; \
    localPosition.x = dot4(temp, matX); \
    localPosition.y = dot4(temp, matY); \
    localPosition.z = dot4(temp, matZ); \
    localPosition.w = 1; \
    float4 tempNormal = localNormal; \
    localNormal.x = dot3(tempNormal, matX); \
    localNormal.y = dot3(tempNormal, matY); \
    localNormal.z = dot3(tempNormal, matZ); \
    float4 tempTangent = localTangent; \
    localTangent.x = dot3(tempTangent, matX); \
    localTangent.y = dot3(tempTangent, matY); \
    localTangent.z = dot3(tempTangent, matZ); \
}

#define DERIVE_BITANGENT(localNormal, localTangent, localBitangent) \
{ \
    localBitangent.xyz = cross(localNormal.xyz, localTangent.xyz) * localTangent.w; \
	localBitangent.w = 0.0; \
}

#define DERIVE_WORLD_BITANGENT(normalWS, tangentWS, tangentW, output) \
{ \
    output.xyz = cross(normalWS.xyz, tangentWS.xyz) * tangentW; \
}

#define PROJECT_POSITION_MVP(local, mvp0, mvp1, mvp2, mvp3, output) \
{ \
    output.x = dot4(local, mvp0); \
    output.y = dot4(local, mvp1); \
    output.z = dot4(local, mvp2); \
    output.w = dot4(local, mvp3); \
}

#define OUTPUT_WORLD_POSITION(local, matX, matY, matZ, output) \
{ \
    output.x = dot4(local, matX); \
    output.y = dot4(local, matY); \
    output.z = dot4(local, matZ); \
}

#define OUTPUT_CLIP_POSITION(world, matX, matY, matZ, matW, output) \
{ \
    output.x = dot4(world, matX); \
    output.y = dot4(world, matY); \
    output.z = dot4(world, matZ); \
    output.w = dot4(world, matW); \
}

#define OUTPUT_WORLD_NORMAL(localNormal, matX, matY, matZ, normalWS) \
{ \
    normalWS.x = dot3(localNormal, matX); \
    normalWS.y = dot3(localNormal, matY); \
    normalWS.z = dot3(localNormal, matZ); \
    normalWS.xyz = normalize(normalWS.xyz); \
}

#define OUTPUT_WORLD_NORMALS(localNormal, localTangent, matX, matY, matZ, normalWS, tangentWS) \
{ \
    normalWS.x = dot3(localNormal, matX); \
    normalWS.y = dot3(localNormal, matY); \
    normalWS.z = dot3(localNormal, matZ); \
    normalWS.xyz = normalize(normalWS.xyz); \
    tangentWS.x = dot3(localTangent.xyz, matX); \
    tangentWS.y = dot3(localTangent.xyz, matY); \
    tangentWS.z = dot3(localTangent.xyz, matZ); \
    tangentWS.xyz = normalize(tangentWS.xyz); \
}

#define OUTPUT_WORLD_NORMALS_SCALED(localNormal, localTangent, matX, matY, matZ, normalWS, tangentWS) \
{ \
	float3x3 correctionMatrix; \
	correctionMatrix[0].xyz = matX.xyz; \
	correctionMatrix[1].xyz = matY.xyz; \
	correctionMatrix[2].xyz = matZ.xyz; \
	correctionMatrix = transpose(correctionMatrix); \
    OUTPUT_WORLD_NORMALS(localNormal, localTangent, correctionMatrix[0], correctionMatrix[1], correctionMatrix[2], normalWS, tangentWS); \
}
