half scan = step(0.5, frac(uv.y * 120.0 * _ScreenStrength));
col.rgb *= lerp(1.0, 0.85, scan * _ScreenStrength);
