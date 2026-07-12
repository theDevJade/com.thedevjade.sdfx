using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class PathSimplifier
    {
        public static List<Vector2> Decimate(IReadOnlyList<Vector2> points, int maxEdges, List<RasterIssue> issues)
        {
            if (points.Count <= maxEdges + 1)
            {
                return new List<Vector2>(points);
            }

            issues?.Add(new RasterIssue(
                RasterIssueSeverity.Warning,
                $"Path detail reduced to {maxEdges} edges.",
                "raster",
                0,
                RasterIssueCode.PathDetailReduced));

            var result = new List<Vector2>(maxEdges + 1);
            var step = (points.Count - 1) / (float)maxEdges;
            for (var i = 0; i < maxEdges; i++)
            {
                result.Add(points[Mathf.Min((int)(i * step), points.Count - 1)]);
            }

            result.Add(points[points.Count - 1]);
            return result;
        }

        /// <summary>
        /// Reduces a path under an edge budget by re-running Douglas-Peucker with
        /// progressively larger tolerance while keeping corners.
        /// </summary>
        public static List<Vector2> SimplifyToEdgeBudget(IReadOnlyList<Vector2> points, int maxEdges, List<RasterIssue> issues)
        {
            if (points.Count <= maxEdges + 1)
            {
                return new List<Vector2>(points);
            }

            var tolerance = 0.5f;
            List<Vector2> simplified = null;
            for (var attempt = 0; attempt < 12; attempt++)
            {
                simplified = DouglasPeucker(points, tolerance);
                if (simplified.Count <= maxEdges + 1)
                {
                    issues?.Add(new RasterIssue(
                        RasterIssueSeverity.Warning,
                        $"Path detail reduced to {simplified.Count - 1} edges (budget {maxEdges}).",
                        "raster",
                        0,
                        RasterIssueCode.PathDetailReduced));
                    return simplified;
                }

                tolerance *= 1.7f;
            }

            return Decimate(simplified ?? new List<Vector2>(points), maxEdges, issues);
        }

        public static List<Vector2> DouglasPeucker(IReadOnlyList<Vector2> points, float tolerance)
        {
            if (points.Count < 3)
            {
                return new List<Vector2>(points);
            }

            var keep = new bool[points.Count];
            keep[0] = true;
            keep[points.Count - 1] = true;
            DouglasPeuckerRange(points, 0, points.Count - 1, Mathf.Max(0.0001f, tolerance), keep);

            var result = new List<Vector2>(points.Count);
            for (var i = 0; i < points.Count; i++)
            {
                if (keep[i])
                {
                    result.Add(points[i]);
                }
            }

            return result;
        }

        private static void DouglasPeuckerRange(IReadOnlyList<Vector2> points, int first, int last, float tolerance, bool[] keep)
        {
            if (last <= first + 1)
            {
                return;
            }

            var maxDist = 0f;
            var index = first;
            var a = points[first];
            var b = points[last];
            for (var i = first + 1; i < last; i++)
            {
                var dist = PerpendicularDistance(points[i], a, b);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    index = i;
                }
            }

            if (maxDist <= tolerance)
            {
                return;
            }

            keep[index] = true;
            DouglasPeuckerRange(points, first, index, tolerance, keep);
            DouglasPeuckerRange(points, index, last, tolerance, keep);
        }

        private static float PerpendicularDistance(Vector2 point, Vector2 lineStart, Vector2 lineEnd)
        {
            var dx = lineEnd.x - lineStart.x;
            var dy = lineEnd.y - lineStart.y;
            if (dx == 0f && dy == 0f)
            {
                return Vector2.Distance(point, lineStart);
            }

            var t = ((point.x - lineStart.x) * dx + (point.y - lineStart.y) * dy) / (dx * dx + dy * dy);
            t = Mathf.Clamp01(t);
            var proj = new Vector2(lineStart.x + t * dx, lineStart.y + t * dy);
            return Vector2.Distance(point, proj);
        }
    }
}
