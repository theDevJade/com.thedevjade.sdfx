float3 lit = sdfxSignals.lightColor + SdfxAmbientL0();
lit = max(lit, _FlatMinBrightness.xxx);
lit = min(lit, _FlatMaxBrightness.xxx);
col.rgb *= lerp(float3(1.0, 1.0, 1.0), lit, _FlatStrength);
