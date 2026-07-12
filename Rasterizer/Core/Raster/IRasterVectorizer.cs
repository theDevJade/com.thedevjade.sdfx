using UnityEngine;

namespace SDFX.Rasterizer
{
    internal interface IRasterVectorizer
    {
        RasterVectorizationAlgorithm Algorithm { get; }
        RasterVectorizerBase.RasterToSvgWork Build(Texture2D source, RasterParsingOptions rasterOptions);
    }
}
