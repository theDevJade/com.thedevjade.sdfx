half slope = 1.0 - saturate(dot(worldNormal, float3(0,1,0)));
col.rgb = lerp(col.rgb, _WorldColorA.rgb, slope * _WorldStrength);
