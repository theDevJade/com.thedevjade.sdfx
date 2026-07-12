col.rgb = lerp(col.rgb, _SurfaceColor.rgb * col.rgb, _SurfaceAmount * saturate(dot(worldNormal, float3(0,1,0))));
