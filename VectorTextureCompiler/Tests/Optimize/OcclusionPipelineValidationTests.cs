using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Tests.Optimize
{
    public sealed class OcclusionPipelineValidationTests
    {
        [Test]
        public void OffCanvasBleed_DoesNotEvictOnCanvasUnderMaxPerCell()
        {
            var onCanvasSmall = new Primitive
            {
                Type = PrimitiveKind.Rectangle,
                Position = new Vector2(0.01f, 0.01f),
                Size = new Vector2(0.04f, 0.04f),
                Color = Color.red
            };
            var bleed = new Primitive
            {
                Type = PrimitiveKind.Rectangle,
                Position = new Vector2(-8f, -8f),
                Size = new Vector2(4f, 4f),
                Color = Color.gray
            };
            var badge = new Primitive
            {
                Type = PrimitiveKind.Rectangle,
                Position = new Vector2(0.3f, 0.3f),
                Size = new Vector2(0.4f, 0.4f),
                Color = Color.blue
            };

            var afterBoolean = BooleanResolver.Resolve(new[] { onCanvasSmall, bleed, badge });
            var afterCanvas = CanvasDomainCuller.Cull(afterBoolean.ToArray());
            Assert.IsFalse(afterCanvas.Any(p => p.Color == Color.gray));

            var grid = SpatialGridBuilder.Build(afterCanvas, 8, 8, maxPerCell: 2);
            Assert.AreEqual(0, grid.DroppedPrimitiveReferences);
            Assert.IsTrue(grid.PrimitiveIndices.All(i => i >= 0 && i < afterCanvas.Length));
        }

        [Test]
        public void OpaqueBadge_CullsFullyHiddenContent_KeepsPartial()
        {
            var hidden = new Primitive
            {
                Type = PrimitiveKind.Rectangle,
                Position = new Vector2(0.35f, 0.35f),
                Size = new Vector2(0.1f, 0.1f),
                Color = Color.red
            };
            var peeking = new Primitive
            {
                Type = PrimitiveKind.Rectangle,
                Position = new Vector2(0.55f, 0.55f),
                Size = new Vector2(0.2f, 0.2f),
                Color = Color.green
            };
            var badge = new Primitive
            {
                Type = PrimitiveKind.Rectangle,
                Position = new Vector2(0.3f, 0.3f),
                Size = new Vector2(0.3f, 0.3f),
                Color = Color.blue
            };

            var output = BooleanResolver.Resolve(new[] { hidden, peeking, badge });
            Assert.IsFalse(output.Exists(p => p.Color == Color.red));
            Assert.IsTrue(output.Exists(p => p.Color == Color.green));
            Assert.IsTrue(output.Exists(p => p.Color == Color.blue));
        }
    }
}
