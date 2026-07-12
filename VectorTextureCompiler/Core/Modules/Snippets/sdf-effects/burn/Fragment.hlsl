float n = SdfxValueNoise(uv * _BurnScale);
float edge = smoothstep(_BurnAmount, _BurnAmount + 0.08, n) - smoothstep(_BurnAmount + 0.08, _BurnAmount + 0.16, n);
col.rgb += _BurnColor.rgb * edge * 3.0;
col.a *= step(_BurnAmount, n);
