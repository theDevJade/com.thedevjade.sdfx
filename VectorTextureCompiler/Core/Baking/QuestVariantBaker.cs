using System;
using System.Linq;
using SDFX.VectorTextureCompiler.Core.Primitives;

namespace SDFX.VectorTextureCompiler.Core.Baking
{
    public static class QuestVariantBaker
    {
        public const int DefaultMaxPrimitives = 2048;

        public static Primitive[] BuildSimplifiedPrimitives(Primitive[] primitives, int maxPrimitives)
        {
            if (primitives == null)
            {
                return new Primitive[0];
            }

            if (primitives.Length <= maxPrimitives)
            {
                return primitives;
            }

            var keptOriginalIndices = Enumerable.Range(0, primitives.Length)
                .OrderByDescending(i => Math.Abs(primitives[i].Size.x * primitives[i].Size.y))
                .Take(maxPrimitives)
                .OrderBy(i => i)
                .ToArray();

            var output = new Primitive[keptOriginalIndices.Length];
            for (var i = 0; i < keptOriginalIndices.Length; i++)
            {
                output[i] = primitives[keptOriginalIndices[i]];
            }

            return output;
        }
    }
}
