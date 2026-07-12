float3 n = abs(normalize(worldNormal));
n /= max(n.x + n.y + n.z, 1e-4);
float3 nx = tex2D(_NormalMap, i.worldPos.zy).rgb * 2.0 - 1.0;
float3 ny = tex2D(_NormalMap, i.worldPos.xz).rgb * 2.0 - 1.0;
float3 nz = tex2D(_NormalMap, i.worldPos.xy).rgb * 2.0 - 1.0;
float3 blended = nx * n.x + ny * n.y + nz * n.z;
worldNormal = normalize(lerp(worldNormal, blended, _NormalStrength));
