col.rgb = lerp(col.rgb, _SurfaceColor.rgb, _SurfaceAmount * saturate(1.0 - dot(worldNormal, float3(0,1,0))));
