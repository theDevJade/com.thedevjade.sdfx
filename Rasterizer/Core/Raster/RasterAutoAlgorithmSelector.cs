using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;
using UnityEngine;

namespace SDFX.Rasterizer
{
    public readonly struct RasterAlgorithmRecommendation
    {
        public RasterAlgorithmRecommendation(RasterVectorizationAlgorithm algorithm, RasterParsingOptions options, string reason)
        {
            Algorithm = algorithm;
            Options = options;
            Reason = reason ?? string.Empty;
        }

        public RasterVectorizationAlgorithm Algorithm { get; }
        public RasterParsingOptions Options { get; }
        public string Reason { get; }
    }

    public static class RasterAutoAlgorithmSelector
    {
        private const int MaxSamplesPerAxis = 512;

        public static RasterAlgorithmRecommendation Analyze(Texture2D source)
        {
            if (source == null)
            {
                return Fallback(SdfxLanguage.Rasterizer.AutoReasonNoSourceTexture);
            }

            var image = RasterImageIO.Load(source);
            if (image == null)
            {
                return Fallback(SdfxLanguage.Rasterizer.AutoReasonPixelsUnreadable);
            }

            try
            {
                return Analyze(image.Pixels, image.Width, image.Height);
            }
            finally
            {
                image.DisposeReadableCopy();
            }
        }

        internal static RasterAlgorithmRecommendation Analyze(Color32[] pixels, int width, int height)
        {
            var stats = GatherStats(pixels, width, height);
            var options = new RasterParsingOptions { UseComputeAcceleration = true };
            var imageArea = width * height;
            var minRegionArea = Mathf.Clamp(imageArea / 40000, 8, 96);
            var simplifyTolerance = imageArea > 1024 * 1024 ? 0.75f : 0.5f;

            if (stats.TransparentFraction > 0.05f && stats.OpaqueDominantColorCoverage > 0.92f)
            {
                options.Algorithm = RasterVectorizationAlgorithm.SuzukiAbeContours;
                options.Contour.ThresholdMode = RasterThresholdMode.Alpha;
                options.Contour.TraceHoles = true;
                options.Contour.SimplifyTolerance = simplifyTolerance + 0.5f;
                return new RasterAlgorithmRecommendation(
                    options.Algorithm,
                    options,
                    SdfxLanguage.Rasterizer.AutoReasonAlphaSilhouette(
                        stats.TransparentFraction,
                        stats.OpaqueDominantColorCoverage));
            }

            if (stats.BinaryLumaFraction > 0.94f && stats.MeanSaturation < 0.08f && stats.SoftAlphaFraction < 0.1f)
            {
                options.Algorithm = RasterVectorizationAlgorithm.PotraceTracing;
                options.Potrace.TurdSize = Mathf.Max(2, minRegionArea / 4);
                return new RasterAlgorithmRecommendation(
                    options.Algorithm,
                    options,
                    SdfxLanguage.Rasterizer.AutoReasonBlackWhiteArt(stats.BinaryLumaFraction, stats.MeanSaturation));
            }

            if (stats.DominantColorCount <= 16 && stats.DominantColorCoverage > 0.97f && stats.SoftEdgeFraction < 0.05f)
            {
                options.Algorithm = RasterVectorizationAlgorithm.ColorQuantMarchingSquares;
                options.ColorQuant.ColorCount = NextPaletteSize(stats.DominantColorCount);
                options.ColorQuant.SimplifyTolerance = simplifyTolerance;
                options.ColorQuant.MinRegionArea = minRegionArea;
                return new RasterAlgorithmRecommendation(
                    options.Algorithm,
                    options,
                    SdfxLanguage.Rasterizer.AutoReasonFlatPixelArt(
                        stats.DominantColorCount,
                        stats.DominantColorCoverage,
                        options.ColorQuant.ColorCount));
            }

            options.Algorithm = RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf;
            options.ColorQuant.ColorCount = NextPaletteSize(stats.DominantColorCount);
            options.Hybrid.SimplifyTolerance = simplifyTolerance;
            options.Hybrid.MinRegionArea = minRegionArea;
            options.Hybrid.UseBezierFit = stats.SoftEdgeFraction > 0.25f;

            return new RasterAlgorithmRecommendation(
                options.Algorithm,
                options,
                SdfxLanguage.Rasterizer.AutoReasonGeneralImage(
                    stats.DominantColorCount,
                    stats.SoftEdgeFraction,
                    options.ColorQuant.ColorCount,
                    options.Hybrid.UseBezierFit));
        }

        private static RasterAlgorithmRecommendation Fallback(string reason)
        {
            var options = new RasterParsingOptions
            {
                Algorithm = RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf,
                UseComputeAcceleration = true
            };
            return new RasterAlgorithmRecommendation(options.Algorithm, options, reason);
        }

        private static int NextPaletteSize(int measured)
        {
            if (measured <= 6)
            {
                return 8;
            }

            if (measured <= 12)
            {
                return 16;
            }

            if (measured <= 24)
            {
                return 32;
            }

            if (measured <= 48)
            {
                return 48;
            }

            return 64;
        }

        private struct ImageStats
        {
            public float TransparentFraction;
            public float SoftAlphaFraction;
            public float OpaqueDominantColorCoverage;
            public int DominantColorCount;
            public float DominantColorCoverage;
            public float BinaryLumaFraction;
            public float MeanSaturation;
            public float SoftEdgeFraction;
        }

        private static ImageStats GatherStats(Color32[] pixels, int width, int height)
        {
            var stats = new ImageStats();
            var stride = Mathf.Max(1, Mathf.Max(width, height) / MaxSamplesPerAxis);

            var colorBins = new Dictionary<int, int>(1024);
            long sampleCount = 0;
            long transparent = 0;
            long softAlpha = 0;
            long binaryLuma = 0;
            long edgeSamples = 0;
            long softEdges = 0;
            var saturationSum = 0.0;

            for (var y = 0; y < height; y += stride)
            {
                var row = y * width;
                for (var x = 0; x < width; x += stride)
                {
                    var p = pixels[row + x];
                    sampleCount++;

                    if (p.a < 10)
                    {
                        transparent++;
                        continue;
                    }

                    if (p.a < 245)
                    {
                        softAlpha++;
                    }

                    var bin = ((p.r >> 4) << 8) | ((p.g >> 4) << 4) | (p.b >> 4);
                    colorBins.TryGetValue(bin, out var n);
                    colorBins[bin] = n + 1;

                    var maxC = Mathf.Max(p.r, Mathf.Max(p.g, p.b));
                    var minC = Mathf.Min(p.r, Mathf.Min(p.g, p.b));
                    saturationSum += maxC > 0 ? (maxC - minC) / (float)maxC : 0f;

                    var luma = (p.r * 299 + p.g * 587 + p.b * 114) / 1000;
                    if (luma < 40 || luma > 215)
                    {
                        binaryLuma++;
                    }

                    if (x + stride < width)
                    {
                        var q = pixels[row + x + stride];
                        if (q.a >= 10)
                        {
                            var diff = Mathf.Abs(p.r - q.r) + Mathf.Abs(p.g - q.g) + Mathf.Abs(p.b - q.b);
                            if (diff > 12)
                            {
                                edgeSamples++;
                                if (diff < 96)
                                {
                                    softEdges++;
                                }
                            }
                        }
                    }
                }
            }

            if (sampleCount == 0)
            {
                return stats;
            }

            var opaque = sampleCount - transparent;
            stats.TransparentFraction = transparent / (float)sampleCount;
            stats.SoftAlphaFraction = opaque > 0 ? softAlpha / (float)opaque : 0f;
            stats.BinaryLumaFraction = opaque > 0 ? binaryLuma / (float)opaque : 0f;
            stats.MeanSaturation = opaque > 0 ? (float)(saturationSum / opaque) : 0f;
            stats.SoftEdgeFraction = edgeSamples > 0 ? softEdges / (float)edgeSamples : 0f;

            if (colorBins.Count > 0 && opaque > 0)
            {
                var counts = new List<int>(colorBins.Values);
                counts.Sort((a, b) => b.CompareTo(a));

                stats.OpaqueDominantColorCoverage = counts[0] / (float)opaque;

                long covered = 0;
                var needed = 0;
                var target = (long)(opaque * 0.97f);
                for (var i = 0; i < counts.Count && covered < target; i++)
                {
                    covered += counts[i];
                    needed++;
                }

                stats.DominantColorCount = needed;
                stats.DominantColorCoverage = covered / (float)opaque;
            }

            return stats;
        }
    }
}
