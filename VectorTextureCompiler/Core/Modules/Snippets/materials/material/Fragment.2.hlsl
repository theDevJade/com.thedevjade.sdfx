half ndl = saturate(dot(worldNormal, SdfxLightDir(i.worldPos)));
col.rgb *= lerp(1.0, 0.6 + 0.4 * ndl, _MaterialStrength);
