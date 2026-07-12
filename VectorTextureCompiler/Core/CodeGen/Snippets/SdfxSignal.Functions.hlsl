struct SdfxSignals
{
#ifdef _SDFX_PRECISION_HALF
    half noise;
    half fresnel;
    half rim;
    half curvature;
    half sdfDist;
    half audioEnvelope;
    half ndv;
    half ndl;
    half3 lightDir;
    half3 lightColor;
    half3 ambient;
#else
    float noise;
    float fresnel;
    float rim;
    float curvature;
    float sdfDist;
    float audioEnvelope;
    float ndv;
    float ndl;
    float3 lightDir;
    float3 lightColor;
    float3 ambient;
#endif
};

SdfxSignals SdfxComputeSignals(float2 uv, float3 worldPos, float3 worldNormal, float3 viewDir, float sdfDist)
{
    SdfxSignals s;
    s.sdfDist = sdfDist;
    s.ndv = saturate(dot(worldNormal, viewDir));
    s.fresnel = pow(1.0 - s.ndv, 3.0);
    s.rim = s.fresnel;
    s.curvature = saturate(dot(worldNormal, float3(0, 1, 0)));
    s.noise = frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
    s.audioEnvelope = 0.0;
    s.lightDir = SdfxLightDir(worldPos);
    s.lightColor = SdfxLightColor();
    s.ambient = SdfxAmbient(worldNormal);
    s.ndl = saturate(dot(worldNormal, s.lightDir));
    return s;
}
