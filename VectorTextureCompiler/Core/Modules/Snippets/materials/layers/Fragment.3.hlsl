half4 mask = tex2D(_LayerTex, uv);
col.r = lerp(col.r, col.r * mask.r, _LayerStrength);
col.g = lerp(col.g, col.g * mask.g, _LayerStrength);
col.b = lerp(col.b, col.b * mask.b, _LayerStrength);
col.a *= lerp(1.0, mask.a, _LayerStrength);
