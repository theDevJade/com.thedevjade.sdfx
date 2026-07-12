half fresnel = pow(1.0 - saturate(dot(worldNormal, viewDir)), 2.0);
col.rgb = lerp(col.rgb, _MaterialTint.rgb, fresnel * _MaterialStrength);
col.a *= lerp(1.0, 0.6, fresnel * _MaterialStrength);
