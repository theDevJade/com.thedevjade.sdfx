using System.Collections.Generic;
using NUnit.Framework;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Tests.Optimize
{
    public sealed class OcclusionPathClipperTests
    {
        [Test]
        public void Apply_OpaqueRectOverPath_ClipsOrDropsFullyCovered()
        {
            var edges = new List<Vector4>
            {
                new Vector4(0.1f, 0.1f, 0.9f, 0.1f),
                new Vector4(0.9f, 0.1f, 0.9f, 0.9f),
                new Vector4(0.9f, 0.9f, 0.1f, 0.9f),
                new Vector4(0.1f, 0.9f, 0.1f, 0.1f)
            };
            var path = new Primitive
            {
                Type = PrimitiveKind.Polygon,
                Position = new Vector2(0.1f, 0.1f),
                Size = new Vector2(0.8f, 0.8f),
                Color = Color.red,
                ParameterIndex = 0,
                ParameterCount = 4,
                Layer = 0
            };
            var cover = new Primitive
            {
                Type = PrimitiveKind.Rectangle,
                Position = new Vector2(0.05f, 0.05f),
                Size = new Vector2(0.9f, 0.9f),
                Color = Color.blue,
                ParameterCount = 0,
                Layer = 0
            };

            var result = OcclusionPathClipper.Apply(new[] { path, cover }, edges);
            Assert.AreEqual(1, result.Primitives.Count);
            Assert.AreEqual(PrimitiveKind.Rectangle, result.Primitives[0].Type);
            Assert.AreEqual(1, result.ClippedCount);
        }

        [Test]
        public void Apply_PartialCover_EmitsRemainderPolygon()
        {
            var edges = new List<Vector4>
            {
                new Vector4(0.0f, 0.0f, 1.0f, 0.0f),
                new Vector4(1.0f, 0.0f, 1.0f, 1.0f),
                new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                new Vector4(0.0f, 1.0f, 0.0f, 0.0f)
            };
            var path = new Primitive
            {
                Type = PrimitiveKind.Polygon,
                Position = Vector2.zero,
                Size = Vector2.one,
                Color = Color.red,
                ParameterIndex = 0,
                ParameterCount = 4,
                Layer = 0
            };
            var cover = new Primitive
            {
                Type = PrimitiveKind.Rectangle,
                Position = new Vector2(0f, 0f),
                Size = new Vector2(0.5f, 1f),
                Color = Color.blue,
                ParameterCount = 0,
                Layer = 0
            };

            var result = OcclusionPathClipper.Apply(new[] { path, cover }, edges);
            Assert.GreaterOrEqual(result.Primitives.Count, 2); // cover + remainder(s)
            Assert.IsTrue(result.Primitives.Exists(p => p.Type == PrimitiveKind.Polygon && p.ParameterCount >= 3));
            Assert.AreEqual(1, result.ClippedCount);
            Assert.GreaterOrEqual(result.EmittedPolygons, 1);
        }

        [Test]
        public void Apply_NoOpaqueCover_KeepsOriginal()
        {
            var edges = new List<Vector4>
            {
                new Vector4(0.2f, 0.2f, 0.8f, 0.2f),
                new Vector4(0.8f, 0.2f, 0.8f, 0.8f),
                new Vector4(0.8f, 0.8f, 0.2f, 0.8f),
                new Vector4(0.2f, 0.8f, 0.2f, 0.2f)
            };
            var path = new Primitive
            {
                Type = PrimitiveKind.Polygon,
                Position = new Vector2(0.2f, 0.2f),
                Size = new Vector2(0.6f, 0.6f),
                Color = Color.red,
                ParameterIndex = 0,
                ParameterCount = 4
            };

            var result = OcclusionPathClipper.Apply(new[] { path }, edges);
            Assert.AreEqual(1, result.Primitives.Count);
            Assert.AreEqual(0, result.ClippedCount);
            Assert.AreEqual(4, result.PathEdges.Count);
        }
    }
}
