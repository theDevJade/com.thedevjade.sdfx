half3 L = sdfxSignals.lightDir;
half3 H = normalize(L + viewDir);
half ndl = sdfxSignals.ndl;
half ndh = saturate(dot(worldNormal, H));
half roughness = _Roughness * tex2D(_RoughnessMap, uv).r;
half spec = pow(ndh, lerp(128.0, 4.0, roughness)) * _Specular;
col.rgb = col.rgb * (sdfxSignals.lightColor * ndl + sdfxSignals.ambient) + _SpecularColor.rgb * spec;
