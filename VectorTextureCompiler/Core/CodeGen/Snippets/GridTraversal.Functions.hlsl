#ifdef _SDFX_PRECISION_HALF
#define SDFX_LOOP_SCALAR half
#else
#define SDFX_LOOP_SCALAR float
#endif

#define SDFX_EDGE_AA_SCALE 0.70710678

fixed4 SdfxEvaluate(float2 uv, out float minDist)
{
    float4 cellData = tex2Dlod(_GridLookupTex, float4(uv, 0, 0));
    int startIdx = (int)(cellData.r + 0.5);
    int count    = (int)(cellData.g + 0.5);

    fixed4 result = fixed4(0.0, 0.0, 0.0, 0.0);
    minDist = 1e5;

    SDFX_LOOP_SCALAR edgeAA = max(length(fwidth(uv)) * SDFX_EDGE_AA_SCALE, 0.0001);

#ifdef SDFX_GRID_FIXED_BOUND
    #ifdef SDFX_GRID_UNROLL
    [unroll({{MAX_PRIMITIVES_PER_CELL}})]
    #else
    [loop]
    #endif
    for (int i = 0; i < {{MAX_PRIMITIVES_PER_CELL}}; i++)
    {
        float active = (i < count) ? 1.0 : 0.0;

        float rawIdx = SdfxFetchGridIndexFlat(startIdx + i).r;
        int   primIdx = (int)(rawIdx + 0.5);

        SdfxPrimitive p = SdfxFetchPrimitive(primIdx);
        SDFX_LOOP_SCALAR d = SdfxEvalPrimitive(p, uv);
        minDist = lerp(minDist, min(minDist, d), active);

        SDFX_LOOP_SCALAR softEdge = max(p.softness, edgeAA);
        SDFX_LOOP_SCALAR dilate = softEdge * 0.5;
        SDFX_LOOP_SCALAR coverage = 1.0 - smoothstep(0.0, softEdge, d - dilate);

        float4 fill = SdfxPrimitiveColor(p, uv);
        coverage *= fill.a * active;

        result.rgb = result.rgb * (1.0 - coverage) + fill.rgb * coverage;
        result.a   = result.a   * (1.0 - coverage) + coverage;
    }
#else
    [loop]
    for (int i = 0; i < count; i++)
    {
        float rawIdx = SdfxFetchGridIndexFlat(startIdx + i).r;
        int   primIdx = (int)(rawIdx + 0.5);

        SdfxPrimitive p = SdfxFetchPrimitive(primIdx);
        SDFX_LOOP_SCALAR d = SdfxEvalPrimitive(p, uv);
        minDist = min(minDist, d);

        SDFX_LOOP_SCALAR softEdge = max(p.softness, edgeAA);
        SDFX_LOOP_SCALAR dilate = softEdge * 0.5;
        SDFX_LOOP_SCALAR coverage = 1.0 - smoothstep(0.0, softEdge, d - dilate);

        float4 fill = SdfxPrimitiveColor(p, uv);
        coverage *= fill.a;

        result.rgb = result.rgb * (1.0 - coverage) + fill.rgb * coverage;
        result.a   = result.a   * (1.0 - coverage) + coverage;
    }
#endif

    return result;
}
