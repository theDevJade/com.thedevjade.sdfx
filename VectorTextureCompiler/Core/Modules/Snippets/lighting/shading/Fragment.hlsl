float3 lighting = sdfxSignals.lightColor * sdfxSignals.ndl + sdfxSignals.ambient;
lighting = max(lighting, _ShadingMinBrightness.xxx);
col.rgb *= lerp(float3(1.0, 1.0, 1.0), lighting, _ShadingStrength);
