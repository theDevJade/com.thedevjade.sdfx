half beat = tex2D(_AudioReactTex, float2(0.75, 0.125)).r;
sdfxSignals.audioEnvelope = beat;
col.rgb += col.rgb * step(0.6, beat) * _AudioReactStrength;
