float halo = saturate(1.0 - max(sdfDist, 0.0) / _GlowRadius);
halo = halo * halo * _GlowIntensity;
col.rgb += _GlowColor.rgb * halo;
col.a = max(col.a, saturate(halo) * _GlowColor.a);
