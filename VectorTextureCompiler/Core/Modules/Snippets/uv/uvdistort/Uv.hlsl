float2 distort = float2(
    SdfxValueNoise(uv * _UvDistortScale),
    SdfxValueNoise(uv * _UvDistortScale + 17.3)) * 2.0 - 1.0;
uv += distort * _UvDistortStrength;
