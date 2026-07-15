float3 lit = sdfxSignals.lightColor + ShadeSH9(half4(0.0, 0.0, 0.0, 1.0));
lit = max(lit, _FlatMinBrightness.xxx);
lit = min(lit, _FlatMaxBrightness.xxx);
col.rgb *= lerp(float3(1.0, 1.0, 1.0), lit, _FlatStrength);
