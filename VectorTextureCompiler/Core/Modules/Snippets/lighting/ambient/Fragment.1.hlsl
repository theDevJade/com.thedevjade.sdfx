half ao = SdfxSampleAmbientOcclusion(uv);
col.rgb += _FakeAmbientColor.rgb * _AmbientStrength * col.a * ao;
