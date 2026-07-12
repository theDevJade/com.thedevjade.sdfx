using System.Collections.Generic;
using NUnit.Framework;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Tests.Optimize
{
    public sealed class SimplifierTests
    {
        [Test]
        public void Simplify_DropsDegenerateAndTransparent()
        {
            var input = new List<Primitive>
            {
                new Primitive
                {
                    Type = PrimitiveKind.Rectangle,
                    Position = Vector2.zero,
                    Size = new Vector2(1f, 1f),
                    Color = Color.red
                },
                new Primitive
                {
                    Type = PrimitiveKind.Rectangle,
                    Position = Vector2.one,
                    Size = new Vector2(0f, 1f),
                    Color = Color.green
                },
                new Primitive
                {
                    Type = PrimitiveKind.Circle,
                    Position = Vector2.zero,
                    Size = new Vector2(1f, 1f),
                    Color = new Color(1f, 1f, 1f, 0f)
                }
            };

            var output = Simplifier.Simplify(input);
            Assert.AreEqual(1, output.Count);
            Assert.AreEqual(Color.red, output[0].Color);
        }

        [Test]
        public void Simplify_MergesNearIdenticalPrimitives()
        {
            var settings = OptimizationSettings.FromProfile(OptimizationProfile.Pc);
            var a = new Primitive
            {
                Type = PrimitiveKind.Rectangle,
                Position = new Vector2(0.5f, 0.5f),
                Size = new Vector2(0.2f, 0.2f),
                Color = Color.white,
                Softness = 0.01f
            };
            var b = a;
            b.Position = a.Position + new Vector2(settings.MergeDistanceEpsilon * 0.5f, 0f);
            b.Size = a.Size + new Vector2(settings.MergeSizeEpsilon * 0.5f, 0f);

            var output = Simplifier.Simplify(new[] { a, b }, settings);
            Assert.AreEqual(1, output.Count);
        }
    }
}
