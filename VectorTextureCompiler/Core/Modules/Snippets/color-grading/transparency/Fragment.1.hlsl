float2 cell = floor(uv * _TransparencyScale);
half d = step(0.5, frac(cell.x * 0.5 + cell.y * 0.25));
col.a *= lerp(1.0, d, _TransparencyAmount);
