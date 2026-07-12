using System.Collections.Generic;
using UnityEngine;

namespace SDFX.Rasterizer
{
    internal abstract class RasterVectorizerBase : IRasterVectorizer
    {
        public abstract RasterVectorizationAlgorithm Algorithm { get; }

        public RasterToSvgWork Build(Texture2D source, RasterParsingOptions rasterOptions)
        {
            rasterOptions ??= new RasterParsingOptions();
            var issues = new List<RasterIssue>();

            if (source == null)
            {
                issues.Add(new RasterIssue(RasterIssueSeverity.Error, "Raster input is missing.", "raster", 0, RasterIssueCode.InvalidInput));
                return new RasterToSvgWork(string.Empty, null, issues, 0);
            }

            var image = RasterImageIO.Load(source);
            if (image == null)
            {
                issues.Add(new RasterIssue(RasterIssueSeverity.Error, "Failed to read raster pixels.", "raster", 0, RasterIssueCode.InvalidInput));
                return new RasterToSvgWork(string.Empty, null, issues, 0);
            }

            try
            {
                RasterGpuBuffers gpuBuffers = null;
                if (rasterOptions.UseComputeAcceleration)
                {
                    gpuBuffers = RasterGpuPreprocessor.PreprocessSync(Algorithm, image, rasterOptions, issues);
                }

                var svg = new RasterSvgDocument(image.Width, image.Height);
                var ctx = new RasterVectorizerContext(rasterOptions, image, svg, issues, gpuBuffers);
                var previewPixels = Vectorize(ctx);
                var preview = previewPixels != null
                    ? RasterImageIO.CreatePreviewTexture(image.Width, image.Height, previewPixels, "SDFX_RasterPreview")
                    : null;
                return new RasterToSvgWork(svg.ToSvgString(), preview, issues, ctx.PathCount);
            }
            finally
            {
                image.DisposeReadableCopy();
            }
        }

        internal VectorizeWorkResult VectorizeWork(
            RasterImageBuffer image,
            RasterParsingOptions rasterOptions,
            RasterGpuBuffers gpuBuffers)
        {
            rasterOptions ??= new RasterParsingOptions();
            var issues = new List<RasterIssue>();
            var svg = new RasterSvgDocument(image.Width, image.Height);
            var ctx = new RasterVectorizerContext(rasterOptions, image, svg, issues, gpuBuffers);
            var previewPixels = Vectorize(ctx);
            return new VectorizeWorkResult(svg.ToSvgString(), previewPixels, image.Width, image.Height, issues, ctx.PathCount);
        }

        internal readonly struct VectorizeWorkResult
        {
            public VectorizeWorkResult(string svgText, Color32[] previewPixels, int width, int height, List<RasterIssue> issues, int pathCount)
            {
                SvgText = svgText;
                PreviewPixels = previewPixels;
                Width = width;
                Height = height;
                Issues = issues;
                PathCount = pathCount;
            }

            public string SvgText { get; }
            public Color32[] PreviewPixels { get; }
            public int Width { get; }
            public int Height { get; }
            public List<RasterIssue> Issues { get; }
            public int PathCount { get; }
        }

        internal readonly struct RasterToSvgWork
        {
            public RasterToSvgWork(string svgText, Texture2D overlay, List<RasterIssue> issues, int pathCount)
            {
                SvgText = svgText;
                OverlayPreview = overlay;
                Issues = issues;
                PathCount = pathCount;
            }

            public string SvgText { get; }
            public Texture2D OverlayPreview { get; }
            public List<RasterIssue> Issues { get; }
            public int PathCount { get; }
        }

        protected abstract Color32[] Vectorize(RasterVectorizerContext ctx);

        internal Color32[] RunVectorize(RasterVectorizerContext ctx) => Vectorize(ctx);

        protected static Color AverageColorAlongContour(RasterImageBuffer image, IReadOnlyList<Vector2> contour)
        {
            if (contour.Count == 0)
            {
                return Color.clear;
            }

            long r = 0, g = 0, b = 0, a = 0;
            for (var i = 0; i < contour.Count; i++)
            {
                var x = Mathf.Clamp(Mathf.RoundToInt(contour[i].x), 0, image.Width - 1);
                var y = Mathf.Clamp(Mathf.RoundToInt(contour[i].y), 0, image.Height - 1);
                var c = image.GetPixel(x, y);
                r += c.r; g += c.g; b += c.b; a += c.a;
            }

            var count = contour.Count;
            return new Color(r / (255f * count), g / (255f * count), b / (255f * count), a / (255f * count));
        }
    }
}
