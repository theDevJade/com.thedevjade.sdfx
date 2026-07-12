float morph = _MorphAmount + sin(_Time.y * _MorphSpeed) * _MorphAmount;
float edge = 1.0 - smoothstep(0.0, 0.01 + abs(morph), abs(sdfDist + morph));
col.rgb = lerp(col.rgb, col.rgb * 1.2, edge);
