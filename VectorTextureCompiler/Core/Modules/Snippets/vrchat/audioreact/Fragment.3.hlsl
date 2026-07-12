half bass = tex2D(_AudioReactTex, float2(0.05, 0.125)).r;
sdfxSignals.audioEnvelope = bass;
col.rgb *= 1.0 + bass * _AudioReactStrength * 1.5;
