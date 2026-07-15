half3 SdfxOverlayBlendRgb(half3 baseRgb, half3 blendRgb, int mode)
{
    if (mode == 3) // Multiply
    {
        return baseRgb * blendRgb;
    }
    if (mode == 4) // Add
    {
        return baseRgb + blendRgb;
    }
    if (mode == 5) // Screen
    {
        return 1.0h - (1.0h - baseRgb) * (1.0h - blendRgb);
    }
    if (mode == 6) // Overlay
    {
        return SdfxBlendOverlay(baseRgb, blendRgb);
    }
    if (mode == 7) // SoftLight
    {
        return SdfxBlendSoftLight(baseRgb, blendRgb);
    }
    if (mode == 8) // ColorBurn
    {
        return saturate(1.0h - (1.0h - baseRgb) / max(blendRgb, 1e-3h));
    }
    if (mode == 9) // ColorDodge
    {
        return saturate(baseRgb / max(1.0h - blendRgb, 1e-3h));
    }
    if (mode == 10) // Subtract
    {
        return baseRgb - blendRgb;
    }
    if (mode == 11) // Lighten
    {
        return max(baseRgb, blendRgb);
    }
    if (mode == 12) // Darken
    {
        return min(baseRgb, blendRgb);
    }

    // Alpha / Underlay / SoftUnderlay: replace with blend color
    return blendRgb;
}

half SdfxOverlayApplyWeight(half alpha, int mode, half uncovered)
{
    if (mode == 1) // Underlay
    {
        return alpha * uncovered;
    }
    if (mode == 2) // SoftUnderlay
    {
        return alpha * lerp(1.0h, uncovered, 0.65h);
    }
    return alpha;
}

half3 SdfxApplyOverlayLayer(half3 baseRgb, half4 src, half strength, int mode, half uncovered)
{
    half a = saturate(src.a * strength);
    if (a <= 1e-4h)
    {
        return baseRgb;
    }

    half w = SdfxOverlayApplyWeight(a, mode, uncovered);
    half3 blended = SdfxOverlayBlendRgb(baseRgb, src.rgb, mode);
    return lerp(baseRgb, blended, w);
}
