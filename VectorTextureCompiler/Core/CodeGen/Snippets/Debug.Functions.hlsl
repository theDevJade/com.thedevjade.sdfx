fixed4 SdfxDebugGrid(float2 uv)
{
    float4 cell = tex2Dlod(_GridLookupTex, float4(uv, 0, 0));
    float occ = saturate(cell.g / (float)MAX_PRIMITIVES_PER_CELL);
    return fixed4(occ, 1.0 - occ, 0.0, 1.0);
}

fixed4 SdfxDebugHeatmapFn(float2 uv)
{
    float4 cell = tex2Dlod(_GridLookupTex, float4(uv, 0, 0));
    float t = saturate(cell.g / (float)MAX_PRIMITIVES_PER_CELL);
    float3 c = lerp(float3(0, 0, 1), float3(1, 0, 0), t);
    return fixed4(c, 1.0);
}

fixed4 SdfxDebugDistanceFn(float d)
{
    float band = abs(frac(d * 20.0) - 0.5) * 2.0;
    float3 inside  = float3(0.2, 0.5, 1.0);
    float3 outside = float3(1.0, 0.6, 0.2);
    float3 c = lerp(inside, outside, step(0.0, d)) * (0.4 + 0.6 * band);
    return fixed4(c, 1.0);
}
