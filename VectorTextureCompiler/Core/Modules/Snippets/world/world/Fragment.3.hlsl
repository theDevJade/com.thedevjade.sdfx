half dist = length(i.worldPos - _WorldSpaceCameraPos);
half fog = saturate(dist * _WorldScale);
col.rgb = lerp(col.rgb, _WorldColorB.rgb, fog * _WorldStrength);
