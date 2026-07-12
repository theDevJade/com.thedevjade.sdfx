using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class ContourTracer
    {
        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(-1, 0),
            new Vector2Int(0, -1)
        };

        public static List<List<Vector2>> TraceSuzukiAbe(bool[] mask, int width, int height, bool traceHoles)
        {
            var visited = new bool[mask.Length];
            return TraceSuzukiAbe(mask, visited, width, height, traceHoles);
        }

        public static List<List<Vector2>> TraceLabelBoundaries(int[] labels, int width, int height, int labelId)
        {
            var mask = new bool[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                mask[i] = labels[i] == labelId;
            }

            return TraceSuzukiAbe(mask, width, height, true);
        }

        public static Dictionary<int, List<List<Vector2>>> TraceAllLabelBoundaries(int[] labels, int width, int height)
        {
            var maxLabel = FindMaxLabel(labels);
            if (maxLabel < 0)
            {
                return new Dictionary<int, List<List<Vector2>>>();
            }

            var present = new bool[maxLabel + 1];
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label >= 0)
                {
                    present[label] = true;
                }
            }

            var mask = new bool[labels.Length];
            var visited = new bool[labels.Length];
            var result = new Dictionary<int, List<List<Vector2>>>();

            for (var label = 0; label < present.Length; label++)
            {
                if (!present[label])
                {
                    continue;
                }

                for (var i = 0; i < labels.Length; i++)
                {
                    mask[i] = labels[i] == label;
                    visited[i] = false;
                }

                var contours = TraceSuzukiAbe(mask, visited, width, height, true);
                if (contours.Count > 0)
                {
                    result[label] = contours;
                }
            }

            return result;
        }

        private static int FindMaxLabel(int[] labels)
        {
            var max = -1;
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i] > max)
                {
                    max = labels[i];
                }
            }

            return max < 0 ? -1 : max;
        }

        private static List<List<Vector2>> TraceSuzukiAbe(bool[] mask, bool[] visited, int width, int height, bool traceHoles)
        {
            var contours = new List<List<Vector2>>();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var idx = y * width + x;
                    if (!mask[idx] || visited[idx] || !IsBoundaryPixel(mask, width, height, x, y))
                    {
                        continue;
                    }

                    var isHole = x > 0 && !mask[idx - 1];
                    if (!traceHoles && isHole)
                    {
                        continue;
                    }

                    var contour = FollowBoundary(mask, visited, width, height, x, y);
                    if (contour.Count >= 3)
                    {
                        contours.Add(contour);
                    }
                }
            }

            return contours;
        }

        private static bool IsBoundaryPixel(bool[] mask, int width, int height, int x, int y)
        {
            if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
            {
                return true;
            }

            var idx = y * width + x;
            return !mask[idx - 1]
                || !mask[idx + 1]
                || !mask[idx - width]
                || !mask[idx + width];
        }

        private static List<Vector2> FollowBoundary(bool[] mask, bool[] visited, int width, int height, int startX, int startY)
        {
            var contour = new List<Vector2>(64);
            var x = startX;
            var y = startY;
            var dir = 0;
            contour.Add(new Vector2(x + 0.5f, y + 0.5f));

            for (var guard = 0; guard < width * height * 4; guard++)
            {
                visited[y * width + x] = true;
                var turned = false;
                for (var t = 0; t < 4; t++)
                {
                    var checkDir = (dir + 3 + t) % 4;
                    var nx = x + Dirs[checkDir].x;
                    var ny = y + Dirs[checkDir].y;
                    if (nx < 0 || ny < 0 || nx >= width || ny >= height)
                    {
                        continue;
                    }

                    if (mask[ny * width + nx])
                    {
                        x = nx;
                        y = ny;
                        dir = checkDir;
                        contour.Add(new Vector2(x + 0.5f, y + 0.5f));
                        turned = true;
                        break;
                    }
                }

                if (!turned)
                {
                    break;
                }

                if (x == startX && y == startY && contour.Count > 3)
                {
                    break;
                }
            }

            return contour;
        }
    }
}
