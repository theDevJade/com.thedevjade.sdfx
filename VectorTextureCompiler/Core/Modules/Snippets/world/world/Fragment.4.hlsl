half hfog = saturate((i.worldPos.y - _WorldScale) * 2.0);
col.rgb = lerp(col.rgb, _WorldColorB.rgb, hfog * _WorldStrength);
