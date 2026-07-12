half e = abs(sin(uv.x * _SpecFxScale * 10.0 + _Time.y * 20.0)) * step(0.7, SdfxHash21(uv * _SpecFxScale));
col.rgb += _SpecFxColor.rgb * e * _SpecFxStrength;
