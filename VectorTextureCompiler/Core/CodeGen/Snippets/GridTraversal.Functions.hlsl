#ifdef _SDFX_PRECISION_HALF
#define SDFX_LOOP_SCALAR half
#else
#define SDFX_LOOP_SCALAR float
#endif

#define SDFX_EDGE_AA_SCALE 0.70710678
#define SDFX_OPAQUE_ALPHA 0.999

fixed4 SdfxEvaluate(float2 uv, out float minDist)
{
    // Snap to grid-cell texel centers so adjacent fragments in a quad agree
    float2 gridLookupUv = (floor(uv * _GridLookupTex_TexelSize.zw) + 0.5) * _GridLookupTex_TexelSize.xy;
    float4 cellData = tex2Dlod(_GridLookupTex, float4(gridLookupUv, 0, 0));
    int startIdx = (int)(cellData.r + 0.5);
    int count    = (int)(cellData.g + 0.5);

    fixed4 result = fixed4(0.0, 0.0, 0.0, 0.0);
    minDist = 1e5;

    // Screen-space AA width in UV. Hard-edge mode still keeps a 1px band so
    // baked/low-res SDFs do not stair-step; it only skips the softer softness pad.
    SDFX_LOOP_SCALAR edgeAA = max(length(fwidth(uv)) * SDFX_EDGE_AA_SCALE, 0.0001);
    bool hardEdges = _HardEdgeCoverage > 0.5;
#if defined(SDFX_HARD_EDGE_COVERAGE)
    hardEdges = true;
#endif

#ifdef SDFX_GRID_FIXED_BOUND
    #ifdef SDFX_GRID_UNROLL
    [unroll({{MAX_PRIMITIVES_PER_CELL}})]
    #else
    [loop]
    #endif
    for (int i = 0; i < {{MAX_PRIMITIVES_PER_CELL}}; i++)
    {
        float active = (count > 0 && i < count && result.a < SDFX_OPAQUE_ALPHA) ? 1.0 : 0.0;

        [branch]
        if (active > 0.5)
        {
            float rawIdx = SdfxFetchGridIndexFlat(startIdx + i).r;
            int   primIdx = (int)(rawIdx + 0.5);

            SdfxPrimitive p = SdfxFetchPrimitive(primIdx);
            SDFX_LOOP_SCALAR d = SdfxEvalPrimitive(p, uv);
            minDist = min(minDist, d);

            float4 fill = SdfxPrimitiveColor(p, uv);
            // Baked path SDFs always need soft AA — hard threshold follows the
            // low-res zero contour and looks stair-stepped under magnification.
            bool bakedPath = p.pathCount < 0;
            bool primHard = !bakedPath && (hardEdges || p.softness < 0.0);
            SDFX_LOOP_SCALAR softEdge = primHard
                ? edgeAA
                : max(abs(p.softness), edgeAA);
            SDFX_LOOP_SCALAR dilate = softEdge * 0.5;
            SDFX_LOOP_SCALAR coverage = (1.0 - smoothstep(0.0, softEdge, d - dilate)) * fill.a;

            SDFX_LOOP_SCALAR oneMinusA = 1.0 - result.a;
            result.rgb += oneMinusA * coverage * fill.rgb;
            result.a   += oneMinusA * coverage;
        }
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

        float4 fill = SdfxPrimitiveColor(p, uv);
        bool bakedPath = p.pathCount < 0;
        bool primHard = !bakedPath && (hardEdges || p.softness < 0.0);
        SDFX_LOOP_SCALAR softEdge = primHard
            ? edgeAA
            : max(abs(p.softness), edgeAA);
        SDFX_LOOP_SCALAR dilate = softEdge * 0.5;
        SDFX_LOOP_SCALAR coverage = (1.0 - smoothstep(0.0, softEdge, d - dilate)) * fill.a;

        SDFX_LOOP_SCALAR oneMinusA = 1.0 - result.a;
        result.rgb += oneMinusA * coverage * fill.rgb;
        result.a   += oneMinusA * coverage;

        if (result.a >= SDFX_OPAQUE_ALPHA)
            break;
    }
#endif

    return result;
}
