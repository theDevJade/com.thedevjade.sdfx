using System.Collections.Generic;
using SDFX.Rasterizer;
using UnityEngine;

namespace SDFX.Rasterizer.Vectorizers
{
    internal sealed class GradientEdgeVectorizer : RasterVectorizerBase
    {
        public override RasterVectorizationAlgorithm Algorithm => RasterVectorizationAlgorithm.GradientEdgeVectorization;

        protected override Color32[] Vectorize(RasterVectorizerContext ctx)
        {
            var image = ctx.Image;
            var options = ctx.RasterOptions;
            var edgeMap = GradientEdgeMap.Compute(image, options, ctx.Issues, ctx.GpuBuffers);
            var stride = Mathf.Max(1, options.SampleStride);
            var edgeMask = ctx.GpuBuffers?.EdgeMask;
            if (edgeMask == null)
            {
                edgeMask = new bool[image.Pixels.Length];
                for (var y = 1; y < image.Height - 1; y++)
                {
                    for (var x = 1; x < image.Width - 1; x++)
                    {
                        var idx = image.Index(x, y);
                        var c = image.Pixels[idx];
                        if (c.a / 255f < options.MinAlpha)
                        {
                            continue;
                        }

                        var edge = GradientEdgeMap.Sample(image.Pixels, edgeMap, image.Width, image.Height, x, y, options.EdgeThreshold);
                        edgeMask[idx] = edge >= options.EdgeThreshold;
                    }
                }
            }

            switch (options.Gradient.OutputMode)
            {
                case RasterGradientOutputMode.Chain:
                    EmitChains(ctx, edgeMask);
                    break;
                case RasterGradientOutputMode.Bezier:
                    EmitBezierChains(ctx, edgeMask);
                    break;
                default:
                    EmitStamps(ctx, edgeMap, stride);
                    break;
            }

            if (ctx.Svg.PathCount == 0)
            {
                ctx.Issues.Add(new RasterIssue(RasterIssueSeverity.Warning, "No vector contours detected.", "raster", 0, RasterIssueCode.InvalidGeometry));
            }

            return GradientEdgeMap.BuildEdgePreview(image, edgeMap, options, ctx.GpuBuffers);
        }

        private static void EmitStamps(RasterVectorizerContext ctx, float[] edgeMap, int stride)
        {
            var image = ctx.Image;
            var options = ctx.RasterOptions;
            for (var y = 1; y < image.Height - 1; y += stride)
            {
                for (var x = 1; x < image.Width - 1; x += stride)
                {
                    var idx = image.Index(x, y);
                    var c = image.Pixels[idx];
                    if (c.a / 255f < options.MinAlpha)
                    {
                        continue;
                    }

                    var edge = GradientEdgeMap.Sample(image.Pixels, edgeMap, image.Width, image.Height, x, y, options.EdgeThreshold);
                    if (edge < options.EdgeThreshold)
                    {
                        continue;
                    }

                    RasterPathBuilder.AddRectangle(ctx, x, y, stride, stride, c, "raster");
                }
            }
        }

        private static void EmitChains(RasterVectorizerContext ctx, bool[] edgeMask)
        {
            var chains = GradientEdgeMap.ChainEdgePixels(edgeMask, ctx.Image.Width, ctx.Image.Height);
            for (var i = 0; i < chains.Count; i++)
            {
                var uv = RasterPathBuilder.PixelPointsToUv(chains[i], ctx.Image.Width, ctx.Image.Height, null);
                var color = AverageColorAlongContour(ctx.Image, chains[i]);
                RasterPathBuilder.AddPolyline(ctx, uv, color, 0.001f, "raster");
            }
        }

        private static void EmitBezierChains(RasterVectorizerContext ctx, bool[] edgeMask)
        {
            var chains = GradientEdgeMap.ChainEdgePixels(edgeMask, ctx.Image.Width, ctx.Image.Height);
            var bezier = ctx.RasterOptions.Bezier;
            for (var i = 0; i < chains.Count; i++)
            {
                var fitted = BezierFitter.FitPolyline(chains[i], bezier.MaxError, bezier.CornerAngle, bezier.MinSegmentLength);
                var uv = RasterPathBuilder.PixelPointsToUv(fitted, ctx.Image.Width, ctx.Image.Height, null);
                var color = AverageColorAlongContour(ctx.Image, chains[i]);
                RasterPathBuilder.AddPolyline(ctx, uv, color, 0.001f, "raster");
            }
        }
    }
}
