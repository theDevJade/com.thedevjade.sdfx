half band = smoothstep(_CelThreshold - _CelSoftness, _CelThreshold + _CelSoftness, sdfxSignals.ndl);
float3 lit = sdfxSignals.lightColor + sdfxSignals.ambient;
col.rgb *= lerp(_CelShadowColor.rgb, lit, band);
