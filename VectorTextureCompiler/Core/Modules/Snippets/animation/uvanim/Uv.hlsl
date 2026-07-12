uv += _UvAnimScrollSpeed.xy * _Time.y;
float spinAngle = _UvAnimSpinSpeed * _Time.y;
float spinCos = cos(spinAngle);
float spinSin = sin(spinAngle);
float2 spinCentered = uv - 0.5;
uv = float2(spinCos * spinCentered.x - spinSin * spinCentered.y,
            spinSin * spinCentered.x + spinCos * spinCentered.y) + 0.5;
uv.x += sin(uv.y * _UvAnimWaveFrequency + _Time.y * _UvAnimWaveSpeed) * _UvAnimWaveAmplitude;
