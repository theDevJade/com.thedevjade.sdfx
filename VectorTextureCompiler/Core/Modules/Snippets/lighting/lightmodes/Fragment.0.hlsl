half ndl = sdfxSignals.ndl * 0.5 + 0.5;
float3 lit = sdfxSignals.lightColor * ndl + sdfxSignals.ambient;
col.rgb *= lerp(float3(1,1,1), lit, _LightStrength);
