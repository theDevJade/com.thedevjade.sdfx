half ao = SdfxSampleAmbientOcclusion(uv);
col.rgb *= lerp(float3(1,1,1), SdfxAmbient(worldNormal) * _AmbientStrength * ao, saturate(_AmbientStrength));
