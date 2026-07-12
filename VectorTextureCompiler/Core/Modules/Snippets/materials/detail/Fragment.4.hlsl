half det = tex2D(_DetailTex, uv * _DetailScale).a; col.rgb = lerp(col.rgb, col.rgb * det * 2.0, _DetailStrength);
