float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_V, worldNormal));
float2 matcapUv = viewNormal.xy * 0.5 + 0.5;
fixed3 a = tex2D(_MatcapTexA, matcapUv).rgb;
fixed3 b = tex2D(_MatcapTexB, matcapUv).rgb;
col.rgb = lerp(col.rgb, col.rgb * lerp(a, b, _DualMatcapBlend) * 2.0 * _DualMatcapIntensity, 0.5);
