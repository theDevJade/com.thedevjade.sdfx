half flicker = step(0.3, frac(sin(_Time.y * _EmissionSpeed * 12.0) * 43758.5453));
col.rgb += _EmissionColor.rgb * flicker * _EmissionStrength;
