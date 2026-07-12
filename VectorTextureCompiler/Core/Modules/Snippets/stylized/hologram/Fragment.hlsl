half scan = sin(uv.y * 200.0 + _Time.y * _HologramSpeed) * 0.5 + 0.5;
half flicker = step(0.1, frac(sin(_Time.y * 7.0) * 43758.5453));
half fresnel = pow(1.0 - sdfxSignals.ndv, 2.0);
col.rgb = lerp(col.rgb, _HologramColor.rgb, scan * fresnel * flicker * _HologramStrength);
col.a *= lerp(1.0, 0.6 + 0.4 * scan, _HologramStrength);
