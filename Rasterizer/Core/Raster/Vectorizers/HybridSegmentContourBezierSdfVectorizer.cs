using SDFX.Rasterizer;
using UnityEngine;

namespace SDFX.Rasterizer.Vectorizers
{
    internal sealed class HybridSegmentContourBezierSdfVectorizer : RasterVectorizerBase
    {
        public override RasterVectorizationAlgorithm Algorithm => RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf;

        protected override Color32[] Vectorize(RasterVectorizerContext ctx)
        {
            var image = ctx.Image;
            var options = ctx.RasterOptions;
            var palette = ctx.GpuBuffers?.QuantPalette ?? ColorQuantizer.BuildPalette(image, options.ColorQuant, options.MinAlpha);
            var labels = ColorQuantizer.AssignLabels(image, palette, options.MinAlpha, ctx.GpuBuffers?.QuantLabels);

            // Preview must show the same polygons written to SVG — not the palette label map.
            var preview = RasterQuantizedFillEmitter.Emit(
                ctx,
                labels,
                palette,
                options.Hybrid.SimplifyTolerance,
                useBezierFit: false,
                bezierMaxError: options.Hybrid.BezierMaxError,
                minRegionArea: options.Hybrid.MinRegionArea,
                overlayAlpha: options.Hybrid.SoftOverlayAlpha);

            if (ctx.Svg.PathCount == 0)
            {
                ctx.Issues.Add(new RasterIssue(RasterIssueSeverity.Warning, "No vector contours detected.", "raster", 0, RasterIssueCode.InvalidGeometry));
            }

            return preview;
        }
    }
}
