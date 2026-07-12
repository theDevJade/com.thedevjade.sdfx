half spec = tex2D(_AudioReactTex, float2(uv.x, 0.25)).r;
sdfxSignals.audioEnvelope = spec;
col.rgb *= 1.0 + spec * _AudioReactStrength;
