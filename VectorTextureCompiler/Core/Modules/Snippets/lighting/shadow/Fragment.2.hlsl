half ndl = saturate(sdfxSignals.ndl + _ShadowOffset);
half band = floor(ndl * _ShadowBands) / max(_ShadowBands - 1.0, 1.0);
col.rgb *= lerp(_ShadowTint.rgb, float3(1,1,1), band);
