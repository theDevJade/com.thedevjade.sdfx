col.rgb = saturate(floor(col.rgb * _PosterizeSteps) / max(_PosterizeSteps - 1.0, 1.0));
