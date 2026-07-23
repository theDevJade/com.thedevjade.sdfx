using System.Collections.Generic;
using NUnit.Framework;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Tests.Optimize
{
    public sealed class BooleanResolverTests
    {
        [Test]
        public void Resolve_OpaqueDifferentPaintRect_CullsCovered()
        {
            var under = Make(PrimitiveKind.Rectangle, new Vector2(0.2f, 0.2f), new Vector2(0.2f, 0.2f), Color.red);
            var cover = Make(PrimitiveKind.Rectangle, new Vector2(0.1f, 0.1f), new Vector2(0.5f, 0.5f), Color.blue);

            var output = BooleanResolver.Resolve(new[] { under, cover });
            Assert.AreEqual(1, output.Count);
            Assert.AreEqual(Color.blue, output[0].Color);
        }

        [Test]
        public void Resolve_TranslucentCover_KeepsUnder()
        {
            var under = Make(PrimitiveKind.Rectangle, new Vector2(0.2f, 0.2f), new Vector2(0.2f, 0.2f), Color.red);
            var cover = Make(PrimitiveKind.Rectangle, new Vector2(0.1f, 0.1f), new Vector2(0.5f, 0.5f), new Color(0f, 0f, 1f, 0.5f));

            var output = BooleanResolver.Resolve(new[] { under, cover });
            Assert.AreEqual(2, output.Count);
        }

        [Test]
        public void Resolve_SoftCoverTooSmallInset_KeepsUnder()
        {
            var under = Make(PrimitiveKind.Rectangle, new Vector2(0.2f, 0.2f), new Vector2(0.2f, 0.2f), Color.red);
            var cover = Make(PrimitiveKind.Rectangle, new Vector2(0.2f, 0.2f), new Vector2(0.2f, 0.2f), Color.blue);
            cover.Softness = 0.05f;

            var output = BooleanResolver.Resolve(new[] { under, cover });
            Assert.AreEqual(2, output.Count);
        }

        [Test]
        public void Resolve_CircleCover_CullsContainedRect()
        {
            var under = Make(PrimitiveKind.Rectangle, new Vector2(0.4f, 0.4f), new Vector2(0.1f, 0.1f), Color.red);
            var cover = Make(PrimitiveKind.Circle, new Vector2(0.25f, 0.25f), new Vector2(0.5f, 0.5f), Color.blue);

            var output = BooleanResolver.Resolve(new[] { under, cover });
            Assert.AreEqual(1, output.Count);
            Assert.AreEqual(PrimitiveKind.Circle, output[0].Type);
        }

        [Test]
        public void Resolve_CircleCover_KeepsCornerOutside()
        {
            var under = Make(PrimitiveKind.Rectangle, new Vector2(0.25f, 0.25f), new Vector2(0.5f, 0.5f), Color.red);
            var cover = Make(PrimitiveKind.Circle, new Vector2(0.25f, 0.25f), new Vector2(0.5f, 0.5f), Color.blue);

            var output = BooleanResolver.Resolve(new[] { under, cover });
            Assert.AreEqual(2, output.Count);
        }

        [Test]
        public void Resolve_RotatedCover_FailOpen()
        {
            var under = Make(PrimitiveKind.Rectangle, new Vector2(0.2f, 0.2f), new Vector2(0.2f, 0.2f), Color.red);
            var cover = Make(PrimitiveKind.Rectangle, new Vector2(0.1f, 0.1f), new Vector2(0.5f, 0.5f), Color.blue);
            cover.RotationDegrees = 15f;

            var output = BooleanResolver.Resolve(new[] { under, cover });
            Assert.AreEqual(2, output.Count);
        }

        [Test]
        public void Resolve_GradientWithTranslucentStop_NotACover()
        {
            var pathEdges = BuildGradientRun(opaque: false);
            var under = Make(PrimitiveKind.Rectangle, new Vector2(0.2f, 0.2f), new Vector2(0.2f, 0.2f), Color.red);
            var cover = Make(PrimitiveKind.Rectangle, new Vector2(0.1f, 0.1f), new Vector2(0.5f, 0.5f), Color.white);
            cover.GradientIndex = 1;

            var output = BooleanResolver.Resolve(new[] { under, cover }, pathEdges);
            Assert.AreEqual(2, output.Count);
        }

        [Test]
        public void Resolve_OpaqueGradient_CanCover()
        {
            var pathEdges = BuildGradientRun(opaque: true);
            var under = Make(PrimitiveKind.Rectangle, new Vector2(0.2f, 0.2f), new Vector2(0.2f, 0.2f), Color.red);
            var cover = Make(PrimitiveKind.Rectangle, new Vector2(0.1f, 0.1f), new Vector2(0.5f, 0.5f), Color.white);
            cover.GradientIndex = 1;

            var output = BooleanResolver.Resolve(new[] { under, cover }, pathEdges);
            Assert.AreEqual(1, output.Count);
            Assert.AreEqual(1, output[0].GradientIndex);
        }

        [Test]
        public void Resolve_RoundRectAndEllipse_CoverContained()
        {
            var under = Make(PrimitiveKind.Rectangle, new Vector2(0.4f, 0.4f), new Vector2(0.05f, 0.05f), Color.red);
            var round = Make(PrimitiveKind.RoundedRectangle, new Vector2(0.2f, 0.2f), new Vector2(0.6f, 0.6f), Color.blue);
            Assert.AreEqual(1, BooleanResolver.Resolve(new[] { under, round }).Count);

            var under2 = Make(PrimitiveKind.Rectangle, new Vector2(0.4f, 0.4f), new Vector2(0.05f, 0.05f), Color.red);
            var ellipse = Make(PrimitiveKind.Ellipse, new Vector2(0.2f, 0.2f), new Vector2(0.6f, 0.6f), Color.green);
            Assert.AreEqual(1, BooleanResolver.Resolve(new[] { under2, ellipse }).Count);
        }

        [Test]
        public void Resolve_TwoAbuttingRects_UnionCullsUnder()
        {
            var under = Make(PrimitiveKind.Rectangle, new Vector2(0.2f, 0.2f), new Vector2(0.4f, 0.2f), Color.red);
            var left = Make(PrimitiveKind.Rectangle, new Vector2(0.15f, 0.15f), new Vector2(0.25f, 0.3f), Color.blue);
            var right = Make(PrimitiveKind.Rectangle, new Vector2(0.4f, 0.15f), new Vector2(0.25f, 0.3f), Color.green);

            var output = BooleanResolver.Resolve(new[] { under, left, right });
            Assert.AreEqual(2, output.Count);
            Assert.IsFalse(output.Exists(p => p.Color == Color.red));
        }

        [Test]
        public void RectUnionCovers_PartialGap_ReturnsFalse()
        {
            var target = (min: new Vector2(0f, 0f), max: new Vector2(1f, 1f));
            var rects = new List<(Vector2 min, Vector2 max)>
            {
                (new Vector2(0f, 0f), new Vector2(0.4f, 1f)),
                (new Vector2(0.6f, 0f), new Vector2(1f, 1f))
            };
            Assert.IsFalse(BooleanResolver.RectUnionCovers(target, rects));
        }

        private static Primitive Make(PrimitiveKind type, Vector2 pos, Vector2 size, Color color)
        {
            return new Primitive
            {
                Type = type,
                Position = pos,
                Size = size,
                Color = color,
                Softness = 0f,
                Layer = 0,
                ParameterIndex = -1,
                ParameterCount = 0,
                GradientIndex = 0,
                RotationDegrees = 0f
            };
        }

        private static List<Vector4> BuildGradientRun(bool opaque)
        {
            var edges = new List<Vector4>
            {
                new Vector4(0f, 0f, 0f, 0f),
                new Vector4(1f, 0f, 0f, 0f),
                new Vector4(0f, 1f, 0f, 0f)
            };
            for (var i = 0; i < BooleanResolver.GradientRampSize; i++)
            {
                var a = opaque ? 1f : (i == 3 ? 0.5f : 1f);
                edges.Add(new Vector4(1f, 1f, 1f, a));
            }

            return edges;
        }
    }
}
