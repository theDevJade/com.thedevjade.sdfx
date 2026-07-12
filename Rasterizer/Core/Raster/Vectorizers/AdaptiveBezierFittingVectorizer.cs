using SDFX.Rasterizer;
using UnityEngine;

namespace SDFX.Rasterizer.Vectorizers
{
    internal sealed class AdaptiveBezierFittingVectorizer : RasterVectorizerBase
    {
        public override RasterVectorizationAlgorithm Algorithm => RasterVectorizationAlgorithm.AdaptiveBezierFitting;

        protected override Color32[] Vectorize(RasterVectorizerContext ctx)
        {
            var image = ctx.Image;
            var options = ctx.RasterOptions;
            var edgeMap = GradientEdgeMap.Compute(image, options, ctx.Issues, ctx.GpuBuffers);
            var edgeMask = ctx.GpuBuffers?.EdgeMask ?? BuildEdgeMask(image, edgeMap, options);

            var chains = GradientEdgeMap.ChainEdgePixels(edgeMask, image.Width, image.Height);
            var bezier = options.Bezier;
            for (var i = 0; i < chains.Count; i++)
            {
                var fitted = BezierFitter.FitPolyline(chains[i], bezier.MaxError, bezier.CornerAngle, bezier.MinSegmentLength);
                var uv = RasterPathBuilder.PixelPointsToUv(fitted, image.Width, image.Height, null);
                var color = AverageColorAlongContour(image, chains[i]);
                RasterPathBuilder.AddPolyline(ctx, uv, color, 0.001f, "raster");
            }

            if (ctx.Svg.PathCount == 0)
            {
                ctx.Issues.Add(new RasterIssue(RasterIssueSeverity.Warning, "No vector contours detected.", "raster", 0, RasterIssueCode.InvalidGeometry));
            }

            return GradientEdgeMap.BuildEdgePreview(image, edgeMap, options, ctx.GpuBuffers);
        }

        private static bool[] BuildEdgeMask(RasterImageBuffer image, float[] edgeMap, RasterParsingOptions options)
        {
            var edgeMask = new bool[image.Pixels.Length];
            for (var y = 1; y < image.Height - 1; y++)
            {
                for (var x = 1; x < image.Width - 1; x++)
                {
                    var idx = image.Index(x, y);
                    if (image.Pixels[idx].a / 255f < options.MinAlpha)
                    {
                        continue;
                    }

                    edgeMask[idx] = GradientEdgeMap.Sample(image.Pixels, edgeMap, image.Width, image.Height, x, y, options.EdgeThreshold) >= options.EdgeThreshold;
                }
            }

            return edgeMask;
        }
    }
}
