half mid = tex2D(_AudioReactTex, float2(0.35, 0.125)).r;
sdfxSignals.audioEnvelope = mid;
col.rgb += col.rgb * mid * _AudioReactStrength * 0.8;
