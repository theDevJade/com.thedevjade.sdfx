float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_V, worldNormal));
float2 matcapUv = viewNormal.xy * 0.5 + 0.5;
fixed3 matcap = tex2D(_MatcapTex, matcapUv).rgb * _MatcapIntensity;
col.rgb = lerp(col.rgb, col.rgb * matcap * 2.0, _MatcapBlend);
