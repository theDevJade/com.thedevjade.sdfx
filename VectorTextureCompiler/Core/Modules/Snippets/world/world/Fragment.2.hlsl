half curv = saturate(1.0 - dot(worldNormal, float3(0,1,0)));
col.rgb = lerp(col.rgb, col.rgb * (1.0 + curv * 0.3), _WorldStrength);
