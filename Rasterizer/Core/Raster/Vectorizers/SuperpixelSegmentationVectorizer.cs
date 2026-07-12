using System.Collections.Generic;
using SDFX.Rasterizer;
using UnityEngine;

namespace SDFX.Rasterizer.Vectorizers
{
    internal sealed class SuperpixelSegmentationVectorizer : RasterVectorizerBase
    {
        public override RasterVectorizationAlgorithm Algorithm => RasterVectorizationAlgorithm.SuperpixelSegmentation;

        protected override Color32[] Vectorize(RasterVectorizerContext ctx)
        {
            var image = ctx.Image;
            var options = ctx.RasterOptions;
            var labels = SuperpixelSegmenter.Segment(image, options.Superpixel, options.MinAlpha, ctx.GpuBuffers?.SuperpixelLabels);
            var maxLabel = -1;
            for (var i = 0; i < labels.Length; i++)
            {
                if (labels[i] > maxLabel)
                {
                    maxLabel = labels[i];
                }
            }

            var labelCount = maxLabel + 1;
            var palette = BuildPalette(image, labels, labelCount);
            var boundaries = ContourTracer.TraceAllLabelBoundaries(labels, image.Width, image.Height);
            var simplify = options.Superpixel.MergeThreshold * 10f;
            var orderedLabels = RasterPathBuilder.OrderLabelsByAreaDescending(labels, boundaries.Keys);

            for (var i = 0; i < orderedLabels.Count; i++)
            {
                var label = orderedLabels[i];
                if (!boundaries.TryGetValue(label, out var contours) || contours.Count == 0)
                {
                    continue;
                }

                var color = label >= 0 && label < palette.Length ? palette[label] : new Color32(0, 0, 0, 0);
                var uvContours = new List<List<Vector2>>(contours.Count);
                for (var c = 0; c < contours.Count; c++)
                {
                    var simplified = PathSimplifier.DouglasPeucker(contours[c], simplify);
                    if (simplified.Count < 3)
                    {
                        continue;
                    }

                    uvContours.Add(RasterPathBuilder.PixelPointsToUv(
                        simplified,
                        image.Width,
                        image.Height,
                        null));
                }

                RasterPathBuilder.AddPolygonContours(ctx, uvContours, color, "raster");
            }

            if (ctx.Svg.PathCount == 0)
            {
                ctx.Issues.Add(new RasterIssue(RasterIssueSeverity.Warning, "No vector contours detected.", "raster", 0, RasterIssueCode.InvalidGeometry));
            }

            return ColorQuantizer.BuildLabelPreview(image, labels, palette);
        }

        private static Color32[] BuildPalette(RasterImageBuffer image, int[] labels, int count)
        {
            if (count <= 0)
            {
                return System.Array.Empty<Color32>();
            }

            var palette = new Color32[count];
            var sums = new Vector3[count];
            var counts = new int[count];
            for (var i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label < 0 || label >= count)
                {
                    continue;
                }

                var c = image.Pixels[i];
                sums[label] += new Vector3(c.r, c.g, c.b);
                counts[label]++;
            }

            for (var i = 0; i < count; i++)
            {
                if (counts[i] == 0)
                {
                    palette[i] = new Color32(0, 0, 0, 0);
                    continue;
                }

                var avg = sums[i] / counts[i];
                palette[i] = new Color32((byte)avg.x, (byte)avg.y, (byte)avg.z, 255);
            }

            return palette;
        }
    }
}
