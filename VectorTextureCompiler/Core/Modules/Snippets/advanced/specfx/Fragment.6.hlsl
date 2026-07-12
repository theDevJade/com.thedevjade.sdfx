half shock = saturate(1.0 - length(uv - 0.5) * 2.0 + frac(_Time.y));
col.rgb += _SpecFxColor.rgb * shock * _SpecFxStrength;
