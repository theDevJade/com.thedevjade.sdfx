half high = tex2D(_AudioReactTex, float2(0.65, 0.125)).r;
sdfxSignals.audioEnvelope = high;
col.rgb += col.rgb * high * _AudioReactStrength * 0.5;
