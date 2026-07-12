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
            return algorithm switch
            {
                RasterVectorizationAlgorithm.ColorQuantMarchingSquares => new RasterAlgorithmInfo(
                    "Color Quant + Marching Squares",
                    "Quantize colors then trace region boundaries.",
                    "Flat pixel-art atlases"),
                RasterVectorizationAlgorithm.SuzukiAbeContours => new RasterAlgorithmInfo(
                    "Suzuki–Abe Contours",
                    "Binary contour tracing with optional holes.",
                    "Silhouettes / alpha masks"),
                RasterVectorizationAlgorithm.PotraceTracing => new RasterAlgorithmInfo(
                    "Potrace",
                    "Polygon tracing for binary masks.",
                    "Clean black/white art"),
                RasterVectorizationAlgorithm.AdaptiveBezierFitting => new RasterAlgorithmInfo(
                    "Adaptive Bezier",
                    "Fit curves to edge chains.",
                    "Smooth outlines"),
                RasterVectorizationAlgorithm.NeuralVectorization => new RasterAlgorithmInfo(
                    "Neural",
                    "Sentis segmentation then path fit.",
                    "Photos / soft edges"),
                RasterVectorizationAlgorithm.SuperpixelSegmentation => new RasterAlgorithmInfo(
                    "Superpixels",
                    "SLIC-like regions to polygons.",
                    "Painterly regions"),
                RasterVectorizationAlgorithm.VoronoiDelaunay => new RasterAlgorithmInfo(
                    "Voronoi",
                    "Site-based mesh cells.",
                    "Stylized tessellation"),
                RasterVectorizationAlgorithm.GradientEdgeVectorization => new RasterAlgorithmInfo(
                    "Gradient Edges",
                    "Edge map stamps or chains.",
                    "Line art / strokes"),
                RasterVectorizationAlgorithm.HybridSegmentContourBezierSdf => new RasterAlgorithmInfo(
                    "Hybrid Contour",
                    "Quantize + contours (SVG export).",
                    "Character atlases"),
                RasterVectorizationAlgorithm.HybridNeuralClassical => new RasterAlgorithmInfo(
                    "Hybrid Neural",
                    "Neural regions + classical contours.",
                    "Mixed photos/sprites"),
                RasterVectorizationAlgorithm.HybridMultiResolutionLod => new RasterAlgorithmInfo(
                    "Hybrid LOD",
                    "Delegates to base algorithm.",
                    "Large textures"),
                _ => new RasterAlgorithmInfo("Unknown", string.Empty, string.Empty)
            };
        }
    }
}
