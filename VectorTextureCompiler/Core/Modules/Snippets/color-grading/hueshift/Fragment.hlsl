float hueAmount = frac(_HueShift + _Time.y * _HueShiftSpeed);
col.rgb = max(SdfxHueRotate(col.rgb, hueAmount), 0.0);
