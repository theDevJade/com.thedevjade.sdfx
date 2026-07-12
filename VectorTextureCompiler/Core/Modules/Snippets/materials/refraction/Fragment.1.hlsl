half fresnel = pow(1.0 - sdfxSignals.ndv, 3.0);
col.rgb = lerp(col.rgb, _RefractionTint.rgb, fresnel * _RefractionStrength * 5.0);
col.a *= lerp(1.0, _RefractionTint.a, fresnel);
