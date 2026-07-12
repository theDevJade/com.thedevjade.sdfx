half dist = saturate(length(i.worldPos - _WorldSpaceCameraPos) * 0.1 * _TransparencyScale);
col.a *= lerp(1.0, 1.0 - dist, _TransparencyAmount);
