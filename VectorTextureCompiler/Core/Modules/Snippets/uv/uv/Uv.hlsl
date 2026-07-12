uv = uv * _UvScaleOffset.xy + _UvScaleOffset.zw;
int mode = (int)round(_UvMode);
if (mode == 1) { uv += _UvScrollSpeed.xy * _Time.y; }
if (mode == 2) {
    float a = _UvSpinSpeed * _Time.y;
    float2 c = uv - 0.5;
    float cs = cos(a); float sn = sin(a);
    uv = float2(cs * c.x - sn * c.y, sn * c.x + cs * c.y) + 0.5;
}
if (mode == 3) {
    float2 centered = uv - 0.5;
    float r = length(centered);
    float ang = atan2(centered.y, centered.x) / 6.2831853;
    uv = float2(r * 2.0, ang);
}
if (mode == 4) {
    float3 wp = i.worldPos;
    float3 n = abs(normalize(i.worldNormal));
    n /= max(n.x + n.y + n.z, 1e-4);
    uv = wp.xy * n.z + wp.xz * n.y + wp.yz * n.x;
}
if (mode == 5) {
    float cols = max(_UvFlipbookCols, 1.0);
    float rows = max(_UvFlipbookRows, 1.0);
    float frame = floor(_Time.y * _UvFlipbookSpeed);
    float col = fmod(frame, cols);
    float row = floor(frame / cols);
    uv = frac(uv) / float2(cols, rows) + float2(col, row) / float2(cols, rows);
}
if (mode == 6) {
    uv = i.worldPos.xy * _UvScaleOffset.xy + _UvScaleOffset.zw;
}
if (mode == 7) {
    float4 sp = ComputeScreenPos(i.pos);
    uv = (sp.xy / sp.w) * _UvScaleOffset.xy + _UvScaleOffset.zw;
}
