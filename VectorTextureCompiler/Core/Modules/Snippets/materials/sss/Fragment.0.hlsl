half3 L = sdfxSignals.lightDir;
half back = saturate(dot(viewDir, -L + worldNormal * _SssDistortion));
col.rgb += _SssColor.rgb * back * _SssStrength;
