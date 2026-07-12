col.rgb += _EmissionColor.rgb * tex2D(_EmissionMask, uv).r * _EmissionStrength;
