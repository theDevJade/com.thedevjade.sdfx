half h = tex2D(_LayerTex, uv).r;
col.rgb = lerp(col.rgb, col.rgb * 1.2, smoothstep(0.4, 0.6, h) * _LayerStrength);
