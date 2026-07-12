half rim = pow(1.0 - saturate(dot(worldNormal, viewDir)), 4.0);
col.rgb = lerp(col.rgb, _MaterialTint.rgb, rim * _MaterialStrength);
