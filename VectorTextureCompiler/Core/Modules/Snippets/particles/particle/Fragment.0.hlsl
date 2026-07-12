half dist = length(uv - 0.5);
col.a *= lerp(1.0, saturate(1.0 - dist * 2.0), _ParticleStrength);
