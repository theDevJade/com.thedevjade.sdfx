half3 H = normalize(SdfxLightDir(i.worldPos) + viewDir);
half spec = pow(saturate(dot(worldNormal, H)), 64.0);
col.rgb += spec * _MaterialTint.rgb * _MaterialStrength;
