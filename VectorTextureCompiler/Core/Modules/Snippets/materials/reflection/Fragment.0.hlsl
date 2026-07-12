float3 refl = reflect(-viewDir, worldNormal);
float2 matcapUv = normalize(refl).xy * 0.5 + 0.5;
half3 refCol = tex2D(_ReflectionTex, matcapUv).rgb * _ReflectionTint.rgb;
col.rgb = lerp(col.rgb, refCol, _ReflectionStrength);
