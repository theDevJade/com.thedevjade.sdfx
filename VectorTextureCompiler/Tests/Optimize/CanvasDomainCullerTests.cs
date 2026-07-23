using System.Linq;
using NUnit.Framework;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Tests.Optimize
{
    public sealed class CanvasDomainCullerTests
    {
        [Test]
        public void Cull_DropsFullyOffCanvas_KeepsPartialAndInside()
        {
            var inside = MakeRect(new Vector2(0.1f, 0.1f), new Vector2(0.2f, 0.2f), Color.red);
            var partial = MakeRect(new Vector2(0.8f, 0.8f), new Vector2(0.4f, 0.4f), Color.green);
            var offLeft = MakeRect(new Vector2(-2f, 0f), new Vector2(1f, 1f), Color.blue);
            var offRight = MakeRect(new Vector2(1.5f, 0f), new Vector2(2f, 2f), Color.yellow);
            var touching = MakeRect(new Vector2(-0.5f, 0.25f), new Vector2(0.5f, 0.5f), Color.cyan);

            var output = CanvasDomainCuller.Cull(new[] { inside, partial, offLeft, offRight, touching });
            Assert.AreEqual(3, output.Length);
            Assert.AreEqual(Color.red, output[0].Color);
            Assert.AreEqual(Color.green, output[1].Color);
            Assert.AreEqual(Color.cyan, output[2].Color);
        }

        [Test]
        public void SpatialGrid_OffCanvasGiantDoesNotEnterBorderCells()
        {
            var onCanvas = MakeRect(new Vector2(0.01f, 0.01f), new Vector2(0.05f, 0.05f), Color.red);
            var offCanvasGiant = MakeRect(new Vector2(-10f, -10f), new Vector2(5f, 5f), Color.blue);

            var culled = CanvasDomainCuller.Cull(new[] { onCanvas, offCanvasGiant });
            Assert.AreEqual(1, culled.Length);

            var grid = SpatialGridBuilder.Build(culled, width: 4, height: 4, maxPerCell: 1);
            Assert.AreEqual(0, grid.DroppedPrimitiveReferences);
            Assert.IsTrue(grid.PrimitiveIndices.All(i => i == 0));
            Assert.IsFalse(grid.PrimitiveIndices.Contains(1));
        }

        [Test]
        public void SpatialGrid_BuildSkipsOffCanvasEvenWithoutFacadeCull()
        {
            var onCanvas = MakeRect(new Vector2(0.01f, 0.01f), new Vector2(0.05f, 0.05f), Color.red);
            var offCanvasGiant = MakeRect(new Vector2(-10f, -10f), new Vector2(5f, 5f), Color.blue);

            var grid = SpatialGridBuilder.Build(new[] { onCanvas, offCanvasGiant }, width: 4, height: 4, maxPerCell: 1);
            Assert.AreEqual(0, grid.DroppedPrimitiveReferences);

            var cornerCell = grid.Cells[0];
            Assert.AreEqual(1, cornerCell.Count);
            Assert.AreEqual(0, grid.PrimitiveIndices[cornerCell.StartIndex]);
        }

        [Test]
        public void SpatialGrid_CellOrderIsTopmostFirst()
        {
            var bottom = MakeRect(new Vector2(0.2f, 0.2f), new Vector2(0.4f, 0.4f), Color.red);
            var top = MakeRect(new Vector2(0.3f, 0.3f), new Vector2(0.2f, 0.2f), Color.blue);

            var grid = SpatialGridBuilder.Build(new[] { bottom, top }, width: 4, height: 4, maxPerCell: 8);
            var found = false;
            for (var c = 0; c < grid.Cells.Length; c++)
            {
                var cell = grid.Cells[c];
                if (cell.Count < 2)
                {
                    continue;
                }

                found = true;
                Assert.AreEqual(1, grid.PrimitiveIndices[cell.StartIndex], "Topmost primitive must be evaluated first");
                Assert.AreEqual(0, grid.PrimitiveIndices[cell.StartIndex + 1]);
                break;
            }

            Assert.IsTrue(found, "Expected at least one cell containing both primitives");
        }

        [Test]
        public void SpatialGrid_OverCapacityKeepsTopmostNotLargest()
        {
            var hugeBottom = MakeRect(new Vector2(0f, 0f), new Vector2(1f, 1f), Color.red);
            var smallTop = MakeRect(new Vector2(0.4f, 0.4f), new Vector2(0.1f, 0.1f), Color.blue);

            var grid = SpatialGridBuilder.Build(new[] { hugeBottom, smallTop }, width: 2, height: 2, maxPerCell: 1);
            Assert.Greater(grid.DroppedPrimitiveReferences, 0);

            for (var c = 0; c < grid.Cells.Length; c++)
            {
                var cell = grid.Cells[c];
                if (cell.Count == 0)
                {
                    continue;
                }

                Assert.AreEqual(1, cell.Count);
                Assert.AreEqual(1, grid.PrimitiveIndices[cell.StartIndex]);
            }
        }

        private static Primitive MakeRect(Vector2 position, Vector2 size, Color color)
        {
            return new Primitive
            {
                Type = PrimitiveKind.Rectangle,
                Position = position,
                Size = size,
                Color = color,
                Softness = 0f,
                Layer = 0
            };
        }
    }
}
