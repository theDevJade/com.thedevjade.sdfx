float outlineFw = max(length(fwidth(uv)) * 0.70710678, 0.0001);
half outline = 1.0 - smoothstep(0.0, outlineFw * 4.0, abs(sdfDist));
col.rgb = lerp(col.rgb, _SsOutlineColor.rgb, outline * _SsOutlineStrength);
