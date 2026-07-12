half ndl = saturate(sdfxSignals.ndl + _ShadowOffset);
ndl = pow(ndl, _ShadowSharpness);
half3 ramp = tex2D(_ShadowRampTex, float2(ndl, 0.5)).rgb;
col.rgb *= lerp(_ShadowTint.rgb, ramp, ndl);
