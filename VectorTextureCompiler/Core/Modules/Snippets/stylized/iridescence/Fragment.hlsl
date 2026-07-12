half fresnel = pow(1.0 - sdfxSignals.ndv, 2.0);
half3 iri = half3(
    sin(fresnel * _IridescenceScale + 0.0),
    sin(fresnel * _IridescenceScale + 2.094),
    sin(fresnel * _IridescenceScale + 4.188)) * 0.5 + 0.5;
col.rgb += iri * _IridescenceStrength * col.a;
