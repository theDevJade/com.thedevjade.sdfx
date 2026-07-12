using UnityEngine;

namespace SDFX.Rasterizer.Vectorizers
{
    internal sealed class HybridMultiResolutionLodVectorizer : RasterVectorizerBase
    {
        public override RasterVectorizationAlgorithm Algorithm => RasterVectorizationAlgorithm.HybridMultiResolutionLod;

        protected override Color32[] Vectorize(RasterVectorizerContext ctx)
        {
            var baseAlgorithm = ctx.RasterOptions.Lod.BaseAlgorithm;
            if (baseAlgorithm == RasterVectorizationAlgorithm.HybridMultiResolutionLod)
            {
                baseAlgorithm = RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf;
            }

            var vectorizer = RasterVectorizerRegistry.Resolve(baseAlgorithm);
            if (vectorizer is RasterVectorizerBase concrete)
            {
                return concrete.RunVectorize(ctx);
            }

            ctx.Issues.Add(new RasterIssue(
                RasterIssueSeverity.Warning,
                "No vector contours detected.",
                "raster",
                0,
                RasterIssueCode.InvalidGeometry));
            return ColorQuantizer.BuildLabelPreview(
                ctx.Image,
                new int[ctx.Image.Pixels.Length],
                new[] { new Color32(0, 0, 0, 0) });
        }
    }
}
