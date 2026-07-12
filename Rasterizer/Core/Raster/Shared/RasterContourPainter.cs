using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class RasterContourPainter
    {
        public static Color32[] CreateTransparent(int width, int height)
        {
            return new Color32[Mathf.Max(1, width) * Mathf.Max(1, height)];
        }

        public static void Fill(Color32[] pixels, Color32 color)
        {
            if (pixels == null)
            {
                return;
            }

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
        }

        public static void FillPolygon(Color32[] pixels, int width, int height, IReadOnlyList<Vector2> points, Color32 color)
        {
            if (points == null || points.Count < 3)
            {
                return;
            }

            FillPolygonsEvenOdd(pixels, width, height, new[] { points }, color);
        }

        public static void FillPolygonsEvenOdd(Color32[] pixels, int width, int height, IReadOnlyList<IReadOnlyList<Vector2>> contours, Color32 color)
        {
            if (pixels == null || contours == null || contours.Count == 0 || width < 1 || height < 1)
            {
                return;
            }

            var minY = int.MaxValue;
            var maxY = int.MinValue;
            for (var c = 0; c < contours.Count; c++)
            {
                var points = contours[c];
                if (points == null || points.Count < 3)
                {
                    continue;
                }

                for (var i = 0; i < points.Count; i++)
                {
                    var y = Mathf.Clamp(Mathf.FloorToInt(points[i].y), 0, height - 1);
                    if (y < minY)
                    {
                        minY = y;
                    }

                    if (y > maxY)
                    {
                        maxY = y;
                    }
                }
            }

            if (minY > maxY)
            {
                return;
            }

            var nodeX = new List<float>(16);
            for (var y = minY; y <= maxY; y++)
            {
                nodeX.Clear();
                var scanY = y + 0.5f;
                for (var c = 0; c < contours.Count; c++)
                {
                    var points = contours[c];
                    if (points == null || points.Count < 3)
                    {
                        continue;
                    }

                    for (var i = 0; i < points.Count; i++)
                    {
                        var a = points[i];
                        var b = points[(i + 1) % points.Count];
                        if ((a.y <= scanY && b.y > scanY) || (b.y <= scanY && a.y > scanY))
                        {
                            var t = (scanY - a.y) / (b.y - a.y);
                            nodeX.Add(a.x + t * (b.x - a.x));
                        }
                    }
                }

                if (nodeX.Count < 2)
                {
                    continue;
                }

                nodeX.Sort();
                for (var n = 0; n + 1 < nodeX.Count; n += 2)
                {
                    var x0 = Mathf.Clamp(Mathf.CeilToInt(nodeX[n]), 0, width - 1);
                    var x1 = Mathf.Clamp(Mathf.FloorToInt(nodeX[n + 1]), 0, width - 1);
                    var row = y * width;
                    for (var x = x0; x <= x1; x++)
                    {
                        pixels[row + x] = color;
                    }
                }
            }
        }
    }
}
