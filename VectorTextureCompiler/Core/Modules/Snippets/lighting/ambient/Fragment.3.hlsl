half ao = lerp(1.0, saturate(0.5 + dot(worldNormal, float3(0,1,0)) * 0.5), _AmbientOcclusion);
col.rgb *= ao;
