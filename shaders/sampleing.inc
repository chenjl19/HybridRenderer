
static void Downsample(TEXTURE2D_PARAM(_MainTex, _sampler), in float2 tc, in float2 texelSize, out float4 c, out float4 c0, out float4 c1, out float4 c2, out float4 c3) {
	 c = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, tc, 0);
	 c0 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, tc + texelSize * float2(0.86, 0.50), 0);
	 c1 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, tc + texelSize * float2(-0.50, 0.86), 0);
	 c2 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, tc + texelSize * float2(-0.86, -0.5), 0);
	 c3 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, tc + texelSize * float2(0.50, -0.86), 0);
}

// 3-tap median filter
static float3 Median(float3 a, float3 b, float3 c) {
	return a + b + c - min(min(a, b), c) - max(max(a, b), c);
}

static void DownsampleBloomPrefilter(TEXTURE2D_PARAM(_MainTex, _sampler), in float2 baseUV, in float2 texelSize, in float sampleOffset, in float threshold, in float3 curve, out float4 resultColor) {
	const float2 uv = baseUV + texelSize.xy * sampleOffset;
	const float3 d = texelSize.xyx * float3(1.0, 1.0, 0.0);
	const float3 s0 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv, 0).rgb;
	const float3 s1 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv - d.xz, 0).rgb;
	const float3 s2 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.xz, 0).rgb;
	const float3 s3 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv - d.zy, 0).rgb;
	const float3 s4 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.zy, 0).rgb;
	resultColor.rgb = Median(Median(s0, s1, s2), s3, s4);
#if 0
	// Pixel brightness
	float br = Brightness(resultColor.rgb);

	// Under-threshold part: quadratic curve
	float rq = clamp(br - curve.x, 0.0, curve.y);
	rq = curve.z * rq * rq;

	// Combine and apply the brightness response curve.
	resultColor.rgb *= max(rq, br - threshold) / max(br, 1e-5);
#else
	const float luma = GetLuma(resultColor.xyz);
	resultColor.xyz *= smoothstep(0, 1, saturate(luma - threshold));
#endif
	resultColor.a = 1;
}

static void DownsampleBloomAntiFlickerFilter(TEXTURE2D_PARAM(_MainTex, _sampler), in float2 uv, in float2 texelSize, out float4 resultColor)
{
	const float4 d = texelSize.xyxy * float4(-1, -1, 1, 1);

	const float3 s1 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.xy, 0).rgb;
	const float3 s2 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.zy, 0).rgb;
	const float3 s3 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.xw, 0).rgb;
	const float3 s4 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.zw, 0).rgb;
	
	// Karis's luma weighted average (using brightness instead of luma)
	const float s1w = 1.0 / (Brightness(s1) + 1.0);
	const float s2w = 1.0 / (Brightness(s2) + 1.0);
	const float s3w = 1.0 / (Brightness(s3) + 1.0);
	const float s4w = 1.0 / (Brightness(s4) + 1.0);
	const float one_div_wsum = 1.0 / (s1w + s2w + s3w + s4w);

	resultColor.xyz = (s1 * s1w + s2 * s2w + s3 * s3w + s4 * s4w) * one_div_wsum;
	resultColor.a = 1;
}

static void DownsampleBox4Tap(TEXTURE2D_PARAM(_MainTex, _sampler), in float2 uv, in float2 texelSize, out float4 resultColor)
{
	const float4 d = texelSize.xyxy * float4(-1, -1, 1, 1);

	const float3 s1 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.xy, 0).rgb;
	const float3 s2 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.zy, 0).rgb;
	const float3 s3 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.xw, 0).rgb;
	const float3 s4 = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.zw, 0).rgb;

	resultColor.rgb = (s1 + s2 + s3 + s4) / 4;
	resultColor.a = 1;
}

static void DownsampleBox13Tap(TEXTURE2D_PARAM(_MainTex, _sampler), in float2 uv, in float2 texelSize, out float4 resultColor)
{
	float4 A = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(-1.0, -1.0)), 0);
	float4 B = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(0.0, -1.0)), 0);
	float4 C = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(1.0, -1.0)), 0);
	float4 D = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(-0.5, -0.5)), 0);
	float4 E = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(0.5, -0.5)), 0);
	float4 F = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(-1.0, 0.0)), 0);
	float4 G = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv), 0);
	float4 H = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(1.0, 0.0)), 0);
	float4 I = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(-0.5, 0.5)), 0);
	float4 J = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(0.5, 0.5)), 0);
	float4 K = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(-1.0, 1.0)), 0);
	float4 L = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(0.0, 1.0)), 0);
	float4 M = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + texelSize * float2(1.0, 1.0)), 0);

	float2 div = (1.0 / 4.0) * float2(0.5, 0.125);

	float4 o = (D + E + I + J) * div.x;
	o += (A + B + G + F) * div.y;
	o += (B + C + H + G) * div.y;
	o += (F + G + L + K) * div.y;
	o += (G + H + M + L) * div.y;

	resultColor = o;
}

// 9-tap bilinear upsampler (tent filter)
static void UpsampleTent(TEXTURE2D_PARAM(_MainTex, _sampler), in float2 uv, in float2 texelSize, in float sampleScale, out float4 outputColor)
{
	float4 d = texelSize.xyxy * float4(1.0, 1.0, -1.0, 0.0) * sampleScale;

	float4 s;
	s = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv - d.xy), 0);
	s += SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv - d.wy), 0) * 2.0;
	s += SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv - d.zy), 0);

	s += SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + d.zw), 0) * 2.0;
	s += SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv), 0) * 4.0;
	s += SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + d.xw), 0) * 2.0;

	s += SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + d.zy), 0);
	s += SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + d.wy), 0) * 2.0;
	s += SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, (uv + d.xy), 0);

	outputColor = s * (1.0 / 16.0);
}

// Standard box filtering
static void UpsampleBox(TEXTURE2D_PARAM(_MainTex, _sampler), in float2 uv, in float2 texelSize, in float4 sampleScale, out float4 outputColor)
{
	float4 d = texelSize.xyxy * float4(-1.0, -1.0, 1.0, 1.0) * (sampleScale * 0.5);

	float4 s;
	s = SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.xy, 0);
	s += SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.zy, 0);
	s += SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.xw, 0);
	s += SAMPLE_TEXTURE2D_LOD(_MainTex, _sampler, uv + d.zw, 0);

	outputColor = s * (1.0 / 4.0);
}