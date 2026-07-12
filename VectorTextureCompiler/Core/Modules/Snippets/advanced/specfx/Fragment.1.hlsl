half s = step(0.98, SdfxHash21(uv * _SpecFxScale + _Time.y));
col.rgb += _SpecFxColor.rgb * s * _SpecFxStrength * 2.0;
