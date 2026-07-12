float2 cell = floor(uv * _ProceduralScale);
float2 f = frac(uv * _ProceduralScale);
float d = 1.0;
[unroll] for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) {
    float2 g = float2(x, y);
    float2 r = g + SdfxHash21(cell + g) - f;
    d = min(d, dot(r, r));
}
col.rgb += sqrt(d) * _ProceduralStrength;
