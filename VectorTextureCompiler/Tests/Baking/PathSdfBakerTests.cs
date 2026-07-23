using NUnit.Framework;
using SDFX.VectorTextureCompiler.Core.Baking;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Tests.Baking
{
    public sealed class PathSdfBakerTests
    {
        [Test]
        public void JumpFlood_SquareMask_InteriorNegativeExteriorPositive()
        {
            const int res = 64;
            var mask = new bool[res * res];
            for (var y = 0; y < res; y++)
            {
                for (var x = 0; x < res; x++)
                {
                    mask[y * res + x] = x >= 16 && x < 48 && y >= 16 && y < 48;
                }
            }

            var pixelSize = new Vector2(1f / res, 1f / res);
            var sdf = PathSdfBaker.JumpFloodSignedDistanceCpu(mask, res, pixelSize);

            Assert.Less(sdf[32 * res + 32], -0.05f);
            Assert.Greater(sdf[2 * res + 2], 0.05f);
            var outsideLeft = sdf[32 * res + 8];
            Assert.Greater(outsideLeft, 0f);
            Assert.Less(Mathf.Abs(outsideLeft - (16 - 8.5f) * pixelSize.x), 0.05f);
        }

        [Test]
        public void Bake_HighEdgePolygon_MarksPathCountNegative()
        {
            var edges = new System.Collections.Generic.List<Vector4>();
            const int edgeCount = 64;
            for (var i = 0; i < edgeCount; i++)
            {
                var a = (i / (float)edgeCount) * Mathf.PI * 2f;
                var b = ((i + 1) / (float)edgeCount) * Mathf.PI * 2f;
                edges.Add(new Vector4(
                    0.5f + Mathf.Cos(a) * 0.3f,
                    0.5f + Mathf.Sin(a) * 0.3f,
                    0.5f + Mathf.Cos(b) * 0.3f,
                    0.5f + Mathf.Sin(b) * 0.3f));
            }

            var primitives = new[]
            {
                new Primitive
                {
                    Type = PrimitiveKind.Polygon,
                    Position = new Vector2(0.2f, 0.2f),
                    Size = new Vector2(0.6f, 0.6f),
                    Color = Color.white,
                    ParameterIndex = 0,
                    ParameterCount = edgeCount,
                    Softness = 0f
                },
                new Primitive
                {
                    Type = PrimitiveKind.Circle,
                    Position = new Vector2(0.1f, 0.1f),
                    Size = new Vector2(0.2f, 0.2f),
                    Color = Color.red,
                    Softness = 0f
                }
            };

            var result = PathSdfBaker.Bake(primitives, edges);
            Assert.AreEqual(1, result.BakedCount);
            Assert.NotNull(result.Atlas);
            Assert.NotNull(result.Meta);
            Assert.AreEqual(-1, primitives[0].ParameterCount);
            Assert.AreEqual(0, primitives[0].ParameterIndex);
            Assert.AreEqual(PrimitiveKind.Circle, primitives[1].Type);
            Assert.GreaterOrEqual(primitives[1].ParameterCount, 0);

            Object.DestroyImmediate(result.Atlas);
            Object.DestroyImmediate(result.Meta);
        }

        [Test]
        public void Bake_LowEdgePolygon_StaysAnalytic()
        {
            var edges = new System.Collections.Generic.List<Vector4>
            {
                new Vector4(0.2f, 0.2f, 0.8f, 0.2f),
                new Vector4(0.8f, 0.2f, 0.8f, 0.8f),
                new Vector4(0.8f, 0.8f, 0.2f, 0.8f),
                new Vector4(0.2f, 0.8f, 0.2f, 0.2f)
            };

            var primitives = new[]
            {
                new Primitive
                {
                    Type = PrimitiveKind.Polygon,
                    Position = new Vector2(0.2f, 0.2f),
                    Size = new Vector2(0.6f, 0.6f),
                    Color = Color.white,
                    ParameterIndex = 0,
                    ParameterCount = 4
                }
            };

            var result = PathSdfBaker.Bake(primitives, edges);
            Assert.AreEqual(0, result.BakedCount);
            Assert.AreEqual(4, primitives[0].ParameterCount);
        }
    }
}
