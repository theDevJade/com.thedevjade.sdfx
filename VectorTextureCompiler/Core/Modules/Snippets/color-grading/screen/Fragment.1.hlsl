float2 d = float2(sin(uv.y * 20.0 + _Time.y), cos(uv.x * 20.0)) * _ScreenStrength * 0.01;
col.rgb = lerp(col.rgb, col.rgb * (1.0 + d.x + d.y), _ScreenStrength);
