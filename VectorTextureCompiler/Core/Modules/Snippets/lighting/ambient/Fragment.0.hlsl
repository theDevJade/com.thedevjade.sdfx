col.rgb *= lerp(float3(1,1,1), SdfxAmbient(worldNormal) * _AmbientStrength, saturate(_AmbientStrength));
