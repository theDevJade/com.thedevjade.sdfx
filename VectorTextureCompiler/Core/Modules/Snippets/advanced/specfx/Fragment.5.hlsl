half2 c = uv - 0.5;
half ring = smoothstep(0.2, 0.22, length(c)) - smoothstep(0.28, 0.3, length(c));
col.rgb += _SpecFxColor.rgb * ring * _SpecFxStrength;
