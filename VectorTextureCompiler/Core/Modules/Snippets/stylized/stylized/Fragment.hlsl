int stylizedMode = (int)round(_StylizedMode);
half halftoneAmt = _StylizedHalftone;
half ditherAmt = _StylizedDither;
if (stylizedMode == 0) halftoneAmt = max(halftoneAmt, _StylizedStrength);
else if (stylizedMode == 1) ditherAmt = max(ditherAmt, _StylizedStrength);
else if (stylizedMode == 6) {
    halftoneAmt = max(halftoneAmt, _StylizedStrength);
    ditherAmt = max(ditherAmt, _StylizedStrength);
}
SdfxApplyHalftone(col.rgb, uv, _StylizedScale, halftoneAmt);
SdfxApplyDither(col.rgb, uv, _StylizedScale, _StylizedDitherLevels, ditherAmt);
if (stylizedMode == 2) {
    float2 puv = floor(uv * _StylizedScale) / _StylizedScale;
    uv = lerp(uv, puv, _StylizedStrength * 0.5);
}
else if (stylizedMode == 3) {
    half scan = sin(uv.y * _StylizedScale * 3.14159) * 0.5 + 0.5;
    col.rgb *= lerp(1.0, 0.8 + 0.2 * scan, _StylizedStrength);
    col.r *= lerp(1.0, 1.05, _StylizedStrength);
}
else if (stylizedMode == 4) {
    half noise = SdfxHash21(uv * _Time.y + _StylizedScale);
    col.rgb = lerp(col.rgb, col.rgb * (0.9 + noise * 0.2), _StylizedStrength);
    half roll = step(0.98, frac(_Time.y * 0.5));
    col.rgb = lerp(col.rgb, float3(1,0,1), roll * _StylizedStrength * 0.3);
}
else if (stylizedMode == 5) {
    half lines = step(0.5, frac(uv.y * _StylizedScale));
    col.rgb *= lerp(1.0, 0.7 + 0.3 * lines, _StylizedStrength);
}
