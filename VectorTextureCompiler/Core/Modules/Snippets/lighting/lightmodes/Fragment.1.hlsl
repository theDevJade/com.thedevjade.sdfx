half ndl = saturate((dot(worldNormal, sdfxSignals.lightDir) + _LightWrap) / (1.0 + _LightWrap));
float3 lit = sdfxSignals.lightColor * ndl + sdfxSignals.ambient;
col.rgb *= lerp(float3(1,1,1), lit, _LightStrength);
