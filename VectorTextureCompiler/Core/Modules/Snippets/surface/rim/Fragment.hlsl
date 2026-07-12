half rim = pow(1.0 - sdfxSignals.ndv, _RimPower);
half rimB = pow(1.0 - sdfxSignals.ndv, _RimPowerB);
col.rgb += (_RimColor.rgb * rim + _RimColorB.rgb * rimB) * _RimIntensity * col.a;
