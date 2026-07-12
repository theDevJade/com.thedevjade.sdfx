half f = SdfxValueNoise(uv * _SpecFxScale + float2(0, -_Time.y * 2.0));
col.rgb += _SpecFxColor.rgb * f * _SpecFxStrength;
