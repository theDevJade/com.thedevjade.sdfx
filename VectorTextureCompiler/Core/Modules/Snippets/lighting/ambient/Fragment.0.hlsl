half ao = SdfxSampleAmbientOcclusion(uv);
col.rgb *= lerp(float3(1,1,1), sdfxSignals.ambient * _AmbientStrength * ao, saturate(_AmbientStrength));
