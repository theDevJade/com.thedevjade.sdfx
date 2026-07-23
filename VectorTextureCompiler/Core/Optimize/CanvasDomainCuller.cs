using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Primitives;

namespace SDFX.VectorTextureCompiler.Core.Optimize
{
    public static class CanvasDomainCuller
    {
        public static List<Primitive> Cull(IReadOnlyList<Primitive> input)
        {
            if (input == null || input.Count == 0)
            {
                return new List<Primitive>();
            }

            var output = new List<Primitive>(input.Count);
            for (var i = 0; i < input.Count; i++)
            {
                if (PrimitiveBounds.IntersectsUnitCanvas(input[i]))
                {
                    output.Add(input[i]);
                }
            }

            return output;
        }

        public static Primitive[] Cull(Primitive[] input)
        {
            if (input == null || input.Length == 0)
            {
                return input ?? System.Array.Empty<Primitive>();
            }

            var kept = 0;
            for (var i = 0; i < input.Length; i++)
            {
                if (PrimitiveBounds.IntersectsUnitCanvas(input[i]))
                {
                    kept++;
                }
            }

            if (kept == input.Length)
            {
                return input;
            }

            var output = new Primitive[kept];
            var write = 0;
            for (var i = 0; i < input.Length; i++)
            {
                if (PrimitiveBounds.IntersectsUnitCanvas(input[i]))
                {
                    output[write++] = input[i];
                }
            }

            return output;
        }
    }
}
