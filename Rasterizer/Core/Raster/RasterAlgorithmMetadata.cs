using SDFX.VectorTextureCompiler.Core.Localization;

namespace SDFX.Rasterizer
{
    public readonly struct RasterAlgorithmInfo
    {
        public RasterAlgorithmInfo(string name, string description, string bestFor)
        {
            Name = name;
            Description = description;
            BestFor = bestFor;
        }

        public string Name { get; }
        public string Description { get; }
        public string BestFor { get; }
    }

    public static class RasterAlgorithmMetadata
    {
        public static RasterAlgorithmInfo Get(RasterVectorizationAlgorithm algorithm)
        {
            var key = ToAlgorithmKey(algorithm);
            return new RasterAlgorithmInfo(
                SdfxLanguage.Rasterizer.AlgorithmName(key, GetFallbackName(algorithm)),
                SdfxLanguage.Rasterizer.AlgorithmDescription(key, GetFallbackDescription(algorithm)),
                SdfxLanguage.Rasterizer.AlgorithmBestFor(key, GetFallbackBestFor(algorithm)));
        }

        private static string ToAlgorithmKey(RasterVectorizationAlgorithm algorithm) => algorithm switch
        {
            RasterVectorizationAlgorithm.ColorQuantMarchingSquares => nameof(RasterVectorizationAlgorithm.ColorQuantMarchingSquares),
            RasterVectorizationAlgorithm.SuzukiAbeContours => nameof(RasterVectorizationAlgorithm.SuzukiAbeContours),
            RasterVectorizationAlgorithm.PotraceTracing => nameof(RasterVectorizationAlgorithm.PotraceTracing),
            RasterVectorizationAlgorithm.AdaptiveBezierFitting => nameof(RasterVectorizationAlgorithm.AdaptiveBezierFitting),
            RasterVectorizationAlgorithm.NeuralVectorization => nameof(RasterVectorizationAlgorithm.NeuralVectorization),
            RasterVectorizationAlgorithm.SuperpixelSegmentation => nameof(RasterVectorizationAlgorithm.SuperpixelSegmentation),
            RasterVectorizationAlgorithm.VoronoiDelaunay => nameof(RasterVectorizationAlgorithm.VoronoiDelaunay),
            RasterVectorizationAlgorithm.GradientEdgeVectorization => nameof(RasterVectorizationAlgorithm.GradientEdgeVectorization),
            RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf => nameof(RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf),
            RasterVectorizationAlgorithm.HybridNeuralClassical => nameof(RasterVectorizationAlgorithm.HybridNeuralClassical),
            RasterVectorizationAlgorithm.HybridMultiResolutionLod => nameof(RasterVectorizationAlgorithm.HybridMultiResolutionLod),
            _ => "Unknown"
        };

        private static string GetFallbackName(RasterVectorizationAlgorithm algorithm) => algorithm switch
        {
            RasterVectorizationAlgorithm.ColorQuantMarchingSquares => "Color Quant + Marching Squares",
            RasterVectorizationAlgorithm.SuzukiAbeContours => "Suzuki–Abe Contours",
            RasterVectorizationAlgorithm.PotraceTracing => "Potrace",
            RasterVectorizationAlgorithm.AdaptiveBezierFitting => "Adaptive Bezier",
            RasterVectorizationAlgorithm.NeuralVectorization => "Neural",
            RasterVectorizationAlgorithm.SuperpixelSegmentation => "Superpixels",
            RasterVectorizationAlgorithm.VoronoiDelaunay => "Voronoi",
            RasterVectorizationAlgorithm.GradientEdgeVectorization => "Gradient Edges",
            RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf => "Hybrid Contour",
            RasterVectorizationAlgorithm.HybridNeuralClassical => "Hybrid Neural",
            RasterVectorizationAlgorithm.HybridMultiResolutionLod => "Hybrid LOD",
            _ => "Unknown"
        };

        private static string GetFallbackDescription(RasterVectorizationAlgorithm algorithm) => algorithm switch
        {
            RasterVectorizationAlgorithm.ColorQuantMarchingSquares => "Quantize colors then trace region boundaries.",
            RasterVectorizationAlgorithm.SuzukiAbeContours => "Binary contour tracing with optional holes.",
            RasterVectorizationAlgorithm.PotraceTracing => "Polygon tracing for binary masks.",
            RasterVectorizationAlgorithm.AdaptiveBezierFitting => "Fit curves to edge chains.",
            RasterVectorizationAlgorithm.NeuralVectorization => "Sentis segmentation then path fit.",
            RasterVectorizationAlgorithm.SuperpixelSegmentation => "SLIC-like regions to polygons.",
            RasterVectorizationAlgorithm.VoronoiDelaunay => "Site-based mesh cells.",
            RasterVectorizationAlgorithm.GradientEdgeVectorization => "Edge map stamps or chains.",
            RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf => "Quantize + contours (SVG export).",
            RasterVectorizationAlgorithm.HybridNeuralClassical => "Neural regions + classical contours.",
            RasterVectorizationAlgorithm.HybridMultiResolutionLod => "Delegates to base algorithm.",
            _ => string.Empty
        };

        private static string GetFallbackBestFor(RasterVectorizationAlgorithm algorithm) => algorithm switch
        {
            RasterVectorizationAlgorithm.ColorQuantMarchingSquares => "Flat pixel-art atlases",
            RasterVectorizationAlgorithm.SuzukiAbeContours => "Silhouettes / alpha masks",
            RasterVectorizationAlgorithm.PotraceTracing => "Clean black/white art",
            RasterVectorizationAlgorithm.AdaptiveBezierFitting => "Smooth outlines",
            RasterVectorizationAlgorithm.NeuralVectorization => "Photos / soft edges",
            RasterVectorizationAlgorithm.SuperpixelSegmentation => "Painterly regions",
            RasterVectorizationAlgorithm.VoronoiDelaunay => "Stylized tessellation",
            RasterVectorizationAlgorithm.GradientEdgeVectorization => "Line art / strokes",
            RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf => "Character atlases",
            RasterVectorizationAlgorithm.HybridNeuralClassical => "Mixed photos/sprites",
            RasterVectorizationAlgorithm.HybridMultiResolutionLod => "Large textures",
            _ => string.Empty
        };
    }
}
