using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Core.Optimize
{
    public static class DecalCompositor
    {
        public sealed class DecalLayer
        {
            public Texture2D Albedo;
            public Vector2 UvOffset;
            public Vector2 UvScale = Vector2.one;
            public float BlendStrength = 1f;
        }

        public static List<Primitive> ApplyDecals(IReadOnlyList<Primitive> primitives, IReadOnlyList<DecalLayer> decals)
        {
            if (decals == null || decals.Count == 0)
            {
                return new List<Primitive>(primitives);
            }

            var output = new List<Primitive>(primitives);
            foreach (var decal in decals)
            {
                if (decal.Albedo == null)
                {
                    continue;
                }

                var color = SampleCenterColor(decal.Albedo);
                output.Add(new Primitive
                {
                    Type = PrimitiveKind.Rectangle,
                    Position = decal.UvOffset,
                    Size = decal.UvScale,
                    Color = Color.Lerp(Color.white, color, decal.BlendStrength),
                    Layer = 200
                });
            }

            return output;
        }

        private static Color SampleCenterColor(Texture2D texture)
        {
            if (!texture.isReadable)
            {
                return Color.white;
            }

            var cx = texture.width / 2;
            var cy = texture.height / 2;
            return texture.GetPixel(cx, cy);
        }
    }
}
