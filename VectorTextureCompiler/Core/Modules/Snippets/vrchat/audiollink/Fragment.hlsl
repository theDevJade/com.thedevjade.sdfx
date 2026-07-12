float2 alUv = float2(0.25, (_AudioLinkBand + 0.5) / 4.0);
half bass = tex2D(_AudioLinkTex, alUv).r;
col.rgb += col.rgb * bass * _AudioLinkStrength;
