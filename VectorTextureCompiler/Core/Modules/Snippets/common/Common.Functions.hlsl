float SdfxHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float SdfxValueNoise(float2 p)
{
    float2 ip = floor(p);
    float2 fp = frac(p);
    fp = fp * fp * (3.0 - 2.0 * fp);
    float a = SdfxHash21(ip);
    float b = SdfxHash21(ip + float2(1.0, 0.0));
    float c = SdfxHash21(ip + float2(0.0, 1.0));
    float d = SdfxHash21(ip + float2(1.0, 1.0));
    return lerp(lerp(a, b, fp.x), lerp(c, d, fp.x), fp.y);
}

float SdfxFbm(float2 p, int octaves)
{
    float v = 0.0;
    float a = 0.5;
    float2 shift = float2(100.0, 100.0);
    [loop]
    for (int i = 0; i < octaves; i++)
    {
        v += a * SdfxValueNoise(p);
        p = p * 2.0 + shift;
        a *= 0.5;
    }
    return v;
}

float SdfxVoronoiCell(float2 p)
{
    float2 cell = floor(p);
    float2 f = frac(p);
    float d = 1.0;
    [unroll] for (int y = -1; y <= 1; y++) for (int x = -1; x <= 1; x++) {
        float2 g = float2(x, y);
        float2 r = g + SdfxHash21(cell + g) - f;
        d = min(d, dot(r, r));
    }
    return sqrt(d);
}

float3 SdfxRgbToHsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 SdfxHsvToRgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}
