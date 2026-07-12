half wet = _SurfaceAmount * (0.5 + 0.5 * saturate(dot(worldNormal, float3(0,1,0))));
col.rgb = lerp(col.rgb, col.rgb * 1.3 + 0.1, wet);
