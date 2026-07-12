half SdfxHalftoneMask(float2 uv, float scale, half lum)
{
    float2 cellUV = frac(uv * scale) - 0.5;
    half dist = length(cellUV) * 2.0;
    return 1.0 - smoothstep(lum - 0.04, lum + 0.04, dist);
}

void SdfxApplyHalftone(inout fixed3 rgb, float2 uv, float scale, half strength)
{
    if (strength <= 0.0001) return;
    half lum = saturate(dot(rgb, half3(0.299, 0.587, 0.114)));
    half mask = SdfxHalftoneMask(uv, scale, lum);
    half3 paper = rgb * lerp(1.0, 0.55, strength);
    rgb = lerp(paper, rgb, mask);
}

void SdfxApplyDither(inout fixed3 rgb, float2 uv, float scale, half levels, half strength)
{
    if (strength <= 0.0001) return;
    float2 cell = floor(uv * scale);
    half threshold = SdfxHash21(cell);
    half lv = max(levels, 2.0);
    half3 quantized = floor(rgb * lv + threshold) / max(lv - 1.0, 1.0);
    rgb = lerp(rgb, quantized, strength);
}
