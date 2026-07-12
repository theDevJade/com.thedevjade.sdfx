using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class ColorQuantizer
    {
        public static int[] Quantize(RasterImageBuffer image, RasterColorQuantOptions options, float minAlpha, out Color32[] palette)
        {
            palette = BuildPalette(image, options, minAlpha);
            return AssignLabels(image, palette, minAlpha);
        }

        public static Color32[] BuildPalette(RasterImageBuffer image, RasterColorQuantOptions options, float minAlpha, int sampleLimit = 0)
        {
            var colorCount = Mathf.Clamp(options.ColorCount, 2, 256);
            var histogram = BuildHistogram(image, minAlpha);
            if (histogram.Count == 0)
            {
                return new[] { new Color32(0, 0, 0, 0) };
            }

            // Pixel-art / atlas textures: keep exact source colors.
            if (options.Method == RasterColorQuantMethod.MedianCut || options.Method == RasterColorQuantMethod.Octree)
            {
                return SelectExactPalette(histogram, colorCount);
            }

            var samples = FlattenHistogram(histogram);
            samples = LimitSamples(samples, sampleLimit);
            return KMeans(samples, colorCount);
        }

        public static int[] AssignLabels(RasterImageBuffer image, Color32[] palette, float minAlpha, int[] preassignedLabels = null)
        {
            if (preassignedLabels != null && preassignedLabels.Length == image.Pixels.Length)
            {
                return preassignedLabels;
            }

            var exact = new Dictionary<int, int>(palette.Length);
            for (var i = 0; i < palette.Length; i++)
            {
                var key = PackRgb(palette[i]);
                if (!exact.ContainsKey(key))
                {
                    exact[key] = i;
                }
            }

            var pixels = image.Pixels;
            var labels = new int[pixels.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a / 255f < minAlpha)
                {
                    labels[i] = -1;
                    continue;
                }

                if (exact.TryGetValue(PackRgb(pixels[i]), out var exactIndex))
                {
                    labels[i] = exactIndex;
                    continue;
                }

                labels[i] = NearestPaletteIndex(pixels[i], palette);
            }

            return labels;
        }

        private static Dictionary<int, int> BuildHistogram(RasterImageBuffer image, float minAlpha)
        {
            var histogram = new Dictionary<int, int>();
            var pixels = image.Pixels;
            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                if (c.a / 255f < minAlpha)
                {
                    continue;
                }

                var key = PackRgb(c);
                histogram.TryGetValue(key, out var count);
                histogram[key] = count + 1;
            }

            return histogram;
        }

        private static Color32[] SelectExactPalette(Dictionary<int, int> histogram, int colorCount)
        {
            if (histogram.Count <= colorCount)
            {
                var exact = new Color32[histogram.Count];
                var i = 0;
                foreach (var pair in histogram)
                {
                    exact[i++] = UnpackRgb(pair.Key);
                }

                return exact;
            }

            var ranked = new List<KeyValuePair<int, int>>(histogram);
            ranked.Sort((a, b) => b.Value.CompareTo(a.Value));

            var palette = new Color32[colorCount];
            var used = new bool[ranked.Count];
            palette[0] = UnpackRgb(ranked[0].Key);
            used[0] = true;

            for (var slot = 1; slot < colorCount; slot++)
            {
                var bestIndex = -1;
                var bestScore = -1f;
                for (var c = 0; c < ranked.Count; c++)
                {
                    if (used[c])
                    {
                        continue;
                    }

                    var candidate = UnpackRgb(ranked[c].Key);
                    var minDist = float.MaxValue;
                    for (var p = 0; p < slot; p++)
                    {
                        var d = ColorDistSq(candidate, palette[p]);
                        if (d < minDist)
                        {
                            minDist = d;
                        }
                    }

                    var score = minDist + ranked[c].Value * 0.01f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIndex = c;
                    }
                }

                if (bestIndex < 0)
                {
                    break;
                }

                used[bestIndex] = true;
                palette[slot] = UnpackRgb(ranked[bestIndex].Key);
            }

            return palette;
        }

        private static float ColorDistSq(Color32 a, Color32 b)
        {
            var dr = a.r - b.r;
            var dg = a.g - b.g;
            var db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }

        private static List<Color32> FlattenHistogram(Dictionary<int, int> histogram)
        {
            var samples = new List<Color32>(histogram.Count);
            foreach (var pair in histogram)
            {
                samples.Add(UnpackRgb(pair.Key));
            }

            return samples;
        }

        private static List<Color32> LimitSamples(List<Color32> samples, int sampleLimit)
        {
            if (sampleLimit <= 0 || samples.Count <= sampleLimit)
            {
                return samples;
            }

            var step = Mathf.Max(1, samples.Count / sampleLimit);
            var limited = new List<Color32>(sampleLimit);
            for (var i = 0; i < samples.Count && limited.Count < sampleLimit; i += step)
            {
                limited.Add(samples[i]);
            }

            return limited;
        }

        public static Color32[] BuildLabelPreview(RasterImageBuffer image, int[] labels, Color32[] palette)
        {
            var output = new Color32[image.Pixels.Length];
            for (var i = 0; i < output.Length; i++)
            {
                var label = labels[i];
                output[i] = label < 0 ? new Color32(0, 0, 0, 0) : palette[label];
            }

            return output;
        }

        private static Color32[] KMeans(List<Color32> samples, int colorCount)
        {
            var palette = SelectExactPalette(BuildTempHistogram(samples), colorCount);
            if (palette.Length == 0)
            {
                return new[] { new Color32(0, 0, 0, 0) };
            }

            var centroids = new Vector3[palette.Length];
            for (var i = 0; i < palette.Length; i++)
            {
                centroids[i] = ToVector(palette[i]);
            }

            for (var iter = 0; iter < 6; iter++)
            {
                var sums = new Vector3[centroids.Length];
                var counts = new int[centroids.Length];
                for (var s = 0; s < samples.Count; s++)
                {
                    var idx = NearestPaletteIndex(samples[s], centroids);
                    sums[idx] += ToVector(samples[s]);
                    counts[idx]++;
                }

                for (var i = 0; i < centroids.Length; i++)
                {
                    if (counts[i] > 0)
                    {
                        centroids[i] = sums[i] / counts[i];
                        palette[i] = ToColor(centroids[i]);
                    }
                }
            }

            return palette;
        }

        private static Dictionary<int, int> BuildTempHistogram(List<Color32> samples)
        {
            var histogram = new Dictionary<int, int>();
            for (var i = 0; i < samples.Count; i++)
            {
                var key = PackRgb(samples[i]);
                histogram.TryGetValue(key, out var count);
                histogram[key] = count + 1;
            }

            return histogram;
        }

        private static int NearestPaletteIndex(Color32 color, Color32[] palette)
        {
            var best = 0;
            var bestDist = float.MaxValue;
            var v = ToVector(color);
            for (var i = 0; i < palette.Length; i++)
            {
                var d = (ToVector(palette[i]) - v).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }

            return best;
        }

        private static int NearestPaletteIndex(Color32 color, Vector3[] centroids)
        {
            var best = 0;
            var bestDist = float.MaxValue;
            var v = ToVector(color);
            for (var i = 0; i < centroids.Length; i++)
            {
                var d = (centroids[i] - v).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }

            return best;
        }

        private static int PackRgb(Color32 c) => (c.r << 16) | (c.g << 8) | c.b;

        private static Color32 UnpackRgb(int packed)
            => new Color32((byte)((packed >> 16) & 0xFF), (byte)((packed >> 8) & 0xFF), (byte)(packed & 0xFF), 255);

        private static Vector3 ToVector(Color32 c) => new Vector3(c.r, c.g, c.b);
        private static Color32 ToColor(Vector3 v) => new Color32((byte)Mathf.Clamp(v.x, 0, 255), (byte)Mathf.Clamp(v.y, 0, 255), (byte)Mathf.Clamp(v.z, 0, 255), 255);
    }
}
