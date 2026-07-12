half ndl = saturate(sdfxSignals.ndl + _ShadowOffset);
col.rgb *= lerp(_ShadowTint.rgb, sdfxSignals.lightColor, ndl);
