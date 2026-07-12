using SDFX.Rasterizer;
using UnityEngine;

namespace SDFX.Rasterizer.Vectorizers
{
    internal sealed class VoronoiDelaunayVectorizer : RasterVectorizerBase
    {
        public override RasterVectorizationAlgorithm Algorithm => RasterVectorizationAlgorithm.VoronoiDelaunay;

        protected override Color32[] Vectorize(RasterVectorizerContext ctx)
        {
            var image = ctx.Image;
            var options = ctx.RasterOptions;
            var edgeMap = GradientEdgeMap.Compute(image, options, ctx.Issues, ctx.GpuBuffers);
            var cells = VoronoiMeshBuilder.BuildCells(image, edgeMap, options.Voronoi, options.MinAlpha, options.EdgeThreshold, ctx.GpuBuffers?.VoronoiNearest);
            for (var i = 0; i < cells.Count; i++)
            {
                var uv = RasterPathBuilder.PixelPointsToUv(cells[i], image.Width, image.Height, null);
                var color = AverageColorAlongContour(image, cells[i]);
                RasterPathBuilder.AddPolygon(ctx, uv, color, "raster");
            }

            if (ctx.Svg.PathCount == 0)
            {
                ctx.Issues.Add(new RasterIssue(RasterIssueSeverity.Warning, "No vector contours detected.", "raster", 0, RasterIssueCode.InvalidGeometry));
            }

            return GradientEdgeMap.BuildEdgePreview(image, edgeMap, options, ctx.GpuBuffers);
        }
    }
}
