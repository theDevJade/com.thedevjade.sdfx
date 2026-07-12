half g = step(0.95, SdfxHash21(floor(uv * _SpecFxScale) + floor(_Time.y * 5.0)));
col.rgb += _SpecFxColor.rgb * g * _SpecFxStrength;
