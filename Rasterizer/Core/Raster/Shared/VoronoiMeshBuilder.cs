using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class VoronoiMeshBuilder
    {
        public static List<List<Vector2>> BuildCells(RasterImageBuffer image, float[] edgeMap, RasterVoronoiOptions options, float minAlpha, float edgeThreshold, int[] precomputedNearest = null)
        {
            if (precomputedNearest != null && precomputedNearest.Length == image.Pixels.Length)
            {
                return BuildCellsFromNearest(image, precomputedNearest, options.MaxCells, minAlpha);
            }

            var sites = SampleSites(image, edgeMap, options.SampleDensity, minAlpha, edgeThreshold);
            if (sites.Count == 0)
            {
                return new List<List<Vector2>>();
            }

            sites = sites.GetRange(0, Mathf.Min(sites.Count, options.MaxCells));
            var width = image.Width;
            var height = image.Height;
            var nearest = new int[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var idx = y * width + x;
                    if (image.Pixels[idx].a / 255f < minAlpha)
                    {
                        nearest[idx] = -1;
                        continue;
                    }

                    var best = 0;
                    var bestDist = float.MaxValue;
                    for (var s = 0; s < sites.Count; s++)
                    {
                        var d = (sites[s] - new Vector2(x, y)).sqrMagnitude;
                        if (d < bestDist)
                        {
                            bestDist = d;
                            best = s;
                        }
                    }

                    nearest[idx] = best;
                }
            }

            var cells = new List<List<Vector2>>(sites.Count);
            for (var i = 0; i < sites.Count; i++)
            {
                cells.Add(new List<Vector2>());
            }

            for (var y = 0; y < height - 1; y++)
            {
                for (var x = 0; x < width - 1; x++)
                {
                    var idx = y * width + x;
                    var a = nearest[idx];
                    var b = nearest[idx + 1];
                    var c = nearest[idx + width];
                    if (a < 0)
                    {
                        continue;
                    }

                    if (b >= 0 && a != b)
                    {
                        cells[a].Add(new Vector2(x + 1f, y + 0.5f));
                    }

                    if (c >= 0 && a != c)
                    {
                        cells[a].Add(new Vector2(x + 0.5f, y + 1f));
                    }
                }
            }

            var contours = new List<List<Vector2>>();
            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i].Count < 3)
                {
                    continue;
                }

                var hull = ConvexHull(cells[i]);
                if (hull.Count >= 3)
                {
                    contours.Add(hull);
                }
            }

            return contours;
        }

        private static List<List<Vector2>> BuildCellsFromNearest(RasterImageBuffer image, int[] nearest, int maxCells, float minAlpha)
        {
            var siteCount = 0;
            for (var i = 0; i < nearest.Length; i++)
            {
                if (nearest[i] >= siteCount)
                {
                    siteCount = nearest[i] + 1;
                }
            }

            siteCount = Mathf.Min(siteCount, maxCells);
            if (siteCount <= 0)
            {
                return new List<List<Vector2>>();
            }

            var width = image.Width;
            var height = image.Height;
            var cells = new List<List<Vector2>>(siteCount);
            for (var i = 0; i < siteCount; i++)
            {
                cells.Add(new List<Vector2>());
            }

            for (var y = 0; y < height - 1; y++)
            {
                for (var x = 0; x < width - 1; x++)
                {
                    var idx = y * width + x;
                    var a = nearest[idx];
                    var b = nearest[idx + 1];
                    var c = nearest[idx + width];
                    if (a < 0 || a >= siteCount)
                    {
                        continue;
                    }

                    if (b >= 0 && b < siteCount && a != b)
                    {
                        cells[a].Add(new Vector2(x + 1f, y + 0.5f));
                    }

                    if (c >= 0 && c < siteCount && a != c)
                    {
                        cells[a].Add(new Vector2(x + 0.5f, y + 1f));
                    }
                }
            }

            var contours = new List<List<Vector2>>();
            for (var i = 0; i < cells.Count; i++)
            {
                if (cells[i].Count < 3)
                {
                    continue;
                }

                var hull = ConvexHull(cells[i]);
                if (hull.Count >= 3)
                {
                    contours.Add(hull);
                }
            }

            return contours;
        }

        public static List<Vector2> SampleSites(RasterImageBuffer image, float[] edgeMap, int density, float minAlpha, float edgeThreshold)
        {
            var stride = Mathf.Max(1, density);
            var sites = new List<Vector2>();
            for (var y = 0; y < image.Height; y += stride)
            {
                for (var x = 0; x < image.Width; x += stride)
                {
                    if (image.GetAlpha(x, y) < minAlpha)
                    {
                        continue;
                    }

                    var edge = GradientEdgeMap.Sample(image.Pixels, edgeMap, image.Width, image.Height, x, y, edgeThreshold);
                    if (edge >= edgeThreshold)
                    {
                        sites.Add(new Vector2(x + 0.5f, y + 0.5f));
                    }
                }
            }

            return sites;
        }

        private static List<Vector2> ConvexHull(List<Vector2> points)
        {
            if (points.Count < 3)
            {
                return points;
            }

            points.Sort((a, b) =>
            {
                var cmp = a.x.CompareTo(b.x);
                return cmp != 0 ? cmp : a.y.CompareTo(b.y);
            });

            var lower = new List<Vector2>();
            foreach (var p in points)
            {
                while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], p) <= 0)
                {
                    lower.RemoveAt(lower.Count - 1);
                }

                lower.Add(p);
            }

            var upper = new List<Vector2>();
            for (var i = points.Count - 1; i >= 0; i--)
            {
                var p = points[i];
                while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], p) <= 0)
                {
                    upper.RemoveAt(upper.Count - 1);
                }

                upper.Add(p);
            }

            lower.RemoveAt(lower.Count - 1);
            lower.AddRange(upper);
            return lower;
        }

        private static float Cross(Vector2 a, Vector2 b, Vector2 c)
            => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }
}
