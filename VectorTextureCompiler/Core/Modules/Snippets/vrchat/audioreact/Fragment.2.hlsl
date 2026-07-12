half level = tex2D(_AudioReactTex, float2(0.25, 0.125)).r;
sdfxSignals.audioEnvelope = level;
col.rgb *= 1.0 + level * _AudioReactStrength;
col.a *= saturate(1.0 + level * 0.2);
