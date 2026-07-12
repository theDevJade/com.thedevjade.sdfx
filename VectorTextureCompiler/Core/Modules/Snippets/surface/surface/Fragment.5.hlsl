half edge = 1.0 - smoothstep(0.0, 0.02, abs(sdfDist));
col.rgb = lerp(col.rgb, _SurfaceColor.rgb, edge * _SurfaceAmount);
