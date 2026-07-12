using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class PotraceTracer
    {
        public static List<List<Vector2>> Trace(bool[] mask, int width, int height, RasterPotraceOptions options)
        {
            var contours = ContourTracer.TraceSuzukiAbe(mask, width, height, true);
            var filtered = new List<List<Vector2>>();
            for (var i = 0; i < contours.Count; i++)
            {
                if (contours[i].Count < options.TurdSize)
                {
                    continue;
                }

                var simplified = PathSimplifier.DouglasPeucker(contours[i], options.OptTolerance);
                var fitted = BezierFitter.FitPolyline(simplified, options.AlphaMax, 45f, 2f);
                if (fitted.Count >= 3)
                {
                    filtered.Add(fitted);
                }
            }

            return filtered;
        }
    }
}
