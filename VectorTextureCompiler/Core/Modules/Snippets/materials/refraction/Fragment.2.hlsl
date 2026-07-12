col.rgb = lerp(col.rgb, col.rgb * _RefractionTint.rgb, _RefractionStrength * 5.0);
col.a *= lerp(1.0, 0.7, _RefractionStrength * 5.0);
