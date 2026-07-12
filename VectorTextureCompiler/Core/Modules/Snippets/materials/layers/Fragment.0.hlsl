half4 layer = tex2D(_LayerTex, uv);
col.rgb = lerp(col.rgb, layer.rgb, layer.a * _LayerStrength);
