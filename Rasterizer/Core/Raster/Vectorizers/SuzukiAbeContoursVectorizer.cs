using System.Collections.Generic;
using SDFX.Rasterizer;
using UnityEngine;

namespace SDFX.Rasterizer.Vectorizers
{
    internal sealed class SuzukiAbeContoursVectorizer : RasterVectorizerBase
    {
        public override RasterVectorizationAlgorithm Algorithm => RasterVectorizationAlgorithm.SuzukiAbeContours;

        protected override Color32[] Vectorize(RasterVectorizerContext ctx)
        {
            var image = ctx.Image;
            var options = ctx.RasterOptions;
            int[] quantLabels = null;
            if (options.Contour.ThresholdMode == RasterThresholdMode.Quantized)
            {
                var palette = ctx.GpuBuffers?.QuantPalette ?? ColorQuantizer.BuildPalette(image, options.ColorQuant, options.MinAlpha);
                quantLabels = ColorQuantizer.AssignLabels(image, palette, options.MinAlpha, ctx.GpuBuffers?.QuantLabels);
            }

            var mask = ConnectedComponents.BuildBinaryMask(image, options.Contour.ThresholdMode, options.MinAlpha, quantLabels, ctx.GpuBuffers);
            var contours = ContourTracer.TraceSuzukiAbe(mask, image.Width, image.Height, options.Contour.TraceHoles);
            var preview = new Color32[image.Pixels.Length];

            for (var i = 0; i < preview.Length; i++)
            {
                preview[i] = mask[i]
                    ? new Color32(80, 180, 255, 255)
                    : new Color32((byte)(image.Pixels[i].r / 2), (byte)(image.Pixels[i].g / 2), (byte)(image.Pixels[i].b / 2), 255);
            }

            var uvContours = new List<List<Vector2>>(contours.Count);
            Color color = Color.black;
            for (var c = 0; c < contours.Count; c++)
            {
                var simplified = PathSimplifier.DouglasPeucker(contours[c], options.Contour.SimplifyTolerance);
                if (simplified.Count < 3)
                {
                    continue;
                }

                if (uvContours.Count == 0)
                {
                    color = AverageColorAlongContour(image, contours[c]);
                }

                uvContours.Add(RasterPathBuilder.PixelPointsToUv(
                    simplified,
                    image.Width,
                    image.Height,
                    null));
            }

            RasterPathBuilder.AddPolygonContours(ctx, uvContours, color, "raster");

            if (ctx.Svg.PathCount == 0)
            {
                ctx.Issues.Add(new RasterIssue(RasterIssueSeverity.Warning, "No vector contours detected.", "raster", 0, RasterIssueCode.InvalidGeometry));
            }

            return preview;
        }
    }
}
