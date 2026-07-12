using SDFX.Rasterizer;
using UnityEngine;

namespace SDFX.Rasterizer.Vectorizers
{
    internal sealed class ColorQuantMarchingSquaresVectorizer : RasterVectorizerBase
    {
        public override RasterVectorizationAlgorithm Algorithm => RasterVectorizationAlgorithm.ColorQuantMarchingSquares;

        protected override Color32[] Vectorize(RasterVectorizerContext ctx)
        {
            var image = ctx.Image;
            var options = ctx.RasterOptions;
            var palette = ctx.GpuBuffers?.QuantPalette ?? ColorQuantizer.BuildPalette(image, options.ColorQuant, options.MinAlpha);
            var labels = ColorQuantizer.AssignLabels(image, palette, options.MinAlpha, ctx.GpuBuffers?.QuantLabels);

            var preview = RasterQuantizedFillEmitter.Emit(
                ctx,
                labels,
                palette,
                options.ColorQuant.SimplifyTolerance,
                useBezierFit: false,
                bezierMaxError: 2f,
                minRegionArea: options.ColorQuant.MinRegionArea,
                overlayAlpha: options.ColorQuant.SoftOverlayAlpha);

            if (ctx.Svg.PathCount == 0)
            {
                ctx.Issues.Add(new RasterIssue(RasterIssueSeverity.Warning, "No vector contours detected.", "raster", 0, RasterIssueCode.InvalidGeometry));
            }

            return preview;
        }
    }
}
