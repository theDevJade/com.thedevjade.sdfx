half dmg = _SurfaceAmount * saturate(SdfxHash21(uv * 40.0));
col.rgb = lerp(col.rgb, col.rgb * 0.5, dmg);
