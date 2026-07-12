using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class SuperpixelSegmenter
    {
        public static int[] Segment(RasterImageBuffer image, RasterSuperpixelOptions options, float minAlpha, int[] precomputedLabels = null)
        {
            if (precomputedLabels != null && precomputedLabels.Length == image.Pixels.Length)
            {
                return precomputedLabels;
            }

            var width = image.Width;
            var height = image.Height;
            var target = Mathf.Clamp(options.SuperpixelCount, 4, width * height);
            var grid = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt((width * height) / (float)target)));
            var labels = new int[image.Pixels.Length];
            var centers = new List<Center>();

            for (var gy = grid / 2; gy < height; gy += grid)
            {
                for (var gx = grid / 2; gx < width; gx += grid)
                {
                    var idx = gy * width + gx;
                    if (image.Pixels[idx].a / 255f < minAlpha)
                    {
                        continue;
                    }

                    centers.Add(new Center(gx, gy, image.Pixels[idx]));
                }
            }

            if (centers.Count == 0)
            {
                return labels;
            }

            for (var iter = 0; iter < 5; iter++)
            {
                var sums = new Vector3[centers.Count];
                var counts = new int[centers.Count];
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var idx = y * width + x;
                        var c = image.Pixels[idx];
                        if (c.a / 255f < minAlpha)
                        {
                            labels[idx] = -1;
                            continue;
                        }

                        var best = 0;
                        var bestDist = float.MaxValue;
                        for (var i = 0; i < centers.Count; i++)
                        {
                            var spatial = (new Vector2(x - centers[i].X, y - centers[i].Y)).sqrMagnitude;
                            var color = (ToVector(c) - ToVector(centers[i].Color)).sqrMagnitude;
                            var dist = spatial + options.Compactness * color;
                            if (dist < bestDist)
                            {
                                bestDist = dist;
                                best = i;
                            }
                        }

                        labels[idx] = best;
                        sums[best] += ToVector(c);
                        counts[best]++;
                    }
                }

                for (var i = 0; i < centers.Count; i++)
                {
                    if (counts[i] == 0)
                    {
                        continue;
                    }

                    centers[i] = new Center(centers[i].X, centers[i].Y, ToColor(sums[i] / counts[i]));
                }
            }

            MergeSimilar(labels, image, centers, options.MergeThreshold, minAlpha);
            return RelabelContiguous(labels);
        }

        public static void BuildInitialCenters(RasterImageBuffer image, RasterSuperpixelOptions options, float minAlpha, out List<Vector2> positions, out List<Color32> colors)
        {
            var width = image.Width;
            var height = image.Height;
            var target = Mathf.Clamp(options.SuperpixelCount, 4, width * height);
            var grid = Mathf.Max(1, Mathf.RoundToInt(Mathf.Sqrt((width * height) / (float)target)));
            positions = new List<Vector2>();
            colors = new List<Color32>();
            for (var gy = grid / 2; gy < height; gy += grid)
            {
                for (var gx = grid / 2; gx < width; gx += grid)
                {
                    var idx = gy * width + gx;
                    if (image.Pixels[idx].a / 255f < minAlpha)
                    {
                        continue;
                    }

                    positions.Add(new Vector2(gx, gy));
                    colors.Add(image.Pixels[idx]);
                }
            }
        }

        public static void UpdateCenters(RasterImageBuffer image, int[] labels, List<Vector2> positions, List<Color32> colors, float minAlpha)
        {
            var sums = new Vector3[colors.Count];
            var counts = new int[colors.Count];
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label < 0 || label >= colors.Count || image.Pixels[i].a / 255f < minAlpha)
                {
                    continue;
                }

                sums[label] += ToVector(image.Pixels[i]);
                counts[label]++;
            }

            for (var i = 0; i < colors.Count; i++)
            {
                if (counts[i] == 0)
                {
                    continue;
                }

                colors[i] = ToColor(sums[i] / counts[i]);
            }
        }

        public static int[] RelabelConnected(int[] labels) => RelabelContiguous(labels);

        private static void MergeSimilar(int[] labels, RasterImageBuffer image, List<Center> centers, float threshold, float minAlpha)
        {
            var parent = new int[centers.Count];
            for (var i = 0; i < parent.Length; i++)
            {
                parent[i] = i;
            }

            int Find(int x)
            {
                while (parent[x] != x)
                {
                    parent[x] = parent[parent[x]];
                    x = parent[x];
                }

                return x;
            }

            for (var a = 0; a < centers.Count; a++)
            {
                for (var b = a + 1; b < centers.Count; b++)
                {
                    if ((ToVector(centers[a].Color) - ToVector(centers[b].Color)).magnitude <= threshold * 255f)
                    {
                        parent[Find(b)] = Find(a);
                    }
                }
            }

            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i] < 0 || image.Pixels[i].a / 255f < minAlpha)
                {
                    continue;
                }

                labels[i] = Find(labels[i]);
            }
        }

        private static int[] RelabelContiguous(int[] labels)
        {
            var map = new Dictionary<int, int>();
            var next = 0;
            var output = new int[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i] < 0)
                {
                    output[i] = -1;
                    continue;
                }

                if (!map.TryGetValue(labels[i], out var mapped))
                {
                    mapped = next++;
                    map[labels[i]] = mapped;
                }

                output[i] = mapped;
            }

            return output;
        }

        private static Vector3 ToVector(Color32 c) => new Vector3(c.r, c.g, c.b);
        private static Color32 ToColor(Vector3 v) => new Color32((byte)v.x, (byte)v.y, (byte)v.z, 255);

        private readonly struct Center
        {
            public Center(int x, int y, Color32 color)
            {
                X = x;
                Y = y;
                Color = color;
            }

            public int X { get; }
            public int Y { get; }
            public Color32 Color { get; }
        }
    }
}
