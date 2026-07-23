using System.Collections.Generic;
using NUnit.Framework;
using SDFX.VectorTextureCompiler.Core.Optimize;
using SDFX.VectorTextureCompiler.Core.Primitives;
using UnityEngine;
using PrimitiveKind = SDFX.VectorTextureCompiler.Core.Primitives.PrimitiveType;

namespace SDFX.VectorTextureCompiler.Tests.Optimize
{
    public sealed class SpatialGridBuilderTests
    {
        [Test]
        public void Build_WritesDescendingIndices_FrontToBack()
        {
            var primitives = new List<Primitive>
            {
                MakeRect(new Vector2(0.1f, 0.1f), new Vector2(0.4f, 0.4f)),
                MakeRect(new Vector2(0.2f, 0.2f), new Vector2(0.4f, 0.4f)),
            };

            var grid = SpatialGridBuilder.Build(primitives, width: 4, height: 4, maxPerCell: 8);
            AssertCellIndicesDescending(grid);
        }

        [Test]
        public void Build_TruncateKeepsLargestThenDescendingOrder()
        {
            var primitives = new List<Primitive>
            {
                MakeRect(new Vector2(0.25f, 0.25f), new Vector2(0.1f, 0.1f)),
                MakeRect(new Vector2(0.1f, 0.1f), new Vector2(0.5f, 0.5f)),
                MakeRect(new Vector2(0.15f, 0.15f), new Vector2(0.4f, 0.4f)),
            };

            var grid = SpatialGridBuilder.Build(primitives, width: 2, height: 2, maxPerCell: 2);
            Assert.Greater(grid.DroppedPrimitiveReferences, 0);
            AssertCellIndicesDescending(grid);

            for (var i = 0; i < grid.Cells.Length; i++)
            {
                var cell = grid.Cells[i];
                if (cell.Count == 0)
                {
                    continue;
                }

                Assert.AreEqual(2, cell.Count);
                Assert.AreEqual(2, grid.PrimitiveIndices[cell.StartIndex]);
                Assert.AreEqual(1, grid.PrimitiveIndices[cell.StartIndex + 1]);
            }
        }

        private static Primitive MakeRect(Vector2 position, Vector2 size)
        {
            return new Primitive
            {
                Type = PrimitiveKind.Rectangle,
                Position = position,
                Size = size,
                Color = Color.white
            };
        }

        private static void AssertCellIndicesDescending(SpatialGrid grid)
        {
            for (var i = 0; i < grid.Cells.Length; i++)
            {
                var cell = grid.Cells[i];
                for (var j = 1; j < cell.Count; j++)
                {
                    var prev = grid.PrimitiveIndices[cell.StartIndex + j - 1];
                    var next = grid.PrimitiveIndices[cell.StartIndex + j];
                    Assert.Greater(prev, next,
                        $"Cell {i} expected descending indices, got {prev} then {next}");
                }
            }
        }
    }
}
