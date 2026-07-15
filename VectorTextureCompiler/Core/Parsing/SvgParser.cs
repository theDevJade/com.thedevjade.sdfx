using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Primitives;

namespace SDFX.VectorTextureCompiler.Core.Parsing
{
    public static class SvgParser
    {
        public static List<Primitive> Parse(string svgText)
            => Parse(svgText, new ParserOptions()).Primitives;

        public static ParseResult Parse(string svgText, ParserOptions options)
            => VectorGraphicsSvgAdapter.Parse(svgText, options);
    }
}
