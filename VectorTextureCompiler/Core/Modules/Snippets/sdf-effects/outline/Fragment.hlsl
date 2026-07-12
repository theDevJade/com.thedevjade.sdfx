float outlineFw = max(length(fwidth(uv)) * 0.70710678, 0.0001);
half outline = 1.0 - smoothstep(_OutlineWidth, _OutlineWidth + outlineFw * 2.0, abs(sdfDist));
outline *= _OutlineColor.a;
col.rgb = lerp(col.rgb, _OutlineColor.rgb, outline);
col.a = max(col.a, outline);
