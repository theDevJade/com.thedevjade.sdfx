half mask = tex2D(_ReflectionMask, uv).r;
half fresnel = pow(1.0 - saturate(dot(worldNormal, viewDir)), 2.0);
col.rgb += _ReflectionTint.rgb * fresnel * mask * _ReflectionStrength;
