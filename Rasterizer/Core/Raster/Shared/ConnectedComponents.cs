using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal static class ConnectedComponents
    {
        public static int[] LabelRegions(int[] labels, int width, int height)
        {
            return LabelSameColorRegions(labels, width, height);
        }

        public static int[] LabelSameColorRegions(int[] labels, int width, int height)
        {
            var regionLabels = new int[labels.Length];
            var currentLabel = 1;
            var parent = new Dictionary<int, int>();

            int Find(int x)
            {
                if (!parent.TryGetValue(x, out var p) || p == x)
                {
                    return x;
                }

                p = Find(p);
                parent[x] = p;
                return p;
            }

            void Union(int a, int b)
            {
                var ra = Find(a);
                var rb = Find(b);
                if (ra != rb)
                {
                    parent[ra] = rb;
                }
            }

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var idx = y * width + x;
                    var color = labels[idx];
                    if (color < 0)
                    {
                        regionLabels[idx] = 0;
                        continue;
                    }

                    var left = 0;
                    if (x > 0 && labels[idx - 1] == color)
                    {
                        left = regionLabels[idx - 1];
                    }

                    var up = 0;
                    if (y > 0 && labels[idx - width] == color)
                    {
                        up = regionLabels[idx - width];
                    }

                    if (left == 0 && up == 0)
                    {
                        regionLabels[idx] = currentLabel;
                        parent[currentLabel] = currentLabel;
                        currentLabel++;
                    }
                    else if (left != 0 && up != 0)
                    {
                        regionLabels[idx] = left;
                        Union(left, up);
                    }
                    else
                    {
                        regionLabels[idx] = left != 0 ? left : up;
                    }
                }
            }

            var remap = new Dictionary<int, int>();
            var next = 1;
            for (var i = 0; i < regionLabels.Length; i++)
            {
                if (regionLabels[i] == 0)
                {
                    continue;
                }

                var root = Find(regionLabels[i]);
                if (!remap.TryGetValue(root, out var mapped))
                {
                    mapped = next++;
                    remap[root] = mapped;
                }

                regionLabels[i] = mapped;
            }

            return regionLabels;
        }

        public static int FindBorderBackgroundLabel(int[] labels, int width, int height)
        {
            if (labels == null || width < 1 || height < 1)
            {
                return -1;
            }

            var counts = new Dictionary<int, int>();
            void Acc(int idx)
            {
                var label = labels[idx];
                if (label < 0)
                {
                    return;
                }

                counts.TryGetValue(label, out var n);
                counts[label] = n + 1;
            }

            for (var x = 0; x < width; x++)
            {
                Acc(x);
                Acc((height - 1) * width + x);
            }

            for (var y = 1; y < height - 1; y++)
            {
                Acc(y * width);
                Acc(y * width + (width - 1));
            }

            var bestLabel = -1;
            var bestCount = 0;
            foreach (var pair in counts)
            {
                if (pair.Value > bestCount)
                {
                    bestCount = pair.Value;
                    bestLabel = pair.Key;
                }
            }

            var borderPixelCount = width * 2 + Mathf.Max(0, height - 2) * 2;
            if (bestLabel < 0 || bestCount * 2 < borderPixelCount)
            {
                return -1;
            }

            return bestLabel;
        }

        public static void MajorityFilter(int[] labels, int width, int height, int iterations = 2)
        {
            if (labels == null || width < 1 || height < 1 || iterations < 1)
            {
                return;
            }

            var scratch = new int[labels.Length];
            var hist = new Dictionary<int, int>(9);
            for (var iter = 0; iter < iterations; iter++)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var idx = y * width + x;
                        var self = labels[idx];
                        if (self < 0)
                        {
                            scratch[idx] = self;
                            continue;
                        }

                        hist.Clear();
                        for (var oy = -1; oy <= 1; oy++)
                        {
                            var yy = y + oy;
                            if (yy < 0 || yy >= height)
                            {
                                continue;
                            }

                            for (var ox = -1; ox <= 1; ox++)
                            {
                                var xx = x + ox;
                                if (xx < 0 || xx >= width)
                                {
                                    continue;
                                }

                                var sample = labels[yy * width + xx];
                                if (sample < 0)
                                {
                                    continue;
                                }

                                hist.TryGetValue(sample, out var n);
                                hist[sample] = n + 1;
                            }
                        }

                        var bestLabel = self;
                        var bestCount = -1;
                        foreach (var pair in hist)
                        {
                            if (pair.Value > bestCount || (pair.Value == bestCount && pair.Key == self))
                            {
                                bestCount = pair.Value;
                                bestLabel = pair.Key;
                            }
                        }

                        scratch[idx] = bestLabel;
                    }
                }

                Array.Copy(scratch, labels, labels.Length);
            }
        }

        public static void AbsorbSmallRegions(int[] labels, int width, int height, int minArea)
        {
            if (labels == null || minArea <= 1 || width < 1 || height < 1)
            {
                return;
            }

            var components = LabelSameColorRegions(labels, width, height);
            var areas = new Dictionary<int, int>();
            for (var i = 0; i < components.Length; i++)
            {
                var c = components[i];
                if (c <= 0)
                {
                    continue;
                }

                areas.TryGetValue(c, out var n);
                areas[c] = n + 1;
            }

            var neighborVotes = new Dictionary<int, int>();
            var absorbTo = new Dictionary<int, int>();

            foreach (var pair in areas)
            {
                if (pair.Value >= minArea)
                {
                    continue;
                }

                neighborVotes.Clear();
                var component = pair.Key;
                var totalVotes = 0;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        if (components[y * width + x] != component)
                        {
                            continue;
                        }

                        for (var oy = -1; oy <= 1; oy++)
                        {
                            for (var ox = -1; ox <= 1; ox++)
                            {
                                if (ox == 0 && oy == 0)
                                {
                                    continue;
                                }

                                var xx = x + ox;
                                var yy = y + oy;
                                if (xx < 0 || yy < 0 || xx >= width || yy >= height)
                                {
                                    continue;
                                }

                                var nIdx = yy * width + xx;
                                if (components[nIdx] == component)
                                {
                                    continue;
                                }

                                var nLabel = labels[nIdx];
                                if (nLabel < 0)
                                {
                                    continue;
                                }

                                neighborVotes.TryGetValue(nLabel, out var votes);
                                neighborVotes[nLabel] = votes + 1;
                                totalVotes++;
                            }
                        }
                    }
                }

                if (totalVotes == 0)
                {
                    continue;
                }

                var bestLabel = -1;
                var bestVotes = 0;
                foreach (var vote in neighborVotes)
                {
                    if (vote.Value > bestVotes)
                    {
                        bestVotes = vote.Value;
                        bestLabel = vote.Key;
                    }
                }

                if (bestLabel >= 0 && bestVotes * 2 >= totalVotes)
                {
                    absorbTo[component] = bestLabel;
                }
            }

            if (absorbTo.Count == 0)
            {
                return;
            }

            for (var i = 0; i < labels.Length; i++)
            {
                var component = components[i];
                if (component > 0 && absorbTo.TryGetValue(component, out var newLabel))
                {
                    labels[i] = newLabel;
                }
            }
        }

        /// <summary>
        /// Merges AA fringe ribbons, near-white edge scraps, and flattened dirt speckles
        /// into the dominant surrounding solid so they are not emitted as separate fills.
        /// </summary>
        public static void AbsorbFringesAndSpeckles(
            int[] labels,
            Color32[] palette,
            int width,
            int height,
            int maxArea,
            bool absorbDarkSpeckles)
        {
            if (labels == null || palette == null || maxArea < 2 || width < 1 || height < 1)
            {
                return;
            }

            var components = LabelSameColorRegions(labels, width, height);
            var areas = new Dictionary<int, int>();
            var componentColor = new Dictionary<int, int>();
            for (var i = 0; i < components.Length; i++)
            {
                var c = components[i];
                if (c <= 0)
                {
                    continue;
                }

                areas.TryGetValue(c, out var n);
                areas[c] = n + 1;
                if (!componentColor.ContainsKey(c))
                {
                    componentColor[c] = labels[i];
                }
            }

            var neighborVotes = new Dictionary<int, int>();
            var absorbTo = new Dictionary<int, int>();

            foreach (var pair in areas)
            {
                if (pair.Value > maxArea)
                {
                    continue;
                }

                var component = pair.Key;
                if (!componentColor.TryGetValue(component, out var selfLabel)
                    || selfLabel < 0
                    || selfLabel >= palette.Length)
                {
                    continue;
                }

                neighborVotes.Clear();
                var totalVotes = 0;
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        if (components[y * width + x] != component)
                        {
                            continue;
                        }

                        for (var oy = -1; oy <= 1; oy++)
                        {
                            for (var ox = -1; ox <= 1; ox++)
                            {
                                if (ox == 0 && oy == 0)
                                {
                                    continue;
                                }

                                var xx = x + ox;
                                var yy = y + oy;
                                if (xx < 0 || yy < 0 || xx >= width || yy >= height)
                                {
                                    continue;
                                }

                                var nIdx = yy * width + xx;
                                if (components[nIdx] == component)
                                {
                                    continue;
                                }

                                var nLabel = labels[nIdx];
                                if (nLabel < 0 || nLabel >= palette.Length)
                                {
                                    continue;
                                }

                                neighborVotes.TryGetValue(nLabel, out var votes);
                                neighborVotes[nLabel] = votes + 1;
                                totalVotes++;
                            }
                        }
                    }
                }

                if (totalVotes == 0)
                {
                    continue;
                }

                var bestLabel = -1;
                var bestVotes = 0;
                foreach (var vote in neighborVotes)
                {
                    if (vote.Value > bestVotes)
                    {
                        bestVotes = vote.Value;
                        bestLabel = vote.Key;
                    }
                }

                if (bestLabel < 0 || bestVotes * 2 < totalVotes || bestLabel >= palette.Length)
                {
                    continue;
                }

                var self = palette[selfLabel];
                var surround = palette[bestLabel];
                var selfLuma = Luma(self);
                var surroundLuma = Luma(surround);
                var colorDist = ColorDistance(self, surround);

                var similar = colorDist <= 48f;
                var whiteFringe = selfLuma >= 0.85f && surroundLuma <= 0.8f && colorDist >= 20f;
                var darkSpeckle = absorbDarkSpeckles
                    && surroundLuma - selfLuma >= 0.18f
                    && pair.Value <= maxArea;

                // Keep high-contrast micro features (eyes): very dark, compact, on mid-tone skin.
                var likelyEye = selfLuma <= 0.08f && pair.Value <= 64
                    && surroundLuma > 0.12f && surroundLuma < 0.55f;
                if (likelyEye)
                {
                    continue;
                }

                if (similar || whiteFringe || darkSpeckle)
                {
                    absorbTo[component] = bestLabel;
                }
            }

            if (absorbTo.Count == 0)
            {
                return;
            }

            for (var i = 0; i < labels.Length; i++)
            {
                var component = components[i];
                if (component > 0 && absorbTo.TryGetValue(component, out var newLabel))
                {
                    labels[i] = newLabel;
                }
            }
        }

        private static float ColorDistance(Color32 a, Color32 b)
        {
            var dr = a.r - b.r;
            var dg = a.g - b.g;
            var db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        public static bool[] BuildBinaryMask(RasterImageBuffer image, RasterThresholdMode mode, float minAlpha, int[] quantizedLabels = null, RasterGpuBuffers gpuBuffers = null)
        {
            if (gpuBuffers?.BinaryMask != null)
            {
                return gpuBuffers.BinaryMask;
            }

            var mask = new bool[image.Pixels.Length];
            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    var idx = image.Index(x, y);
                    var c = image.Pixels[idx];
                    mask[idx] = mode switch
                    {
                        RasterThresholdMode.Luma => Luma(c) >= minAlpha,
                        RasterThresholdMode.Quantized => quantizedLabels != null && quantizedLabels[idx] >= 0,
                        _ => c.a / 255f >= minAlpha
                    };
                }
            }

            return mask;
        }

        private static float Luma(Color32 c) => (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
    }
}
