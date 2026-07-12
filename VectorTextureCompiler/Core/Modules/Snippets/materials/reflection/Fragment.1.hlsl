half fresnel = pow(1.0 - sdfxSignals.ndv, 3.0);
col.rgb += _ReflectionTint.rgb * fresnel * _ReflectionStrength;
