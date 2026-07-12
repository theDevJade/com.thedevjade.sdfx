using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class BezierFitter
    {
        public static List<Vector2> FitPolyline(IReadOnlyList<Vector2> points, float maxError, float cornerAngleDeg, float minSegmentLength)
        {
            if (points.Count < 2)
            {
                return new List<Vector2>(points);
            }

            var simplified = PathSimplifier.DouglasPeucker(points, Mathf.Max(0.5f, maxError * 0.5f));
            if (simplified.Count <= 2)
            {
                return simplified;
            }

            var corners = DetectCorners(simplified, cornerAngleDeg);
            var result = new List<Vector2>(simplified.Count) { simplified[0] };
            var minLenSq = minSegmentLength * minSegmentLength;
            for (var i = 0; i < corners.Count - 1; i++)
            {
                var start = corners[i];
                var end = corners[i + 1];
                if (end - start < 2)
                {
                    continue;
                }

                var a = simplified[start];
                var b = simplified[end];
                if ((b - a).sqrMagnitude < minLenSq)
                {
                    result.Add(b);
                    continue;
                }

                var mid = simplified[start + ((end - start) >> 1)];
                result.Add(mid);
                result.Add(b);
            }

            return result;
        }

        private static List<int> DetectCorners(IReadOnlyList<Vector2> points, float cornerAngleDeg)
        {
            var corners = new List<int>(points.Count / 4 + 2) { 0 };
            for (var i = 1; i < points.Count - 1; i++)
            {
                var a = (points[i] - points[i - 1]).normalized;
                var b = (points[i + 1] - points[i]).normalized;
                var angle = Vector2.Angle(a, b);
                if (angle >= cornerAngleDeg)
                {
                    corners.Add(i);
                }
            }

            corners.Add(points.Count - 1);
            return corners;
        }
    }
}
