using System.Collections.Generic;

namespace SDFX.Rasterizer
{
    internal static class RasterVectorizerRegistry
    {
        private static readonly List<IRasterVectorizer> VectorizerList = new List<IRasterVectorizer>();

        private static readonly Dictionary<RasterVectorizationAlgorithm, IRasterVectorizer> Vectorizers = Build();

        public static IRasterVectorizer Resolve(RasterVectorizationAlgorithm algorithm)
        {
            if (Vectorizers.TryGetValue(algorithm, out var vectorizer))
            {
                return vectorizer;
            }

            return Vectorizers[RasterVectorizationAlgorithm.GradientEdgeVectorization];
        }

        public static IReadOnlyList<IRasterVectorizer> All => VectorizerList;

        private static Dictionary<RasterVectorizationAlgorithm, IRasterVectorizer> Build()
        {
            var list = new IRasterVectorizer[]
            {
                new Vectorizers.ColorQuantMarchingSquaresVectorizer(),
                new Vectorizers.SuzukiAbeContoursVectorizer(),
                new Vectorizers.PotraceTracingVectorizer(),
                new Vectorizers.AdaptiveBezierFittingVectorizer(),
                new Vectorizers.NeuralVectorizationVectorizer(),
                new Vectorizers.SuperpixelSegmentationVectorizer(),
                new Vectorizers.VoronoiDelaunayVectorizer(),
                new Vectorizers.GradientEdgeVectorizer(),
                new Vectorizers.HybridSegmentContourBezierSdfVectorizer(),
                new Vectorizers.HybridNeuralClassicalVectorizer(),
                new Vectorizers.HybridMultiResolutionLodVectorizer()
            };

            var map = new Dictionary<RasterVectorizationAlgorithm, IRasterVectorizer>();
            VectorizerList.Clear();
            for (var i = 0; i < list.Length; i++)
            {
                map[list[i].Algorithm] = list[i];
                VectorizerList.Add(list[i]);
            }

            return map;
        }
    }
}
