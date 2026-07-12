col.r = lerp(col.r, col.r * (1.0 + _RefractionStrength * 2.0), 0.5);
col.b = lerp(col.b, col.b * (1.0 - _RefractionStrength * 2.0), 0.5);
