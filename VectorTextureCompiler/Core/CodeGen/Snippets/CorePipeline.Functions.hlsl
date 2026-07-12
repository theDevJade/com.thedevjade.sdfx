half3 SdfxApplyBaseColorGrading(half3 rgb, half4 vertexColor, half useVertexColor, half vertexColorMode)
{
    if (useVertexColor > 0.5)
    {
        if (vertexColorMode < 0.5)
        {
            rgb *= vertexColor.rgb;
        }
        else if (vertexColorMode < 1.5)
        {
            rgb += vertexColor.rgb;
        }
        else
        {
            rgb = vertexColor.rgb;
        }
    }

    rgb = saturate((rgb - 0.5h) * _Contrast + 0.5h + _Brightness);
    half luma = dot(rgb, half3(0.299h, 0.587h, 0.114h));
    rgb = lerp(half3(luma, luma, luma), rgb, _Saturation);
    rgb *= _Exposure;
    return rgb;
}

half3 SdfxBlendOverlay(half3 baseCol, half3 blendCol)
{
    half3 low = 2.0h * baseCol * blendCol;
    half3 high = 1.0h - 2.0h * (1.0h - baseCol) * (1.0h - blendCol);
    return lerp(low, high, step(0.5h, baseCol));
}

half3 SdfxBlendSoftLight(half3 baseCol, half3 blendCol)
{
    half3 low = baseCol - (1.0h - 2.0h * blendCol) * baseCol * (1.0h - baseCol);
    half3 high = baseCol + (2.0h * blendCol - 1.0h) * (sqrt(baseCol) - baseCol);
    return lerp(low, high, step(0.5h, blendCol));
}

fixed4 SdfxFinalizeColor(fixed4 col, half3 baseRgb)
{
    int mode = (int)round(_BlendMode);
    if (mode == 9)
    {
        col.rgb *= col.a;
    }

    if (mode == 7)
    {
        col.rgb = SdfxBlendOverlay(baseRgb, col.rgb);
    }
    else if (mode == 8)
    {
        col.rgb = SdfxBlendSoftLight(baseRgb, col.rgb);
    }

    return col;
}
