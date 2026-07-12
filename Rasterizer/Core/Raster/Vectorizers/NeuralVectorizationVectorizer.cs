using System.Collections.Generic;
using SDFX.Rasterizer;
using SDFX.Rasterizer.Inference;
using UnityEngine;

namespace SDFX.Rasterizer.Vectorizers
{
    internal sealed class NeuralVectorizationVectorizer : RasterVectorizerBase
    {
        public override RasterVectorizationAlgorithm Algorithm => RasterVectorizationAlgorithm.NeuralVectorization;

        protected override Color32[] Vectorize(RasterVectorizerContext ctx)
        {
            var options = ctx.RasterOptions.Neural;
            if (!SentisInferenceService.TryRunVectorPrediction(
                    ctx.Image,
                    options.ModelAssetPath,
                    options.ConfidenceThreshold,
                    options.MaxCurves,
                    out var curves,
                    ctx.Issues))
            {
                if (ctx.RasterOptions.Strictness == RasterStrictness.Strict)
                {
                    return null;
                }
            }

            curves ??= new List<List<Vector2>>();
            for (var i = 0; i < curves.Count; i++)
            {
                var fitted = BezierFitter.FitPolyline(curves[i], ctx.RasterOptions.Bezier.MaxError, ctx.RasterOptions.Bezier.CornerAngle, ctx.RasterOptions.Bezier.MinSegmentLength);
                var uv = RasterPathBuilder.PixelPointsToUv(fitted, ctx.Image.Width, ctx.Image.Height, null);
                var color = AverageColorAlongContour(ctx.Image, curves[i]);
                RasterPathBuilder.AddPolyline(ctx, uv, color, 0.001f, "raster");
            }

            if (ctx.Svg.PathCount == 0)
            {
                ctx.Issues.Add(new RasterIssue(RasterIssueSeverity.Warning, "No vector contours detected.", "raster", 0, RasterIssueCode.InvalidGeometry));
            }

            var labels = ColorQuantizer.Quantize(ctx.Image, ctx.RasterOptions.ColorQuant, ctx.RasterOptions.MinAlpha, out var palette);
            return ColorQuantizer.BuildLabelPreview(ctx.Image, labels, palette);
        }
    }
}
