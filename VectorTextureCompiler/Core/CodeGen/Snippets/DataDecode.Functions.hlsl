struct SdfxPrimitive
{
    float2 pos;
    float2 size;
    float4 color;
    int    type;
    float  softness;
    float  rotation;
    int    pathStart;
    int    pathCount;
    float  strokeRadius;
    int    gradStart;
};

float4 SdfxFetchFlat(sampler2D tex, float4 ts, int idx)
{
    uint w = (uint)max((int)(ts.z + 0.5), 1);
    uint uidx = (uint)max(idx, 0);
    uint row = uidx / w;
    uint col = uidx - row * w;
    float2 uv = (float2(col, row) + 0.5) * ts.xy;
    return tex2Dlod(tex, float4(uv, 0, 0));
}

#ifdef SDFX_PRIMITIVE_FLAT_POT_SHIFT
float4 SdfxFetchPrimitiveFlat(int idx)
{
    uint uidx = (uint)max(idx, 0);
    uint col = uidx & ((1u << SDFX_PRIMITIVE_FLAT_POT_SHIFT) - 1u);
    uint row = uidx >> SDFX_PRIMITIVE_FLAT_POT_SHIFT;
    float2 fetchUv = (float2(col, row) + 0.5) * _PrimitiveDataTex_TexelSize.xy;
    return tex2Dlod(_PrimitiveDataTex, float4(fetchUv, 0, 0));
}
#else
#define SdfxFetchPrimitiveFlat(idx) SdfxFetchFlat(_PrimitiveDataTex, _PrimitiveDataTex_TexelSize, idx)
#endif

#ifdef SDFX_GRID_INDEX_FLAT_POT_SHIFT
float4 SdfxFetchGridIndexFlat(int idx)
{
    uint uidx = (uint)max(idx, 0);
    uint col = uidx & ((1u << SDFX_GRID_INDEX_FLAT_POT_SHIFT) - 1u);
    uint row = uidx >> SDFX_GRID_INDEX_FLAT_POT_SHIFT;
    float2 fetchUv = (float2(col, row) + 0.5) * _GridIndexTex_TexelSize.xy;
    return tex2Dlod(_GridIndexTex, float4(fetchUv, 0, 0));
}
#else
#define SdfxFetchGridIndexFlat(idx) SdfxFetchFlat(_GridIndexTex, _GridIndexTex_TexelSize, idx)
#endif

#ifdef SDFX_PATH_FLAT_POT_SHIFT
float4 SdfxFetchPathFlat(int idx)
{
    uint uidx = (uint)max(idx, 0);
    uint col = uidx & ((1u << SDFX_PATH_FLAT_POT_SHIFT) - 1u);
    uint row = uidx >> SDFX_PATH_FLAT_POT_SHIFT;
    float2 fetchUv = (float2(col, row) + 0.5) * _PathDataTex_TexelSize.xy;
    return tex2Dlod(_PathDataTex, float4(fetchUv, 0, 0));
}
#else
#define SdfxFetchPathFlat(idx) SdfxFetchFlat(_PathDataTex, _PathDataTex_TexelSize, idx)
#endif

SdfxPrimitive SdfxFetchPrimitive(int primIdx)
{
    int baseIdx = primIdx * SDFX_TEXELS_PER_PRIM;
    float4 t0 = SdfxFetchPrimitiveFlat(baseIdx + 0);
    float4 t1 = SdfxFetchPrimitiveFlat(baseIdx + 1);
    float4 t2 = SdfxFetchPrimitiveFlat(baseIdx + 2);
    float4 t3 = SdfxFetchPrimitiveFlat(baseIdx + 3);
    SdfxPrimitive p;
    p.pos          = t0.rg;
    p.size         = t0.ba;
    p.color        = t1;
    p.type         = (int)(t2.r + 0.5);
    p.softness     = t2.g;
    p.rotation     = t2.b * 6.28318530718;
    p.pathStart    = (int)(t3.r + 0.5);
    p.pathCount    = (int)(t3.g + 0.5);
    p.strokeRadius = t3.b;
    p.gradStart    = (int)(t3.a + 0.5) - 1;
    return p;
}

float4 SdfxFetchPathEdge(int edgeIdx)
{
    return SdfxFetchPathFlat(edgeIdx);
}

float2 SdfxRayUnitCircleFirstHit(float2 rayStart, float2 rayDir)
{
    float tca = dot(-rayStart, rayDir);
    float d2 = dot(rayStart, rayStart) - tca * tca;
    float thc = sqrt(max(1.0 - d2, 0.0));
    float t0 = tca - thc;
    float t1 = tca + thc;
    float t = min(t0, t1);
    if (t < 0.0) { t = max(t0, t1); }
    return rayStart + rayDir * t;
}

float SdfxRadialAddress(float2 g, float2 focus)
{
    g = (g - 0.5) * 2.0;
    float2 diffDir = g - focus;
    float len = length(diffDir);
    if (len < 1e-5) { return 0.0; }
    float2 perimeter = SdfxRayUnitCircleFirstHit(focus, diffDir / len);
    float2 diff = perimeter - focus;
    if (abs(diff.x) > 0.0001) { return (g.x - focus.x) / diff.x; }
    if (abs(diff.y) > 0.0001) { return (g.y - focus.y) / diff.y; }
    return 0.0;
}

float4 SdfxPrimitiveColor(SdfxPrimitive p, float2 uv)
{
    if (p.gradStart < 0)
    {
        return p.color;
    }

    float4 h  = SdfxFetchPathFlat(p.gradStart + 0);
    float4 r0 = SdfxFetchPathFlat(p.gradStart + 1);
    float4 r1 = SdfxFetchPathFlat(p.gradStart + 2);

    float2 n = (uv - p.pos) / max(p.size, 1e-6);
    n.y = 1.0 - n.y;

    float2 g = float2(
        r0.x * n.x + r0.y * n.y + r0.z,
        r1.x * n.x + r1.y * n.y + r1.z);
    g.y = 1.0 - g.y;

    float t = (h.x > 0.5) ? SdfxRadialAddress(g, h.zw) : g.x;

    int addr = (int)(h.y + 0.5);
    t = (addr == 0) ? frac(t) : t;
    t = (addr == 1) ? saturate(t) : t;
    float m = fmod(abs(t), 2.0);
    t = (addr == 2) ? (m > 1.0 ? 2.0 - m : m) : t;

    float ft = saturate(t) * 7.0;
    uint i0 = (uint)ft;
    uint i1 = min(i0 + 1u, 7u);
    float4 c0 = SdfxFetchPathFlat(p.gradStart + 3 + (int)i0);
    float4 c1 = SdfxFetchPathFlat(p.gradStart + 3 + (int)i1);
    float4 ramp = lerp(c0, c1, ft - (float)i0);

    return ramp * p.color;
}
