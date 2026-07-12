float n = SdfxValueNoise(uv * _ProceduralScale);
float2 e = float2(0.01, 0.0);
float dx = SdfxValueNoise((uv + e.xy) * _ProceduralScale) - SdfxValueNoise((uv - e.xy) * _ProceduralScale);
float dy = SdfxValueNoise((uv + e.yx) * _ProceduralScale) - SdfxValueNoise((uv - e.yx) * _ProceduralScale);
col.rgb += float3(dy, -dx, 0.0) * _ProceduralStrength;
