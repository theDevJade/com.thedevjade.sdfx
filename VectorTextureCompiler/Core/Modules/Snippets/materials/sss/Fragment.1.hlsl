half wrap = saturate((dot(worldNormal, SdfxLightDir(i.worldPos)) + 0.5) / 1.5);
col.rgb = lerp(col.rgb, col.rgb * _SssColor.rgb, wrap * _SssStrength);
