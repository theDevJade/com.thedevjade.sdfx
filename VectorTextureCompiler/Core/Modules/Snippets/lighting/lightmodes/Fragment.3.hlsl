half ndl = sdfxSignals.ndl;
half burley = ndl * (1.0 + _LightWrap) / (1.0 + _LightWrap * (1.0 - ndl));
float3 lit = sdfxSignals.lightColor * burley + sdfxSignals.ambient;
col.rgb *= lerp(float3(1,1,1), lit, _LightStrength);
