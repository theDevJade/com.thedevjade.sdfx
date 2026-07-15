half SdfxSampleAmbientOcclusion(float2 uv)
{
    half ao = tex2D(_AmbientOcclusionMap, uv).r;
    return lerp(1.0h, ao, _AmbientOcclusion);
}
