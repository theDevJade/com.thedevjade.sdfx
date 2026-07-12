using System.Collections.Generic;
using SDFX.Rasterizer;
using UnityEngine;

namespace SDFX.Rasterizer.Vectorizers
{
    internal sealed class PotraceTracingVectorizer : RasterVectorizerBase
    {
        public override RasterVectorizationAlgorithm Algorithm => RasterVectorizationAlgorithm.PotraceTracing;

        protected override Color32[] Vectorize(RasterVectorizerContext ctx)
        {
            var image = ctx.Image;
            var mask = ConnectedComponents.BuildBinaryMask(image, RasterThresholdMode.Alpha, ctx.RasterOptions.MinAlpha, gpuBuffers: ctx.GpuBuffers);
            var contours = PotraceTracer.Trace(mask, image.Width, image.Height, ctx.RasterOptions.Potrace);
            var preview = new Color32[image.Pixels.Length];
            for (var i = 0; i < preview.Length; i++)
            {
                preview[i] = mask[i] ? new Color32(30, 30, 30, 255) : new Color32(220, 220, 220, 255);
            }

            var uvContours = new List<List<Vector2>>(contours.Count);
            for (var c = 0; c < contours.Count; c++)
            {
                if (contours[c] == null || contours[c].Count < 3)
                {
                    continue;
                }

                uvContours.Add(RasterPathBuilder.PixelPointsToUv(
                    contours[c],
                    image.Width,
                    image.Height,
                    null));
            }

            RasterPathBuilder.AddPolygonContours(ctx, uvContours, Color.black, "raster");

            if (ctx.Svg.PathCount == 0)
            {
                ctx.Issues.Add(new RasterIssue(RasterIssueSeverity.Warning, "No vector contours detected.", "raster", 0, RasterIssueCode.InvalidGeometry));
            }

            return preview;
        }
    }
}
