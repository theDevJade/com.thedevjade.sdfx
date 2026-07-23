using NUnit.Framework;
using SDFX.VectorTextureCompiler.Core.Baking;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Tests.Baking
{
    public sealed class DataTextureBakerFormatTests
    {
        [Test]
        public void CanUseHalfIndices_SmallGrid_ReturnsTrue()
        {
            var prims = new System.Collections.Generic.List<Primitive>
            {
                new Primitive
                {
                    Type = PrimitiveKind.Rectangle,
                    Position = new Vector2(0.1f, 0.1f),
                    Size = new Vector2(0.2f, 0.2f),
                    Color = Color.white
                }
            };
            var grid = SpatialGridBuilder.Build(prims, 8, 8, 8);
            Assert.IsTrue(DataTextureBaker.CanUseHalfIndices(grid));

            var lookup = DataTextureBaker.BakeGridLookupTexture(grid, useHalf: true);
            var index = DataTextureBaker.BakeGridIndexTexture(grid, 256, useHalf: true);
            Assert.AreEqual(TextureFormat.RGHalf, lookup.format);
            Assert.AreEqual(TextureFormat.RHalf, index.format);
            Object.DestroyImmediate(lookup);
            Object.DestroyImmediate(index);
        }

        [Test]
        public void BakePrimitiveTexture_UsesRgbaHalf()
        {
            var prims = new[]
            {
                new Primitive
                {
                    Type = PrimitiveKind.Circle,
                    Position = Vector2.zero,
                    Size = Vector2.one,
                    Color = Color.white,
                    ParameterCount = -1,
                    ParameterIndex = 3
                }
            };
            var tex = DataTextureBaker.BakePrimitiveTexture(prims);
            Assert.AreEqual(TextureFormat.RGBAHalf, tex.format);
            var t3 = tex.GetPixels()[3];
            Assert.AreEqual(3f, t3.r, 0.01f);
            Assert.AreEqual(-1f, t3.g, 0.01f);
            Object.DestroyImmediate(tex);
        }
    }
}
