half band = saturate(floor(sdfxSignals.ndl * _ToonSteps * _ToonBandWidth) / max(_ToonSteps - 1.0, 1.0));
half3 ramp = tex2D(_ToonRampTex, float2(band, 0.5)).rgb;
float3 lit = sdfxSignals.lightColor + sdfxSignals.ambient;
float3 toon = lerp(_ToonShadowTint.rgb, lit * ramp, band);
col.rgb *= lerp(float3(1.0, 1.0, 1.0), toon, _ToonStrength);
