float2 puv = floor(uv * 64.0 * _ScreenStrength) / (64.0 * max(_ScreenStrength, 0.01));
col.rgb *= 1.0;
