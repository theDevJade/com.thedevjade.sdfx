half fresnel = pow(1.0 - sdfxSignals.ndv, 3.0);
half pulse = sin(_Time.y * 5.0) * 0.5 + 0.5;
col.rgb += _SpecFxColor.rgb * fresnel * pulse * _SpecFxStrength;
