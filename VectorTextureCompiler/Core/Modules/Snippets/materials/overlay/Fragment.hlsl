half uncovered = saturate(1.0 - art.a);
half g = _OverlayGlobalStrength;

if (_OverlayStrength0 * g > 1e-4)
{
    float2 ouv0 = uv * _OverlayST0.xy + _OverlayST0.zw;
    half4 s0 = tex2D(_OverlayTex0, ouv0);
    col.rgb = SdfxApplyOverlayLayer(col.rgb, s0, _OverlayStrength0 * g, (int)round(_OverlayMode0), uncovered);
}
if (_OverlayStrength1 * g > 1e-4)
{
    float2 ouv1 = uv * _OverlayST1.xy + _OverlayST1.zw;
    half4 s1 = tex2D(_OverlayTex1, ouv1);
    col.rgb = SdfxApplyOverlayLayer(col.rgb, s1, _OverlayStrength1 * g, (int)round(_OverlayMode1), uncovered);
}
if (_OverlayStrength2 * g > 1e-4)
{
    float2 ouv2 = uv * _OverlayST2.xy + _OverlayST2.zw;
    half4 s2 = tex2D(_OverlayTex2, ouv2);
    col.rgb = SdfxApplyOverlayLayer(col.rgb, s2, _OverlayStrength2 * g, (int)round(_OverlayMode2), uncovered);
}
if (_OverlayStrength3 * g > 1e-4)
{
    float2 ouv3 = uv * _OverlayST3.xy + _OverlayST3.zw;
    half4 s3 = tex2D(_OverlayTex3, ouv3);
    col.rgb = SdfxApplyOverlayLayer(col.rgb, s3, _OverlayStrength3 * g, (int)round(_OverlayMode3), uncovered);
}
