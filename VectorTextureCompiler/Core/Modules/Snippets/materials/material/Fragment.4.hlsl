half subsurface = saturate(dot(viewDir, -SdfxLightDir(i.worldPos)) * 0.5 + 0.5);
col.rgb += _MaterialTint.rgb * subsurface * _MaterialStrength * 0.3;
