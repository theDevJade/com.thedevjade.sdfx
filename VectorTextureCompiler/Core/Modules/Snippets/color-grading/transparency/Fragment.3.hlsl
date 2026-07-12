half fr = pow(1.0 - saturate(dot(worldNormal, viewDir)), 2.0);
col.a *= lerp(1.0, fr, _TransparencyAmount);
