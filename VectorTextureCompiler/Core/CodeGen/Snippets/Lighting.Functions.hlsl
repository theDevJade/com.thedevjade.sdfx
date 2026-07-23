float3 SdfxLightDir(float3 worldPos)
{
    float3 dir = _WorldSpaceLightPos0.xyz - worldPos * _WorldSpaceLightPos0.w;
    float hasLight = step(1e-6, dot(dir, dir));
    return normalize(lerp(float3(0.3, 0.8, 0.5), dir, hasLight));
}

float3 SdfxLightColor()
{
    float3 c = _LightColor0.rgb;
    float hasColor = step(1e-6, dot(c, c));
    return lerp(float3(1.0, 1.0, 1.0), c, hasColor);
}

float3 SdfxAmbient(float3 normal)
{
    return ShadeSH9(half4(normal, 1.0));
}

float3 SdfxAmbientL0()
{
    return float3(unity_SHAr.w, unity_SHAg.w, unity_SHAb.w);
}
