half4 layer = tex2D(_LayerTex, uv);
col.rgb *= lerp(1.0, layer.rgb, layer.a * _LayerStrength);
