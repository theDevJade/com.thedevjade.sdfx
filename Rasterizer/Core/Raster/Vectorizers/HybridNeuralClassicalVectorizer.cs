using System.Collections.Generic;
using SDFX.Rasterizer;
using SDFX.Rasterizer.Inference;
using UnityEngine;

namespace SDFX.Rasterizer.Vectorizers
{
    internal sealed class HybridNeuralClassicalVectorizer : RasterVectorizerBase
    {
        public override RasterVectorizationAlgorithm Algorithm => RasterVectorizationAlgorithm.HybridNeuralClassical;

        protected override Color32[] Vectorize(RasterVectorizerContext ctx)
        {
            var options = ctx.RasterOptions.NeuralHybrid;
            if (!SentisInferenceService.TryRunSegmentation(
                    ctx.Image,
                    options.SegmentationModelPath,
                    ctx.RasterOptions.Neural.ConfidenceThreshold,
                    out var labels,
                    ctx.Issues))
            {
                if (ctx.RasterOptions.Strictness == RasterStrictness.Strict)
                {
                    return null;
                }

                labels = ColorQuantizer.Quantize(ctx.Image, ctx.RasterOptions.ColorQuant, ctx.RasterOptions.MinAlpha, out _);
            }

            var unique = new HashSet<int>();
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i] >= 0)
                {
                    unique.Add(labels[i]);
                }
            }

            var orderedLabels = RasterPathBuilder.OrderLabelsByAreaDescending(labels, unique);
            for (var i = 0; i < orderedLabels.Count; i++)
            {
                var label = orderedLabels[i];
                if (CountLabelArea(labels, label) < options.MinRegionArea)
                {
                    continue;
                }

                if (options.PerRegionAlgorithm == RasterPerRegionAlgorithm.PotraceTracing)
                {
                    EmitPotraceRegion(ctx, labels, label);
                }
                else
                {
                    EmitContourRegion(ctx, labels, label);
                }
            }

            if (ctx.Svg.PathCount == 0)
            {
                ctx.Issues.Add(new RasterIssue(RasterIssueSeverity.Warning, "No vector contours detected.", "raster", 0, RasterIssueCode.InvalidGeometry));
            }

            var palette = BuildPalette(ctx.Image, labels, unique.Count);
            return ColorQuantizer.BuildLabelPreview(ctx.Image, labels, palette);
        }

        private static void EmitContourRegion(RasterVectorizerContext ctx, int[] labels, int label)
        {
            var contours = ContourTracer.TraceLabelBoundaries(labels, ctx.Image.Width, ctx.Image.Height, label);
            var uvContours = new List<List<Vector2>>(contours.Count);
            Color color = Color.clear;
            for (var c = 0; c < contours.Count; c++)
            {
                var simplified = PathSimplifier.DouglasPeucker(contours[c], ctx.RasterOptions.Hybrid.SimplifyTolerance);
                if (simplified.Count < 3)
                {
                    continue;
                }

                if (uvContours.Count == 0)
                {
                    color = AverageColorAlongContour(ctx.Image, contours[c]);
                }

                uvContours.Add(RasterPathBuilder.PixelPointsToUv(
                    simplified,
                    ctx.Image.Width,
                    ctx.Image.Height,
                    null));
            }

            RasterPathBuilder.AddPolygonContours(ctx, uvContours, color, "raster");
        }

        private static void EmitPotraceRegion(RasterVectorizerContext ctx, int[] labels, int label)
        {
            var mask = new bool[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                mask[i] = labels[i] == label;
            }

            var contours = PotraceTracer.Trace(mask, ctx.Image.Width, ctx.Image.Height, ctx.RasterOptions.Potrace);
            var uvContours = new List<List<Vector2>>(contours.Count);
            Color color = Color.clear;
            for (var c = 0; c < contours.Count; c++)
            {
                if (contours[c] == null || contours[c].Count < 3)
                {
                    continue;
                }

                if (uvContours.Count == 0)
                {
                    color = AverageColorAlongContour(ctx.Image, contours[c]);
                }

                uvContours.Add(RasterPathBuilder.PixelPointsToUv(
                    contours[c],
                    ctx.Image.Width,
                    ctx.Image.Height,
                    null));
            }

            RasterPathBuilder.AddPolygonContours(ctx, uvContours, color, "raster");
        }

        private static int CountLabelArea(int[] labels, int label)
        {
            var count = 0;
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i] == label)
                {
                    count++;
                }
            }

            return count;
        }

        private static Color32[] BuildPalette(RasterImageBuffer image, int[] labels, int count)
        {
            var palette = new Color32[Mathf.Max(1, count)];
            for (var i = 0; i < palette.Length; i++)
            {
                palette[i] = new Color32(128, 128, 128, 255);
            }

            return palette;
        }
    }
}
