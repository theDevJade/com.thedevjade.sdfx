float haze = sin(uv.y * 40.0 + _Time.y * 3.0) * _RefractionStrength;
col.rgb = lerp(col.rgb, col.rgb * (1.0 + haze), abs(haze) * 10.0);
