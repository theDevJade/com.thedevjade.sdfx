half h = saturate(i.worldPos.y * _WorldScale);
col.rgb = lerp(col.rgb, lerp(_WorldColorA.rgb, _WorldColorB.rgb, h), _WorldStrength);
