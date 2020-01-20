cbuffer _InstanceUniforms : register(b1) {
    float4 _ModelIndices;
    float4 _ModelParms;
	float4 _Lightmap_ST; // StaticModel only
    float4 _LocalToWorldMatrix0;
    float4 _LocalToWorldMatrix1;
    float4 _LocalToWorldMatrix2;
    float4 _WorldToLocalMatrix0;
    float4 _WorldToLocalMatrix1;
    float4 _WorldToLocalMatrix2;
    float4 _Matrix0;
    float4 _Matrix1;
    float4 _Matrix2;
    float4 _Matrix3;
};

StructuredBuffer<float4> _ModelMatrices : register(t0);