float dissolveNoise = SdfxVoronoiCell(uv * _DissolveScale * 0.1);
float cutoff = _DissolveAmount * (1.0 + _DissolveEdgeWidth * 2.0);
float burn = smoothstep(cutoff, cutoff + _DissolveEdgeWidth, dissolveNoise);
col.rgb = lerp(_DissolveEdgeColor.rgb, col.rgb, burn);
col.a *= step(cutoff - _DissolveEdgeWidth, dissolveNoise);
